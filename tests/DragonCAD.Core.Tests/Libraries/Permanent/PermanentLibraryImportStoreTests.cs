using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;
using DragonCAD.Core.Libraries.Permanent;

namespace DragonCAD.Core.Tests.Libraries.Permanent;

public sealed class PermanentLibraryImportStoreTests
{
    [Fact]
    public void SparkFunStyleEagleImportWritesPermanentComponentWithSourceProvenance()
    {
        PermanentLibraryImportStore store = new();
        ComponentDefinition resistor = SparkFunResistor();
        PermanentLibraryImportSource source = PermanentLibraryImportSource.EagleLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero));

        PermanentLibraryImportResult result = store.Import(source, [resistor]);

        PermanentLibraryComponentRecord record = Assert.Single(store.Components);
        Assert.Single(result.WrittenComponents);
        Assert.Empty(result.LinkedComponents);
        Assert.Empty(result.ReviewItems);
        Assert.Equal("perm:sparkfun/resistor-0603", record.CanonicalComponentId.Value);
        Assert.Equal(resistor, record.Component);
        Assert.Equal(PermanentLibraryVerificationState.Imported, record.VerificationState);
        PermanentLibrarySourceProvenance provenance = Assert.Single(record.Provenance);
        Assert.Equal("sparkfun-eagle-libraries", provenance.SourceName);
        Assert.Equal("libraries/SparkFun-Resistors.lbr", provenance.SourceLocation);
        Assert.Equal(PermanentLibraryImportSourceKind.EagleLibrary, provenance.Kind);
        Assert.Equal(resistor.Id, provenance.ImportedComponentId);
    }

    [Fact]
    public void UserLibraryImportWritesPermanentComponentWithUserSourceProvenance()
    {
        PermanentLibraryImportStore store = new();
        ComponentDefinition header = UserHeader();
        PermanentLibraryImportSource source = PermanentLibraryImportSource.UserLibrary(
            "bench-library",
            "C:/cad/libs/bench-connectors.hclib.json",
            new DateTimeOffset(2026, 6, 3, 11, 0, 0, TimeSpan.Zero));

        PermanentLibraryImportResult result = store.Import(source, [header]);

        PermanentLibraryComponentRecord record = Assert.Single(result.WrittenComponents);
        Assert.Equal("perm:acme/bench-header-2x03", record.CanonicalComponentId.Value);
        Assert.Equal("bench-library", Assert.Single(record.Provenance).SourceName);
        Assert.Equal(PermanentLibraryImportSourceKind.UserLibrary, Assert.Single(record.Provenance).Kind);
        Assert.Empty(result.ReviewItems);
    }

    [Fact]
    public void DuplicateSourceImportLinksToExistingCanonicalComponent()
    {
        PermanentLibraryImportStore store = new();
        ComponentDefinition resistor = SparkFunResistor();
        PermanentLibraryImportSource firstSource = PermanentLibraryImportSource.EagleLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero));
        PermanentLibraryImportSource duplicateSource = PermanentLibraryImportSource.EagleLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero));
        store.Import(firstSource, [resistor]);

        PermanentLibraryImportResult result = store.Import(duplicateSource, [resistor]);

        Assert.Empty(result.WrittenComponents);
        PermanentLibraryImportLink link = Assert.Single(result.LinkedComponents);
        Assert.Equal("perm:sparkfun/resistor-0603", link.CanonicalComponentId.Value);
        Assert.Equal(resistor.Id, link.ImportedComponentId);
        Assert.Single(store.Components);
        Assert.Equal(2, Assert.Single(store.Components).Provenance.Count);
        Assert.Empty(result.ReviewItems);
    }

    [Fact]
    public void ConflictingImportCreatesReviewItemWithoutOverwritingVerifiedAsset()
    {
        PermanentLibraryImportStore store = new();
        ComponentDefinition verified = SparkFunResistor();
        ComponentDefinition conflict = SparkFunResistor() with
        {
            Description = "Conflicting resistor definition with changed geometry.",
            Footprints =
            [
                SparkFunResistor().Footprints.Single() with
                {
                    Pads =
                    [
                        SparkFunResistor().Footprints.Single().Pads[0],
                        SparkFunResistor().Footprints.Single().Pads[1] with { Size = new CadVector(1_200_000, 700_000) }
                    ]
                }
            ]
        };
        PermanentLibraryImportSource verifiedSource = PermanentLibraryImportSource.EagleLibrary(
            "sparkfun-eagle-libraries",
            "libraries/SparkFun-Resistors.lbr",
            new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero));
        PermanentLibraryImportSource conflictingSource = PermanentLibraryImportSource.EagleLibrary(
            "user-modified-sparkfun",
            "C:/cad/libs/SparkFun-Resistors-modified.lbr",
            new DateTimeOffset(2026, 6, 3, 13, 0, 0, TimeSpan.Zero));
        store.Import(verifiedSource, [verified]);
        store.MarkVerified(verified.Id, "Reviewed against SparkFun public Eagle library.");

        PermanentLibraryImportResult result = store.Import(conflictingSource, [conflict]);

        PermanentLibraryConflictReviewItem reviewItem = Assert.Single(result.ReviewItems);
        Assert.Equal("perm:sparkfun/resistor-0603", reviewItem.CanonicalComponentId.Value);
        Assert.Equal(conflict.Id, reviewItem.ImportedComponentId);
        Assert.Equal("user-modified-sparkfun", reviewItem.Provenance.SourceName);
        Assert.Contains("verified permanent component", reviewItem.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(verified, Assert.Single(store.Components).Component);
        Assert.Equal(PermanentLibraryVerificationState.Verified, Assert.Single(store.Components).VerificationState);
        Assert.Single(store.ReviewItems);
    }

    private static ComponentDefinition SparkFunResistor() =>
        FixtureComponent(
            id: "hawkcad:sparkfun/resistor-0603",
            displayName: "sparkfun/RESISTOR-0603",
            manufacturer: "Yageo",
            manufacturerPartNumber: "RC0603FR-0710KL",
            description: "Generic 0603 resistor",
            provenance: new ComponentProvenanceRecord(
                ComponentProvenanceKind.EagleImport,
                "sparkfun-eagle-libraries",
                "Imported from SparkFun Eagle library fixture."));

    private static ComponentDefinition UserHeader() =>
        FixtureComponent(
            id: "hawkcad:acme/bench-header-2x03",
            displayName: "acme/BENCH-HEADER-2X03",
            manufacturer: "Acme Labs",
            manufacturerPartNumber: "BH-2X03",
            description: "User bench header",
            provenance: new ComponentProvenanceRecord(
                ComponentProvenanceKind.Manual,
                "bench-library",
                "Imported from user-maintained library fixture."));

    private static ComponentDefinition FixtureComponent(
        string id,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string description,
        ComponentProvenanceRecord provenance)
    {
        string slug = id["hawkcad:".Length..];
        ComponentSymbolId symbolId = new($"{id}:symbol");
        ComponentFootprintId footprintId = new($"{id}:footprint");
        ComponentVariantId variantId = new($"{id}:variant");

        return new ComponentDefinition(
            new ComponentId(id),
            displayName,
            ComponentKind.Passive,
            manufacturer,
            manufacturerPartNumber,
            description,
            [new ComponentAttribute("Package", "0603")],
            [
                new ComponentPin(new ComponentPinId($"{id}:pin:1"), "1", "1", ComponentPinElectricalType.Passive),
                new ComponentPin(new ComponentPinId($"{id}:pin:2"), "2", "2", ComponentPinElectricalType.Passive)
            ],
            [
                new ComponentGate(
                    new ComponentGateId($"{id}:gate"),
                    "G$1",
                    symbolId,
                    [new ComponentPinId($"{id}:pin:1"), new ComponentPinId($"{id}:pin:2")])
            ],
            [
                new ComponentSymbol(
                    symbolId,
                    $"{slug}:symbol",
                    [
                        new ComponentSymbolPin(new ComponentPinId($"{id}:pin:1"), new CadPoint(-2_540_000, 0), ComponentPinOrientation.Right),
                        new ComponentSymbolPin(new ComponentPinId($"{id}:pin:2"), new CadPoint(2_540_000, 0), ComponentPinOrientation.Left)
                    ],
                    [new ComponentLine(new CadPoint(-1_270_000, 0), new CadPoint(1_270_000, 0))],
                    [new ComponentSymbolText(ComponentSymbolTextKind.Reference, ">NAME", new CadPoint(0, 1_270_000))])
            ],
            [
                new ComponentFootprint(
                    footprintId,
                    $"{slug}:footprint",
                    [
                        new ComponentFootprintPad(new ComponentPadId($"{id}:pad:1"), "1", new CadPoint(-750_000, 0), new CadVector(900_000, 700_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle),
                        new ComponentFootprintPad(new ComponentPadId($"{id}:pad:2"), "2", new CadPoint(750_000, 0), new CadVector(900_000, 700_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)
                    ],
                    [new ComponentLine(new CadPoint(-1_300_000, -500_000), new CadPoint(1_300_000, -500_000))],
                    [])
            ],
            [new ComponentVariant(variantId, "0603", footprintId, [])],
            [
                new ComponentPinPadMapping(variantId, new ComponentPinId($"{id}:pin:1"), new ComponentPadId($"{id}:pad:1")),
                new ComponentPinPadMapping(variantId, new ComponentPinId($"{id}:pin:2"), new ComponentPadId($"{id}:pad:2"))
            ],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            [provenance]);
    }
}
