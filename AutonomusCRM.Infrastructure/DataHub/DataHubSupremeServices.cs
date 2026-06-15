using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DataHub;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubIntelligenceService : IDataHubIntelligenceService
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRx = new(@"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex AmountRx = new(@"^[\d.,$€£\s-]+$", RegexOptions.Compiled);

    private readonly IDataHubFieldCatalog _fields;

    public DataHubIntelligenceService(IDataHubFieldCatalog fields) => _fields = fields;

    public DataHubAiAnalysisResultDto AnalyzeFile(
        string fileName,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows,
        string? preferredEntity = null)
    {
        var entityHint = preferredEntity ?? "Customer";
        var preliminary = DetectColumns(entityHint, columns, sampleRows);
        var entityScores = ScoreEntities(columns, sampleRows, preliminary);
        var bestEntity = preferredEntity ?? entityScores.OrderByDescending(kv => kv.Value).First().Key;
        var detections = string.Equals(bestEntity, entityHint, StringComparison.OrdinalIgnoreCase)
            ? preliminary
            : DetectColumns(bestEntity, columns, sampleRows);
        var confidence = entityScores.TryGetValue(bestEntity, out var c) ? c : 70;

        var mappings = _fields.SuggestMappings(bestEntity, columns).Mappings.ToList();
        foreach (var det in detections.Where(d => d.SuggestedTargetField != null))
        {
            if (mappings.All(m => m.SourceColumn != det.SourceColumn))
                mappings.Add(new DataHubMappingDto(null, det.SourceColumn, det.SuggestedTargetField!, false, null, SuggestTransform(det.DetectedType)));
        }

        var contentTypes = new List<string>();
        if (detections.Any(d => d.DetectedType is "Email" or "Name" or "Phone"))
            contentTypes.Add(bestEntity == "Lead" ? "Leads" : bestEntity == "Deal" ? "Deals" : "Contacts");
        if (detections.Any(d => d.DetectedType == "Company"))
            contentTypes.Add("Companies");
        if (detections.Any(d => d.DetectedType == "Amount"))
            contentTypes.Add("Deals / Revenue");

        var issues = new List<string>();
        if (detections.Any(d => d.ConfidencePercent < 70))
            issues.Add("Some columns have low mapping confidence — review Mapping Studio.");
        if (!detections.Any(d => d.DetectedType == "Email") && bestEntity is "Customer" or "Lead")
            issues.Add("No email column detected — duplicates harder to prevent.");
        if (sampleRows.Count == 0)
            issues.Add("File appears empty or unreadable.");

        var rules = RecommendRules(detections, bestEntity);
        var summary = BuildSummary(bestEntity, contentTypes, confidence, sampleRows.Count);

        return new DataHubAiAnalysisResultDto(
            bestEntity,
            Math.Round(confidence, 1),
            contentTypes,
            detections,
            issues,
            rules,
            mappings,
            summary);
    }

    public IReadOnlyList<DataHubColumnDetectionDto> DetectColumns(
        string targetEntity,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows)
    {
        var results = new List<DataHubColumnDetectionDto>();
        foreach (var col in columns)
        {
            var samples = sampleRows.Take(20)
                .Select(r => r.TryGetValue(col, out var v) ? v : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Take(5)
                .ToList();

            var match = DataHubSmartMatchingEngine.MatchColumn(targetEntity, col, samples);
            results.Add(new DataHubColumnDetectionDto(
                col, match.DetectedType, match.TargetField, match.ConfidencePercent,
                samples.Where(s => s != null).Select(s => s!.Length > 40 ? s[..40] + "…" : s!).ToList(),
                match.Explanation));
        }
        return results;
    }

    public IReadOnlyList<DataHubSmartMatchResult> MatchColumnsV2(
        string targetEntity,
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> sampleRows)
        => DataHubSmartMatchingEngine.MatchColumns(targetEntity, columns, sampleRows);

    private static (string Type, string? Field, double Confidence) InferColumn(string header, IReadOnlyList<string> samples)
    {
        var h = header.ToLowerInvariant();
        var headerScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["Email"] = ScoreHeader(h, ["email", "correo", "e-mail", "mail"]),
            ["Phone"] = ScoreHeader(h, ["phone", "telefono", "tel", "mobile", "celular"]),
            ["Name"] = ScoreHeader(h, ["name", "nombre", "fullname", "contact", "lead"]),
            ["FirstName"] = ScoreHeader(h, ["firstname", "first_name", "nombre"]),
            ["LastName"] = ScoreHeader(h, ["lastname", "last_name", "apellido"]),
            ["Company"] = ScoreHeader(h, ["company", "empresa", "organization", "account", "org"]),
            ["City"] = ScoreHeader(h, ["city", "ciudad"]),
            ["Country"] = ScoreHeader(h, ["country", "pais", "país", "nacion"]),
            ["Amount"] = ScoreHeader(h, ["amount", "monto", "value", "revenue", "deal"]),
            ["Date"] = ScoreHeader(h, ["date", "fecha", "created", "closed"]),
            ["Pipeline"] = ScoreHeader(h, ["pipeline", "stage", "etapa", "fase"]),
            ["Owner"] = ScoreHeader(h, ["owner", "assigned", "responsable", "agent"]),
            ["Title"] = ScoreHeader(h, ["title", "deal", "subject", "opportunity"]),
            ["Source"] = ScoreHeader(h, ["source", "origen", "leadsource"])
        };

        var sampleScores = new Dictionary<string, double>();
        if (samples.Count > 0)
        {
            var emailRate = samples.Count(s => EmailRx.IsMatch(s)) / (double)samples.Count;
            var phoneRate = samples.Count(s => PhoneRx.IsMatch(s)) / (double)samples.Count;
            var amountRate = samples.Count(s => AmountRx.IsMatch(s) && decimal.TryParse(NormalizeAmount(s), NumberStyles.Any, CultureInfo.InvariantCulture, out _)) / (double)samples.Count;
            var dateRate = samples.Count(s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) / (double)samples.Count;
            sampleScores["Email"] = emailRate * 100;
            sampleScores["Phone"] = phoneRate * 100;
            sampleScores["Amount"] = amountRate * 100;
            sampleScores["Date"] = dateRate * 100;
        }

        var best = headerScores.OrderByDescending(kv => kv.Value).First();
        var type = best.Value >= 50 ? best.Key : GuessFromSamples(sampleScores) ?? "Text";
        var headerConf = headerScores.GetValueOrDefault(type, 30);
        var sampleConf = sampleScores.GetValueOrDefault(type, 50);
        var confidence = headerConf * 0.55 + sampleConf * 0.45;
        if (confidence < 40) confidence = 40;
        if (confidence > 100) confidence = 100;

        var field = MapTypeToField(type);
        return (type, field, confidence);
    }

    private static string? GuessFromSamples(Dictionary<string, double> sampleScores)
    {
        var best = sampleScores.Where(kv => kv.Value >= 60).OrderByDescending(kv => kv.Value).FirstOrDefault();
        return best.Key == default ? null : best.Key;
    }

    private static double ScoreHeader(string h, string[] tokens)
    {
        foreach (var t in tokens)
            if (h.Contains(t, StringComparison.OrdinalIgnoreCase)) return t.Length > 5 ? 98 : 92;
        return 0;
    }

    private static string? MapTypeToField(string type) => type switch
    {
        "Email" => "Email",
        "Phone" => "Phone",
        "Name" => "Name",
        "Company" => "Company",
        "Amount" => "Amount",
        "Title" => "Title",
        "Source" => "Source",
        "Pipeline" => "Stage",
        "Owner" => "AssignedToUserId",
        _ => null
    };

    private static string? SuggestTransform(string type) => type switch
    {
        "Email" => "NormalizeEmail",
        "Phone" => "NormalizePhone",
        "Amount" => "NormalizeCurrency",
        "Date" => "NormalizeDate",
        "Company" => "TitleCase",
        "Name" => "TitleCase",
        _ => "Trim"
    };

    private static Dictionary<string, double> ScoreEntities(
        IReadOnlyList<string> columns,
        IReadOnlyList<Dictionary<string, string?>> samples,
        IReadOnlyList<DataHubColumnDetectionDto> detections)
    {
        var types = detections.Select(d => d.DetectedType).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scores = new Dictionary<string, double>
        {
            ["Customer"] = 60,
            ["Lead"] = 55,
            ["Deal"] = 50,
            ["User"] = 40
        };
        if (types.Contains("Source")) scores["Lead"] += 25;
        if (types.Contains("Amount") && types.Contains("Title")) scores["Deal"] += 30;
        if (types.Contains("Email") && types.Contains("Name")) { scores["Customer"] += 15; scores["Lead"] += 15; }
        if (columns.Any(c => c.Contains("password", StringComparison.OrdinalIgnoreCase))) scores["User"] += 40;
        return scores;
    }

    private static List<string> RecommendRules(IReadOnlyList<DataHubColumnDetectionDto> detections, string entity)
    {
        var rules = new List<string> { "Trim all text fields", "Skip empty rows" };
        if (detections.Any(d => d.DetectedType == "Email"))
            rules.Add("Normalize email to lowercase");
        if (detections.Any(d => d.DetectedType == "Phone"))
            rules.Add("Clean phone characters and validate length");
        if (entity == "Customer")
            rules.Add("If customer exists by email → Update; else → Create");
        if (entity == "Lead" && !detections.Any(d => d.DetectedType == "Source"))
            rules.Add("If Source empty → set default 'Other'");
        return rules;
    }

    private static string BuildSummary(string entity, IReadOnlyList<string> types, double confidence, int rowCount)
    {
        var typeList = types.Count > 0 ? string.Join(", ", types) : entity;
        return $"This file appears to contain {typeList}. Suggested destination: {entity}. " +
               $"Analyzed {rowCount} sample rows. Confidence: {confidence:F0}%.";
    }

    private static string NormalizeAmount(string s)
        => s.Replace("$", "").Replace("€", "").Trim();
}

