namespace DragonCAD.Fabrication.Bom;

public sealed record BomComponent(
    string? Reference,
    string? Part,
    string? Value,
    string? Package);
