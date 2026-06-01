using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Tests.Components.Definitions;

public sealed class ComponentDefinitionSerializerTests
{
    [Fact]
    public void ComponentDefinitionRoundTripsWithDeterministicJson()
    {
        ComponentDefinition component = CreateTimer();

        string first = ComponentDefinitionSerializer.Serialize(component);
        ComponentDefinition reloaded = ComponentDefinitionSerializer.Deserialize(first);
        string second = ComponentDefinitionSerializer.Serialize(reloaded);

        Assert.Equal(component, reloaded);
        Assert.Equal(first, second);
        Assert.Contains("\"id\": \"dragon:fx555\"", first, StringComparison.Ordinal);
        Assert.Contains("\"symbols\"", first, StringComparison.Ordinal);
        Assert.Contains("\"footprints\"", first, StringComparison.Ordinal);
        Assert.Contains("\"datasheets\"", first, StringComparison.Ordinal);
        Assert.Contains("\"sourcing\"", first, StringComparison.Ordinal);
        Assert.Contains("\"packageModels3d\"", first, StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentDefinitionRejectsMappingsThatReferenceMissingAssets()
    {
        ComponentDefinition component = CreateTimer() with
        {
            PinPadMappings =
            [
                new ComponentPinPadMapping(
                    new ComponentVariantId("missing"),
                    new ComponentPinId("trig"),
                    new ComponentPadId("1"))
            ]
        };

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(component.Validate);
        Assert.Contains("variant", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ComponentDefinition CreateTimer() =>
        new(
            new ComponentId("dragon:fx555"),
            "FX555 Timer",
            ComponentKind.IntegratedCircuit,
            Manufacturer: "Fixture Semi",
            ManufacturerPartNumber: "FX555DR",
            Description: "Timer fixture component.",
            Attributes:
            [
                new ComponentAttribute("category", "timer"),
                new ComponentAttribute("prefix", "U")
            ],
            Pins:
            [
                new ComponentPin(new ComponentPinId("gnd"), "GND", "1", ComponentPinElectricalType.Power),
                new ComponentPin(new ComponentPinId("trig"), "TRIG", "2", ComponentPinElectricalType.Input),
                new ComponentPin(new ComponentPinId("out"), "OUT", "3", ComponentPinElectricalType.Output)
            ],
            Gates:
            [
                new ComponentGate(
                    new ComponentGateId("main"),
                    "Main",
                    new ComponentSymbolId("timer-symbol"),
                    [new ComponentPinId("gnd"), new ComponentPinId("trig"), new ComponentPinId("out")])
            ],
            Symbols:
            [
                new ComponentSymbol(
                    new ComponentSymbolId("timer-symbol"),
                    "Timer Symbol",
                    [
                        new ComponentSymbolPin(new ComponentPinId("gnd"), new CadPoint(-2_540_000, 0), ComponentPinOrientation.Left),
                        new ComponentSymbolPin(new ComponentPinId("trig"), new CadPoint(-2_540_000, 2_540_000), ComponentPinOrientation.Left),
                        new ComponentSymbolPin(new ComponentPinId("out"), new CadPoint(2_540_000, 0), ComponentPinOrientation.Right)
                    ],
                    [new ComponentLine(new CadPoint(-1_270_000, -2_540_000), new CadPoint(1_270_000, -2_540_000))],
                    [new ComponentSymbolText(ComponentSymbolTextKind.Reference, ">NAME", new CadPoint(0, -3_000_000))])
            ],
            Footprints:
            [
                new ComponentFootprint(
                    new ComponentFootprintId("soic-8"),
                    "SOIC-8",
                    [
                        new ComponentFootprintPad(new ComponentPadId("1"), "1", new CadPoint(0, 0), new CadVector(600_000, 300_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle),
                        new ComponentFootprintPad(new ComponentPadId("2"), "2", new CadPoint(1_270_000, 0), new CadVector(600_000, 300_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle),
                        new ComponentFootprintPad(new ComponentPadId("3"), "3", new CadPoint(2_540_000, 0), new CadVector(600_000, 300_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)
                    ],
                    [new ComponentLine(new CadPoint(-500_000, -500_000), new CadPoint(3_000_000, -500_000))],
                    [])
            ],
            Variants:
            [
                new ComponentVariant(new ComponentVariantId("soic"), "SOIC", new ComponentFootprintId("soic-8"), [new ComponentAttribute("package", "SOIC-8")])
            ],
            PinPadMappings:
            [
                new ComponentPinPadMapping(new ComponentVariantId("soic"), new ComponentPinId("gnd"), new ComponentPadId("1")),
                new ComponentPinPadMapping(new ComponentVariantId("soic"), new ComponentPinId("trig"), new ComponentPadId("2")),
                new ComponentPinPadMapping(new ComponentVariantId("soic"), new ComponentPinId("out"), new ComponentPadId("3"))
            ],
            Datasheets:
            [
                new ComponentDatasheetReference("FX555", ComponentDatasheetLocationKind.Url, "https://example.invalid/fx555.pdf", "Fixture Semi", "FX555DR")
            ],
            Sourcing:
            [
                new ComponentSourcingReference("digikey", "FX555DRCT-ND", "Fixture Semi", "FX555DR")
            ],
            PackageModels3D:
            [
                new ComponentPackageModel3D("soic-step", ComponentPackageModel3DFormat.Step, "models/soic-8.step", new ComponentVariantId("soic"))
            ],
            Provenance:
            [
                new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "test", "created by unit test")
            ]);
}
