using System.Collections;
using DragonCAD.App.Fabrication;

namespace DragonCAD.App.Fabrication.Ordering;

public sealed class FabricationOrderingReadinessViewModel
{
    private FabricationOrderingReadinessViewModel(IReadOnlyList<FabricationOrderingReadinessRow> rows)
    {
        Rows = rows;
        ProviderCount = rows.Count;
        ReadyProviderCount = rows.Count(row => string.Equals(row.PackageReadiness, "Ready", StringComparison.OrdinalIgnoreCase));
        BlockedProviderCount = rows.Count(row => string.Equals(row.PackageReadiness, "Blocked", StringComparison.OrdinalIgnoreCase));
        WarningCount = rows.Sum(row => row.Warnings.Count);
        MissingFileCount = rows.Sum(row => row.MissingFiles.Count);
        SummaryText = CreateSummaryText();
        EmptyStateText = HasRows
            ? string.Empty
            : "Select a marketplace or manufacturing provider to review package readiness.";
    }

    public IReadOnlyList<FabricationOrderingReadinessRow> Rows { get; }

    public bool HasRows => Rows.Count > 0;

    public int ProviderCount { get; }

    public int ReadyProviderCount { get; }

    public int BlockedProviderCount { get; }

    public int WarningCount { get; }

    public int MissingFileCount { get; }

    public string SummaryText { get; }

    public string EmptyStateText { get; }

    public static FabricationOrderingReadinessViewModel FromSources(IEnumerable<FabricationOrderingReadinessSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        FabricationOrderingReadinessRow[] rows = sources
            .Select(FabricationOrderingReadinessRow.FromSource)
            .ToArray();

        return new FabricationOrderingReadinessViewModel(rows);
    }

    public static FabricationOrderingReadinessViewModel FromDomainPackages(IEnumerable<FabricationOrderingDomainPackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);

        FabricationOrderingReadinessSource[] sources = packages
            .Select(CreateSourceFromDomainPackage)
            .ToArray();

