using DragonCAD.App.Fabrication.Handoff;

namespace DragonCAD.App.Fabrication.Export;

public static class FabricationChecklistExportPreview
{
    public static FabricationHandoffActionPlan CreateActionPlan(FabricationHandoffOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        FabricationHandoffPackageFile[] files = option.RequiredFiles
            .Select(file => file.IsReady
                ? FabricationHandoffPackageFile.Present(file.DisplayName, file.RelativePath)
                : FabricationHandoffPackageFile.Missing(file.DisplayName))
            .ToArray();

        FabricationHandoffPackageOption packageOption = option.ProviderName == "OSH Park"
            ? FabricationHandoffPackageOption.CreateUploadPage(
                option.ProviderId,
                option.ProviderName,
                option.OrderKindLabel,
                "https://oshpark.com",
                files)
            : FabricationHandoffPackageOption.CreateQuotePage(
                option.ProviderId,
                option.ProviderName,
                option.OrderKindLabel,
                "https://www.pcbcart.com/quote",
                files);

        return FabricationHandoffActionPlanner.Plan(packageOption);
    }

    public static FabricationChecklistPreview FromOption(
        FabricationHandoffOptionViewModel option,
        FabricationHandoffActionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(plan);

        FabricationChecklistRow[] rows = option.RequiredFiles
            .OrderBy(file => file.DisplayName, StringComparer.Ordinal)
            .Select(file => new FabricationChecklistRow(
                file.DisplayName,
                file.StatusLabel,
                file.RelativePath))
            .ToArray();

        string actionLabel = plan.Action?.Label ?? "Blocked";
        string status = plan.IsReady ? "Ready" : "Blocked";
        string[] csvLines =
        [
            "Provider,Mode,Status,Action",
            Csv(option.ProviderName, option.OrderKindLabel, status, actionLabel),
            "File,Status,Path",
            .. rows.Select(row => Csv(row.FileName, row.Status, row.RelativePath))
        ];

        return new FabricationChecklistPreview(
            option.ProviderName,
            option.OrderKindLabel,
            status,
            actionLabel,
            rows,
            plan.Diagnostics,
            csvLines);
    }

    private static string Csv(params string[] values) =>
        string.Join(",", values.Select(EscapeCsv));

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}

public sealed record FabricationChecklistPreview(
    string ProviderName,
    string Mode,
    string Status,
    string ActionLabel,
    IReadOnlyList<FabricationChecklistRow> Rows,
    IReadOnlyList<string> Diagnostics,
    IReadOnlyList<string> CsvLines);

public sealed record FabricationChecklistRow(
    string FileName,
    string Status,
    string RelativePath);
