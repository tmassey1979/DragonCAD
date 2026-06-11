using DragonCAD.App.Shell;

namespace DragonCAD.App.Tests.Shell;

public sealed class DockLayoutPresetCatalogTests
{
    [Fact]
    public void CatalogExposesVisualStudioLikeWorkflowPresets()
    {
        DockLayoutPresetCatalog catalog = DockLayoutPresetCatalog.CreateDefault();

        Assert.Equal(
            [
                "Schematic Focus",
                "PCB Focus",
                "Component Authoring",
                "Marketplace",
                "Manufacturing Review"
            ],
            catalog.Presets.Select(preset => preset.Name).ToArray());
    }

    [Theory]
    [InlineData("Schematic Focus", "Schematic")]
    [InlineData("PCB Focus", "PcbLayout")]
    [InlineData("Component Authoring", "ComponentEditor")]
    [InlineData("Marketplace", "Marketplace")]
    [InlineData("Manufacturing Review", "Fabrication")]
    public void PresetsDescribeDocumentsPanelsAndActiveWorkspace(string presetName, string expectedWorkspace)
    {
        DockLayoutPreset preset = DockLayoutPresetCatalog.CreateDefault().GetRequired(presetName);

        Assert.Equal(expectedWorkspace, preset.ActiveWorkspaceTab);
        Assert.NotEmpty(preset.DocumentTabs);
        Assert.Contains(preset.DocumentTabs, tab => tab.WorkspaceTab == expectedWorkspace && tab.IsActive);
        Assert.NotEmpty(preset.SidePanels);
        Assert.NotEmpty(preset.BottomPanels);
        Assert.All(preset.SidePanels, panel => Assert.False(string.IsNullOrWhiteSpace(panel.Placement)));
        Assert.All(preset.BottomPanels, panel => Assert.Equal("Bottom", panel.Placement));
    }

    [Fact]
    public void ApplyingPresetIsDeterministicAndPreservesOpenDocumentIdentity()
    {
        DockLayoutPresetCatalog catalog = DockLayoutPresetCatalog.CreateDefault();
        ShellDockLayoutState state = ShellDockLayoutState.Create(
            activeWorkspaceTab: "Marketplace",
            openDocuments:
            [
                new ShellOpenDocument("schematic:main", "Main schematic", "Schematic"),
                new ShellOpenDocument("board:main", "Main board", "PcbLayout"),
                new ShellOpenDocument("datasheet:lm7805", "LM7805 datasheet", "Datasheets")
            ]);

        ShellDockLayoutApplyResult first = catalog.TryApply("PCB Focus", state);
        ShellDockLayoutApplyResult second = catalog.TryApply("PCB Focus", first.State);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.State, second.State);
        Assert.Equal(
            ["schematic:main", "board:main", "datasheet:lm7805"],
            first.State.OpenDocuments.Select(document => document.Id).ToArray());
        Assert.Equal("PcbLayout", first.State.ActiveWorkspaceTab);
        Assert.Equal("PCB Focus", first.State.SelectedPresetName);
    }

    [Fact]
    public void UnknownPresetReturnsValidationDiagnosticWithoutChangingState()
    {
        ShellDockLayoutState state = ShellDockLayoutState.Create(
            activeWorkspaceTab: "Schematic",
            openDocuments: [new ShellOpenDocument("schematic:main", "Main schematic", "Schematic")]);

        ShellDockLayoutApplyResult result = DockLayoutPresetCatalog.CreateDefault().TryApply("Unknown", state);

        Assert.False(result.Succeeded);
        Assert.Equal(state, result.State);
        ShellDockLayoutDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("UnknownPreset", diagnostic.Code);
        Assert.Contains("Unknown", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StorePersistsSelectedPresetAndPlacementMetadata()
    {
        string root = Path.Combine(Path.GetTempPath(), "DragonCAD.ShellLayout.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            ShellDockLayoutState state = DockLayoutPresetCatalog.CreateDefault()
                .TryApply(
                    "Manufacturing Review",
                    ShellDockLayoutState.Create(
                        activeWorkspaceTab: "Schematic",
                        openDocuments: [new ShellOpenDocument("board:main", "Main board", "PcbLayout")]))
                .State;
            ShellDockLayoutStateStore store = new();

            store.Save(root, state);
            ShellDockLayoutState? loaded = store.Load(root);

            Assert.Equal(state, loaded);
            Assert.Equal("Manufacturing Review", loaded?.SelectedPresetName);
            Assert.Contains(loaded!.SidePanels, panel => panel.Id == "layers" && panel.Placement == "Right");
            Assert.Contains(loaded.BottomPanels, panel => panel.Id == "output" && panel.Placement == "Bottom");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