        return FromSources(sources);
    }

    public static FabricationOrderingReadinessViewModel FromSelectedHandoffOption(FabricationHandoffViewModel handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        return handoff.SelectedOption is null
            ? new FabricationOrderingReadinessViewModel([])
            : FromHandoffOptions([handoff.SelectedOption]);
    }

    public static FabricationOrderingReadinessViewModel FromHandoffOptions(IEnumerable<FabricationHandoffOptionViewModel> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        FabricationOrderingReadinessSource[] sources = options
            .Select(CreateSourceFromHandoffOption)
            .ToArray();

        return FromSources(sources);
    }

    private static FabricationOrderingReadinessSource CreateSourceFromDomainPackage(FabricationOrderingDomainPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);

        object provider = GetRequiredProperty(package.Package, "Provider");
        object profile = GetRequiredProperty(provider, "Profile");

        return new FabricationOrderingReadinessSource(
            ProviderName: GetRequiredString(provider, "DisplayName"),
            ProviderKind: GetRequiredProperty(profile, "ProviderKind").ToString() ?? string.Empty,
            Mode: ToDisplayText(GetRequiredProperty(package.Package, "OrderMode").ToString() ?? string.Empty),
            SupportedLayers: GetIntegerList(profile, "SupportedLayerCounts"),
            MinimumQuantity: GetRequiredInt32(profile, "MinimumQuantity"),
            MaximumQuantity: GetRequiredInt32(profile, "MaximumQuantity"),
            ValidationDiagnostics: GetDiagnostics(package.ValidationResult));
    }

    private static FabricationOrderingReadinessSource CreateSourceFromHandoffOption(FabricationHandoffOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        FabricationOrderingProviderStyle style = FabricationOrderingProviderStyle.For(option);

        return new FabricationOrderingReadinessSource(
            ProviderName: option.ProviderName,
            ProviderKind: style.ProviderKind,
            Mode: option.OrderKindLabel,
            SupportedLayers: style.SupportedLayers,
            MinimumQuantity: style.MinimumQuantity,
            MaximumQuantity: style.MaximumQuantity,
            ValidationDiagnostics: option.RequiredFiles
                .Where(file => !file.IsReady)
                .Select(file => FabricationOrderingDiagnostic.Error(
                    "fabrication-handoff-missing-file",
                    $"Missing {file.DisplayName} for {option.ProviderName} {option.OrderKindLabel}.",
                    file.DisplayName))
                .ToArray());
    }

    private static IReadOnlyList<FabricationOrderingDiagnostic> GetDiagnostics(object validationResult)
    {
        object diagnosticsValue = GetRequiredProperty(validationResult, "Diagnostics");
        if (diagnosticsValue is not IEnumerable diagnostics)
        {
            throw new ArgumentException("Validation result Diagnostics property must be enumerable.", nameof(validationResult));
        }

        return diagnostics
            .Cast<object>()
            .Select(diagnostic => new FabricationOrderingDiagnostic(
                Severity: GetRequiredProperty(diagnostic, "Severity").ToString() ?? string.Empty,
                Code: GetRequiredString(diagnostic, "Code"),
                Message: GetRequiredString(diagnostic, "Message"),
                FileRole: GetOptionalProperty(diagnostic, "FileRole")?.ToString()))
            .ToArray();
    }

    private static IReadOnlyList<int> GetIntegerList(object source, string propertyName)
    {
        object value = GetRequiredProperty(source, propertyName);
        if (value is not IEnumerable enumerable)
        {
            throw new ArgumentException($"{propertyName} property must be enumerable.", nameof(source));
        }

        return enumerable
            .Cast<object>()
            .Select(Convert.ToInt32)
            .ToArray();
    }

    private static string GetRequiredString(object source, string propertyName)
    {
        object value = GetRequiredProperty(source, propertyName);
        string? text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"{propertyName} property must not be empty.", nameof(source));
        }

        return text.Trim();
    }

    private static int GetRequiredInt32(object source, string propertyName)
    {
        object value = GetRequiredProperty(source, propertyName);
        return Convert.ToInt32(value);
    }

    private static object GetRequiredProperty(object source, string propertyName)
    {
        object? value = GetOptionalProperty(source, propertyName);
        if (value is null)
        {
            throw new ArgumentException($"{source.GetType().Name} must expose a non-null {propertyName} property.", nameof(source));
        }

        return value;
    }

    private static object? GetOptionalProperty(object source, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.GetType().GetProperty(propertyName)?.GetValue(source);
    }

    internal static string ToDisplayText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        List<char> characters = [];
        for (int index = 0; index < trimmed.Length; index++)
        {
            char current = trimmed[index];
            if (index > 0 && char.IsUpper(current) && !char.IsWhiteSpace(trimmed[index - 1]))
            {
                characters.Add(' ');
            }

            characters.Add(current);
        }

        string spaced = new(characters.ToArray());
        return char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }

    private string CreateSummaryText()
    {
        if (!HasRows)
        {
            return "No fabrication provider selected.";
        }

        return $"{ProviderCount} {Pluralize("provider", ProviderCount)}: " +
            $"{ReadyProviderCount} ready, " +
            $"{BlockedProviderCount} blocked, " +
            $"{WarningCount} {Pluralize("warning", WarningCount)}, " +
            $"{MissingFileCount} missing {Pluralize("file", MissingFileCount)}.";
    }

    private static string Pluralize(string singular, int count) =>
        count == 1 ? singular : $"{singular}s";
}

public sealed record FabricationOrderingDomainPackage(object Package, object ValidationResult);

public sealed record FabricationOrderingReadinessSource(
    string ProviderName,
    string ProviderKind,
    string Mode,
    IReadOnlyList<int> SupportedLayers,
    int MinimumQuantity,
    int MaximumQuantity,
    IReadOnlyList<FabricationOrderingDiagnostic> ValidationDiagnostics);

public sealed record FabricationOrderingDiagnostic(
    string Severity,
    string Code,
    string Message,
    string? FileRole)
{
    public static FabricationOrderingDiagnostic Error(string code, string message, string? fileRole) =>
        new("Error", code, message, fileRole);

    public static FabricationOrderingDiagnostic Warning(string code, string message, string? fileRole = null) =>
        new("Warning", code, message, fileRole);
}

