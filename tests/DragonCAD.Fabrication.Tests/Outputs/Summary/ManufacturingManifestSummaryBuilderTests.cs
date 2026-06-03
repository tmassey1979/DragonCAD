using DragonCAD.Fabrication.Outputs;
using DragonCAD.Fabrication.Outputs.Summary;

namespace DragonCAD.Fabrication.Tests.Outputs.Summary;

public sealed class ManufacturingManifestSummaryBuilderTests
{
    [Fact]
    public void Create_GroupsCompletePackageByManufacturingRole()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/bottom-copper.gbr", "bottom"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl", "drill"),
            Entry(ManufacturingFileRole.SolderPaste, "paste/top-paste.gtp", "paste"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv", "bom"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pos.csv", "pnp"),
            Entry(ManufacturingFileRole.AssemblyDrawing, "assembly/project.pdf", "assembly")));

        Assert.Equal(7, summary.TotalFileCount);
        Assert.Empty(summary.MissingRequiredRoles);
        Assert.Empty(summary.ReviewWarnings);
        Assert.Equal(
            [
                ManufacturingManifestSummaryRole.Gerber,
                ManufacturingManifestSummaryRole.Drill,
                ManufacturingManifestSummaryRole.Paste,
                ManufacturingManifestSummaryRole.BillOfMaterials,
                ManufacturingManifestSummaryRole.PickAndPlace,
                ManufacturingManifestSummaryRole.Assembly,
                ManufacturingManifestSummaryRole.Auxiliary
            ],
            summary.RoleSummaries.Select(role => role.Role).ToArray());

        ManufacturingManifestRoleSummary gerber = Role(summary, ManufacturingManifestSummaryRole.Gerber);
        Assert.Equal(2, gerber.FileCount);
        Assert.True(gerber.HasChecksums);
        Assert.Equal(["gerbers/bottom-copper.gbr", "gerbers/top-copper.gbr"], gerber.RelativePaths);

        ManufacturingManifestRoleSummary auxiliary = Role(summary, ManufacturingManifestSummaryRole.Auxiliary);
        Assert.Equal(0, auxiliary.FileCount);
        Assert.Empty(auxiliary.RelativePaths);
    }

    [Fact]
    public void Create_ReportsMissingDrillWithoutInventingPlaceholderFile()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv", "bom"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pos.csv", "pnp")));

        Assert.Equal([ManufacturingManifestSummaryRole.Drill], summary.MissingRequiredRoles);
        Assert.Empty(Role(summary, ManufacturingManifestSummaryRole.Drill).RelativePaths);
        Assert.Contains(summary.ReviewWarnings, warning =>
            warning.Code == ManufacturingManifestReviewWarningCodes.MissingRequiredRole &&
            warning.Role == ManufacturingManifestSummaryRole.Drill);
    }

    [Fact]
    public void Create_ReportsMissingBomWithoutInventingPlaceholderFile()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl", "drill"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pos.csv", "pnp")));

        Assert.Equal([ManufacturingManifestSummaryRole.BillOfMaterials], summary.MissingRequiredRoles);
        Assert.Empty(Role(summary, ManufacturingManifestSummaryRole.BillOfMaterials).RelativePaths);
        Assert.Contains(summary.ReviewWarnings, warning =>
            warning.Code == ManufacturingManifestReviewWarningCodes.MissingRequiredRole &&
            warning.Role == ManufacturingManifestSummaryRole.BillOfMaterials);
    }

    [Fact]
    public void Create_ReportsMissingPickAndPlaceWithoutInventingPlaceholderFile()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl", "drill"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv", "bom")));

        Assert.Equal([ManufacturingManifestSummaryRole.PickAndPlace], summary.MissingRequiredRoles);
        Assert.Empty(Role(summary, ManufacturingManifestSummaryRole.PickAndPlace).RelativePaths);
        Assert.Contains(summary.ReviewWarnings, warning =>
            warning.Code == ManufacturingManifestReviewWarningCodes.MissingRequiredRole &&
            warning.Role == ManufacturingManifestSummaryRole.PickAndPlace);
    }

    [Fact]
    public void Create_WarnsWhenSingletonRolesAreDuplicated()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl", "drill"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project-a.csv", "bom-a"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project-b.csv", "bom-b"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pos.csv", "pnp")));

        ManufacturingManifestReviewWarning warning = Assert.Single(
            summary.ReviewWarnings,
            warning => warning.Code == ManufacturingManifestReviewWarningCodes.DuplicateRole);
        Assert.Equal(ManufacturingManifestSummaryRole.BillOfMaterials, warning.Role);
        Assert.Contains("2 Bill of materials files", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WarnsWhenAnyOutputIsMissingChecksum()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            EntryWithoutChecksum(ManufacturingFileRole.Drill, "drill/project.drl"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv", "bom"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pos.csv", "pnp")));

        ManufacturingManifestRoleSummary drill = Role(summary, ManufacturingManifestSummaryRole.Drill);
        Assert.False(drill.HasChecksums);
        Assert.Equal(0, drill.ChecksumCount);
        Assert.Contains(summary.ReviewWarnings, warning =>
            warning.Code == ManufacturingManifestReviewWarningCodes.MissingChecksum &&
            warning.Role == ManufacturingManifestSummaryRole.Drill &&
            warning.RelativePath == "drill/project.drl");
    }

    [Fact]
    public void Create_UsesDeterministicRolePathAndWarningOrdering()
    {
        ManufacturingManifestSummary summary = ManufacturingManifestSummaryBuilder.Create(Manifest(
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-b-pos.csv", "pnp-b"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project-b.csv", "bom-b"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "top"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project-a.csv", "bom-a"),
            EntryWithoutChecksum(ManufacturingFileRole.PickAndPlace, "assembly/project-a-pos.csv")));

        Assert.Equal(
            [
                ManufacturingManifestSummaryRole.Gerber,
                ManufacturingManifestSummaryRole.Drill,
                ManufacturingManifestSummaryRole.Paste,
                ManufacturingManifestSummaryRole.BillOfMaterials,
                ManufacturingManifestSummaryRole.PickAndPlace,
                ManufacturingManifestSummaryRole.Assembly,
                ManufacturingManifestSummaryRole.Auxiliary
            ],
            summary.RoleSummaries.Select(role => role.Role).ToArray());
        Assert.Equal(["bom/project-a.csv", "bom/project-b.csv"], Role(summary, ManufacturingManifestSummaryRole.BillOfMaterials).RelativePaths);
        Assert.Equal(
            [
                ManufacturingManifestReviewWarningCodes.MissingRequiredRole,
                ManufacturingManifestReviewWarningCodes.DuplicateRole,
                ManufacturingManifestReviewWarningCodes.DuplicateRole,
                ManufacturingManifestReviewWarningCodes.MissingChecksum
            ],
            summary.ReviewWarnings.Select(warning => warning.Code).ToArray());
        Assert.Equal(
            [
                ManufacturingManifestSummaryRole.Drill,
                ManufacturingManifestSummaryRole.BillOfMaterials,
                ManufacturingManifestSummaryRole.PickAndPlace,
                ManufacturingManifestSummaryRole.PickAndPlace
            ],
            summary.ReviewWarnings.Select(warning => warning.Role).ToArray());
    }

    private static ManufacturingOutputManifest Manifest(params ManufacturingOutputEntry[] entries) =>
        ManufacturingOutputManifest.Create(entries);

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path, string checksumToken) =>
        new(role, ManufacturingRelativePath.Create(path), ManufacturingChecksum.Create($"pending:{checksumToken}"));

    private static ManufacturingOutputEntry EntryWithoutChecksum(ManufacturingFileRole role, string path) =>
        new(role, ManufacturingRelativePath.Create(path), null!);

    private static ManufacturingManifestRoleSummary Role(
        ManufacturingManifestSummary summary,
        ManufacturingManifestSummaryRole role) =>
        Assert.Single(summary.RoleSummaries, roleSummary => roleSummary.Role == role);
}
