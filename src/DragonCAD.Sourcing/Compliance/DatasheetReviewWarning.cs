namespace DragonCAD.Sourcing.Compliance;

public sealed record DatasheetReviewWarning
{
    public DatasheetReviewWarning(string code, string message)
    {
        Code = RequireText(code, nameof(code));
        Message = RequireText(message, nameof(message));
    }

    public string Code { get; }

    public string Message { get; }

    private static string RequireText(string value, string parameterName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }
}
