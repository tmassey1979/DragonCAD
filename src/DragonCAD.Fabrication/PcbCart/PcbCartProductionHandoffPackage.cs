namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartProductionHandoffPackage(
    PcbCartProviderCapabilities ProviderCapabilities,
    IReadOnlyList<PcbCartHandoffArtifact> Artifacts,
    IReadOnlyList<PcbCartDiagnostic> Diagnostics,
    string ReviewSummary)
{
    public bool IsReadyForQuote => Diagnostics.All(diagnostic => diagnostic.Severity != PcbCartDiagnosticSeverity.Error);
}
