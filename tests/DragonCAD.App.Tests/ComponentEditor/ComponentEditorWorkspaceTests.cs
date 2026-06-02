using DragonCAD.App.ComponentEditor;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.ComponentEditor;

public sealed class ComponentEditorWorkspaceTests
{
    [Fact]
    public void NewSessionStartsWithDeterministicValidationSummaryAndCannotSaveUntilChangedAndValid()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:new-component");

        Assert.Equal(ComponentEditorSessionKind.New, workspace.SessionKind);
        Assert.False(workspace.IsDirty);
        Assert.Equal(ComponentEditorSaveReadiness.BlockedByValidation, workspace.SaveReadiness.State);
        Assert.Equal("Resolve 4 validation issues before saving.", workspace.SaveReadiness.Message);
        Assert.Equal(
            [
                ComponentEditorValidationIssueKind.MissingSymbol,
                ComponentEditorValidationIssueKind.MissingFootprint,
                ComponentEditorValidationIssueKind.MissingPackage,
                ComponentEditorValidationIssueKind.MissingMapping
            ],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));
        Assert.Equal("Missing symbol, missing footprint, missing package, missing pin-pad mapping", workspace.ValidationSummary.DisplayText);
    }

    [Fact]
    public void EditSessionIsUnchangedUntilDraftDiffersFromOriginal()
    {
        ComponentDefinition original = ValidComponent("dragon:edit-component", "Editable Component");
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(original);

        Assert.Equal(ComponentEditorSessionKind.Edit, workspace.SessionKind);
        Assert.False(workspace.IsDirty);
        Assert.Equal(ComponentEditorSaveReadiness.Unchanged, workspace.SaveReadiness.State);
        Assert.Equal("No component changes to save.", workspace.SaveReadiness.Message);

        workspace.ViewModel.DisplayName = "Edited Component";

        Assert.True(workspace.IsDirty);
        Assert.Empty(workspace.ValidationSummary.Issues);
        Assert.Equal(ComponentEditorSaveReadiness.Ready, workspace.SaveReadiness.State);
        Assert.Equal("Ready to save component changes.", workspace.SaveReadiness.Message);
    }

    [Fact]
    public void ValidationSummaryClearsIssuesInDeterministicOrderAsDraftBecomesComplete()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:complete-component");
        ComponentEditorViewModel editor = workspace.ViewModel;

        editor.DisplayName = "Complete Component";
        editor.AddPin("1", "IO1");
        editor.AddSymbol("Primary Symbol");
        editor.AddFootprint("SOIC-8", [new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))]);

        Assert.Equal(
            [
                ComponentEditorValidationIssueKind.MissingPackage,
                ComponentEditorValidationIssueKind.MissingMapping
            ],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));
        Assert.Equal(ComponentEditorSaveReadiness.BlockedByValidation, workspace.SaveReadiness.State);

        editor.AddPackage("SOIC package", "SOIC-8");

        Assert.Equal(
            [ComponentEditorValidationIssueKind.MissingMapping],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));

        editor.MapPinToPad("1", "1");

        Assert.Empty(workspace.ValidationSummary.Issues);
        Assert.True(workspace.IsDirty);
        Assert.Equal(ComponentEditorSaveReadiness.Ready, workspace.SaveReadiness.State);
    }

    [Fact]
    public void NewComponentCanBeAuthoredWithCommandMethodsAndReadableSummaries()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:lm7805");
        ComponentEditorViewModel editor = workspace.ViewModel;

        editor.SetDisplayName("LM7805 5V Regulator");
        editor.SetManufacturer("Texas Instruments");
        editor.SetManufacturerPartNumber("LM7805CT");
        editor.SetDescription("Fixed 5V linear regulator in a TO-220 package.");
        editor.SetKind(ComponentKind.IntegratedCircuit);
        editor.AddBasicPinPackageAndMapping("1", "VIN", "TO-220-3");

        Assert.True(workspace.IsDirty);
        Assert.Equal(ComponentEditorSaveReadiness.Ready, workspace.SaveReadiness.State);
        Assert.Empty(workspace.ValidationSummary.Issues);
        Assert.Equal("LM7805 5V Regulator", editor.IdentitySummary.DisplayName);
        Assert.Equal("Texas Instruments - LM7805CT", editor.IdentitySummary.ManufacturerLine);
        Assert.Equal("Integrated Circuit", editor.IdentitySummary.KindText);
        Assert.Equal("Fixed 5V linear regulator in a TO-220 package.", editor.IdentitySummary.Description);
        Assert.Equal(["1 VIN (Bidirectional)"], editor.PinSummaries.Select(summary => summary.DisplayText));
        Assert.Equal(["Default Symbol - 1 pin"], editor.SymbolSummaries.Select(summary => summary.DisplayText));
        Assert.Equal(["TO-220-3 - 1 pad"], editor.FootprintSummaries.Select(summary => summary.DisplayText));
        Assert.Equal(["TO-220-3 - TO-220-3"], editor.PackageSummaries.Select(summary => summary.DisplayText));
        Assert.Equal(["No validation issues"], workspace.ValidationIssueDisplay.Select(issue => issue.DisplayText));
    }

    [Fact]
    public void SaveReadinessReturnsToUnchangedWhenEditDraftMatchesOriginalAgain()
    {
        ComponentDefinition original = ValidComponent("dragon:revert-component", "Revertable Component");
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(original);

        workspace.ViewModel.DisplayName = "Temporary Name";
        Assert.Equal(ComponentEditorSaveReadiness.Ready, workspace.SaveReadiness.State);

        workspace.ViewModel.DisplayName = "Revertable Component";

        Assert.False(workspace.IsDirty);
        Assert.Equal(ComponentEditorSaveReadiness.Unchanged, workspace.SaveReadiness.State);
    }

    private static ComponentDefinition ValidComponent(string id, string displayName)
    {
        ComponentPinId pinId = new($"{id}:pin:1");
        ComponentSymbolId symbolId = new($"{id}:symbol:main");
        ComponentFootprintId footprintId = new($"{id}:footprint:soic-8");
        ComponentPadId padId = new($"{id}:pad:1");
        ComponentVariantId variantId = new($"{id}:variant:soic-8");

        return new ComponentDefinition(
            new ComponentId(id),
            displayName,
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DC-1",
            Description: "",
            Attributes: [],
            Pins: [new ComponentPin(pinId, "IO1", "1", ComponentPinElectricalType.Bidirectional)],
            Gates: [],
            Symbols:
            [
                new ComponentSymbol(
                    symbolId,
                    "Primary Symbol",
                    [new ComponentSymbolPin(pinId, new CadPoint(0, 0), ComponentPinOrientation.Right)],
                    [],
                    [])
            ],
            Footprints:
            [
                new ComponentFootprint(
                    footprintId,
                    "SOIC-8",
                    [new ComponentFootprintPad(padId, "1", new CadPoint(0, 0), new CadVector(60_000, 80_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)],
                    [],
                    [])
            ],
            Variants: [new ComponentVariant(variantId, "SOIC package", footprintId, [])],
            PinPadMappings: [new ComponentPinPadMapping(variantId, pinId, padId)],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
    }
}
