using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Libraries.Permanent;

namespace DragonCAD.App.ComponentEditor;

public sealed class ComponentEditorTrustedLibrarySaveService
{
    private static readonly DateTimeOffset AuthoredComponentImportTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly PermanentLibraryImportStore trustedLibraryStore;

    public ComponentEditorTrustedLibrarySaveService(PermanentLibraryImportStore trustedLibraryStore)
    {
        ArgumentNullException.ThrowIfNull(trustedLibraryStore);
        this.trustedLibraryStore = trustedLibraryStore;
    }

    public ComponentEditorTrustedLibrarySaveResult Save(ComponentEditorWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        ComponentEditorValidationSummary validation = workspace.ValidationSummary;
        if (validation.Issues.Count > 0)
        {
            return ComponentEditorTrustedLibrarySaveResult.Failed(validation.Issues.Select(ToDiagnostic).ToArray());
        }

        ComponentDefinition trustedDefinition = WithAuthoredProvenance(workspace.ViewModel.ToDefinition());
        string serializedComponent = ComponentDefinitionSerializer.Serialize(trustedDefinition);
        PermanentLibraryImportSource importSource = PermanentLibraryImportSource.UserLibrary(
            "Component Editor",
            "component-editor://authored",
            AuthoredComponentImportTime);
        PermanentLibraryImportResult importResult = trustedLibraryStore.Import(importSource, [trustedDefinition]);
        PermanentLibraryComponentRecord component = importResult.WrittenComponents.FirstOrDefault()
            ?? trustedLibraryStore.Components.Single(record => record.Component.Id == trustedDefinition.Id);

        return ComponentEditorTrustedLibrarySaveResult.Success(component, serializedComponent);
    }

    private static ComponentDefinition WithAuthoredProvenance(ComponentDefinition definition)
    {
        ComponentProvenanceRecord authoredProvenance = new(
            ComponentProvenanceKind.Manual,
            "Component Editor",
            "Authored component saved to trusted library.");

        if (definition.Provenance.Contains(authoredProvenance))
        {
            return definition;
        }

        return definition with
        {
            Provenance = [.. definition.Provenance, authoredProvenance]
        };
    }

    private static ComponentEditorTrustedLibrarySaveDiagnostic ToDiagnostic(ComponentEditorValidationIssue issue) =>
        new(ToDiagnosticCode(issue.Kind), issue.DisplayText);

    private static ComponentEditorTrustedLibrarySaveDiagnosticCode ToDiagnosticCode(ComponentEditorValidationIssueKind kind) =>
        kind switch
        {
            ComponentEditorValidationIssueKind.MissingSymbol => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingSymbol,
            ComponentEditorValidationIssueKind.MissingPins => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingPins,
            ComponentEditorValidationIssueKind.MissingFootprint => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingFootprint,
            ComponentEditorValidationIssueKind.MissingPackage => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingPackage,
            ComponentEditorValidationIssueKind.MissingPackageName => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingPackageName,
            ComponentEditorValidationIssueKind.MissingMapping => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingMapping,
            ComponentEditorValidationIssueKind.MissingPin => ComponentEditorTrustedLibrarySaveDiagnosticCode.MissingPin,
            ComponentEditorValidationIssueKind.DuplicatePinName => ComponentEditorTrustedLibrarySaveDiagnosticCode.DuplicatePinName,
            ComponentEditorValidationIssueKind.IncompleteMapping => ComponentEditorTrustedLibrarySaveDiagnosticCode.IncompleteMapping,
            ComponentEditorValidationIssueKind.UnmappedPin => ComponentEditorTrustedLibrarySaveDiagnosticCode.UnmappedPin,
            ComponentEditorValidationIssueKind.DuplicatePadMapping => ComponentEditorTrustedLibrarySaveDiagnosticCode.DuplicatePadMapping,
            _ => ComponentEditorTrustedLibrarySaveDiagnosticCode.Validation
        };
}

public sealed record ComponentEditorTrustedLibrarySaveResult(
    bool Succeeded,
    PermanentLibraryComponentRecord? Component,
    string SerializedComponent,
    IReadOnlyList<ComponentEditorTrustedLibrarySaveDiagnostic> Diagnostics)
{
    public static ComponentEditorTrustedLibrarySaveResult Success(
        PermanentLibraryComponentRecord component,
        string serializedComponent) =>
        new(true, component, serializedComponent, []);

    public static ComponentEditorTrustedLibrarySaveResult Failed(
        IReadOnlyList<ComponentEditorTrustedLibrarySaveDiagnostic> diagnostics) =>
        new(false, null, string.Empty, diagnostics);
}

public sealed record ComponentEditorTrustedLibrarySaveDiagnostic(
    ComponentEditorTrustedLibrarySaveDiagnosticCode Code,
    string Message);

public enum ComponentEditorTrustedLibrarySaveDiagnosticCode
{
    Validation,
    MissingSymbol,
    MissingPins,
    MissingFootprint,
    MissingPackage,
    MissingPackageName,
    MissingMapping,
    MissingPin,
    DuplicatePinName,
    IncompleteMapping,
    UnmappedPin,
    DuplicatePadMapping
}
