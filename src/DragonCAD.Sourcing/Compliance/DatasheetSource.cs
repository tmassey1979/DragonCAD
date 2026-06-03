namespace DragonCAD.Sourcing.Compliance;

public sealed record DatasheetSource
{
    public DatasheetSource(Uri url, string? documentTitle, DateTimeOffset retrievedAtUtc)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        DocumentTitle = Normalize(documentTitle);
        RetrievedAtUtc = retrievedAtUtc;
    }

    public Uri Url { get; }

    public string DocumentTitle { get; }

    public DateTimeOffset RetrievedAtUtc { get; }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
