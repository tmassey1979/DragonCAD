using DragonCAD.App.Fabrication;

namespace DragonCAD.App.Tests.Fabrication;

public sealed class FabricationHandoffViewModelTests
{
    [Fact]
    public void ProviderFilterOptionsExposePrototypeAndProductionVendors()
    {
        FabricationHandoffViewModel viewModel = FabricationHandoffViewModel.CreateSample();

        Assert.Equal(["All", "OSH Park", "PCBCart"], viewModel.ProviderFilterOptions);
    }

    [Fact]
    public void SelectedProviderFilterNarrowsOptionsByProvider()
    {
        FabricationHandoffViewModel viewModel = FabricationHandoffViewModel.CreateSample();

        viewModel.SelectedProviderFilter = "PCBCart";

        FabricationHandoffOptionViewModel option = Assert.Single(viewModel.Options);
        Assert.Equal("PCBCart", option.ProviderName);
        Assert.Equal("Production / assembly", option.OrderKindLabel);
    }

    [Fact]
    public void ReadyPackageShowsRequiredFilesAndEnabledHandoff()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "OSH Park");

        Assert.True(option.IsReady);
        Assert.True(option.CanStartHandoff);
        Assert.Equal("Ready for handoff", option.StatusText);
        Assert.Equal("Upload prototype package", option.HandoffLabel);
        Assert.All(option.RequiredFiles, file => Assert.True(file.IsReady));
        Assert.Contains(option.RequiredFiles, file => file.DisplayName == "Gerbers" && file.RelativePath == "manufacturing/gerbers.zip");
        Assert.Contains(option.RequiredFiles, file => file.DisplayName == "Drill files" && file.RelativePath == "manufacturing/drill.zip");
    }

    [Fact]
    public void MissingGerberAndBomDiagnosticsAreSurfaced()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "PCBCart");

        Assert.False(option.IsReady);
        Assert.False(option.CanStartHandoff);
        Assert.Equal("2 required files missing", option.StatusText);
        Assert.Equal("Open production quote", option.HandoffLabel);
        Assert.Contains(option.Diagnostics, diagnostic => diagnostic.Contains("Missing Gerbers", StringComparison.Ordinal));
        Assert.Contains(option.Diagnostics, diagnostic => diagnostic.Contains("Missing BOM", StringComparison.Ordinal));
    }

    [Fact]
    public void RequiredFileRowsExposeReadyAndMissingLabels()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "PCBCart");

        FabricationRequiredFileViewModel gerbers = option.RequiredFiles.Single(file => file.DisplayName == "Gerbers");
        FabricationRequiredFileViewModel drill = option.RequiredFiles.Single(file => file.DisplayName == "Drill files");
        FabricationRequiredFileViewModel bom = option.RequiredFiles.Single(file => file.DisplayName == "BOM");

        Assert.Equal("Missing", gerbers.StatusLabel);
        Assert.Equal("", gerbers.RelativePath);
        Assert.Equal("Ready", drill.StatusLabel);
        Assert.Equal("manufacturing/drill.zip", drill.RelativePath);
        Assert.Equal("Missing", bom.StatusLabel);
    }
}
