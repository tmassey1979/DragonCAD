namespace DragonCAD.Sourcing.Deduplication;

public sealed record ComponentMergeWarning(
    ComponentMergeWarningKind Kind,
    string Message,
    IReadOnlyList<string> Values,
    IReadOnlyList<string> SourceKeys);
