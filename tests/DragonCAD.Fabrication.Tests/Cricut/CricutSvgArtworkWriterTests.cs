using DragonCAD.Fabrication.Cricut;

namespace DragonCAD.Fabrication.Tests.Cricut;

public sealed class CricutSvgArtworkWriterTests
{
    [Fact]
    public void Write_EmitsTopCopperSvgWithUnitsLayerMetadataAndArtworkIntent()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(
            Manifest(
                Entry(
                    CricutArtworkOutputKind.CopperVinyl,
                    sourceLayerName: "Top Copper",
                    outputFileName: "dragon-badge-top-copper-vinyl.svg",
                    units: CricutArtworkUnits.Millimeters,
                    scale: 1.25m,
                    mirror: false)));

        CricutArtworkSvgFile file = Assert.Single(result.Files);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("dragon-badge-top-copper-vinyl.svg", file.OutputFileName);
        Assert.Contains("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100mm\" height=\"100mm\" viewBox=\"0 0 100 100\">", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<title>Dragon Badge - Top Copper</title>", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<metadata>{\"kind\":\"CopperVinyl\",\"sourceLayer\":\"Top Copper\",\"units\":\"Millimeters\",\"scale\":\"1.25\",\"mirror\":false,\"side\":\"Top\"}</metadata>", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<g id=\"top-copper\" data-output-kind=\"CopperVinyl\" data-source-layer=\"Top Copper\" data-units=\"Millimeters\" data-scale=\"1.25\" data-mirror=\"false\" data-side=\"Top\">", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<path id=\"top-copper-artwork\" d=\"M 10 10 H 90 V 90 H 10 Z\" fill=\"#b87333\" stroke=\"none\" />", file.SvgText, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_EmitsMirroredBottomCopperSvg()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(
            Manifest(
                Entry(
                    CricutArtworkOutputKind.CopperVinyl,
                    sourceLayerName: "Bottom Copper",
                    outputFileName: "dragon-badge-bottom-copper-vinyl.svg",
                    units: CricutArtworkUnits.Millimeters,
                    scale: 1m,
                    mirror: true)));

        CricutArtworkSvgFile file = Assert.Single(result.Files);
        Assert.Contains("<g id=\"bottom-copper\" data-output-kind=\"CopperVinyl\" data-source-layer=\"Bottom Copper\" data-units=\"Millimeters\" data-scale=\"1\" data-mirror=\"true\" data-side=\"Bottom\" transform=\"translate(100 0) scale(-1 1)\">", file.SvgText, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_EmitsPasteSvgWithStencilArtworkIntent()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(
            Manifest(
                Entry(
                    CricutArtworkOutputKind.SolderPaste,
                    sourceLayerName: "Top Paste",
                    outputFileName: "dragon-badge-top-solder-paste-stencil.svg",
                    units: CricutArtworkUnits.Inches,
                    scale: 0.5m,
                    mirror: false)));

        CricutArtworkSvgFile file = Assert.Single(result.Files);
        Assert.Contains("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"3.937in\" height=\"3.937in\" viewBox=\"0 0 100 100\">", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<metadata>{\"kind\":\"SolderPaste\",\"sourceLayer\":\"Top Paste\",\"units\":\"Inches\",\"scale\":\"0.5\",\"mirror\":false,\"side\":\"Top\"}</metadata>", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<path id=\"top-paste-artwork\" d=\"M 18 18 H 38 V 38 H 18 Z M 62 62 H 82 V 82 H 62 Z\" fill=\"#808080\" stroke=\"none\" />", file.SvgText, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_EmitsRegistrationMarksSvg()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(
            Manifest(
                Entry(
                    CricutArtworkOutputKind.RegistrationMarks,
                    sourceLayerName: null,
                    outputFileName: "dragon-badge-registration-marks.svg",
                    units: CricutArtworkUnits.Millimeters,
                    scale: 1m,
                    mirror: false)));

        CricutArtworkSvgFile file = Assert.Single(result.Files);
        Assert.Contains("<g id=\"registration-marks\" data-output-kind=\"RegistrationMarks\" data-source-layer=\"\" data-units=\"Millimeters\" data-scale=\"1\" data-mirror=\"false\" data-side=\"\">", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<circle id=\"registration-mark-top-left\" cx=\"8\" cy=\"8\" r=\"2\" fill=\"#111111\" />", file.SvgText, StringComparison.Ordinal);
        Assert.Contains("<circle id=\"registration-mark-bottom-right\" cx=\"92\" cy=\"92\" r=\"2\" fill=\"#111111\" />", file.SvgText, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ReturnsDiagnosticsWithoutSvgForBlockedAndEmptyManifestEntries()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(
            new CricutArtworkManifest(
                "Dragon Badge",
                [
                    Entry(
                        CricutArtworkOutputKind.CopperVinyl,
                        sourceLayerName: "Top Copper",
                        outputFileName: "dragon-badge-top-copper-vinyl.svg",
                        units: CricutArtworkUnits.Millimeters,
                        scale: 1m,
                        mirror: false,
                        blockers:
                        [
                            new CricutArtworkBlocker(
                                CricutArtworkBlockerCodes.MissingCopperGeometry,
                                "Source layer 'Top Copper' does not contain exportable artwork geometry.",
                                "Top Copper")
                        ])
                ]));

        Assert.Empty(result.Files);
        CricutArtworkWriteDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CricutArtworkBlockerCodes.MissingCopperGeometry, diagnostic.Code);
        Assert.Equal("dragon-badge-top-copper-vinyl.svg", diagnostic.OutputFileName);
        Assert.Equal("Top Copper", diagnostic.SourceLayerName);
    }

    [Fact]
    public void Write_ReturnsDiagnosticForEmptyManifest()
    {
        CricutArtworkWriteResult result = CricutSvgArtworkWriter.Write(new CricutArtworkManifest("Dragon Badge", []));

        Assert.Empty(result.Files);
        CricutArtworkWriteDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("empty-cricut-artwork-manifest", diagnostic.Code);
        Assert.Equal("Dragon Badge", diagnostic.ProjectName);
    }

    [Fact]
    public void Write_OrdersOutputAndTextDeterministically()
    {
        CricutArtworkManifest manifest = Manifest(
            Entry(CricutArtworkOutputKind.RegistrationMarks, null, "dragon-badge-registration-marks.svg", CricutArtworkUnits.Millimeters, 1m, false),
            Entry(CricutArtworkOutputKind.CopperVinyl, "Top Copper", "dragon-badge-top-copper-vinyl.svg", CricutArtworkUnits.Millimeters, 1m, false),
            Entry(CricutArtworkOutputKind.SolderPaste, "Top Paste", "dragon-badge-top-solder-paste-stencil.svg", CricutArtworkUnits.Millimeters, 1m, false));

        CricutArtworkWriteResult first = CricutSvgArtworkWriter.Write(manifest);
        CricutArtworkWriteResult second = CricutSvgArtworkWriter.Write(manifest);

        Assert.Equal(
            [
                "dragon-badge-top-copper-vinyl.svg",
                "dragon-badge-top-solder-paste-stencil.svg",
                "dragon-badge-registration-marks.svg"
            ],
            first.Files.Select(file => file.OutputFileName).ToArray());
        Assert.Equal(first.Files.Select(file => file.SvgText).ToArray(), second.Files.Select(file => file.SvgText).ToArray());
    }

    private static CricutArtworkManifest Manifest(params CricutArtworkManifestEntry[] entries)
    {
        return new CricutArtworkManifest("Dragon Badge", entries);
    }

    private static CricutArtworkManifestEntry Entry(
        CricutArtworkOutputKind outputKind,
        string? sourceLayerName,
        string outputFileName,
        CricutArtworkUnits units,
        decimal scale,
        bool mirror,
        IReadOnlyList<CricutArtworkBlocker>? blockers = null)
    {
        return new CricutArtworkManifestEntry(outputKind, sourceLayerName, outputFileName, units, scale, mirror, blockers ?? []);
    }
}
