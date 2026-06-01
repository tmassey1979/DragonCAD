using System.Globalization;
using System.Text;

namespace DragonCAD.Fabrication.PickAndPlace;

public static class PickAndPlaceCsvFormatter
{
    private const string NewLine = "\r\n";

    public static string Format(IEnumerable<ComponentPlacementRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        StringBuilder builder = new();
        AppendRow(builder, ["Reference", "Value", "Package", "X", "Y", "Rotation", "Side"]);

        foreach (ComponentPlacementRow row in rows.OrderBy(static row => row.Reference, StringComparer.Ordinal))
        {
            AppendRow(
                builder,
                [
                    row.Reference,
                    row.Value,
                    row.Package,
                    row.X.ToString(CultureInfo.InvariantCulture),
                    row.Y.ToString(CultureInfo.InvariantCulture),
                    row.Rotation.ToString(CultureInfo.InvariantCulture),
                    row.Side.ToString()
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
