using DragonCAD.App.Fabrication;
using DragonCAD.App.Fabrication.Export;
using DragonCAD.App.Fabrication.Handoff;

namespace DragonCAD.App.Tests.Fabrication.Export;

public sealed class FabricationChecklistExportPreviewTests
{
    [Fact]
    public void ReadyOshParkPackagePreviewContainsActionAndReadyFiles()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "OSH Park");
        FabricationHandoffActionPlan plan = FabricationChecklistExportPreview.CreateActionPlan(option);

        FabricationChecklistPreview preview = FabricationChecklistExportPreview.FromOption(option, plan);

        Assert.Equal("OSH Park", preview.ProviderName);
        Assert.Equal("Ready", preview.Status);
        Assert.Equal("Open OSH Park upload page", preview.ActionLabel);
        Assert.Contains(preview.Rows, row => row.FileName == "Drill files" && row.Status == "Ready");
        Assert.Contains(preview.Rows, row => row.FileName == "Gerbers" && row.Status == "Ready");
        Assert.Equal("OSH Park,Prototype board,Ready,Open OSH Park upload page", preview.CsvLines[1]);
    }

    [Fact]
    public void BlockedPcbCartPreviewIncludesDiagnostics()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "PCBCart");
        FabricationHandoffActionPlan plan = FabricationChecklistExportPreview.CreateActionPlan(option);

        FabricationChecklistPreview preview = FabricationChecklistExportPreview.FromOption(option, plan);

        Assert.Equal("Blocked", preview.Status);
        Assert.Equal("Blocked", preview.ActionLabel);
        Assert.Contains(preview.Diagnostics, diagnostic => diagnostic.Contains("Missing BOM", StringComparison.Ordinal));
        Assert.Contains(preview.Diagnostics, diagnostic => diagnostic.Contains("Missing Gerbers", StringComparison.Ordinal));
        Assert.Contains(preview.Rows, row => row.FileName == "BOM" && row.Status == "Missing");
    }

    [Fact]
    public void RowsAreSortedDeterministically()
    {
        FabricationHandoffOptionViewModel option = FabricationHandoffViewModel.CreateSample()
            .Options
            .Single(candidate => candidate.ProviderName == "PCBCart");

        FabricationChecklistPreview preview = FabricationChecklistExportPreview.FromOption(
            option,
            FabricationChecklistExportPreview.CreateActionPlan(option));

        Assert.Equal(["BOM", "Drill files", "Gerbers", "Pick and place"], preview.Rows.Select(row => row.FileName).ToArray());
    }
}
