namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningGroup(
    string GroupKey,
    string CanonicalIdentity,
    string SelectedValue,
    string Package,
    int QuantityPerBuild,
    string SelectedManufacturerPartNumber,
    bool DoNotSubstitute,
    IReadOnlyList<string> Alternates,
    IReadOnlyList<string> Designators);
