namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed record DigiKeyOAuthTokenResult(
    DigiKeyOAuthToken? Token,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics);
