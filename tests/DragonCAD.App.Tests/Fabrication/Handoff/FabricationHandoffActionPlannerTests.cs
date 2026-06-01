using DragonCAD.App.Fabrication.Handoff;

namespace DragonCAD.App.Tests.Fabrication.Handoff;

public sealed class FabricationHandoffActionPlannerTests
{
    [Fact]
    public void PlanReadyOshParkPackage_ProducesSafeUploadAction()
    {
        FabricationHandoffPackageOption option = FabricationHandoffPackageOption.CreateUploadPage(
            "osh-park",
            "OSH Park",
            "Prototype board package",
            "https://oshpark.com/",
            [
                FabricationHandoffPackageFile.Present("Gerbers", "manufacturing/gerbers.zip"),
                FabricationHandoffPackageFile.Present("Drill files", "manufacturing/drill.zip")
            ]);

        FabricationHandoffActionPlan plan = FabricationHandoffActionPlanner.Plan(option);

        Assert.True(plan.IsReady);
        Assert.Empty(plan.Diagnostics);
        Assert.NotNull(plan.Action);
        Assert.Equal(FabricationHandoffActionKind.OpenUploadPage, plan.Action.Kind);
        Assert.Equal("Open OSH Park upload page", plan.Action.Label);
        Assert.Equal("https://oshpark.com/", plan.Action.Target);
        Assert.Equal(
            """
            Provider: OSH Park (osh-park)
            Package: Prototype board package
            Status: Ready
            Action: Open OSH Park upload page -> https://oshpark.com/
            Diagnostics: none
            Files:
            - Drill files: manufacturing/drill.zip
            - Gerbers: manufacturing/gerbers.zip
            """.ReplaceLineEndings("\r\n"),
            plan.Summary);
    }

    [Fact]
    public void PlanPcbCartPackage_BlocksWhenBomAndGerbersAreMissing()
    {
        FabricationHandoffPackageOption option = FabricationHandoffPackageOption.CreateQuotePage(
            "pcbcart",
            "PCBCart",
            "Production assembly package",
            "https://www.pcbcart.com/quote",
            [
                FabricationHandoffPackageFile.Missing("Gerbers"),
                FabricationHandoffPackageFile.Present("Drill files", "manufacturing/drill.zip"),
                FabricationHandoffPackageFile.Missing("BOM"),
                FabricationHandoffPackageFile.Present("Pick and place", "manufacturing/pick-place.csv")
            ]);

        FabricationHandoffActionPlan plan = FabricationHandoffActionPlanner.Plan(option);

        Assert.False(plan.IsReady);
        Assert.Null(plan.Action);
        Assert.Equal(
            [
                "Missing BOM for PCBCart production assembly package.",
                "Missing Gerbers for PCBCart production assembly package."
            ],
            plan.Diagnostics);
    }

    [Fact]
    public void PlanExportPackage_UsesDeterministicExportLabel()
    {
        FabricationHandoffPackageOption option = FabricationHandoffPackageOption.CreateExportPackage(
            "pcbcart",
            "PCBCart",
            "Production assembly package",
            "manufacturing/pcbcart-package.zip",
            [
                FabricationHandoffPackageFile.Present("BOM", "manufacturing/bom.csv"),
                FabricationHandoffPackageFile.Present("Gerbers", "manufacturing/gerbers.zip")
            ]);

        FabricationHandoffActionPlan plan = FabricationHandoffActionPlanner.Plan(option);

        Assert.True(plan.IsReady);
        Assert.NotNull(plan.Action);
        Assert.Equal(FabricationHandoffActionKind.ExportPackage, plan.Action.Kind);
        Assert.Equal("Export PCBCart package", plan.Action.Label);
        Assert.Equal("manufacturing/pcbcart-package.zip", plan.Action.Target);
    }

    [Fact]
    public void PlanMissingPackage_ReturnsDiagnosticsAndNoAction()
    {
        FabricationHandoffPackageOption option = FabricationHandoffPackageOption.CreateExportPackage(
            "osh-park",
            "OSH Park",
            "Prototype board package",
            "manufacturing/osh-park-package.zip",
            [
                FabricationHandoffPackageFile.Missing("Drill files"),
                FabricationHandoffPackageFile.Missing("Gerbers")
            ]);

        FabricationHandoffActionPlan plan = FabricationHandoffActionPlanner.Plan(option);

        Assert.False(plan.IsReady);
        Assert.Null(plan.Action);
        Assert.Equal(
            [
                "Missing Drill files for OSH Park prototype board package.",
                "Missing Gerbers for OSH Park prototype board package."
            ],
            plan.Diagnostics);
        Assert.Contains("Action: blocked", plan.Summary, StringComparison.Ordinal);
    }
}
