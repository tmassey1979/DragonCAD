using DragonCAD.Sourcing.ProjectPlanning;

namespace DragonCAD.Sourcing.Tests.ProjectPlanning;

public sealed class ProjectDerivedBomPlannerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_DerivesBomRowsFromPlacedComponentsAndActivePackageSelections()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                components:
                [
                    Component("R1", "resistor", "10k", isPlaced: true),
                    Component("R2", "resistor", "10k", isPlaced: true),
                    Component("TP1", "test-point", "loop", isPlaced: false)
                ],
                packages:
                [
                    Package("R1", "0603", "RC0603FR-0710KL"),
                    Package("R2", "0603", "RC0603FR-0710KL"),
                    Package("TP1", "TH", "TP-LOOP")
                ],
                offers:
                [
                    Offer("RC0603FR-0710KL", "Digi-Key", "311-10KGRCT-ND", expiresAt: Now.AddDays(3))
                ]),
            Now);

        ProjectBomRow row = Assert.Single(result.Bom.Rows);
        Assert.Equal("resistor|10k|0603|rc0603fr-0710kl", row.RowKey);
        Assert.Equal(["R1", "R2"], row.Designators);
        Assert.Equal(2, row.Quantity);
        Assert.Equal("0603", row.PackageName);
        Assert.Equal("RC0603FR-0710KL", row.SelectedManufacturerPartNumber);
        Assert.False(row.DoNotSubstitute);
        Assert.Equal(["Digi-Key:311-10KGRCT-ND"], row.CurrentVendorOffers.Select(offer => $"{offer.VendorName}:{offer.VendorPartNumber}"));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Plan_UpdatesBomWhenPackageSelectionChanges()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                components: [Component("U1", "timer", "555", isPlaced: true)],
                packages: [Package("U1", "SOIC-8", "NE555DR")],
                offers: [Offer("NE555DR", "Mouser", "595-NE555DR", expiresAt: Now.AddDays(1))]),
            Now);

        ProjectBomRow row = Assert.Single(result.Bom.Rows);
        Assert.Equal("timer|555|soic-8|ne555dr", row.RowKey);
        Assert.Equal("SOIC-8", row.PackageName);
        Assert.Equal("NE555DR", row.SelectedManufacturerPartNumber);
    }

    [Fact]
    public void Plan_RemovesBomRowsForComponentsNoLongerInActiveDesign()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                components:
                [
                    Component("R1", "resistor", "10k", isPlaced: true),
                    Component("R2", "resistor", "10k", isPlaced: false)
                ],
                packages:
                [
                    Package("R1", "0603", "RC0603FR-0710KL"),
                    Package("R2", "0603", "RC0603FR-0710KL")
                ],
                offers: [Offer("RC0603FR-0710KL", "Digi-Key", "311-10KGRCT-ND", expiresAt: Now.AddDays(2))]),
            Now);

        ProjectBomRow row = Assert.Single(result.Bom.Rows);
        Assert.Equal(["R1"], row.Designators);
        Assert.Equal(1, row.Quantity);
        Assert.DoesNotContain(result.Bom.Rows, bomRow => bomRow.Designators.Contains("R2", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_FiltersSubstitutionsWhenDoNotSubstituteIsSelected()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                components: [Component("C1", "capacitor", "100nF", isPlaced: true)],
                packages:
                [
                    Package(
                        "C1",
                        "0603",
                        "CL10B104KB8NNNC",
                        doNotSubstitute: true,
                        alternates: ["C0603C104K5RACTU"])
                ],
                offers:
                [
                    Offer("CL10B104KB8NNNC", "Mouser", "81-CL10B104", expiresAt: Now.AddDays(2)),
                    Offer("C0603C104K5RACTU", "Digi-Key", "399-1092-1-ND", expiresAt: Now.AddDays(2))
                ]),
            Now);

        ProjectBomRow row = Assert.Single(result.Bom.Rows);
        Assert.True(row.DoNotSubstitute);
        Assert.Equal(["C0603C104K5RACTU"], row.AlternateManufacturerPartNumbers);
        ProjectVendorOfferRef offer = Assert.Single(row.CurrentVendorOffers);
        Assert.Equal("CL10B104KB8NNNC", offer.ManufacturerPartNumber);
    }

    [Fact]
    public void Plan_ReportsStaleVendorOffersWithoutUsingThemForBomRows()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                components: [Component("R1", "resistor", "10k", isPlaced: true)],
                packages: [Package("R1", "0603", "RC0603FR-0710KL")],
                offers: [Offer("RC0603FR-0710KL", "Digi-Key", "311-10KGRCT-ND", expiresAt: Now.AddMinutes(-1))]),
            Now);

        Assert.Empty(Assert.Single(result.Bom.Rows).CurrentVendorOffers);
        ProjectPlanningDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ProjectPlanningDiagnosticCodes.StaleVendorOffer, diagnostic.Code);
        Assert.Equal("R1", diagnostic.Designator);
        Assert.Equal("311-10KGRCT-ND", diagnostic.VendorPartNumber);
    }

    private static ProjectPlanningRequest Request(
        IReadOnlyList<ProjectDesignComponent> components,
        IReadOnlyList<ProjectPackageSelection> packages,
        IReadOnlyList<ProjectVendorOffer> offers)
    {
        return new ProjectPlanningRequest(
            DesignRevision: "rev-a",
            Components: components,
            PackageSelections: packages,
            VendorOffers: offers,
            Artifacts: []);
    }

    private static ProjectDesignComponent Component(string designator, string identity, string value, bool isPlaced)
    {
        return new ProjectDesignComponent(designator, identity, value, isPlaced);
    }

    private static ProjectPackageSelection Package(
        string designator,
        string packageName,
        string manufacturerPartNumber,
        bool doNotSubstitute = false,
        IReadOnlyList<string>? alternates = null)
    {
        return new ProjectPackageSelection(
            designator,
            packageName,
            manufacturerPartNumber,
            doNotSubstitute,
            alternates ?? []);
    }

    private static ProjectVendorOffer Offer(
        string manufacturerPartNumber,
        string vendorName,
        string vendorPartNumber,
        DateTimeOffset expiresAt)
    {
        return new ProjectVendorOffer(
            manufacturerPartNumber,
            vendorName,
            vendorPartNumber,
            Stock: 100,
            Money.Usd(0.02m),
            CapturedAt: Now.AddDays(-1),
            ExpiresAt: expiresAt);
    }
}
