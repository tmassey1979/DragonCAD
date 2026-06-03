using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Compliance;

public sealed record DatasheetDerivedComponentCompliance
{
    public DatasheetDerivedComponentCompliance(
        string componentId,
        string providerId,
        DatasheetSource datasheetSource,
        IReadOnlyList<DatasheetReviewWarning> reviewWarnings)
    {
        ComponentId = RequireText(componentId, nameof(componentId));
        ProviderId = RequireText(providerId, nameof(providerId));
        DatasheetSource = datasheetSource ?? throw new ArgumentNullException(nameof(datasheetSource));
        ReviewWarnings = new ReadOnlyCollection<DatasheetReviewWarning>(
            [.. reviewWarnings ?? []]);
    }

    public string ComponentId { get; }

    public string ProviderId { get; }

    public DatasheetSource DatasheetSource { get; }

    public IReadOnlyList<DatasheetReviewWarning> ReviewWarnings { get; }

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
