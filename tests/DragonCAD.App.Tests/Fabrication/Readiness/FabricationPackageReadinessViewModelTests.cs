using DragonCAD.App.Fabrication.Handoff;
using DragonCAD.App.Fabrication.Readiness;

namespace DragonCAD.App.Tests.Fabrication.Readiness;

public sealed class FabricationPackageReadinessViewModelTests
{
    [Fact]
    public void MarkGerberReady_UpdatesFileRowAndKeepsRemainingDiagnostics()
    {
        FabricationPackageReadinessViewModel viewModel = CreatePcbCartReview();

        viewModel.MarkFileReady("Gerbers", "manufacturing/gerbers.zip");

        FabricationFileReadinessRow gerbers = viewModel.Files.Single(file => file.DisplayName == "Gerbers");
        Assert.True(gerbers.IsReady);
        Assert.Equal("Ready", gerbers.StatusLabel);
        Assert.Equal("manufacturing/gerbers.zip", gerbers.RelativePath);
        Assert.DoesNotContain(viewModel.Diagnostics, diagnostic => diagnostic.Contains("Gerbers", StringComparison.Ordinal));
        Assert.Contains(viewModel.Diagnostics, diagnostic => diagnostic.Contains("BOM", StringComparison.Ordinal));
    }

    [Fact]
    public void MarkBomMissing_BlocksReadyPackageAndRegeneratesActionPlan()
    {
        FabricationPackageReadinessViewModel viewModel = CreateReadyPcbCartReview();

        viewModel.MarkFileMissing("BOM");

        FabricationFileReadinessRow bom = viewModel.Files.Single(file => file.DisplayName == "BOM");
        Assert.False(bom.IsReady);
        Assert.Equal("Missing", bom.StatusLabel);
        Assert.False(viewModel.IsReady);
        Assert.Null(viewModel.ActionPlan.Action);
        Assert.Contains("Missing BOM for PCBCart production assembly package.", viewModel.Diagnostics);
    }

    [Fact]
    public void ReadinessStatusUpdatesWhenFilesAreToggled()
    {
        FabricationPackageReadinessViewModel viewModel = CreatePcbCartReview();

        Assert.False(viewModel.IsReady);
        Assert.Equal("2 required files missing", viewModel.StatusText);

        viewModel.MarkFileReady("Gerbers", "manufacturing/gerbers.zip");

        Assert.False(viewModel.IsReady);
        Assert.Equal("1 required file missing", viewModel.StatusText);

        viewModel.MarkFileReady("BOM", "manufacturing/bom.csv");

        Assert.True(viewModel.IsReady);
        Assert.Equal("Ready for handoff", viewModel.StatusText);
    }

    [Fact]
    public void DiagnosticsUpdateWhenReadyFileBecomesMissing()
    {
        FabricationPackageReadinessViewModel viewModel = CreateReadyPcbCartReview();

        viewModel.MarkFileMissing("Pick and place");

        Assert.False(viewModel.IsReady);
        Assert.Equal(["Missing Pick and place for PCBCart production assembly package."], viewModel.Diagnostics);

        viewModel.MarkFileReady("Pick and place", "manufacturing/pick-place.csv");

        Assert.True(viewModel.IsReady);
        Assert.Empty(viewModel.Diagnostics);
    }

    [Fact]
    public void ActionPlanBecomesReadyOnlyWhenAllRequiredFilesAreReady()
    {
        FabricationPackageReadinessViewModel viewModel = CreatePcbCartReview();

        Assert.False(viewModel.ActionPlan.IsReady);
        Assert.Null(viewModel.ActionPlan.Action);

        viewModel.MarkFileReady("Gerbers", "manufacturing/gerbers.zip");

        Assert.False(viewModel.ActionPlan.IsReady);
        Assert.Null(viewModel.ActionPlan.Action);

        viewModel.MarkFileReady("BOM", "manufacturing/bom.csv");

        Assert.True(viewModel.ActionPlan.IsReady);
        Assert.NotNull(viewModel.ActionPlan.Action);
        Assert.Equal("Open PCBCart quote page", viewModel.ActionPlan.Action.Label);
    }

    private static FabricationPackageReadinessViewModel CreatePcbCartReview() =>
        FabricationPackageReadinessViewModel.FromPackageOption(
            FabricationHandoffPackageOption.CreateQuotePage(
                "pcbcart",
                "PCBCart",
                "Production assembly package",
                "https://www.pcbcart.com/quote",
                [
                    FabricationHandoffPackageFile.Missing("Gerbers"),
                    FabricationHandoffPackageFile.Present("Drill files", "manufacturing/drill.zip"),
                    FabricationHandoffPackageFile.Missing("BOM"),
                    FabricationHandoffPackageFile.Present("Pick and place", "manufacturing/pick-place.csv")
                ]));

    private static FabricationPackageReadinessViewModel CreateReadyPcbCartReview() =>
        FabricationPackageReadinessViewModel.FromPackageOption(
            FabricationHandoffPackageOption.CreateQuotePage(
                "pcbcart",
                "PCBCart",
                "Production assembly package",
                "https://www.pcbcart.com/quote",
                [
                    FabricationHandoffPackageFile.Present("Gerbers", "manufacturing/gerbers.zip"),
                    FabricationHandoffPackageFile.Present("Drill files", "manufacturing/drill.zip"),
                    FabricationHandoffPackageFile.Present("BOM", "manufacturing/bom.csv"),
                    FabricationHandoffPackageFile.Present("Pick and place", "manufacturing/pick-place.csv")
                ]));
}