internal sealed record FabricationOrderingProviderStyle(
    string ProviderKind,
    IReadOnlyList<int> SupportedLayers,
    int MinimumQuantity,
    int MaximumQuantity)
{
    public static FabricationOrderingProviderStyle For(FabricationHandoffOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return option.ProviderId switch
        {
            "osh-park" => new("Prototype", [2, 4], 3, 3),
            "pcbcart" => new("Production", [1, 2, 4, 6, 8, 10, 12], 5, 10000),
            _ => new("Production", [], 1, int.MaxValue)
        };
    }
}

public sealed class FabricationOrderingReadinessRow
{
    private const string PackageOnlyExplanation =
        "Checkout/submission is disabled: DragonCAD prepares the package only and does not place fabrication orders.";

    private FabricationOrderingReadinessRow(
        string providerName,
        string providerKind,
        string mode,
        string layerSupport,
        string quantitySupport,
        string packageReadiness,
        IReadOnlyList<string> missingFiles,
        IReadOnlyList<string> warnings)
    {
        ProviderName = providerName;
        ProviderKind = providerKind;
        Mode = mode;
        LayerSupport = layerSupport;
        QuantitySupport = quantitySupport;
        PackageReadiness = packageReadiness;
        MissingFiles = missingFiles;
        Warnings = warnings;
        CheckoutSubmissionDisabledExplanation = CreateDisabledExplanation(missingFiles.Count);
    }

    public string ProviderName { get; }

    public string ProviderKind { get; }

    public string Mode { get; }

    public string LayerSupport { get; }

    public string QuantitySupport { get; }

    public string PackageReadiness { get; }

    public IReadOnlyList<string> MissingFiles { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool IsCheckoutSubmissionEnabled => false;

    public string CheckoutSubmissionDisabledExplanation { get; }

    public static FabricationOrderingReadinessRow FromSource(FabricationOrderingReadinessSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        string[] missingFiles = source.ValidationDiagnostics
            .Where(diagnostic => IsSeverity(diagnostic, "Error"))
            .Select(diagnostic => diagnostic.FileRole)
            .OfType<string>()
            .Where(fileRole => !string.IsNullOrWhiteSpace(fileRole))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] warnings = source.ValidationDiagnostics
            .Where(diagnostic => IsSeverity(diagnostic, "Warning"))
            .Select(diagnostic => diagnostic.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        bool hasErrors = source.ValidationDiagnostics.Any(diagnostic => IsSeverity(diagnostic, "Error"));

        return new FabricationOrderingReadinessRow(
            source.ProviderName,
            source.ProviderKind,
            source.Mode,
            FormatLayerSupport(source.SupportedLayers),
            FormatQuantitySupport(source.MinimumQuantity, source.MaximumQuantity),
            hasErrors ? "Blocked" : "Ready",
            missingFiles,
            warnings);
    }

    private static bool IsSeverity(FabricationOrderingDiagnostic diagnostic, string severity) =>
        string.Equals(diagnostic.Severity, severity, StringComparison.OrdinalIgnoreCase);

    private static string FormatLayerSupport(IReadOnlyCollection<int> supportedLayers)
    {
        if (supportedLayers.Count == 0)
        {
            return "Any layer count";
        }

        int[] layers = supportedLayers
            .Distinct()
            .Order()
            .ToArray();

        string suffix = layers.Length == 1 ? "layer" : "layers";
        return $"{string.Join(", ", layers)} {suffix}";
    }

    private static string FormatQuantitySupport(int minimumQuantity, int maximumQuantity)
    {
        if (minimumQuantity == maximumQuantity)
        {
            return $"{minimumQuantity} boards";
        }

        if (maximumQuantity == int.MaxValue)
        {
            return $"{minimumQuantity}+ boards";
        }

        return $"{minimumQuantity}-{maximumQuantity} boards";
    }

    private static string CreateDisabledExplanation(int missingFileCount)
    {
        return missingFileCount switch
        {
            0 => PackageOnlyExplanation,
            1 => "Checkout/submission is disabled: package is blocked by 1 missing required file.",
            _ => $"Checkout/submission is disabled: package is blocked by {missingFileCount} missing required files."
        };
    }
}
