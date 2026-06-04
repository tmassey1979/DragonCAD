namespace DragonCAD.App.BoardEditor;

public sealed record BoardSelectionSnapshot(
    IReadOnlyList<string> ComponentSyncIds,
    IReadOnlyList<string> TraceIds,
    IReadOnlyList<string> ViaIds,
    IReadOnlyList<string> FootprintPrimitiveKinds)
{
    public int TotalObjects => ComponentSyncIds.Count + TraceIds.Count + ViaIds.Count;
}