public sealed class DataHubAutoFixService : IDataHubAutoFixService
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubTransformService _transform;

    public DataHubAutoFixService(IDataHubRepository repo, IDataHubTransformService transform)
    {
        _repo = repo;
        _transform = transform;
    }

    public async Task<DataHubAutoFixResultDto> AutoFixJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var fixed_ = 0;
        var warnings = 0;
        var actions = new List<string>();
        var skip = 0;

        while (true)
        {
            var batch = await _repo.GetRowsAsync(tenantId, jobId, skip, DataHubConstants.DefaultBatchSize, cancellationToken);
            if (batch.Count == 0) break;

            foreach (var row in batch)
            {
                var data = row.RawData.ToDictionary(kv => kv.Key, kv => kv.Value);
                var changed = false;

                foreach (var key in data.Keys.ToList())
                {
                    var val = data[key];
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (key.Contains("email", StringComparison.OrdinalIgnoreCase) || key.Contains("correo", StringComparison.OrdinalIgnoreCase))
                    {
                        var fixedEmail = _transform.ApplyTransform(val, nameof(DataHubTransformType.NormalizeEmail));
                        if (fixedEmail != val) { data[key] = fixedEmail; changed = true; actions.Add("Normalized emails"); }
                    }
                    else if (key.Contains("phone", StringComparison.OrdinalIgnoreCase) || key.Contains("tel", StringComparison.OrdinalIgnoreCase))
                    {
                        var fixedPhone = _transform.ApplyTransform(val, nameof(DataHubTransformType.NormalizePhone));
                        if (fixedPhone != val) { data[key] = fixedPhone; changed = true; actions.Add("Normalized phones"); }
                    }
                    else if (key.Contains("name", StringComparison.OrdinalIgnoreCase) || key.Contains("company", StringComparison.OrdinalIgnoreCase))
                    {
                        var titled = _transform.ApplyTransform(val, nameof(DataHubTransformType.TitleCase));
                        if (titled != val) { data[key] = titled; changed = true; actions.Add("Fixed title case"); }
                    }
                    else
                    {
                        var trimmed = _transform.ApplyTransform(val, nameof(DataHubTransformType.Trim));
                        if (trimmed != val) { data[key] = trimmed; changed = true; actions.Add("Trimmed whitespace"); }
                    }
                }

                if (changed)
                {
                    row.RawData = data;
                    if (row.Status == DataHubRowStatus.Invalid.ToString())
                    {
                        row.Status = DataHubRowStatus.Pending.ToString();
                        warnings++;
                    }
                    fixed_++;
                }
            }

            await _repo.UpdateRowsAsync(batch, cancellationToken);

            skip += batch.Count;
        }

        await _repo.AddLogAsync(new DataHubImportLog
        {
            Id = Guid.NewGuid(), JobId = jobId, TenantId = tenantId,
            Level = "Info", Message = $"Auto-fix applied to {fixed_} rows"
        }, cancellationToken);

        return new DataHubAutoFixResultDto(fixed_, warnings, 0, actions.Distinct().ToList());
    }
}

