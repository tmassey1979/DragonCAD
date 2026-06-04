using DragonCAD.Fabrication.Cricut;

namespace DragonCAD.Fabrication.Tests.Cricut;

public sealed class CricutArtworkExportPlannerTests
{
    [Fact]
    public void Plan_CreatesTopCopperVinylArtworkWithOutline()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: true,
                sourceLayers:
                [
                    Layer("Top Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Top),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board)
                ]));

        Assert.Equal(
            [
                CricutArtworkOutputKind.CopperVinyl,
                CricutArtworkOutputKind.BoardOutline
            ],
            manifest.Entries.Select(entry => entry.OutputKind).ToArray());

        CricutArtworkManifestEntry copper = manifest.Entries[0];
        Assert.Equal("Top Copper", copper.SourceLayerName);
        Assert.Equal("dragon-badge-top-copper-vinyl.svg", copper.OutputFileName);
        Assert.Equal(CricutArtworkUnits.Millimeters, copper.Units);
        Assert.Equal(1.0m, copper.Scale);
        Assert.False(copper.Mirror);
        Assert.Empty(copper.Blockers);
    }

    [Fact]
    public void Plan_MirrorsBottomCopperVinylArtwork()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: true,
                sourceLayers:
                [
                    Layer("Bottom Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Bottom),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board)
                ]));

        CricutArtworkManifestEntry copper = Assert.Single(
            manifest.Entries,
            entry => entry.OutputKind == CricutArtworkOutputKind.CopperVinyl);

        Assert.Equal("dragon-badge-bottom-copper-vinyl.svg", copper.OutputFileName);
        Assert.True(copper.Mirror);
    }

    [Fact]
    public void Plan_CreatesPasteArtworkWithoutCopperVinyl()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: false,
                includeSolderPaste: true,
                sourceLayers:
                [
                    Layer("Top Paste", CricutArtworkSourceLayerKind.SolderPaste, CricutArtworkBoardSide.Top),
                    Layer("Top Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Top),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board)
                ]));

        Assert.Equal(
            [
                CricutArtworkOutputKind.SolderPaste,
                CricutArtworkOutputKind.BoardOutline
            ],
            manifest.Entries.Select(entry => entry.OutputKind).ToArray());
        Assert.Equal("dragon-badge-top-solder-paste-stencil.svg", manifest.Entries[0].OutputFileName);
    }

    [Fact]
    public void Plan_RecordsBlockerWhenBoardOutlineGeometryIsMissing()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: true,
                sourceLayers:
                [
                    Layer("Top Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Top),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board, hasGeometry: false)
                ]));

        CricutArtworkManifestEntry outline = Assert.Single(
            manifest.Entries,
            entry => entry.OutputKind == CricutArtworkOutputKind.BoardOutline);

        Assert.Equal(["missing-board-outline-geometry"], outline.Blockers.Select(blocker => blocker.Code).ToArray());
        Assert.Collection(
            outline.Blockers,
            blocker => Assert.Equal("Board Outline", blocker.SourceLayerName));
    }

    [Fact]
    public void Plan_IncludesRegistrationMarksWhenRequested()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: true,
                includeRegistrationMarks: true,
                sourceLayers:
                [
                    Layer("Top Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Top),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board)
                ]));

        CricutArtworkManifestEntry registrationMarks = Assert.Single(
            manifest.Entries,
            entry => entry.OutputKind == CricutArtworkOutputKind.RegistrationMarks);

        Assert.Null(registrationMarks.SourceLayerName);
        Assert.Equal("dragon-badge-registration-marks.svg", registrationMarks.OutputFileName);
        Assert.False(registrationMarks.Mirror);
        Assert.Empty(registrationMarks.Blockers);
    }

    [Fact]
    public void Plan_OrdersArtworkDeterministically()
    {
        CricutArtworkManifest manifest = CricutArtworkExportPlanner.Plan(
            Request(
                includeCopperVinyl: true,
                includeSolderPaste: true,
                includeRegistrationMarks: true,
                sourceLayers:
                [
                    Layer("Top Paste", CricutArtworkSourceLayerKind.SolderPaste, CricutArtworkBoardSide.Top),
                    Layer("Bottom Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Bottom),
                    Layer("Board Outline", CricutArtworkSourceLayerKind.BoardOutline, CricutArtworkBoardSide.Board),
                    Layer("Bottom Paste", CricutArtworkSourceLayerKind.SolderPaste, CricutArtworkBoardSide.Bottom),
                    Layer("Top Copper", CricutArtworkSourceLayerKind.Copper, CricutArtworkBoardSide.Top)
                ]));

        Assert.Equal(
            [
                "dragon-badge-top-copper-vinyl.svg",
                "dragon-badge-bottom-copper-vinyl.svg",
                "dragon-badge-top-solder-paste-stencil.svg",
                "dragon-badge-bottom-solder-paste-stencil.svg",
                "dragon-badge-board-outline.svg",
                "dragon-badge-registration-marks.svg"
            ],
            manifest.Entries.Select(entry => entry.OutputFileName).ToArray());
    }

    private static CricutArtworkExportPlanRequest Request(
        bool includeCopperVinyl = false,
        bool includeSolderPaste = false,
        bool includeRegistrationMarks = false,
        IReadOnlyList<CricutArtworkSourceLayer>? sourceLayers = null)
    {
        return new CricutArtworkExportPlanRequest(
            ProjectName: "Dragon Badge",
            SourceLayers: sourceLayers ?? [],
            Units: CricutArtworkUnits.Millimeters,
            Scale: 1.0m,
            IncludeCopperVinyl: includeCopperVinyl,
            IncludeSolderPaste: includeSolderPaste,
            IncludeRegistrationMarks: includeRegistrationMarks);
    }

    private static CricutArtworkSourceLayer Layer(
        string name,
        CricutArtworkSourceLayerKind kind,
        CricutArtworkBoardSide side,
        bool hasGeometry = true)
    {
        return new CricutArtworkSourceLayer(name, kind, side, hasGeometry);
    }
}
