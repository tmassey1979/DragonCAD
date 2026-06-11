using DragonCAD.App.ComponentEditor;
using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.ComponentEditor;

public sealed class ComponentEditorWorkspaceTests
{
    [Fact]
    public void WorkspaceCommandsOpenNewComponentWithEmptyEditorSections()
    {
        ComponentEditorWorkspaceHost host = new();

        host.NewComponentCommand.Execute(null);

        ComponentEditorWorkspace workspace = Assert.Single(host.OpenWorkspaces);
        Assert.Same(workspace, host.ActiveWorkspace);
        Assert.Equal(ComponentEditorSessionKind.New, workspace.SessionKind);
        Assert.Equal("dragon:new-component-001", workspace.ViewModel.ComponentId);
        Assert.Equal(ComponentEditorSectionState.Empty, workspace.SymbolSection.State);
        Assert.Equal(ComponentEditorSectionState.Empty, workspace.FootprintSection.State);
        Assert.Equal(ComponentEditorSectionState.Empty, workspace.PackageSection.State);
        Assert.Equal(ComponentEditorSectionState.Empty, workspace.MappingSection.State);
    }

    [Fact]
    public void WorkspaceCommandsOpenEditCopyWithTrustedLibrarySections()
    {
        ComponentDefinition trusted = ValidComponent("dragon:trusted-copy", "Trusted Copy");
        ComponentEditorWorkspaceHost host = new();

        host.EditComponentCommand.Execute(trusted);

        ComponentEditorWorkspace workspace = Assert.Single(host.OpenWorkspaces);
        Assert.Equal(ComponentEditorSessionKind.Edit, workspace.SessionKind);
        Assert.True(workspace.IsTrustedLibraryEntry);
        Assert.Equal(["Primary Symbol - 1 pin"], workspace.SymbolSection.Items.Select(item => item.DisplayText));
        Assert.Equal(["SOIC-8 - 1 pad"], workspace.FootprintSection.Items.Select(item => item.DisplayText));
        Assert.Equal(["SOIC package - SOIC-8"], workspace.PackageSection.Items.Select(item => item.DisplayText));
        Assert.Equal(["SOIC package: 1 -> 1"], workspace.MappingSection.Items.Select(item => item.DisplayText));

        workspace.ViewModel.DisplayName = "Edited Copy";

        Assert.Equal("Trusted Copy", trusted.DisplayName);
        Assert.True(workspace.IsDirty);
    }

    [Fact]
    public void DirtyWorkspaceCloseRequestBlocksCloseUntilDecisionIsConfirmed()
    {
        ComponentEditorWorkspaceHost host = new();
        host.NewComponentCommand.Execute(null);
        ComponentEditorWorkspace workspace = host.ActiveWorkspace!;
        workspace.ViewModel.SetDisplayName("Unsaved Component");

        ComponentEditorCloseDecision decision = host.RequestClose(workspace);

        Assert.Equal(ComponentEditorCloseDecisionKind.PromptRequired, decision.Kind);
        Assert.Equal("Unsaved changes in dragon:new-component-001", decision.Title);
        Assert.Same(decision, host.PendingCloseDecision);
        Assert.Same(workspace, host.ActiveWorkspace);

        host.CancelClose();

        Assert.Null(host.PendingCloseDecision);
        Assert.Same(workspace, host.ActiveWorkspace);

        ComponentEditorCloseDecision secondDecision = host.RequestClose(workspace);
        host.ConfirmClose(secondDecision);

        Assert.Empty(host.OpenWorkspaces);
        Assert.Null(host.ActiveWorkspace);
        Assert.Null(host.PendingCloseDecision);
    }

    [Fact]
    public void CleanWorkspaceCloseRequestClosesWithoutPrompt()
    {
        ComponentEditorWorkspaceHost host = new();
        host.NewComponentCommand.Execute(null);
        ComponentEditorWorkspace workspace = host.ActiveWorkspace!;

        ComponentEditorCloseDecision decision = host.RequestClose(workspace);

        Assert.Equal(ComponentEditorCloseDecisionKind.CloseNow, decision.Kind);
        Assert.Empty(host.OpenWorkspaces);
        Assert.Null(host.PendingCloseDecision);
    }

