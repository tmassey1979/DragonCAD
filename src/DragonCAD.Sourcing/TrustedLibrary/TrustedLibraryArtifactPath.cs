namespace DragonCAD.Sourcing.TrustedLibrary;

public sealed record TrustedLibraryArtifactPath
{
    public TrustedLibraryArtifactPath(string kind, string path, string? checksum)
    {
        Kind = TrustedLibraryPromotionText.Require(kind, nameof(kind));
        Path = TrustedLibraryPromotionText.Require(path, nameof(path));
        Checksum = TrustedLibraryPromotionText.OptionalOrNull(checksum);
    }

    public string Kind { get; }

    public string Path { get; }

    public string? Checksum { get; }

    public string Summary => $"{Kind}:{Path}";
}
