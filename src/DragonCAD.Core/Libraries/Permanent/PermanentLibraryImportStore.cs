using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Libraries.Permanent;

public sealed class PermanentLibraryImportStore
{
    private readonly List<PermanentLibraryComponentRecord> components = [];
    private readonly List<PermanentLibraryConflictReviewItem> reviewItems = [];

    public IReadOnlyList<PermanentLibraryComponentRecord> Components => components;

    public IReadOnlyList<PermanentLibraryConflictReviewItem> ReviewItems => reviewItems;

    public PermanentLibraryImportResult Import(
        PermanentLibraryImportSource source,
        IReadOnlyList<ComponentDefinition> importedComponents)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(importedComponents);

        List<PermanentLibraryComponentRecord> writtenComponents = [];
        List<PermanentLibraryImportLink> linkedComponents = [];
        List<PermanentLibraryConflictReviewItem> createdReviewItems = [];

        foreach (ComponentDefinition importedComponent in importedComponents)
        {
            importedComponent.Validate();
            ComponentId canonicalComponentId = PermanentComponentId.FromImported(importedComponent.Id);
            PermanentLibrarySourceProvenance provenance = PermanentLibrarySourceProvenance.From(source, importedComponent);
            PermanentLibraryComponentRecord? existingComponent = Find(canonicalComponentId);

            if (existingComponent is null)
            {
                PermanentLibraryComponentRecord record = new(
                    canonicalComponentId,
                    importedComponent,
                    PermanentLibraryVerificationState.Imported,
                    ReviewNote: string.Empty,
                    [provenance]);
                components.Add(record);
                writtenComponents.Add(record);
                continue;
            }

            if (existingComponent.Component.Equals(importedComponent))
            {
                PermanentLibraryComponentRecord updated = existingComponent.WithProvenance(provenance);
                Replace(existingComponent, updated);
                linkedComponents.Add(new PermanentLibraryImportLink(
                    canonicalComponentId,
                    importedComponent.Id,
                    provenance));
                continue;
            }

            PermanentLibraryConflictReviewItem reviewItem = new(
                canonicalComponentId,
                importedComponent.Id,
                importedComponent,
                provenance,
                ConflictReason(existingComponent));
            reviewItems.Add(reviewItem);
            createdReviewItems.Add(reviewItem);
        }

        return new PermanentLibraryImportResult(writtenComponents, linkedComponents, createdReviewItems);
    }

    public void MarkVerified(ComponentId importedComponentId, string reviewNote)
    {
        ComponentId canonicalComponentId = PermanentComponentId.FromImported(importedComponentId);
        PermanentLibraryComponentRecord record = Find(canonicalComponentId)
            ?? throw new InvalidOperationException($"Permanent component '{canonicalComponentId}' was not found.");

        Replace(record, record.MarkVerified(reviewNote));
    }

    private PermanentLibraryComponentRecord? Find(ComponentId canonicalComponentId) =>
        components.FirstOrDefault(component => component.CanonicalComponentId == canonicalComponentId);

    private void Replace(PermanentLibraryComponentRecord previous, PermanentLibraryComponentRecord replacement)
    {
        int index = components.IndexOf(previous);
        components[index] = replacement;
    }

    private static string ConflictReason(PermanentLibraryComponentRecord existingComponent) =>
        existingComponent.VerificationState == PermanentLibraryVerificationState.Verified
            ? "Imported component differs from an existing verified permanent component."
            : "Imported component differs from an existing permanent component.";
}

public sealed record PermanentLibraryImportSource(
    PermanentLibraryImportSourceKind Kind,
    string SourceName,
    string SourceLocation,
    DateTimeOffset ImportedAt)
{
    public string SourceName { get; init; } = Normalize(SourceName, nameof(SourceName));

    public string SourceLocation { get; init; } = Normalize(SourceLocation, nameof(SourceLocation));

    public static PermanentLibraryImportSource EagleLibrary(
        string sourceName,
        string sourceLocation,
        DateTimeOffset importedAt) =>
        new(PermanentLibraryImportSourceKind.EagleLibrary, sourceName, sourceLocation, importedAt);

    public static PermanentLibraryImportSource UserLibrary(
        string sourceName,
        string sourceLocation,
        DateTimeOffset importedAt) =>
        new(PermanentLibraryImportSourceKind.UserLibrary, sourceName, sourceLocation, importedAt);

    private static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}

public enum PermanentLibraryImportSourceKind
{
    EagleLibrary,
    UserLibrary,
    AdafruitLibrary,
    SparkFunLibrary
}

public sealed record PermanentLibraryComponentRecord(
    ComponentId CanonicalComponentId,
    ComponentDefinition Component,
    PermanentLibraryVerificationState VerificationState,
    string ReviewNote,
    IReadOnlyList<PermanentLibrarySourceProvenance> Provenance)
{
    public PermanentLibraryComponentRecord WithProvenance(PermanentLibrarySourceProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        return this with { Provenance = [.. Provenance, provenance] };
    }

    public PermanentLibraryComponentRecord MarkVerified(string reviewNote)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewNote);

        return this with
        {
            VerificationState = PermanentLibraryVerificationState.Verified,
            ReviewNote = reviewNote.Trim()
        };
    }
}

public enum PermanentLibraryVerificationState
{
    Imported,
    Verified
}

public sealed record PermanentLibrarySourceProvenance(
    PermanentLibraryImportSourceKind Kind,
    string SourceName,
    string SourceLocation,
    DateTimeOffset ImportedAt,
    ComponentId ImportedComponentId,
    IReadOnlyList<ComponentProvenanceRecord> ComponentProvenance)
{
    public static PermanentLibrarySourceProvenance From(
        PermanentLibraryImportSource source,
        ComponentDefinition importedComponent) =>
        new(
            source.Kind,
            source.SourceName,
            source.SourceLocation,
            source.ImportedAt,
            importedComponent.Id,
            importedComponent.Provenance.ToArray());
}

public sealed record PermanentLibraryImportResult(
    IReadOnlyList<PermanentLibraryComponentRecord> WrittenComponents,
    IReadOnlyList<PermanentLibraryImportLink> LinkedComponents,
    IReadOnlyList<PermanentLibraryConflictReviewItem> ReviewItems);

public sealed record PermanentLibraryImportLink(
    ComponentId CanonicalComponentId,
    ComponentId ImportedComponentId,
    PermanentLibrarySourceProvenance Provenance);

public sealed record PermanentLibraryConflictReviewItem(
    ComponentId CanonicalComponentId,
    ComponentId ImportedComponentId,
    ComponentDefinition ProposedComponent,
    PermanentLibrarySourceProvenance Provenance,
    string Reason);

internal static class PermanentComponentId
{
    public static ComponentId FromImported(ComponentId importedComponentId)
    {
        string value = importedComponentId.Value;
        const string hawkCadPrefix = "hawkcad:";
        if (value.StartsWith(hawkCadPrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[hawkCadPrefix.Length..];
        }

        return new ComponentId($"perm:{value}");
    }
}