    [Fact]
    public void ValidationReportsMissingPackageNameAndIncompleteMappingBeforeSave()
    {
        ComponentPinId pinId = new("dragon:invalid-save:pin:1");
        ComponentVariantId variantId = new("dragon:invalid-save:variant:nameless");
        ComponentDefinition invalid = new(
            new ComponentId("dragon:invalid-save"),
            "Invalid Save",
            ComponentKind.Custom,
            "",
            "",
            Description: "",
            Attributes: [],
            Pins: [new ComponentPin(pinId, "IO1", "1", ComponentPinElectricalType.Bidirectional)],
            Gates: [],
            Symbols: [],
            Footprints: [],
            Variants: [new ComponentVariant(variantId, " ", new ComponentFootprintId("dragon:invalid-save:footprint:missing"), [])],
            PinPadMappings: [new ComponentPinPadMapping(variantId, pinId, new ComponentPadId("dragon:invalid-save:pad:missing"))],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(invalid);

        Assert.Equal(
            [
                ComponentEditorValidationIssueKind.MissingSymbol,
                ComponentEditorValidationIssueKind.MissingFootprint,
                ComponentEditorValidationIssueKind.MissingPackageName,
                ComponentEditorValidationIssueKind.IncompleteMapping
            ],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));
        Assert.Equal(ComponentEditorSaveReadiness.BlockedByValidation, workspace.SaveReadiness.State);
    }

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
                ComponentEditorValidationIssueKind.MissingPins,
                ComponentEditorValidationIssueKind.MissingFootprint,
                ComponentEditorValidationIssueKind.MissingPackage,
                ComponentEditorValidationIssueKind.MissingMapping
            ],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));
        Assert.Equal("Missing symbol, missing pins, missing footprint, missing package, missing pin-pad mapping", workspace.ValidationSummary.DisplayText);
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

    [Fact]
    public void AddPinCommandAddsLogicalAndSymbolPinAtGridSnappedPosition()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:add-pin");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");

        ComponentEditorCommandResult result = editor.AddPin(" 2 ", " OUT ", new CadPoint(149_999, 251_000));

        Assert.Empty(result.Diagnostics);
        ComponentPin pin = Assert.Single(editor.Pins);
        Assert.Equal("2", pin.Number);
        Assert.Equal("OUT", pin.Name);
        ComponentSymbolPin symbolPin = Assert.Single(Assert.Single(editor.Symbols).Pins);
        Assert.Equal(pin.Id, symbolPin.PinId);
        Assert.Equal(new CadPoint(100_000, 300_000), symbolPin.Position);
    }

    [Fact]
    public void AddSymbolPrimitiveCommandAddsDrawablePrimitiveToSymbol()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:add-symbol-primitive");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");

        ComponentEditorCommandResult result = editor.AddSymbolLine("Main", new CadPoint(1, 2), new CadPoint(3, 4));

        Assert.Empty(result.Diagnostics);
        ComponentSymbolLinePrimitive primitive = Assert.IsType<ComponentSymbolLinePrimitive>(
            Assert.Single(Assert.Single(editor.Symbols).Primitives));
        Assert.Equal(new CadPoint(1, 2), primitive.Start);
        Assert.Equal(new CadPoint(3, 4), primitive.End);
    }

    [Fact]
    public void SymbolToolActivationExposesExplicitAuthoringTools()
    {
        ComponentEditorViewModel editor = ComponentEditorWorkspace.StartNew("dragon:symbol-tools").ViewModel;

        Assert.Equal(ComponentEditorSymbolTool.Select, editor.ActiveSymbolTool);
        Assert.Equal(
            [
                ComponentEditorSymbolTool.Select,
                ComponentEditorSymbolTool.Pin,
                ComponentEditorSymbolTool.Line,
                ComponentEditorSymbolTool.Arc,
                ComponentEditorSymbolTool.Text
            ],
            editor.AvailableSymbolTools);

        editor.ActivateSymbolTool(ComponentEditorSymbolTool.Pin);

        Assert.Equal(ComponentEditorSymbolTool.Pin, editor.ActiveSymbolTool);
    }

    [Fact]
    public void PinToolPreviewsAndCommitsPinWithDirectionAndConnectionPoint()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:pin-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.ActivateSymbolTool(ComponentEditorSymbolTool.Pin);

        ComponentEditorSymbolPlacementPreview preview = editor.PreviewSymbolPlacement(new CadPoint(149_999, 251_000));
        ComponentEditorCommandResult result = editor.PlaceSymbolPin(" 7 ", " RESET ", ComponentPinOrientation.Left, new CadPoint(149_999, 251_000));

        Assert.Equal(ComponentEditorSymbolTool.Pin, preview.Tool);
        Assert.Equal(new CadPoint(100_000, 300_000), preview.ConnectionPoint);
        Assert.Empty(result.Diagnostics);
        ComponentPin pin = Assert.Single(editor.Pins);
        Assert.Equal("7", pin.Number);
        Assert.Equal("RESET", pin.Name);
        ComponentSymbolPin symbolPin = Assert.Single(Assert.Single(editor.Symbols).Pins);
        Assert.Equal(pin.Id, symbolPin.PinId);
        Assert.Equal(ComponentPinOrientation.Left, symbolPin.Orientation);
        Assert.Equal(new CadPoint(100_000, 300_000), symbolPin.Position);
    }

    [Fact]
    public void LineToolPreviewsAndCommitsGridSnappedPrimitive()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:line-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.ActivateSymbolTool(ComponentEditorSymbolTool.Line);

        ComponentEditorSymbolPlacementPreview preview = editor.PreviewSymbolPlacement(new CadPoint(51_000, 149_999), new CadPoint(251_000, 249_999));
        ComponentEditorCommandResult result = editor.PlaceSymbolLine(new CadPoint(51_000, 149_999), new CadPoint(251_000, 249_999));

        Assert.Equal(ComponentEditorSymbolTool.Line, preview.Tool);
        Assert.Equal(new CadPoint(100_000, 100_000), preview.Start);
        Assert.Equal(new CadPoint(300_000, 200_000), preview.End);
        Assert.Empty(result.Diagnostics);
        ComponentSymbolLinePrimitive primitive = Assert.IsType<ComponentSymbolLinePrimitive>(
            Assert.Single(Assert.Single(editor.Symbols).Primitives));
        Assert.Equal(new CadPoint(100_000, 100_000), primitive.Start);
        Assert.Equal(new CadPoint(300_000, 200_000), primitive.End);
    }

    [Fact]
    public void TextToolPreviewsAndCommitsGridSnappedPrimitive()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:text-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.ActivateSymbolTool(ComponentEditorSymbolTool.Text);

        ComponentEditorSymbolPlacementPreview preview = editor.PreviewSymbolPlacement(new CadPoint(-51_000, 49_999));
        ComponentEditorCommandResult result = editor.PlaceSymbolText("VREF", new CadPoint(-51_000, 49_999));

        Assert.Equal(ComponentEditorSymbolTool.Text, preview.Tool);
        Assert.Equal(new CadPoint(-100_000, 0), preview.Position);
        Assert.Empty(result.Diagnostics);
        ComponentSymbolTextPrimitive primitive = Assert.IsType<ComponentSymbolTextPrimitive>(
            Assert.Single(Assert.Single(editor.Symbols).Primitives));
        Assert.Equal(ComponentSymbolTextKind.Custom, primitive.Kind);
        Assert.Equal("VREF", primitive.Value);
        Assert.Equal(new CadPoint(-100_000, 0), primitive.Position);
    }

    [Fact]
    public void ArcToolPreviewsAndCommitsGridSnappedModelState()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:arc-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.ActivateSymbolTool(ComponentEditorSymbolTool.Arc);

        ComponentEditorSymbolPlacementPreview preview = editor.PreviewSymbolPlacement(new CadPoint(49_999, 51_000), new CadPoint(249_999, 51_000));
        ComponentEditorCommandResult result = editor.PlaceSymbolArc(new CadPoint(49_999, 51_000), new CadPoint(249_999, 51_000), 45, 180);

        Assert.Equal(ComponentEditorSymbolTool.Arc, preview.Tool);
        Assert.Equal(new CadPoint(0, 100_000), preview.Center);
        Assert.Equal(200_000, preview.Radius);
        Assert.Empty(result.Diagnostics);
        ComponentSymbolArcPrimitive primitive = Assert.IsType<ComponentSymbolArcPrimitive>(
            Assert.Single(Assert.Single(editor.Symbols).Primitives));
        Assert.Equal(new CadPoint(0, 100_000), primitive.Center);
        Assert.Equal(200_000, primitive.Radius);
        Assert.Equal(45, primitive.StartAngleDegrees);
        Assert.Equal(180, primitive.SweepAngleDegrees);
    }

    [Fact]
    public void RemoveLastSymbolAuthoringItemRemovesLastCommittedPrimitive()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:remove-last-symbol-item");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.PlaceSymbolLine(new CadPoint(0, 0), new CadPoint(100_000, 0));
        editor.PlaceSymbolText("OUT", new CadPoint(0, 100_000));

        ComponentEditorCommandResult result = editor.RemoveLastSymbolAuthoringItem();

        Assert.Empty(result.Diagnostics);
        ComponentSymbol symbol = Assert.Single(editor.Symbols);
        ComponentSymbolLinePrimitive primitive = Assert.IsType<ComponentSymbolLinePrimitive>(Assert.Single(symbol.Primitives));
        Assert.Equal(new CadPoint(0, 0), primitive.Start);
        Assert.Equal(new CadPoint(100_000, 0), primitive.End);
    }

    [Fact]
    public void MovePinCommandMovesSymbolPinToGridSnappedPosition()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:move-pin");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddSymbol("Main");
        editor.AddPin("1", "IN", new CadPoint(0, 0));

        ComponentEditorCommandResult result = editor.MovePin("1", new CadPoint(51_000, -149_999));

        Assert.Empty(result.Diagnostics);
        ComponentSymbolPin symbolPin = Assert.Single(Assert.Single(editor.Symbols).Pins);
        Assert.Equal(new CadPoint(100_000, -100_000), symbolPin.Position);
    }

    [Fact]
    public void DeletePinCommandRemovesSymbolPinsAndMappings()
    {
        ComponentDefinition original = ValidComponent("dragon:delete-pin", "Delete Pin");
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(original);

        ComponentEditorCommandResult result = workspace.ViewModel.DeletePin("1");

        Assert.Empty(result.Diagnostics);
        Assert.Empty(workspace.ViewModel.Pins);
        Assert.Empty(Assert.Single(workspace.ViewModel.Symbols).Pins);
        Assert.Empty(workspace.ViewModel.PinPadMappings);
    }

    [Fact]
    public void AddPadCommandAddsPadAtGridSnappedPosition()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:add-pad");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("SOIC-8", []);

        ComponentEditorCommandResult result = editor.AddPad("SOIC-8", " 2 ", new CadPoint(151_000, 249_999), new CadVector(60_000, 80_000));

        Assert.Empty(result.Diagnostics);
        ComponentFootprintPad pad = Assert.Single(Assert.Single(editor.Footprints).Pads);
        Assert.Equal("2", pad.Name);
        Assert.Equal(new CadPoint(200_000, 200_000), pad.Position);
    }

    [Fact]
    public void AddSmdPadCommandAddsSurfaceMountRectangularPad()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:add-smd-pad");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("QFN-16", []);

        ComponentEditorCommandResult result = editor.AddSmdPad("QFN-16", "EP", new CadPoint(51_000, 49_999), new CadVector(1_000_000, 1_000_000));

        Assert.Empty(result.Diagnostics);
        ComponentFootprintPad pad = Assert.Single(Assert.Single(editor.Footprints).Pads);
        Assert.Equal(ComponentPadTechnology.SurfaceMount, pad.Technology);
        Assert.Equal(ComponentPadShape.Rectangle, pad.Shape);
        Assert.Null(pad.DrillSize);
        Assert.Equal(new CadPoint(100_000, 0), pad.Position);
    }

    [Fact]
    public void FootprintToolActivationExposesExplicitAuthoringTools()
    {
        ComponentEditorViewModel editor = ComponentEditorWorkspace.StartNew("dragon:footprint-tools").ViewModel;

        Assert.Equal(ComponentEditorFootprintTool.Select, editor.ActiveFootprintTool);
        Assert.Equal(
            [
                ComponentEditorFootprintTool.Select,
                ComponentEditorFootprintTool.ThroughHolePad,
                ComponentEditorFootprintTool.SmdPad,
                ComponentEditorFootprintTool.Outline,
                ComponentEditorFootprintTool.Hole,
                ComponentEditorFootprintTool.SilkscreenText,
                ComponentEditorFootprintTool.Keepout
            ],
            editor.AvailableFootprintTools);

        editor.ActivateFootprintTool(ComponentEditorFootprintTool.ThroughHolePad);

        Assert.Equal(ComponentEditorFootprintTool.ThroughHolePad, editor.ActiveFootprintTool);
    }

    [Fact]
    public void ThroughHolePadToolPreviewsAndCommitsPadWithDrillShapeAndLayerIntent()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:through-hole-pad-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("DIP-8", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.ThroughHolePad);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(151_000, 249_999));
        ComponentEditorCommandResult result = editor.PlaceThroughHolePad(
            "DIP-8",
            " 1 ",
            new CadPoint(151_000, 249_999),
            diameter: 1_300_000,
            drillSize: 700_000,
            ComponentPadShape.Oval,
            ComponentEditorFootprintLayerIntent.AllCopper);

        Assert.Equal(ComponentEditorFootprintTool.ThroughHolePad, preview.Tool);
        Assert.Equal(new CadPoint(200_000, 200_000), preview.Center);
        Assert.Empty(result.Diagnostics);
        ComponentFootprintPad pad = Assert.Single(Assert.Single(editor.Footprints).Pads);
        Assert.Equal("1", pad.Name);
        Assert.Equal(new CadPoint(200_000, 200_000), pad.Position);
        Assert.Equal(new CadVector(1_300_000, 1_300_000), pad.Size);
        Assert.Equal(700_000, pad.DrillSize);
        Assert.Equal(ComponentPadShape.Oval, pad.Shape);
        Assert.Equal(ComponentPadTechnology.ThroughHole, pad.Technology);
        ComponentEditorFootprintPadPrimitive primitive = Assert.IsType<ComponentEditorFootprintPadPrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal(ComponentEditorFootprintLayerIntent.AllCopper, primitive.LayerIntent);
    }

    [Fact]
    public void SmdPadToolPreviewsAndCommitsPadWithSizeRotationAndLayerIntent()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:smd-pad-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("QFN-16", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.SmdPad);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(51_000, 49_999));
        ComponentEditorCommandResult result = editor.PlaceSmdPad(
            "QFN-16",
            "EP",
            new CadPoint(51_000, 49_999),
            new CadVector(1_000_000, 800_000),
            rotationDegrees: 90,
            ComponentEditorFootprintLayerIntent.TopCopper);

        Assert.Equal(ComponentEditorFootprintTool.SmdPad, preview.Tool);
        Assert.Equal(new CadPoint(100_000, 0), preview.Center);
        Assert.Empty(result.Diagnostics);
        ComponentFootprintPad pad = Assert.Single(Assert.Single(editor.Footprints).Pads);
        Assert.Equal(new CadVector(1_000_000, 800_000), pad.Size);
        Assert.Equal(ComponentPadTechnology.SurfaceMount, pad.Technology);
        Assert.Equal(ComponentPadShape.Rectangle, pad.Shape);
        ComponentEditorFootprintPadPrimitive primitive = Assert.IsType<ComponentEditorFootprintPadPrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal(90, primitive.RotationDegrees);
        Assert.Equal(ComponentEditorFootprintLayerIntent.TopCopper, primitive.LayerIntent);
    }

    [Fact]
    public void OutlineToolPreviewsAndCommitsGridSnappedFootprintLine()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:outline-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("SOIC-8", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.Outline);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(51_000, 149_999), new CadPoint(251_000, 249_999));
        ComponentEditorCommandResult result = editor.PlaceFootprintOutline("SOIC-8", new CadPoint(51_000, 149_999), new CadPoint(251_000, 249_999));

        Assert.Equal(ComponentEditorFootprintTool.Outline, preview.Tool);
        Assert.Equal(new CadPoint(100_000, 100_000), preview.Start);
        Assert.Equal(new CadPoint(300_000, 200_000), preview.End);
        Assert.Empty(result.Diagnostics);
        ComponentLine line = Assert.Single(Assert.Single(editor.Footprints).Courtyard);
        Assert.Equal(new CadPoint(100_000, 100_000), line.Start);
        Assert.Equal(new CadPoint(300_000, 200_000), line.End);
        Assert.IsType<ComponentEditorFootprintLinePrimitive>(Assert.Single(editor.FootprintPrimitives));
    }

    [Fact]
    public void HoleToolPreviewsAndCommitsGridSnappedMechanicalHole()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:hole-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("Mounting", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.Hole);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(-51_000, 49_999));
        ComponentEditorCommandResult result = editor.PlaceFootprintHole("Mounting", new CadPoint(-51_000, 49_999), diameter: 3_000_000);

        Assert.Equal(ComponentEditorFootprintTool.Hole, preview.Tool);
        Assert.Equal(new CadPoint(-100_000, 0), preview.Center);
        Assert.Empty(result.Diagnostics);
        ComponentEditorFootprintHolePrimitive primitive = Assert.IsType<ComponentEditorFootprintHolePrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal("Mounting", primitive.FootprintName);
        Assert.Equal(new CadPoint(-100_000, 0), primitive.Center);
        Assert.Equal(3_000_000, primitive.Diameter);
    }

    [Fact]
    public void SilkscreenTextToolPreviewsAndCommitsGridSnappedText()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:silkscreen-text-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("SOIC-8", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.SilkscreenText);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(251_000, 151_000));
        ComponentEditorCommandResult result = editor.PlaceFootprintSilkscreenText("SOIC-8", " REF** ", new CadPoint(251_000, 151_000));

        Assert.Equal(ComponentEditorFootprintTool.SilkscreenText, preview.Tool);
        Assert.Equal(new CadPoint(300_000, 200_000), preview.Position);
        Assert.Empty(result.Diagnostics);
        ComponentEditorFootprintTextPrimitive primitive = Assert.IsType<ComponentEditorFootprintTextPrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal("REF**", primitive.Value);
        Assert.Equal(new CadPoint(300_000, 200_000), primitive.Position);
        Assert.Equal(ComponentEditorFootprintLayerIntent.TopSilkscreen, primitive.LayerIntent);
    }

    [Fact]
    public void KeepoutToolPreviewsAndCommitsGridSnappedKeepout()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:keepout-tool");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("RF", []);
        editor.ActivateFootprintTool(ComponentEditorFootprintTool.Keepout);

        ComponentEditorFootprintPlacementPreview preview = editor.PreviewFootprintPlacement(new CadPoint(49_999, -51_000), new CadPoint(349_999, -251_000));
        ComponentEditorCommandResult result = editor.PlaceFootprintKeepout(
            "RF",
            new CadPoint(49_999, -51_000),
            new CadPoint(349_999, -251_000),
            ComponentEditorFootprintLayerIntent.AllCopper);

        Assert.Equal(ComponentEditorFootprintTool.Keepout, preview.Tool);
        Assert.Equal(new CadPoint(0, -100_000), preview.Start);
        Assert.Equal(new CadPoint(300_000, -300_000), preview.End);
        Assert.Empty(result.Diagnostics);
        ComponentEditorFootprintKeepoutPrimitive primitive = Assert.IsType<ComponentEditorFootprintKeepoutPrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal(new CadPoint(0, -100_000), primitive.Start);
        Assert.Equal(new CadPoint(300_000, -300_000), primitive.End);
        Assert.Equal(ComponentEditorFootprintLayerIntent.AllCopper, primitive.LayerIntent);
    }

    [Fact]
    public void RemoveLastFootprintAuthoringItemRemovesLastCommittedPrimitive()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:remove-last-footprint-item");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddFootprint("SOIC-8", []);
        editor.PlaceFootprintOutline("SOIC-8", new CadPoint(0, 0), new CadPoint(100_000, 0));
        editor.PlaceFootprintKeepout("SOIC-8", new CadPoint(0, 100_000), new CadPoint(100_000, 100_000), ComponentEditorFootprintLayerIntent.TopCopper);

        ComponentEditorCommandResult result = editor.RemoveLastFootprintAuthoringItem();

        Assert.Empty(result.Diagnostics);
        ComponentEditorFootprintLinePrimitive primitive = Assert.IsType<ComponentEditorFootprintLinePrimitive>(Assert.Single(editor.FootprintPrimitives));
        Assert.Equal(new CadPoint(0, 0), primitive.Start);
        Assert.Equal(new CadPoint(100_000, 0), primitive.End);
        Assert.Single(Assert.Single(editor.Footprints).Courtyard);
    }

    [Fact]
    public void RenamePadCommandPreservesExistingMapping()
    {
        ComponentDefinition original = ValidComponent("dragon:rename-pad", "Rename Pad");
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(original);

        ComponentEditorCommandResult result = workspace.ViewModel.RenamePad("SOIC-8", "1", "VIN");

        Assert.Empty(result.Diagnostics);
        ComponentFootprintPad pad = Assert.Single(Assert.Single(workspace.ViewModel.Footprints).Pads);
        Assert.Equal("VIN", pad.Name);
        Assert.Single(workspace.ViewModel.PinPadMappings);
        Assert.Empty(workspace.ValidationSummary.Issues);
    }

    [Fact]
    public void MappingValidationReportsUnmappedPinsAndCommandFailuresAsDiagnostics()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:mapping-validation");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddPin("1", "IN");
        editor.AddPin("2", "OUT");
        editor.AddSymbol("Main");
        editor.AddFootprint("SOIC-8", [new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))]);
        editor.AddPackage("SOIC package", "SOIC-8");
        editor.MapPinToPad("1", "1");

        ComponentEditorCommandResult result = editor.RenamePad("SOIC-8", "missing", "VIN");

        Assert.Equal(
            [ComponentEditorCommandDiagnosticKind.NotFound],
            result.Diagnostics.Select(diagnostic => diagnostic.Kind));
        Assert.Equal(
            [ComponentEditorValidationIssueKind.UnmappedPin],
            workspace.ValidationSummary.Issues.Select(issue => issue.Kind));
        Assert.Equal("Unmapped pin 2", Assert.Single(workspace.ValidationSummary.Issues).DisplayText);
    }

    [Fact]
    public void PackageSpecificMappingTargetsRequestedPackageFootprint()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:package-mapping");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddPin("1", "VIN");
        editor.AddSymbol("Main");
        editor.AddFootprint("SOIC-8", [new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))]);
        editor.AddFootprint("DIP-8", [new ComponentEditorPadDraft("A1", new CadPoint(0, 0), new CadVector(60_000, 80_000))]);
        editor.AddPackage("Surface mount", "SOIC-8");
        editor.AddPackage("Through hole", "DIP-8");

        ComponentEditorCommandResult result = editor.MapPinToPad("Through hole", "1", "A1");

        Assert.Empty(result.Diagnostics);
        ComponentPinPadMapping mapping = Assert.Single(editor.PinPadMappings);
        Assert.Equal(Assert.Single(editor.Variants, variant => variant.Name == "Through hole").Id, mapping.VariantId);
        Assert.Equal(Assert.Single(editor.Footprints, footprint => footprint.Name == "DIP-8").Pads[0].Id, mapping.PadId);
    }

    [Fact]
    public void MappingRowsListSymbolPinsAndPackagePadsWithCurrentState()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:mapping-rows");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddPin("2", "OUT");
        editor.AddPin("1", "IN");
        editor.AddSymbol("Main");
        editor.AddFootprint(
            "SOIC-8",
            [
                new ComponentEditorPadDraft("2", new CadPoint(100_000, 0), new CadVector(60_000, 80_000)),
                new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))
            ]);
        editor.AddPackage("SOIC package", "SOIC-8");
        editor.MapPinToPad("SOIC package", "1", "1");

        Assert.Equal(
            [
                "SOIC package: 1 IN -> 1",
                "SOIC package: 2 OUT -> Unmapped"
            ],
            editor.MappingRows.Select(row => row.DisplayText));
        ComponentEditorPinPadMappingRow firstRow = editor.MappingRows[0];
        Assert.Equal("dragon:mapping-rows:variant:soic-package", firstRow.PackageId);
        Assert.Equal("SOIC package", firstRow.PackageName);
        Assert.Equal("dragon:mapping-rows:pin:1", firstRow.PinId);
        Assert.Equal("1", firstRow.PinNumber);
        Assert.Equal("IN", firstRow.PinName);
        Assert.Equal("dragon:mapping-rows:pad:1", firstRow.SelectedPadId);
        Assert.Equal("1", firstRow.SelectedPadName);
        Assert.Equal(["1", "2"], firstRow.AvailablePads.Select(pad => pad.Name));
    }

    [Fact]
    public void MappingCommandsMapUnmapAndReplacePackageMappingState()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:mapping-commands");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddPin("1", "VIN");
        editor.AddSymbol("Main");
        editor.AddFootprint(
            "SOIC-8",
            [
                new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000)),
                new ComponentEditorPadDraft("8", new CadPoint(800_000, 0), new CadVector(60_000, 80_000))
            ]);
        editor.AddPackage("SOIC package", "SOIC-8");

        Assert.Empty(editor.MapPinToPad("SOIC package", "1", "1").Diagnostics);
        Assert.Equal("1", Assert.Single(editor.MappingRows).SelectedPadName);

        Assert.Empty(editor.MapPinToPad("SOIC package", "1", "8").Diagnostics);
        Assert.Equal("8", Assert.Single(editor.MappingRows).SelectedPadName);
        Assert.Equal("SOIC package: 1 -> 8", Assert.Single(editor.MappingSummaries).DisplayText);

        Assert.Empty(editor.UnmapPinFromPad("SOIC package", "1").Diagnostics);

        Assert.Empty(editor.PinPadMappings);
        ComponentEditorPinPadMappingRow row = Assert.Single(editor.MappingRows);
        Assert.Null(row.SelectedPadId);
        Assert.Equal("Unmapped", row.SelectedPadName);
        Assert.Contains(workspace.ValidationSummary.Issues, issue => issue.Kind == ComponentEditorValidationIssueKind.MissingMapping);
    }

    [Fact]
    public void DuplicatePadMappingsProduceDeterministicValidationDiagnosticAndBlockSave()
    {
        ComponentPinId firstPinId = new("dragon:duplicate-pad:pin:1");
        ComponentPinId secondPinId = new("dragon:duplicate-pad:pin:2");
        ComponentFootprintId footprintId = new("dragon:duplicate-pad:footprint:soic-8");
        ComponentPadId padId = new("dragon:duplicate-pad:pad:1");
        ComponentVariantId variantId = new("dragon:duplicate-pad:variant:soic-8");
        ComponentDefinition invalid = new(
            new ComponentId("dragon:duplicate-pad"),
            "Duplicate Pad",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DUP-1",
            Description: "",
            Attributes: [],
            Pins:
            [
                new ComponentPin(firstPinId, "IN", "1", ComponentPinElectricalType.Input),
                new ComponentPin(secondPinId, "OUT", "2", ComponentPinElectricalType.Output)
            ],
            Gates: [],
            Symbols:
            [
                new ComponentSymbol(
                    new ComponentSymbolId("dragon:duplicate-pad:symbol:main"),
                    "Main",
                    [
                        new ComponentSymbolPin(firstPinId, new CadPoint(0, 0), ComponentPinOrientation.Right),
                        new ComponentSymbolPin(secondPinId, new CadPoint(0, 100_000), ComponentPinOrientation.Right)
                    ],
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
            PinPadMappings:
            [
                new ComponentPinPadMapping(variantId, firstPinId, padId),
                new ComponentPinPadMapping(variantId, secondPinId, padId)
            ],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartEdit(invalid);

        ComponentEditorValidationIssue issue = Assert.Single(workspace.ValidationSummary.Issues);
        Assert.Equal(ComponentEditorValidationIssueKind.DuplicatePadMapping, issue.Kind);
        Assert.Equal("Duplicate pad mapping SOIC package pad 1", issue.DisplayText);
        Assert.Equal(ComponentEditorSaveReadiness.BlockedByValidation, workspace.SaveReadiness.State);
    }

    [Fact]
    public void MappingMetadataIsDeterministicForSaveReviewFlows()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("dragon:mapping-metadata");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.AddPin("2", "OUT");
        editor.AddPin("1", "IN");
        editor.AddSymbol("Main");
        editor.AddFootprint(
            "SOIC-8",
            [
                new ComponentEditorPadDraft("2", new CadPoint(100_000, 0), new CadVector(60_000, 80_000)),
                new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))
            ]);
        editor.AddPackage("SOIC package", "SOIC-8");
        editor.MapPinToPad("SOIC package", "2", "2");
        editor.MapPinToPad("SOIC package", "1", "1");

        ComponentEditorMappingMetadata metadata = editor.MappingMetadata;

        Assert.Equal(2, metadata.MappedPinCount);
        Assert.Equal(2, metadata.TotalRequiredPinCount);
        Assert.Equal("2/2 pins mapped", metadata.DisplayText);
        Assert.Equal(
            [
                "SOIC package|1|1",
                "SOIC package|2|2"
            ],
            metadata.ReviewLines);
        Assert.True(workspace.DirtyState.HasUnsavedChanges);
        Assert.Equal(ComponentEditorSaveReadiness.Ready, workspace.SaveReadiness.State);
    }

    [Fact]
    public void SaveReloadDraftPreservesDraftIdentityWithoutCreatingTrustedLibraryEntry()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("draft:dragon:fx555");
        ComponentEditorViewModel editor = workspace.ViewModel;
        editor.SetDisplayName("FX555 Timer Draft");
        editor.AddPin("1", "GND");
        editor.AddSymbol("Timer Symbol");
        editor.AddFootprint("SOIC-8", [new ComponentEditorPadDraft("1", new CadPoint(0, 0), new CadVector(60_000, 80_000))]);
        editor.AddPackage("SOIC-8", "SOIC-8");
        editor.MapPinToPad("SOIC-8", "1", "1");

        string savedDraftJson = workspace.SaveDraftJson();
        ComponentEditorWorkspace reloaded = ComponentEditorWorkspace.ReloadDraftJson(savedDraftJson);

        Assert.Equal(ComponentEditorSessionKind.Draft, reloaded.SessionKind);
        Assert.False(reloaded.IsTrustedLibraryEntry);
        Assert.Equal("draft:dragon:fx555", reloaded.ViewModel.ComponentId);
        Assert.Equal("FX555 Timer Draft", reloaded.ViewModel.DisplayName);
        Assert.Equal(
            ComponentDraftSerializer.Serialize(workspace.ViewModel.ToDraft()),
            ComponentDraftSerializer.Serialize(reloaded.ViewModel.ToDraft()));
    }

    [Fact]
    public void ValidationReportsMissingPinsDuplicatePinNamesAndIncompleteDeviceMappings()
    {
        ComponentPinId missingPinId = new("dragon:invalid:pin:missing");
        ComponentFootprintId footprintId = new("dragon:invalid:footprint:soic-8");
        ComponentPadId padId = new("dragon:invalid:pad:1");
        ComponentDefinition invalid = new(
            new ComponentId("dragon:invalid"),
            "Invalid Component",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "INV-1",
            Description: "",
            Attributes: [],
            Pins:
            [
                new ComponentPin(new ComponentPinId("dragon:invalid:pin:1"), "IO", "1", ComponentPinElectricalType.Bidirectional),
                new ComponentPin(new ComponentPinId("dragon:invalid:pin:2"), "IO", "2", ComponentPinElectricalType.Bidirectional)
            ],
            Gates: [],
            Symbols:
            [
                new ComponentSymbol(
                    new ComponentSymbolId("dragon:invalid:symbol:main"),
                    "Main",
                    [new ComponentSymbolPin(missingPinId, new CadPoint(0, 0), ComponentPinOrientation.Right)],
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
            Variants: [new ComponentVariant(new ComponentVariantId("dragon:invalid:variant:soic-8"), "SOIC-8", footprintId, [])],
            PinPadMappings: [new ComponentPinPadMapping(new ComponentVariantId("dragon:invalid:variant:soic-8"), new ComponentPinId("dragon:invalid:pin:1"), new ComponentPadId("dragon:invalid:pad:missing"))],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

        ComponentEditorValidationSummary summary = ComponentEditorValidationSummary.FromDefinition(invalid);

        Assert.Equal(
            [
                ComponentEditorValidationIssueKind.MissingPin,
                ComponentEditorValidationIssueKind.DuplicatePinName,
                ComponentEditorValidationIssueKind.IncompleteMapping,
                ComponentEditorValidationIssueKind.UnmappedPin
            ],
            summary.Issues.Select(issue => issue.Kind));
        Assert.Contains(summary.Issues, issue => issue.DisplayText == "Symbol references missing pin dragon:invalid:pin:missing");
        Assert.Contains(summary.Issues, issue => issue.DisplayText == "Duplicate pin name IO");
        Assert.Contains(summary.Issues, issue => issue.DisplayText == "Mapping references missing pad dragon:invalid:pad:missing");
    }

    [Fact]
    public void DraftWorkspaceBlocksTrustedPromotion()
    {
        ComponentEditorWorkspace workspace = ComponentEditorWorkspace.StartNew("draft:promotion-block");
        workspace.ViewModel.AddBasicPinPackageAndMapping("1", "VIN", "SOT-223");

        ComponentEditorTrustedPromotionReadiness readiness = workspace.TrustedPromotionReadiness;

        Assert.False(readiness.CanPromote);
        Assert.Equal("Blocked: component editor drafts must be saved as drafts before trusted-library promotion.", readiness.Message);
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
