namespace AutonomusCRM.Application.Common.Imports;

public record CustomerImportRow(string Name, string? Email, string? Phone, string? Company);

public record LeadImportRow(string Name, string Source = "Other", string? Email = null, string? Phone = null, string? Company = null);

public record DealImportRow(string Title, decimal Amount, string Stage = "Prospecting", string? CustomerEmail = null);

public record ImportResultDto(int Requested, int Created, int Failed, IReadOnlyList<string> Errors);
