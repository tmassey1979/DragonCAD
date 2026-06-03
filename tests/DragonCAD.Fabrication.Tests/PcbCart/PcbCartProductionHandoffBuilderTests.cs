using DragonCAD.Fabrication.Outputs;
using DragonCAD.Fabrication.PcbCart;

namespace DragonCAD.Fabrication.Tests.PcbCart;

public sealed class PcbCartProductionHandoffBuilderTests
{
    [Fact]
    public void BuildQuotePackage_CreatesBareBoardProductionHandoff()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            BareBoardRequest());

        Assert.True(package.IsReadyForQuote);
        Assert.Empty(package.Diagnostics);
        Assert.Equal("quote-order-handoff", package.ProviderCapabilities.HandoffMode);
        Assert.False(package.ProviderCapabilities.AllowsAutomaticProductionSubmission);
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "Gerber" && artifact.RelativePath == "gerbers/top.gbr");
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "Drill" && artifact.RelativePath == "drill/project.drl");
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "Board stackup summary" && artifact.ReviewText.Contains("4 layers", StringComparison.Ordinal));
        Assert.Contains("Finish: Enig", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("Quantity: 25", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("AssemblySide: None", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("Diagnostics: none", package.ReviewSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuotePackage_UsesFormalApiMetadataOnlyWhenConfigured()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            BareBoardRequest(),
            isFormalApiConfigured: true);

        Assert.Equal("formal-api-quote-order", package.ProviderCapabilities.HandoffMode);
        Assert.True(package.ProviderCapabilities.IsFormalApiConfigured);
        Assert.False(package.ProviderCapabilities.AllowsAutomaticProductionSubmission);
        Assert.Contains("HandoffMode: formal-api-quote-order", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("AutomaticProductionSubmission: disabled", package.ReviewSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuotePackage_CreatesAssemblyProductionHandoff()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            AssemblyRequest(
            [
                PcbCartBomItem.Create("C1", 1, "KEMET-C0402C104K4RACTU"),
                PcbCartBomItem.Create("U1", 1, "STM32F042K6T6")
            ],
            [
                PcbCartPlacement.Create("C1", PcbCartAssemblySide.Top),
                PcbCartPlacement.Create("U1", PcbCartAssemblySide.Top)
            ]));

        Assert.True(package.IsReadyForQuote);
        Assert.Empty(package.Diagnostics);
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "BillOfMaterials" && artifact.RelativePath == "bom/project.csv");
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "PickAndPlace" && artifact.RelativePath == "assembly/project-pnp.csv");
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "Assembly side" && artifact.ReviewText == "AssemblySide: Top");
        Assert.Contains("Notes: Quote board fabrication and top-side SMT assembly.", package.ReviewSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuotePackage_ReportsMissingBomAndManufacturerPartNumbers()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            AssemblyRequest(
            [
                PcbCartBomItem.Create("C1", 1, null)
            ],
            [
                PcbCartPlacement.Create("C1", PcbCartAssemblySide.Top)
            ],
            includeBomFile: false));

        Assert.False(package.IsReadyForQuote);
        Assert.Equal(
            [
                "missing-bom",
                "missing-manufacturer-part-number"
            ],
            package.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Contains("Assembly quote package is missing a bill of materials.", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("BOM item C1 is missing a manufacturer part number.", package.ReviewSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuotePackage_ReportsMissingPickAndPlaceAndPlacementData()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            AssemblyRequest(
            [
                PcbCartBomItem.Create("C1", 1, "KEMET-C0402C104K4RACTU")
            ],
            [],
            includePickAndPlaceFile: false));

        Assert.False(package.IsReadyForQuote);
        Assert.Equal(
            [
                "missing-pick-and-place",
                "missing-placement-data"
            ],
            package.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Contains("Assembly quote package is missing pick-and-place placement data.", package.ReviewSummary, StringComparison.Ordinal);
        Assert.Contains("BOM item C1 is missing placement data.", package.ReviewSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildQuotePackage_ReportsMissingBoardStackup()
    {
        PcbCartProductionHandoffPackage package = PcbCartProductionHandoffBuilder.BuildQuotePackage(
            BareBoardRequest(includeStackup: false));

        Assert.False(package.IsReadyForQuote);
        PcbCartDiagnostic diagnostic = Assert.Single(package.Diagnostics);
        Assert.Equal(PcbCartDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("missing-board-stackup", diagnostic.Code);
        Assert.Contains(package.Artifacts, artifact => artifact.Name == "Board stackup summary" && artifact.ReviewText == "Stackup: missing");
        Assert.Contains("Board stackup summary is required for a PCBCart production quote.", package.ReviewSummary, StringComparison.Ordinal);
    }

    private static PcbCartProductionHandoffRequest BareBoardRequest(bool includeStackup = true)
    {
        return PcbCartProductionHandoffRequest.Create(
            ManufacturingOutputManifest.Create(
            [
                Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr"),
                Entry(ManufacturingFileRole.Drill, "drill/project.drl")
            ]),
            quantity: 25,
            finish: PcbCartBoardFinish.Enig,
            stackup: includeStackup ? Stackup() : null,
            assemblySide: PcbCartAssemblySide.None,
            notes: "Quote board fabrication only.");
    }

    private static PcbCartProductionHandoffRequest AssemblyRequest(
        IEnumerable<PcbCartBomItem> bomItems,
        IEnumerable<PcbCartPlacement> placements,
        bool includeBomFile = true,
        bool includePickAndPlaceFile = true)
    {
        List<ManufacturingOutputEntry> entries =
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl")
        ];

        if (includeBomFile)
        {
            entries.Add(Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv"));
        }

        if (includePickAndPlaceFile)
        {
            entries.Add(Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pnp.csv"));
        }

        return PcbCartProductionHandoffRequest.Create(
            ManufacturingOutputManifest.Create(entries),
            quantity: 25,
            finish: PcbCartBoardFinish.Enig,
            stackup: Stackup(),
            assemblySide: PcbCartAssemblySide.Top,
            notes: "Quote board fabrication and top-side SMT assembly.",
            bomItems: bomItems,
            placements: placements);
    }

    private static PcbCartBoardStackupSummary Stackup()
    {
        return PcbCartBoardStackupSummary.Create(
            layerCount: 4,
            material: "FR-4 TG170",
            finishedThickness: "1.6 mm",
            outerCopperWeight: "1 oz");
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create($"pending:{Path.GetFileNameWithoutExtension(path)}"));
    }
}
