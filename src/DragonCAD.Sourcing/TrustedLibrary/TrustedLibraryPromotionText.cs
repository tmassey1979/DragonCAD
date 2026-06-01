namespace DragonCAD.Sourcing.TrustedLibrary;

internal static class TrustedLibraryPromotionText
{
    public static string Require(string? value, string parameterName)
    {
        string normalized = Optional(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }

    public static string Optional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

    public static string? OptionalOrNull(string? value)
    {
        string normalized = Optional(value);
        return normalized.Length == 0 ? null : normalized;
    }

    public static IReadOnlyList<TrustedLibraryArtifactPath> SortArtifacts(IReadOnlyList<TrustedLibraryArtifactPath>? artifactPaths)
    {
        ArgumentNullException.ThrowIfNull(artifactPaths);

        return artifactPaths
            .OrderBy(artifact => artifact.Kind, StringComparer.Ordinal)
            .ThenBy(artifact => artifact.Path, StringComparer.Ordinal)
            .ThenBy(artifact => artifact.Checksum, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> SortWarnings(IReadOnlyList<string>? warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        return warnings
            .Select(Optional)
            .Where(warning => warning.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(warning => warning, StringComparer.Ordinal)
            .ToArray();
    }
}
