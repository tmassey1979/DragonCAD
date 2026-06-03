using DragonCAD.Fabrication.OshPark;
using DragonCAD.Fabrication.Outputs;
using DragonCAD.Fabrication.Outputs.Gerber;

namespace DragonCAD.Fabrication.Tests.OshPark;

public sealed class OshParkPrototypeHandoffPlannerTests
{
    [Fact]
    public void Plan_CreatesValidPrototypePackage()
    {
        OshParkPrototypeHandoffPackage package = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest());

        Assert.True(package.IsReadyForUploadHandoff);
        Assert.Equal(new Uri("https://oshpark.com/uploads/new"), package.UploadHandoffUri);
        Assert.Equal(3, package.Gerbers.Count);
        Assert.Single(package.DrillFiles);
        Assert.NotNull(package.BoardOutline);
        Assert.Equal(new OshParkBoardDimensions(42.25m, 17.5m), package.BoardDimensions);
        Assert.Equal(4, package.Manifest.Entries.Count);
        Assert.Equal(3, package.LayerMappings.Count);
        Assert.Contains(package.Diagnostics, diagnostic => diagnostic.Code == OshParkHandoffDiagnosticCodes.UploadLimitations
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Info
            && diagnostic.Message.Contains("cannot currently fetch OSH Park previews or warnings", StringComparison.Ordinal)
            && diagnostic.Message.Contains("attach this project to the user's OSH Park account", StringComparison.Ordinal));
        Assert.DoesNotContain(package.Diagnostics, diagnostic => diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error);
    }

    [Fact]
    public void Plan_BlocksUploadHandoffWhenDrillFileIsMissing()
    {
        OshParkPrototypeHandoffPackage package = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest(manifest: ManufacturingOutputManifest.Create(
            [
                Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.GTL", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Entry(ManufacturingFileRole.Gerber, "gerbers/bottom-copper.GBL", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                Entry(ManufacturingFileRole.Gerber, "gerbers/outline.GKO", "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd")
            ])));

        Assert.False(package.IsReadyForUploadHandoff);
        Assert.Null(package.UploadHandoffUri);
        Assert.Contains(package.Diagnostics, diagnostic => diagnostic.Code == OshParkHandoffDiagnosticCodes.MissingDrillFile
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error);
    }

    [Fact]
    public void Plan_BlocksUploadHandoffWhenBoardOutlineIsMissing()
    {
        OshParkPrototypeHandoffPackage package = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest(layerMappings:
            [
                Layer("gerbers/top-copper.GTL", GerberBoardLayerKind.Copper, GerberBoardSide.Top, "Top Copper"),
                Layer("gerbers/bottom-copper.GBL", GerberBoardLayerKind.Copper, GerberBoardSide.Bottom, "Bottom Copper")
            ]));

        Assert.False(package.IsReadyForUploadHandoff);
        Assert.Null(package.UploadHandoffUri);
        Assert.Null(package.BoardOutline);
        Assert.Contains(package.Diagnostics, diagnostic => diagnostic.Code == OshParkHandoffDiagnosticCodes.MissingBoardOutline
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error);
    }

    [Fact]
    public void Plan_BlocksUploadHandoffWhenLayerMappingDoesNotMatchManifest()
    {
        OshParkPrototypeHandoffPackage package = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest(layerMappings:
            [
                Layer("gerbers/top-copper.GTL", GerberBoardLayerKind.Copper, GerberBoardSide.Top, "Top Copper"),
                Layer("gerbers/bottom-copper.GBL", GerberBoardLayerKind.Copper, GerberBoardSide.Bottom, "Bottom Copper"),
                Layer("gerbers/mechanical-outline.GKO", GerberBoardLayerKind.BoardOutline, GerberBoardSide.Board, "Board Outline")
            ]));

        Assert.False(package.IsReadyForUploadHandoff);
        Assert.Null(package.UploadHandoffUri);
        Assert.Contains(package.Diagnostics, diagnostic => diagnostic.Code == OshParkHandoffDiagnosticCodes.LayerMismatch
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error
            && diagnostic.Message.Contains("mechanical-outline.GKO", StringComparison.Ordinal));
        Assert.Contains(package.Diagnostics, diagnostic => diagnostic.Code == OshParkHandoffDiagnosticCodes.LayerMismatch
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error
            && diagnostic.Message.Contains("outline.GKO", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_CreatesUploadHandoffWhenWarningsAreAccepted()
    {
        OshParkHandoffWarning warning = new("silkscreen-near-edge", "Silkscreen is close to the board edge.");

        OshParkPrototypeHandoffPackage blockedPackage = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest(warnings: [warning]));
        OshParkPrototypeHandoffPackage acceptedPackage = OshParkPrototypeHandoffPlanner.Plan(
            ValidRequest(warnings: [warning], acceptedWarningCodes: ["silkscreen-near-edge"]));

        Assert.False(blockedPackage.IsReadyForUploadHandoff);
        Assert.Null(blockedPackage.UploadHandoffUri);
        Assert.True(acceptedPackage.IsReadyForUploadHandoff);
        Assert.Equal(new Uri("https://oshpark.com/uploads/new"), acceptedPackage.UploadHandoffUri);
        Assert.Contains(acceptedPackage.Diagnostics, diagnostic => diagnostic.Code == "silkscreen-near-edge"
            && diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Warning);
    }

    private static OshParkPrototypeHandoffRequest ValidRequest(
        ManufacturingOutputManifest? manifest = null,
        IReadOnlyList<OshParkLayerMapping>? layerMappings = null,
        IReadOnlyList<OshParkHandoffWarning>? warnings = null,
        IReadOnlyList<string>? acceptedWarningCodes = null)
    {
        return new OshParkPrototypeHandoffRequest(
            manifest ?? ValidManifest(),
            new OshParkBoardDimensions(42.25m, 17.5m),
            layerMappings ?? ValidLayerMappings(),
            warnings,
            acceptedWarningCodes);
    }

    private static ManufacturingOutputManifest ValidManifest()
    {
        return ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.GTL", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/bottom-copper.GBL", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            Entry(ManufacturingFileRole.Drill, "drill/plated-through.drl", "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/outline.GKO", "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd")
        ]);
    }

    private static OshParkLayerMapping[] ValidLayerMappings()
    {
        return
        [
            Layer("gerbers/top-copper.GTL", GerberBoardLayerKind.Copper, GerberBoardSide.Top, "Top Copper"),
            Layer("gerbers/bottom-copper.GBL", GerberBoardLayerKind.Copper, GerberBoardSide.Bottom, "Bottom Copper"),
            Layer("gerbers/outline.GKO", GerberBoardLayerKind.BoardOutline, GerberBoardSide.Board, "Board Outline")
        ];
    }

    private static OshParkLayerMapping Layer(
        string path,
        GerberBoardLayerKind layerKind,
        GerberBoardSide side,
        string displayName)
    {
        return new OshParkLayerMapping(
            ManufacturingRelativePath.Create(path),
            layerKind,
            side,
            displayName);
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path, string checksum)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create(checksum));
    }
}
