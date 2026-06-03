namespace DragonCAD.Sourcing.Compliance;

public sealed record AttributionRequirement
{
    public AttributionRequirement(bool isRequired, string? notice)
    {
        IsRequired = isRequired;
        Notice = Normalize(notice);
    }

    public static AttributionRequirement NotRequired { get; } = new(false, string.Empty);

    public bool IsRequired { get; }

    public string Notice { get; }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
