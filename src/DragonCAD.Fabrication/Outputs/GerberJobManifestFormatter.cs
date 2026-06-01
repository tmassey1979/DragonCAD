using System.Text;
using System.Text.Json;

namespace DragonCAD.Fabrication.Outputs;

public static class GerberJobManifestFormatter
{
    private const string NewLine = "\r\n";

    public static string FormatCsv(ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        StringBuilder builder = new();
        AppendCsvRow(builder, ["Role", "RelativePath", "Checksum"]);

        foreach (ManufacturingOutputEntry entry in manifest.Entries)
        {
            AppendCsvRow(
                builder,
                [
                    entry.Role.ToString(),
                    entry.RelativePath.Value,
                    entry.Checksum.Value
                ]);
        }

        return builder.ToString();
    }

    public static string FormatJson(ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var summary = new
        {
            FileCount = manifest.Entries.Count,
            Roles = manifest.Entries
                .GroupBy(entry => entry.Role)
                .Select(group => new
                {
                    Role = group.Key.ToString(),
                    Count = group.Count()
                })
                .ToArray(),
            Files = manifest.Entries
                .Select(entry => new
                {
                    Role = entry.Role.ToString(),
                    RelativePath = entry.RelativePath.Value,
                    Checksum = entry.Checksum.Value
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(
            summary,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }

    private static void AppendCsvRow(StringBuilder builder, IReadOnlyList<string> cells)
    {
        for (int index = 0; index < cells.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendCsvCell(builder, cells[index]);
        }

        builder.Append(NewLine);
    }

    private static void AppendCsvCell(StringBuilder builder, string value)
    {
        if (!RequiresEscaping(value))
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
        builder.Append('"');
    }

    private static bool RequiresEscaping(string value)
    {
        return value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal)
            || value.Contains('\n');
    }
}
