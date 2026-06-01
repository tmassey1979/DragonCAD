namespace DragonCAD.Sourcing.Deduplication;

public sealed record ComponentCandidate(
    string ManufacturerPartNumber,
    string Manufacturer,
    string? Package,
    string? Value,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> SourceKeys,
    IReadOnlyList<ComponentMergeWarning> Warnings);
