namespace DragonCAD.Fabrication.Bom;

public sealed record BomComponent(
    string? Reference,
    string? Part,
    string? Value,
    string? Package,
    string? ManufacturerPartNumber = null,
    string? Notes = null);
