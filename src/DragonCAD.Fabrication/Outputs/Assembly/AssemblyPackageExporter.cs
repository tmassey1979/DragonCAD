using System.Globalization;
using System.Text;

namespace DragonCAD.Fabrication.Outputs.Assembly;

public static class AssemblyPackageExporter
{
    private const string NewLine = "\r\n";

    public static AssemblyExportPackage Export(IEnumerable<AssemblyComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        AssemblyComponent[] normalizedComponents = components
            .OrderBy(static component => component.Reference, StringComparer.Ordinal)
            .ToArray();

        return new AssemblyExportPackage(
            FormatBom(normalizedComponents),
            FormatPickAndPlace(normalizedComponents),
            FindDiagnostics(normalizedComponents));
    }

    private static string FormatBom(IEnumerable<AssemblyComponent> components)
    {
        StringBuilder builder = new();
        AppendRow(builder, ["References", "Quantity", "Value", "MPN", "Package", "Footprint", "SourcingStatus"]);

        foreach (IGrouping<BomIdentity, AssemblyComponent> group in components
            .GroupBy(static component => new BomIdentity(
                component.Value,
                component.ManufacturerPartNumber,
                component.Package,
                component.Footprint,
                component.SourcingStatus))
            .OrderBy(static group => group.Key.ManufacturerPartNumber, StringComparer.Ordinal)
            .ThenBy(static group => group.Key.Value, StringComparer.Ordinal)
            .ThenBy(static group => group.Key.Package, StringComparer.Ordinal)
            .ThenBy(static group => group.Key.Footprint, StringComparer.Ordinal)
            .ThenBy(static group => group.Key.SourcingStatus, StringComparer.Ordinal))
        {
            string[] references = group
                .Select(static component => component.Reference)
                .OrderBy(static reference => reference, StringComparer.Ordinal)
                .ToArray();

            AppendRow(
                builder,
                [
                    string.Join(' ', references),
                    references.Length.ToString(CultureInfo.InvariantCulture),
                    group.Key.Value,
                    group.Key.ManufacturerPartNumber,
                    group.Key.Package,
                    group.Key.Footprint,
                    group.Key.SourcingStatus
                ]);
        }

        return builder.ToString();
    }

    private static string FormatPickAndPlace(IEnumerable<AssemblyComponent> components)
    {
        StringBuilder builder = new();
        AppendRow(builder, ["Reference", "X", "Y", "Rotation", "Side", "Package", "PlacementStatus"]);

        foreach (AssemblyComponent component in components
            .Where(static component => component.X.HasValue && component.Y.HasValue && component.Rotation.HasValue && component.Side.HasValue)
            .OrderBy(static component => component.Reference, StringComparer.Ordinal))
        {
            AppendRow(
                builder,
                [
                    component.Reference,
                    component.X.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                    component.Y.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                    component.Rotation.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                    component.Side.GetValueOrDefault().ToString(),
                    component.Package,
                    component.PlacementStatus
                ]);
        }

        return builder.ToString();
    }

    private static AssemblyExportDiagnostic[] FindDiagnostics(IEnumerable<AssemblyComponent> components)
    {
        return components
            .SelectMany(static component => FindComponentDiagnostics(component))
            .OrderBy(static diagnostic => diagnostic.Reference, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Code)
            .ToArray();
    }

    private static IEnumerable<AssemblyExportDiagnostic> FindComponentDiagnostics(AssemblyComponent component)
    {
        if (string.IsNullOrEmpty(component.ManufacturerPartNumber))
        {
            yield return new(
                AssemblyExportDiagnosticCode.MissingManufacturerPartNumber,
                component.Reference,
                $"{component.Reference} is missing manufacturer part number.");
        }

        if (string.IsNullOrEmpty(component.Package))
        {
            yield return new(
                AssemblyExportDiagnosticCode.MissingPackage,
                component.Reference,
                $"{component.Reference} is missing package.");
        }

        if (!component.X.HasValue || !component.Y.HasValue || !component.Rotation.HasValue || !component.Side.HasValue || string.IsNullOrEmpty(component.PlacementStatus))
        {
            yield return new(
                AssemblyExportDiagnosticCode.MissingPlacement,
                component.Reference,
                $"{component.Reference} is missing placement data.");
        }
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

    private sealed record BomIdentity(
        string Value,
        string ManufacturerPartNumber,
        string Package,
        string Footprint,
        string SourcingStatus);
}
