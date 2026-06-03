using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Sourcing.BomPlanning;

public static class BomPlanExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string ExportBomCsv(BomPlanningResult plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder();
        builder.AppendLine("GroupKey,Designators,CanonicalIdentity,SelectedValue,Package,QuantityPerBuild,SelectedManufacturerPartNumber,Alternates,DoNotSubstitute");

        foreach (var group in plan.Groups)
        {
            builder
                .Append(Csv(group.GroupKey)).Append(',')
                .Append(Csv(string.Join(";", group.Designators))).Append(',')
                .Append(Csv(group.CanonicalIdentity)).Append(',')
                .Append(Csv(group.SelectedValue)).Append(',')
                .Append(Csv(group.Package)).Append(',')
                .Append(group.QuantityPerBuild.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(group.SelectedManufacturerPartNumber)).Append(',')
                .Append(Csv(string.Join(";", group.Alternates))).Append(',')
                .Append(group.DoNotSubstitute.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return builder.ToString();
    }

    public static string ExportOrderPlanJson(BomPlanningResult plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return JsonSerializer.Serialize(plan, JsonOptions);
    }

    private static string Csv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
