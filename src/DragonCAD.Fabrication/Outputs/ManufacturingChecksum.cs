namespace DragonCAD.Fabrication.Outputs;

public sealed record ManufacturingChecksum
{
    private const string Sha256Prefix = "sha256:";
    private const string PendingPrefix = "pending:";

    private ManufacturingChecksum(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ManufacturingChecksum Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Manufacturing output checksums must not be empty.", nameof(value));
        }

        string normalized = value.Trim();
        if (IsSha256(normalized) || IsPendingToken(normalized))
        {
            return new ManufacturingChecksum(normalized);
        }

        throw new ArgumentException("Manufacturing output checksums must be sha256 hashes or pending tokens.", nameof(value));
    }

    public override string ToString()
    {
        return Value;
    }

    private static bool IsSha256(string value)
    {
        if (!value.StartsWith(Sha256Prefix, StringComparison.Ordinal) || value.Length != Sha256Prefix.Length + 64)
        {
            return false;
        }

        return value[Sha256Prefix.Length..].All(IsHex);
    }

    private static bool IsPendingToken(string value)
    {
        if (!value.StartsWith(PendingPrefix, StringComparison.Ordinal) || value.Length == PendingPrefix.Length)
        {
            return false;
        }

        return value[PendingPrefix.Length..].All(IsTokenCharacter);
    }

    private static bool IsHex(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private static bool IsTokenCharacter(char value)
    {
        return value is >= '0' and <= '9'
            or >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or '-'
            or '_'
            or '.';
    }
}
