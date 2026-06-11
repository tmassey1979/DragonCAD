using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;
using DragonCAD.Core.Projects;

namespace DragonCAD.Core.Tests.Projects;

public sealed class DragonProjectIdentityPersistenceTests
{
    [Fact]
    public void SaveAndLoadPreservesEditorIdentityState()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        DragonProject project = CreateProjectWithEditorState();

        store.Save(temp.Path, project);
        DragonProjectLoadResult result = store.Load(temp.Path);

        DragonProject loaded = Assert.IsType<DragonProject>(result.Project);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("schematic-component:u1", loaded.Schematic.Components.Single().IdentityId);
        Assert.Equal("wire:vcc-u1", loaded.Schematic.Wires.Single().WireId);
        Assert.Equal("VCC", loaded.Schematic.Wires.Single().NetName);
        Assert.Equal("label:vcc", loaded.Schematic.NetLabels.Single().LabelId);
        Assert.Equal("board-component:u1", loaded.Board.Placements.Single().SyncId);
        Assert.Equal("package:tqfp-32", loaded.Board.Placements.Single().SelectedPackageId);
        Assert.Equal("trace:vcc", loaded.Board.Traces.Single().TraceId);
        Assert.Equal("VCC", loaded.Board.Traces.Single().NetName);
        Assert.Equal("via:vcc-1", loaded.Board.Vias.Single().ViaId);
        Assert.Equal("VCC", loaded.Board.Airwires.Single().NetName);
        ComponentDraft draft = loaded.ProjectComponentDrafts.Single();
        Assert.Equal("draft:timer", draft.Id.Value);
        Assert.Equal("package:tqfp-32", draft.Footprints.Single().Id.Value);
        Assert.Equal("fabrication/identity-gerbers.zip", loaded.FabricationMetadata.Outputs.Single().Path);
        Assert.Equal("package:tqfp-32", loaded.FabricationMetadata.Attributes.Single(attribute => attribute.Name == "selected-package-id").Value);
        Assert.Equal("ready-for-fabrication", loaded.FabricationMetadata.Attributes.Single(attribute => attribute.Name == "review-state").Value);
    }

    [Fact]
    public void LoadReportsIgnoredTransientUiStateWithoutBlockingProject()
    {
        using TempProjectDirectory temp = TempProjectDirectory.Create();
        DragonProjectFolderStore store = new();
        store.Save(temp.Path, CreateProjectWithEditorState());
        string transientStatePath = Path.Combine(temp.Path, "ui", "transient-state.json");
        Directory.CreateDirectory(Path.GetDirectoryName(transientStatePath)!);
        File.WriteAllText(transientStatePath, """{"selectedTool":"route","zoom":1.5}""");

        DragonProjectLoadResult result = store.Load(temp.Path);

        Assert.NotNull(result.Project);
        DragonProjectDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DragonProjectDiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal("ProjectTransientUiStateIgnored", diagnostic.Code);
        Assert.Contains("ui/transient-state.json", diagnostic.Message, StringComparison.Ordinal);
    }

    private static DragonProject CreateProjectWithEditorState() =>
        new(
            new DragonProjectManifest("Identity", new Version(1, 0), "dragoncad"),
            new DragonSchematicDocument(
                "schematic:main",
                components:
                [
                    new DragonSchematicComponent("schematic-component:u1", "U1", "component:timer")
                ],
                nets:
                [
                    new DragonSchematicNet("VCC", ["U1.8"])
                ],
                wires:
                [
                    new DragonSchematicWire(
                        "wire:vcc-u1",
                        "schematic-component:u1",
                        "U1",
                        "8",
                        "schematic-component:j1",
                        "J1",
                        "1",
                        [new CadPoint(0, 0), new CadPoint(2_540_000, 0)],
                        "VCC")
                ],
                netLabels:
                [
                    new DragonSchematicNetLabel("label:vcc", "VCC", new CadPoint(1_270_000, -1_270_000), "wire:vcc-u1")
                ]),
            new DragonBoardDocument(
                "board:main",
                placements:
                [
                    new DragonBoardPlacement(
                        "schematic-component:u1",
                        "U1",
                        "TQFP-32",
                        12.5m,
                        3.75m,
                        90m,
                        "board-component:u1",
                        "package:tqfp-32")
                ],
                traces:
                [
                    new DragonBoardTrace("VCC", "Top", 0.2m, "trace:vcc")
                ],
                vias:
                [
                    new DragonBoardVia("via:vcc-1", "VCC", 10m, 20m, "Top", "Bottom", 0.8m, 0.35m)
                ],
                airwires:
                [
                    new DragonBoardAirwire("VCC", "board-component:u1", "U1", "8", "board-component:j1", "J1", "1")
                ]),
            new DragonLibraryReferences([]),
            new DragonDatasheetIntake([]),
            new DragonFabricationMetadata(
                outputs:
                [
                    new DragonFabricationOutput("gerber", "fabrication/identity-gerbers.zip", "sha256:gerbers")
                ],
                attributes:
                [
                    new DragonFabricationAttribute("selected-package-id", "package:tqfp-32"),
                    new DragonFabricationAttribute("review-state", "ready-for-fabrication")
                ]),
            ProjectComponents: [Component("component:timer", "Timer")],
            ProjectComponentDrafts: [ComponentDraft()]);

    private static ComponentDefinition Component(string id, string displayName) =>
        new(
            new ComponentId(id),
            displayName,
            ComponentKind.Custom,
            Manufacturer: "",
            ManufacturerPartNumber: "",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints: [],
            Variants: [],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

    private static ComponentDraft ComponentDraft() =>
        new(
            new ComponentId("draft:timer"),
            "Timer Draft",
            new ComponentDraftPackage("TQFP-32", "U", [new ComponentDraftAttribute("selected-package-id", "package:tqfp-32")]),
            [new ComponentDraftAttribute("review-state", "draft")],
            [new ComponentDraftPin(new ComponentPinId("pin:vcc"), "VCC", "8", ComponentDraftPinElectricalType.Power)],
            [
                new ComponentDraftSymbol(
                    new ComponentSymbolId("symbol:main"),
                    "Main",
                    [new ComponentDraftSymbolPin(new ComponentPinId("pin:vcc"), new CadPoint(0, 0), new CadPoint(1_270_000, 0), ComponentDraftPinOrientation.Right)],
                    [new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Line, new CadPoint(0, 0), new CadPoint(1_270_000, 0))])
            ],
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("package:tqfp-32"),
                    "TQFP-32",
                    [new ComponentDraftPad(new ComponentPadId("pad:8"), "8", new CadPoint(0, 0), new CadVector(300_000, 1_000_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle)],
                    [],
                    [])
            ],
            [new ComponentDraftDeviceMapping(new ComponentPinId("pin:vcc"), new ComponentFootprintId("package:tqfp-32"), new ComponentPadId("pad:8"))]);

    private sealed class TempProjectDirectory : IDisposable
    {
        private TempProjectDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempProjectDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DragonCAD.ProjectIdentity.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempProjectDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
