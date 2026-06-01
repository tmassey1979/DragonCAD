using DragonCAD.ComponentIntelligence.Datasheets;

namespace DragonCAD.ComponentIntelligence.Tests.Datasheets;

public sealed class DatasheetGenerationPlanTests
{
    [Fact]
    public void DatasheetAssetRequiresSourceUrlAndChecksum()
    {
        Assert.Throws<ArgumentException>(() => new DatasheetAsset(new Uri("https://example.test/part.pdf"), string.Empty));
        Assert.Throws<ArgumentException>(() => new DatasheetAsset(new Uri("file:///local/part.pdf"), "sha256:abc123"));

        var asset = new DatasheetAsset(new Uri("https://example.test/part.pdf"), "sha256:abc123");

        Assert.Equal("https://example.test/part.pdf", asset.SourceUrl.ToString());
        Assert.Equal("sha256:abc123", asset.Checksum);
    }

    [Fact]
    public void ComponentGenerationRequestKeepsAiProviderAndSourceFacts()
    {
        var datasheet = new DatasheetAsset(new Uri("https://vendor.test/lm7805.pdf"), "sha256:7805");
        var facts = new ExtractedDatasheetFacts(
            Manufacturer: "Texas Instruments",
            ManufacturerPartNumber: "LM7805CT",
            Description: "5 V linear regulator",
            PackageName: "TO-220-3",
            PinFacts:
            [
                new DatasheetPinFact("1", "IN", "Power input"),
                new DatasheetPinFact("2", "GND", "Ground"),
                new DatasheetPinFact("3", "OUT", "Regulated output"),
            ],
            PackageDimensions: new PackageDimensionFacts(10_160, 4_570, 15_240));

        var request = new DatasheetComponentGenerationRequest(
            datasheet,
            facts,
            DatasheetAiProvider.Ollama,
            "llama3.1");

        Assert.Same(datasheet, request.Datasheet);
        Assert.Same(facts, request.Facts);
        Assert.Equal(DatasheetAiProvider.Ollama, request.Provider);
        Assert.Equal("llama3.1", request.ModelName);
    }

    [Fact]
    public void GeneratedPlanCapturesSymbolFootprintAndThreeDimensionalModelProposals()
    {
        var plan = CreateValidPlan();

        Assert.Collection(
            plan.Symbol.Pins,
            pin =>
            {
                Assert.Equal("IN", pin.Name);
                Assert.Equal("1", pin.Number);
            },
            pin =>
            {
                Assert.Equal("GND", pin.Name);
                Assert.Equal("2", pin.Number);
            },
            pin =>
            {
                Assert.Equal("OUT", pin.Name);
                Assert.Equal("3", pin.Number);
            });
        Assert.Collection(
            plan.Footprint.Pads,
            pad =>
            {
                Assert.Equal("1", pad.Number);
                Assert.Equal("through-hole round", pad.Shape);
            },
            pad => Assert.Equal("2", pad.Number),
            pad => Assert.Equal("3", pad.Number));
        Assert.Equal("placeholder-to-220-3.step", plan.ThreeDimensionalModel.FileName);
        Assert.Equal(DatasheetThreeDimensionalModelStatus.Placeholder, plan.ThreeDimensionalModel.Status);
    }

    [Fact]
    public void GeneratedPlanWarnsWhenPackageDimensionsAreMissing()
    {
        var datasheet = new DatasheetAsset(new Uri("https://vendor.test/ne555.pdf"), "sha256:555");
        var facts = new ExtractedDatasheetFacts(
            Manufacturer: "Texas Instruments",
            ManufacturerPartNumber: "NE555P",
            Description: "Timer",
            PackageName: "DIP-8",
            PinFacts:
            [
                new DatasheetPinFact("1", "GND", "Ground"),
            ],
            PackageDimensions: null);

        var plan = DatasheetGeneratedComponentPlan.Create(
            request: new DatasheetComponentGenerationRequest(datasheet, facts, DatasheetAiProvider.Codex, "gpt-5"),
            symbol: new GeneratedSymbolProposal("NE555P", [new GeneratedSymbolPinProposal("1", "GND", 0, 0, "left")]),
            footprint: new GeneratedFootprintProposal("DIP-8", [new GeneratedFootprintPadProposal("1", 0, 0, 700, 1_500, "through-hole round")]),
            threeDimensionalModel: new GeneratedThreeDimensionalModelProposal("placeholder-dip-8.step", DatasheetThreeDimensionalModelStatus.Placeholder),
            confidence: DatasheetGenerationConfidence.Medium);

        var warning = Assert.Single(plan.Warnings);
        Assert.Equal(DatasheetGenerationWarningCode.MissingPackageDimensions, warning.Code);
        Assert.Equal(DatasheetHumanReviewStatus.Required, plan.HumanReviewStatus);
    }

    [Fact]
    public void GeneratedPlanIsNeverAutoApproved()
    {
        var plan = CreateValidPlan();

        Assert.False(plan.IsApproved);
        Assert.Equal(DatasheetHumanReviewStatus.Required, plan.HumanReviewStatus);
    }

    private static DatasheetGeneratedComponentPlan CreateValidPlan()
    {
        var datasheet = new DatasheetAsset(new Uri("https://vendor.test/lm7805.pdf"), "sha256:7805");
        var facts = new ExtractedDatasheetFacts(
            Manufacturer: "Texas Instruments",
            ManufacturerPartNumber: "LM7805CT",
            Description: "5 V linear regulator",
            PackageName: "TO-220-3",
            PinFacts:
            [
                new DatasheetPinFact("1", "IN", "Power input"),
                new DatasheetPinFact("2", "GND", "Ground"),
                new DatasheetPinFact("3", "OUT", "Regulated output"),
            ],
            PackageDimensions: new PackageDimensionFacts(10_160, 4_570, 15_240));

        return DatasheetGeneratedComponentPlan.Create(
            request: new DatasheetComponentGenerationRequest(datasheet, facts, DatasheetAiProvider.Codex, "gpt-5"),
            symbol: new GeneratedSymbolProposal(
                "LM7805CT",
                [
                    new GeneratedSymbolPinProposal("1", "IN", -100, 100, "left"),
                    new GeneratedSymbolPinProposal("2", "GND", 0, -100, "down"),
                    new GeneratedSymbolPinProposal("3", "OUT", 100, 100, "right"),
                ]),
            footprint: new GeneratedFootprintProposal(
                "TO-220-3",
                [
                    new GeneratedFootprintPadProposal("1", -2_540, 0, 1_300, 2_000, "through-hole round"),
                    new GeneratedFootprintPadProposal("2", 0, 0, 1_300, 2_000, "through-hole round"),
                    new GeneratedFootprintPadProposal("3", 2_540, 0, 1_300, 2_000, "through-hole round"),
                ]),
            threeDimensionalModel: new GeneratedThreeDimensionalModelProposal("placeholder-to-220-3.step", DatasheetThreeDimensionalModelStatus.Placeholder),
            confidence: DatasheetGenerationConfidence.High);
    }
}
