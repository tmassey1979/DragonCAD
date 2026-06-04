using System.Text;

namespace DragonCAD.Fabrication.Bom;

public static class BomCsvFormatter
{
    private const string NewLine = "\r\n";

    public static string Format(IEnumerable<BomLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        StringBuilder builder = new();
        AppendRow(builder, ["References", "Quantity", "Part", "Value", "Package", "ManufacturerPartNumber", "Notes"]);

        foreach (BomLine line in lines)
        {
            AppendRow(
                builder,
                [
                    string.Join(' ', line.References),
                    line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    line.Identity.Part,
                    line.Identity.Value,
                    line.Identity.Package,
                    line.Identity.ManufacturerPartNumber,
                    line.Notes
                ]);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> cells)
    {
        for (int index = 0; index < cells.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            AppendCell(builder, cells[index]);
        }

        builder.Append(NewLine);
    }

    private static void AppendCell(StringBuilder builder, string value)
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
            || value.Contains('\n', StringComparison.Ordinal);
    }
}
