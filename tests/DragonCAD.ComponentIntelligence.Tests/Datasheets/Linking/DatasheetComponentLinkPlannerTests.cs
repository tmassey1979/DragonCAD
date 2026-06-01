using DragonCAD.ComponentIntelligence.Datasheets;
using DragonCAD.ComponentIntelligence.Datasheets.Linking;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.ComponentIntelligence.Tests.Datasheets.Linking;

public sealed class DatasheetComponentLinkPlannerTests
{
    [Fact]
    public void PlanLinksDatasheetToSingleExistingComponentWithSameManufacturerPartNumber()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(manufacturerPartNumber: "LM7805CT");

        DatasheetComponentLinkPlan plan = planner.Plan(
            facts,
            [
                new ExistingComponentLinkCandidate(
                    new ComponentId("component/linear-regulator/lm7805ct"),
                    Manufacturer: "Texas Instruments",
                    ManufacturerPartNumber: "LM7805CT",
                    PackageName: "TO-220-3"),
            ]);

        Assert.Equal(DatasheetComponentLinkDecision.LinkExistingComponent, plan.Decision);
        Assert.Equal(new ComponentId("component/linear-regulator/lm7805ct"), plan.ComponentId);
        Assert.Empty(plan.ReviewWarnings);
    }

    [Fact]
    public void PlanFindsExistingComponentByCanonicalComponentIdWhenMpnFormattingDiffers()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(manufacturerPartNumber: "LM7805-CT");

        DatasheetComponentLinkPlan plan = planner.Plan(
            facts,
            [
                new ExistingComponentLinkCandidate(
                    new ComponentId("PART:LM7805CT"),
                    Manufacturer: "Texas Instruments",
                    ManufacturerPartNumber: "",
                    PackageName: "TO-220-3"),
            ]);

        Assert.Equal(DatasheetComponentLinkDecision.LinkExistingComponent, plan.Decision);
        Assert.Equal(new ComponentId("PART:LM7805CT"), plan.ComponentId);
    }

    [Fact]
    public void PlanFlagsDuplicateWhenMultipleExistingComponentsMatchSameDatasheetPart()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(manufacturerPartNumber: "NE555P");

        DatasheetComponentLinkPlan plan = planner.Plan(
            facts,
            [
                new ExistingComponentLinkCandidate(
                    new ComponentId("component/timer/ne555p"),
                    Manufacturer: "Texas Instruments",
                    ManufacturerPartNumber: "NE555P",
                    PackageName: "DIP-8"),
                new ExistingComponentLinkCandidate(
                    new ComponentId("PART:NE555P"),
                    Manufacturer: "Texas Instruments",
                    ManufacturerPartNumber: "NE555P",
                    PackageName: "DIP-8"),
            ]);

        Assert.Equal(DatasheetComponentLinkDecision.DuplicateExistingComponents, plan.Decision);
        Assert.Null(plan.ComponentId);
        Assert.Equal(
            [new ComponentId("PART:NE555P"), new ComponentId("component/timer/ne555p")],
            plan.CandidateComponentIds);

        DatasheetComponentLinkReviewWarning warning = Assert.Single(plan.ReviewWarnings);
        Assert.Equal(DatasheetComponentLinkReviewWarningCode.DuplicateMatches, warning.Code);
    }

    [Fact]
    public void PlanRequestsNewComponentWhenNoExistingComponentMatches()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(manufacturerPartNumber: "TL431AIDBZR");

        DatasheetComponentLinkPlan plan = planner.Plan(
            facts,
            [
                new ExistingComponentLinkCandidate(
                    new ComponentId("component/reference/lm4040"),
                    Manufacturer: "Texas Instruments",
                    ManufacturerPartNumber: "LM4040AIM3-2.5",
                    PackageName: "SOT-23-3"),
            ]);

        Assert.Equal(DatasheetComponentLinkDecision.NeedsNewComponent, plan.Decision);
        Assert.Null(plan.ComponentId);
        Assert.Empty(plan.CandidateComponentIds);
    }

    [Fact]
    public void PlanKeepsLinkDecisionButWarnsWhenMatchedMetadataNeedsReview()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(
            manufacturer: "Texas Instruments",
            manufacturerPartNumber: "LM358P",
            packageName: "DIP-8");

        DatasheetComponentLinkPlan plan = planner.Plan(
            facts,
            [
                new ExistingComponentLinkCandidate(
                    new ComponentId("component/op-amp/lm358p"),
                    Manufacturer: "STMicroelectronics",
                    ManufacturerPartNumber: "LM358P",
                    PackageName: "SOIC-8"),
            ]);

        Assert.Equal(DatasheetComponentLinkDecision.LinkExistingComponent, plan.Decision);
        Assert.Collection(
            plan.ReviewWarnings,
            warning => Assert.Equal(DatasheetComponentLinkReviewWarningCode.ManufacturerMismatch, warning.Code),
            warning => Assert.Equal(DatasheetComponentLinkReviewWarningCode.PackageMismatch, warning.Code));
    }

    [Fact]
    public void PlanRequestsNewComponentWithReviewWarningWhenDatasheetMpnIsMissing()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts facts = CreateFacts(manufacturerPartNumber: " ");

        DatasheetComponentLinkPlan plan = planner.Plan(facts, []);

        Assert.Equal(DatasheetComponentLinkDecision.NeedsNewComponent, plan.Decision);
        DatasheetComponentLinkReviewWarning warning = Assert.Single(plan.ReviewWarnings);
        Assert.Equal(DatasheetComponentLinkReviewWarningCode.MissingManufacturerPartNumber, warning.Code);
    }

    private static ExtractedDatasheetFacts CreateFacts(
        string manufacturer = "Texas Instruments",
        string manufacturerPartNumber = "LM7805CT",
        string packageName = "TO-220-3") =>
        new(
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            Description: "Datasheet extracted component",
            PackageName: packageName,
            PinFacts:
            [
                new DatasheetPinFact("1", "IN", "Power input"),
                new DatasheetPinFact("2", "GND", "Ground"),
                new DatasheetPinFact("3", "OUT", "Regulated output"),
            ],
            PackageDimensions: new PackageDimensionFacts(10_160, 4_570, 15_240));
}