public sealed class DataHubRulesEngineService : IDataHubRulesEngineService
{
    private readonly IDataHubRepository _repo;

    public DataHubRulesEngineService(IDataHubRepository repo) => _repo = repo;

    public IReadOnlyList<DataHubVisualRuleDto> GetDefaultRules(string targetEntity)
    {
        var rules = new List<DataHubVisualRuleDto>
        {
            new(null, "Trim all fields", "*", "*", "always", "", "Transform", "Trim", true),
            new(null, "Email lowercase", "Email", "Email", "notempty", "", "Transform", "NormalizeEmail", true),
            new(null, "Phone normalize", "Phone", "Phone", "notempty", "", "Transform", "NormalizePhone", true)
        };

        if (targetEntity == "Customer")
        {
            rules.Add(new(null, "Upsert by email", "Email", "Email", "notempty", "", "Action", "UpsertIfExists", true));
        }
        if (targetEntity == "Lead")
        {
            rules.Add(new(null, "Default source", "Source", "Source", "empty", "", "SetValue", "Other", true));
            rules.Add(new(null, "Default pipeline", "Stage", "Stage", "empty", "", "SetValue", "Prospecting", true));
        }
        if (targetEntity == "Deal")
        {
            rules.Add(new(null, "Default stage", "Stage", "Stage", "empty", "", "SetValue", "Prospecting", true));
        }

        return rules;
    }

