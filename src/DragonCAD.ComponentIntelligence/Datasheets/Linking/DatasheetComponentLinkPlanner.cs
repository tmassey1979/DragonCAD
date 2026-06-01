using DragonCAD.Core.Components.Identity;

namespace DragonCAD.ComponentIntelligence.Datasheets.Linking;

public sealed record ExistingComponentLinkCandidate
{
    public ExistingComponentLinkCandidate(
        ComponentId ComponentId,
        string Manufacturer,
        string ManufacturerPartNumber,
        string PackageName)
    {
        this.ComponentId = ComponentId;
        this.Manufacturer = NormalizeOptional(Manufacturer);
        this.ManufacturerPartNumber = NormalizeOptional(ManufacturerPartNumber);
        this.PackageName = NormalizeOptional(PackageName);
    }

    public ComponentId ComponentId { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string PackageName { get; }

    private static string NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}

public enum DatasheetComponentLinkDecision
{
    LinkExistingComponent,
    DuplicateExistingComponents,
    NeedsNewComponent,
}

public enum DatasheetComponentLinkReviewWarningCode
{
    MissingManufacturerPartNumber,
    DuplicateMatches,
    ManufacturerMismatch,
    PackageMismatch,
}

public sealed record DatasheetComponentLinkReviewWarning(
    DatasheetComponentLinkReviewWarningCode Code,
    string Message);

public sealed record DatasheetComponentLinkPlan(
    DatasheetComponentLinkDecision Decision,
    ComponentId? ComponentId,
    IReadOnlyList<ComponentId> CandidateComponentIds,
    IReadOnlyList<DatasheetComponentLinkReviewWarning> ReviewWarnings);

public sealed class DatasheetComponentLinkPlanner
{
    public DatasheetComponentLinkPlan Plan(
        ExtractedDatasheetFacts facts,
        IReadOnlyList<ExistingComponentLinkCandidate> existingComponents)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(existingComponents);

        string datasheetPartKey = NormalizePartKey(facts.ManufacturerPartNumber);
        if (datasheetPartKey.Length == 0)
        {
            return NeedsNewComponent(
                [
                    new DatasheetComponentLinkReviewWarning(
                        DatasheetComponentLinkReviewWarningCode.MissingManufacturerPartNumber,
                        "Datasheet manufacturer part number was not extracted; component link requires review."),
                ]);
        }

        ExistingComponentLinkCandidate[] matches = existingComponents
            .Where(candidate => CandidateMatches(candidate, datasheetPartKey))
            .OrderBy(candidate => candidate.ComponentId.Value, StringComparer.Ordinal)
            .ToArray();

        if (matches.Length == 0)
        {
            return NeedsNewComponent([]);
        }

        if (matches.Length > 1)
        {
            return new DatasheetComponentLinkPlan(
                DatasheetComponentLinkDecision.DuplicateExistingComponents,
                ComponentId: null,
                CandidateComponentIds: matches.Select(candidate => candidate.ComponentId).ToArray(),
                ReviewWarnings:
                [
                    new DatasheetComponentLinkReviewWarning(
                        DatasheetComponentLinkReviewWarningCode.DuplicateMatches,
                        "Multiple existing components matched the datasheet manufacturer part number."),
                ]);
        }

        ExistingComponentLinkCandidate match = matches[0];
        return new DatasheetComponentLinkPlan(
            DatasheetComponentLinkDecision.LinkExistingComponent,
            match.ComponentId,
            CandidateComponentIds: [match.ComponentId],
            BuildMetadataWarnings(facts, match));
    }

    private static DatasheetComponentLinkPlan NeedsNewComponent(
        IReadOnlyList<DatasheetComponentLinkReviewWarning> warnings) =>
        new(
            DatasheetComponentLinkDecision.NeedsNewComponent,
            ComponentId: null,
            CandidateComponentIds: [],
            ReviewWarnings: warnings);

    private static bool CandidateMatches(
        ExistingComponentLinkCandidate candidate,
        string datasheetPartKey) =>
        CandidatePartKeys(candidate).Contains(datasheetPartKey, StringComparer.Ordinal);

    private static IEnumerable<string> CandidatePartKeys(ExistingComponentLinkCandidate candidate)
    {
        string manufacturerPartKey = NormalizePartKey(candidate.ManufacturerPartNumber);
        if (manufacturerPartKey.Length > 0)
        {
            yield return manufacturerPartKey;
        }

        foreach (string componentIdSegment in SplitComponentIdSegments(candidate.ComponentId.Value))
        {
            string componentIdKey = NormalizePartKey(componentIdSegment);
            if (componentIdKey.Length > 0)
            {
                yield return componentIdKey;
            }
        }
    }

    private static IEnumerable<string> SplitComponentIdSegments(string componentId)
    {
        yield return componentId;

        foreach (char separator in new[] { ':', '/' })
        {
            int separatorIndex = componentId.LastIndexOf(separator);
            if (separatorIndex >= 0 && separatorIndex + 1 < componentId.Length)
            {
                yield return componentId[(separatorIndex + 1)..];
            }
        }
    }

    private static IReadOnlyList<DatasheetComponentLinkReviewWarning> BuildMetadataWarnings(
        ExtractedDatasheetFacts facts,
        ExistingComponentLinkCandidate match)
    {
        List<DatasheetComponentLinkReviewWarning> warnings = [];

        if (!MetadataMatches(facts.Manufacturer, match.Manufacturer))
        {
            warnings.Add(
                new DatasheetComponentLinkReviewWarning(
                    DatasheetComponentLinkReviewWarningCode.ManufacturerMismatch,
                    $"Datasheet manufacturer '{facts.Manufacturer}' differs from existing component manufacturer '{match.Manufacturer}'."));
        }

        if (!MetadataMatches(facts.PackageName, match.PackageName))
        {
            warnings.Add(
                new DatasheetComponentLinkReviewWarning(
                    DatasheetComponentLinkReviewWarningCode.PackageMismatch,
                    $"Datasheet package '{facts.PackageName}' differs from existing component package '{match.PackageName}'."));
        }

        return warnings;
    }

    private static bool MetadataMatches(string datasheetValue, string existingValue)
    {
        if (string.IsNullOrWhiteSpace(datasheetValue) || string.IsNullOrWhiteSpace(existingValue))
        {
            return true;
        }

        return string.Equals(
            datasheetValue.Trim(),
            existingValue.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePartKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(
            value
                .Trim()
                .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '_' and not '.')
                .Select(char.ToUpperInvariant)
                .ToArray());
    }
}
