using System.Text;

namespace DragonCAD.App.Fabrication.Handoff;

public static class FabricationHandoffActionPlanner
{
    private const string NewLine = "\r\n";

    public static FabricationHandoffActionPlan Plan(FabricationHandoffPackageOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        string[] diagnostics = option.Files
            .Where(file => !file.IsPresent)
            .OrderBy(file => file.DisplayName, StringComparer.Ordinal)
            .Select(file => $"Missing {file.DisplayName} for {option.ProviderName} {option.PackageName.ToLowerInvariant()}.")
            .ToArray();

        FabricationHandoffAction? action = diagnostics.Length == 0
            ? CreateAction(option)
            : null;

        return new FabricationHandoffActionPlan(
            diagnostics.Length == 0,
            action,
            diagnostics,
            FormatSummary(option, action, diagnostics));
    }

    private static FabricationHandoffAction CreateAction(FabricationHandoffPackageOption option)
    {
        string label = option.ActionKind switch
        {
            FabricationHandoffActionKind.OpenUploadPage => $"Open {option.ProviderName} upload page",
            FabricationHandoffActionKind.OpenQuotePage => $"Open {option.ProviderName} quote page",
            FabricationHandoffActionKind.ExportPackage => $"Export {option.ProviderName} package",
            _ => throw new InvalidOperationException($"Unknown fabrication handoff action kind {option.ActionKind}.")
        };

        return new FabricationHandoffAction(option.ActionKind, label, option.Target);
    }

    private static string FormatSummary(
        FabricationHandoffPackageOption option,
        FabricationHandoffAction? action,
        IReadOnlyList<string> diagnostics)
    {
        StringBuilder builder = new();
        builder.Append("Provider: ").Append(option.ProviderName).Append(" (").Append(option.ProviderId).Append(')').Append(NewLine);
        builder.Append("Package: ").Append(option.PackageName).Append(NewLine);
        builder.Append("Status: ").Append(diagnostics.Count == 0 ? "Ready" : "Blocked").Append(NewLine);
        builder.Append("Action: ");
        if (action is null)
        {
            builder.Append("blocked").Append(NewLine);
        }
        else
        {
            builder.Append(action.Label).Append(" -> ").Append(action.Target).Append(NewLine);
        }

        builder.Append("Diagnostics: ");
        if (diagnostics.Count == 0)
        {
            builder.Append("none").Append(NewLine);
        }
        else
        {
            builder.Append(NewLine);
            foreach (string diagnostic in diagnostics)
            {
                builder.Append("- ").Append(diagnostic).Append(NewLine);
            }
        }

        builder.Append("Files:").Append(NewLine);
        foreach (FabricationHandoffPackageFile file in option.Files.OrderBy(file => file.DisplayName, StringComparer.Ordinal))
        {
            builder.Append("- ").Append(file.DisplayName).Append(": ");
            builder.Append(file.IsPresent ? file.RelativePath : "missing");
            builder.Append(NewLine);
        }

        if (builder.Length >= NewLine.Length)
        {
            builder.Length -= NewLine.Length;
        }

        return builder.ToString();
    }
}

public sealed record FabricationHandoffActionPlan(
    bool IsReady,
    FabricationHandoffAction? Action,
    IReadOnlyList<string> Diagnostics,
    string Summary);

public sealed record FabricationHandoffAction(
    FabricationHandoffActionKind Kind,
    string Label,
    string Target);

public enum FabricationHandoffActionKind
{
    OpenUploadPage = 100,
    OpenQuotePage = 200,
    ExportPackage = 300
}

public sealed record FabricationHandoffPackageOption
{
    private FabricationHandoffPackageOption(
        string providerId,
        string providerName,
        string packageName,
        FabricationHandoffActionKind actionKind,
        string target,
        IReadOnlyList<FabricationHandoffPackageFile> files)
    {
        ProviderId = NormalizeRequired(providerId, nameof(providerId));
        ProviderName = NormalizeRequired(providerName, nameof(providerName));
        PackageName = NormalizeRequired(packageName, nameof(packageName));
        ActionKind = actionKind;
        Target = NormalizeRequired(target, nameof(target));
        Files = files
            .OrderBy(file => file.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public string ProviderId { get; }

    public string ProviderName { get; }

    public string PackageName { get; }

    public FabricationHandoffActionKind ActionKind { get; }

    public string Target { get; }

    public IReadOnlyList<FabricationHandoffPackageFile> Files { get; }

    public static FabricationHandoffPackageOption CreateUploadPage(
        string providerId,
        string providerName,
        string packageName,
        string uploadPageUrl,
        IEnumerable<FabricationHandoffPackageFile> files) =>
        Create(providerId, providerName, packageName, FabricationHandoffActionKind.OpenUploadPage, uploadPageUrl, files);

    public static FabricationHandoffPackageOption CreateQuotePage(
        string providerId,
        string providerName,
        string packageName,
        string quotePageUrl,
        IEnumerable<FabricationHandoffPackageFile> files) =>
        Create(providerId, providerName, packageName, FabricationHandoffActionKind.OpenQuotePage, quotePageUrl, files);

    public static FabricationHandoffPackageOption CreateExportPackage(
        string providerId,
        string providerName,
        string packageName,
        string packagePath,
        IEnumerable<FabricationHandoffPackageFile> files) =>
        Create(providerId, providerName, packageName, FabricationHandoffActionKind.ExportPackage, packagePath, files);

    private static FabricationHandoffPackageOption Create(
        string providerId,
        string providerName,
        string packageName,
        FabricationHandoffActionKind actionKind,
        string target,
        IEnumerable<FabricationHandoffPackageFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        return new FabricationHandoffPackageOption(
            providerId,
            providerName,
            packageName,
            actionKind,
            target,
            files.ToArray());
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}

public sealed record FabricationHandoffPackageFile
{
    private FabricationHandoffPackageFile(string displayName, bool isPresent, string relativePath)
    {
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        IsPresent = isPresent;
        RelativePath = isPresent ? NormalizeRequired(relativePath, nameof(relativePath)) : string.Empty;
    }

    public string DisplayName { get; }

    public bool IsPresent { get; }

    public string RelativePath { get; }

    public static FabricationHandoffPackageFile Present(string displayName, string relativePath) =>
        new(displayName, true, relativePath);

    public static FabricationHandoffPackageFile Missing(string displayName) =>
        new(displayName, false, string.Empty);

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim().Replace('\\', '/');
    }
}
