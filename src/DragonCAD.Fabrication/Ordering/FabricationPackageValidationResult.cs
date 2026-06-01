namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationPackageValidationResult
{
    private FabricationPackageValidationResult(FabricationPackageDiagnostic[] diagnostics)
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<FabricationPackageDiagnostic> Diagnostics { get; }

    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != FabricationPackageDiagnosticSeverity.Error);

    public static FabricationPackageValidationResult Create(IEnumerable<FabricationPackageDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        FabricationPackageDiagnostic[] sortedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.FileRole)
            .ToArray();

        return new FabricationPackageValidationResult(sortedDiagnostics);
    }
}
