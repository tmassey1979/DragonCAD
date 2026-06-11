namespace DragonCAD.App.BoardEditor;

public sealed record BoardFootprintReplacementResult(
    bool Succeeded,
    IReadOnlyList<BoardFootprintReplacementDiagnostic> Diagnostics,
    string SyncId,
    string PackageLabel)
{
    public static BoardFootprintReplacementResult Success(string syncId, string packageLabel) =>
        new(true, [], syncId, packageLabel);

    public static BoardFootprintReplacementResult Failed(
        string syncId,
        string packageLabel,
        params BoardFootprintReplacementDiagnostic[] diagnostics) =>
        new(false, diagnostics, syncId, packageLabel);
}

public sealed record BoardFootprintReplacementDiagnostic(
    BoardFootprintReplacementDiagnosticCode Code,
    string Target,
    string Message);

public enum BoardFootprintReplacementDiagnosticCode
{
    MissingComponent,
    MissingFootprintMapping,
    MissingPadMapping
}
