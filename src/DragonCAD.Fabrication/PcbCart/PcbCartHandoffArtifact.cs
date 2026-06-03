namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartHandoffArtifact(
    string Name,
    string Kind,
    string? RelativePath,
    string ReviewText);
