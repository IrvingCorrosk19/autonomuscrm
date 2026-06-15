using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Graph;

public static class DbBusinessGraphExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static DbBusinessGraphExportResultDto ExportPng(DbBusinessGraphDto graph)
    {
        var svg = BuildSvg(graph);
        var content = Encoding.UTF8.GetBytes(svg);
        return new DbBusinessGraphExportResultDto(
            DbBusinessGraphExportFormat.Png,
            "image/svg+xml",
            $"business-graph-{graph.ConnectionProfileId:N}.svg",
            content,
            null);
    }

    public static DbBusinessGraphExportResultDto ExportPdf(DbBusinessGraphDto graph)
    {
        var svg = BuildSvg(graph);
        var pdf = BuildSimplePdf(svg, graph.Summary.BusinessViewMessage);
        return new DbBusinessGraphExportResultDto(
            DbBusinessGraphExportFormat.Pdf,
            "application/pdf",
            $"business-graph-{graph.ConnectionProfileId:N}.pdf",
            pdf,
            null);
    }

    public static DbBusinessGraphExportResultDto ExportSnapshot(DbBusinessGraphDto graph)
    {
        var json = JsonSerializer.Serialize(graph, JsonOptions);
        return new DbBusinessGraphExportResultDto(
            DbBusinessGraphExportFormat.Snapshot,
            "application/json",
            $"business-graph-snapshot-{graph.ConnectionProfileId:N}.json",
            Encoding.UTF8.GetBytes(json),
            json);
    }

    private static string BuildSvg(DbBusinessGraphDto graph)
    {
        var nodes = graph.Nodes.ToList();
        var width = 900;
        var height = Math.Max(400, nodes.Count * 90 + 80);
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
        sb.AppendLine("<style>text{font-family:Segoe UI,Arial,sans-serif;font-size:13px;fill:#1a1a2e}.sub{font-size:11px;fill:#666}.edge{stroke:#94a3b8;stroke-width:2;fill:none}</style>");
        sb.AppendLine($"<text x=\"20\" y=\"28\" font-size=\"18\" font-weight=\"bold\">Business graph — score {graph.Summary.GlobalHealthScore} ({graph.Summary.GlobalHealthBand})</text>");
        sb.AppendLine($"<text x=\"20\" y=\"48\" class=\"sub\">{Escape(graph.Summary.BusinessViewMessage)}</text>");

        var positions = new Dictionary<Guid, (int X, int Y)>();
        var startY = 80;
        for (var i = 0; i < nodes.Count; i++)
        {
            positions[nodes[i].Id] = (120, startY + i * 80);
        }

        foreach (var edge in graph.Edges)
        {
            if (!positions.TryGetValue(edge.FromNodeId, out var from) ||
                !positions.TryGetValue(edge.ToNodeId, out var to))
                continue;

            sb.AppendLine($"<line class=\"edge\" x1=\"{from.X + 160}\" y1=\"{from.Y + 20}\" x2=\"{to.X}\" y2=\"{to.Y + 20}\" marker-end=\"url(#arrow)\"/>");
            var midX = (from.X + to.X) / 2 + 80;
            var midY = (from.Y + to.Y) / 2 + 10;
            sb.AppendLine($"<text x=\"{midX}\" y=\"{midY}\" class=\"sub\" text-anchor=\"middle\">{Escape(edge.BusinessLabel)}</text>");
        }

        sb.AppendLine("<defs><marker id=\"arrow\" markerWidth=\"8\" markerHeight=\"8\" refX=\"6\" refY=\"3\" orient=\"auto\"><path d=\"M0,0 L6,3 L0,6 Z\" fill=\"#94a3b8\"/></marker></defs>");

        foreach (var node in nodes)
        {
            var (x, y) = positions[node.Id];
            var fill = node.HealthScore >= 75 ? "#dcfce7" : node.HealthScore >= 50 ? "#fef9c3" : "#fee2e2";
            var stroke = node.HealthScore >= 75 ? "#16a34a" : node.HealthScore >= 50 ? "#ca8a04" : "#dc2626";
            sb.AppendLine($"<rect x=\"{x}\" y=\"{y}\" width=\"160\" height=\"48\" rx=\"8\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"2\"/>");
            sb.AppendLine($"<text x=\"{x + 12}\" y=\"{y + 22}\" font-weight=\"bold\">{Escape(node.Label)}</text>");
            sb.AppendLine($"<text x=\"{x + 12}\" y=\"{y + 38}\" class=\"sub\">Health {node.HealthScore} — {node.HealthBand}</text>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static byte[] BuildSimplePdf(string svgContent, string title)
    {
        var escapedTitle = EscapePdf(title);
        var lines = svgContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var body = new StringBuilder();
        body.AppendLine("BT /F1 14 Tf 50 750 Td (" + escapedTitle + ") Tj ET");
        body.AppendLine("BT /F1 10 Tf 50 730 Td (AutonomusCRM Business Graph Export) Tj ET");
        var y = 710;
        foreach (var line in lines.Take(40))
        {
            if (y < 50) break;
            body.AppendLine($"BT /F1 8 Tf 50 {y} Td ({EscapePdf(line)}) Tj ET");
            y -= 12;
        }

        var stream = body.ToString();
        var objects = new StringBuilder();
        objects.AppendLine("%PDF-1.4");
        objects.AppendLine("1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj");
        objects.AppendLine("2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj");
        objects.AppendLine("3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R >> >> >>endobj");
        objects.AppendLine($"4 0 obj<< /Length {Encoding.ASCII.GetByteCount(stream)} >>stream");
        objects.Append(stream);
        objects.AppendLine("endstream endobj");
        objects.AppendLine("5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj");
        objects.AppendLine("xref");
        objects.AppendLine("0 6");
        objects.AppendLine("0000000000 65535 f ");
        objects.AppendLine("trailer<< /Size 6 /Root 1 0 R >>");
        objects.AppendLine("startxref");
        objects.AppendLine("%%EOF");
        return Encoding.ASCII.GetBytes(objects.ToString());
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string EscapePdf(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
