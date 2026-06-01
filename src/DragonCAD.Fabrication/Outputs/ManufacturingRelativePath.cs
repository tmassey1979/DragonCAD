namespace DragonCAD.Fabrication.Outputs;

public sealed record ManufacturingRelativePath
{
    private ManufacturingRelativePath(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ManufacturingRelativePath Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Manufacturing output paths must not be empty.", nameof(value));
        }

        string normalized = value.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized) || normalized.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("Manufacturing output paths must be relative project paths.", nameof(value));
        }

        string[] segments = normalized.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new ArgumentException("Manufacturing output paths must not contain empty or parent-directory segments.", nameof(value));
        }

        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Manufacturing output paths must not contain control characters.", nameof(value));
        }

        return new ManufacturingRelativePath(normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}
