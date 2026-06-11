namespace DragonCAD.Sourcing.Catalog.Smoke;

internal static class VendorLiveSmokeReportSanitizer
{
    private static readonly string[] EnvironmentNames =
    [
        "DRAGONCAD_DIGIKEY_CLIENT_ID",
        "DRAGONCAD_DIGIKEY_CLIENT_SECRET",
        "DRAGONCAD_MOUSER_API_KEY",
        VendorLiveSmokeHarness.GateEnvironmentVariable,
        VendorLiveSmokeHarness.ModeEnvironmentVariable,
    ];

    public static IReadOnlyList<string> Sanitize(
        IReadOnlyList<CatalogImportDiagnostic> diagnostics,
        IReadOnlyList<string>? redactionTerms = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return diagnostics
            .Select(diagnostic => Sanitize(diagnostic.Message, redactionTerms))
            .ToArray();
    }

    public static string Sanitize(string value, IReadOnlyList<string>? redactionTerms = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var sanitized = value;
        foreach (var environmentName in EnvironmentNames)
        {
            sanitized = sanitized.Replace(environmentName, "[redacted]", StringComparison.Ordinal);
        }

        foreach (var secretTerm in redactionTerms ?? [])
        {
            sanitized = sanitized.Replace(secretTerm, "[redacted]", StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
    }
}