    public async Task<IReadOnlyList<DataHubVisualRuleDto>> GetRulesAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default)
    {
        var stored = await _repo.GetAllValidationRulesAsync(tenantId, targetEntity, cancellationToken);
        if (stored.Count == 0) return GetDefaultRules(targetEntity);

        return stored.Select(MapRule).ToList();
    }

    public async Task<DataHubRuleSetVersionDto?> GetRuleSetVersionAsync(Guid tenantId, string targetEntity, CancellationToken cancellationToken = default)
    {
        var stored = await _repo.GetAllValidationRulesAsync(tenantId, targetEntity, cancellationToken);
        if (stored.Count == 0) return null;
        var first = stored[0];
        var version = int.TryParse(first.Parameters.GetValueOrDefault("rulesetVersion"), out var v) ? v : 1;
        return new DataHubRuleSetVersionDto(targetEntity, version, first.CreatedAt, stored.Count);
    }

    public async Task<DataHubRuleSetVersionDto> SaveRulesAsync(Guid tenantId, string targetEntity, IReadOnlyList<DataHubVisualRuleDto> rules, CancellationToken cancellationToken = default)
    {
        var existing = await _repo.GetAllValidationRulesAsync(tenantId, targetEntity, cancellationToken);
        var nextVersion = existing.Count == 0 ? 1
            : existing.Max(r => int.TryParse(r.Parameters.GetValueOrDefault("rulesetVersion"), out var v) ? v : 1) + 1;
        var savedAt = DateTime.UtcNow;

        var entities = rules.Select((rule, index) => new DataHubValidationRule
        {
            Id = rule.Id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = string.IsNullOrWhiteSpace(rule.Name) ? $"Rule {index + 1}" : rule.Name,
            TargetEntity = targetEntity,
            TargetField = rule.TargetField,
            ValidationType = DataHubValidationType.BusinessRule.ToString(),
            Parameters = new Dictionary<string, string>
            {
                ["conditionField"] = rule.ConditionField,
                ["operator"] = rule.ConditionOperator,
                ["value"] = rule.ConditionValue,
                ["action"] = rule.ActionType,
                ["actionValue"] = rule.ActionValue ?? "",
                ["rulesetVersion"] = nextVersion.ToString(),
                ["savedAt"] = savedAt.ToString("O")
            },
            IsActive = rule.IsActive,
            Priority = rule.Priority > 0 ? rule.Priority : index,
            CreatedAt = savedAt
        }).ToList();

        await _repo.ReplaceValidationRulesAsync(tenantId, targetEntity, entities, cancellationToken);
        return new DataHubRuleSetVersionDto(targetEntity, nextVersion, savedAt, entities.Count);
    }

    private static DataHubVisualRuleDto MapRule(DataHubValidationRule r)
    {
        var version = int.TryParse(r.Parameters.GetValueOrDefault("rulesetVersion"), out var v) ? v : 1;
        return new DataHubVisualRuleDto(
            r.Id, r.Name, r.TargetField,
            r.Parameters.GetValueOrDefault("conditionField") ?? r.TargetField,
            r.Parameters.GetValueOrDefault("operator") ?? "equals",
            r.Parameters.GetValueOrDefault("value") ?? "",
            r.Parameters.GetValueOrDefault("action") ?? "SetValue",
            r.Parameters.GetValueOrDefault("actionValue"),
            r.IsActive, r.Priority, version);
    }

    public Dictionary<string, string?> ApplyRules(Dictionary<string, string?> row, IReadOnlyList<DataHubVisualRuleDto> rules)
    {
        var result = new Dictionary<string, string?>(row, StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules.Where(r => r.IsActive))
        {
            if (rule.ConditionOperator == "always" || rule.ConditionField == "*")
            {
                ApplyAction(result, rule);
                continue;
            }

            result.TryGetValue(rule.ConditionField, out var current);
            var match = rule.ConditionOperator switch
            {
                "empty" => string.IsNullOrWhiteSpace(current),
                "notempty" => !string.IsNullOrWhiteSpace(current),
                "equals" => string.Equals(current, rule.ConditionValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            if (match) ApplyAction(result, rule);
        }
        return result;
    }

    private static void ApplyAction(Dictionary<string, string?> row, DataHubVisualRuleDto rule)
    {
        if (rule.ActionType == "SetValue" && rule.TargetField != "*")
            row[rule.TargetField] = rule.ActionValue;
        else if (rule.ActionType == "MarkError")
            row["_validationError"] = rule.ActionValue ?? rule.Name;
        else if (rule.ActionType == "MarkReview")
            row["_reviewRequired"] = rule.ActionValue ?? "true";
        else if (rule.ActionType == "Transform" && rule.ActionValue != null)
        {
            var transform = new DataHubTransformService();
            foreach (var key in row.Keys.ToList())
            {
                if (rule.TargetField != "*" && !key.Equals(rule.TargetField, StringComparison.OrdinalIgnoreCase)) continue;
                row[key] = transform.ApplyTransform(row[key] ?? "", rule.ActionValue);
            }
        }
    }
}

public sealed class DataHubJobQueue : IDataHubJobQueue
{
    private readonly System.Threading.Channels.Channel<Guid> _channel =
        System.Threading.Channels.Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, byte> _queued = new();

    public void Enqueue(Guid jobId)
    {
        if (!_queued.TryAdd(jobId, 0)) return;
        _channel.Writer.TryWrite(jobId);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var jobId = await _channel.Reader.ReadAsync(cancellationToken);
            _queued.TryRemove(jobId, out _);
            if (!DataHubJobProcessingLock.IsActive(jobId))
                return jobId;
        }
    }

    public void MarkComplete(Guid jobId) => _queued.TryRemove(jobId, out _);
}

public sealed class DataHubQualityScoreService : IDataHubQualityScoreService
{
    private readonly IDataHubValidateService _validate;

    public DataHubQualityScoreService(IDataHubValidateService validate) => _validate = validate;

    public async Task<DataHubQualityScoreDto> CalculateScoreAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var issues = await _validate.ScanQualityAsync(tenantId, cancellationToken);
        var critical = issues.Count(i => i.Severity == "Critical");
        var warning = issues.Count(i => i.Severity == "Warning");
        var penalty = Math.Min(100, critical * 5 + warning * 2 + issues.Count);
        var score = Math.Max(0, 100 - penalty);
        var grade = score >= 90 ? "Excellent" : score >= 75 ? "Good" : score >= 60 ? "Fair" : "Needs Attention";

        return new DataHubQualityScoreDto(score, grade, issues.Count, critical, warning, issues.Take(20).ToList());
    }
}
