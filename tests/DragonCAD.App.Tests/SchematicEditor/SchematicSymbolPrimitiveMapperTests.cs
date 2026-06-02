using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.SchematicEditor;

public sealed class SchematicSymbolPrimitiveMapperTests
{
    [Fact]
    public void FromDefinitionPreservesPrimitiveCoordinatesAndStyleHints()
    {
        ComponentDefinition definition = FixtureComponent(
            "fidelity",
            [
                ComponentSymbolPrimitive.Line(new CadPoint(-3_000_000, 0), new CadPoint(-1_000_000, 0), "94", "green"),
                ComponentSymbolPrimitive.Arc(new CadPoint(0, 0), 1_500_000, 0, 90, "94", "green"),
                ComponentSymbolPrimitive.Rectangle(new CadRectangle(-1_000_000, -500_000, 1_000_000, 500_000), "94", "blue"),
                ComponentSymbolPrimitive.Circle(new CadPoint(2_000_000, 0), 750_000, "94", "green"),
                ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Reference, ">NAME", new CadPoint(0, -1_500_000), "95", "brown")
            ],
            [
                new ComponentSymbolPin(new ComponentPinId("in"), new CadPoint(-4_000_000, 0), ComponentPinOrientation.Right)
            ]);

        SchematicSymbolRenderPreview preview = SchematicSymbolPrimitiveMapper.FromDefinition(definition);

        Assert.Collection(
            preview.Primitives,
            primitive =>
            {
                SchematicSymbolLine line = Assert.IsType<SchematicSymbolLine>(primitive);
                Assert.Equal(new CadPoint(-3_000_000, 0), line.Start);
                Assert.Equal(new CadPoint(-1_000_000, 0), line.End);
                Assert.Equal("94", line.Layer);
                Assert.Equal("green", line.Color);
            },
            primitive =>
            {
                SchematicSymbolArc arc = Assert.IsType<SchematicSymbolArc>(primitive);
                Assert.Equal(new CadPoint(0, 0), arc.Center);
                Assert.Equal(1_500_000, arc.Radius);
                Assert.Equal(0, arc.StartAngleDegrees);
                Assert.Equal(90, arc.SweepAngleDegrees);
            },
            primitive => Assert.IsType<SchematicSymbolRectangle>(primitive),
            primitive => Assert.IsType<SchematicSymbolCircle>(primitive),
            primitive =>
            {
                SchematicSymbolText text = Assert.IsType<SchematicSymbolText>(primitive);
                Assert.Equal(ComponentSymbolTextKind.Reference, text.Kind);
                Assert.Equal(">NAME", text.Value);
                Assert.Equal(new CadPoint(0, -1_500_000), text.Position);
                Assert.Equal("95", text.Layer);
                Assert.Equal("brown", text.Color);
            });
    }

    [Fact]
    public void FromDefinitionBoundsIncludeEveryPrimitiveAndPinLead()
    {
        ComponentDefinition definition = FixtureComponent(
            "bounds",
            [
                ComponentSymbolPrimitive.Line(new CadPoint(-5_000_000, -2_000_000), new CadPoint(-4_000_000, -1_000_000), "94", "green"),
                ComponentSymbolPrimitive.Arc(new CadPoint(1_000_000, 1_000_000), 2_000_000, 180, 180, "94", "green"),
                ComponentSymbolPrimitive.Rectangle(new CadRectangle(-1_000_000, -3_000_000, 1_000_000, -2_000_000), "94", "green"),
                ComponentSymbolPrimitive.Circle(new CadPoint(4_000_000, 3_000_000), 1_000_000, "94", "green"),
                ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Value, ">VALUE", new CadPoint(0, 4_500_000), "95", "brown")
            ],
            [
                new ComponentSymbolPin(new ComponentPinId("out"), new CadPoint(6_000_000, 0), ComponentPinOrientation.Left)
            ]);

        SchematicSymbolRenderPreview preview = SchematicSymbolPrimitiveMapper.FromDefinition(definition);

        Assert.Equal(new CadRectangle(-5_000_000, -3_000_000, 6_000_000, 4_500_000), preview.Bounds);
    }

    [Theory]
    [MemberData(nameof(FixtureShapes))]
    public void FixtureSymbolsExposeRecognizablePrimitiveMix(
        string fixtureName,
        ComponentDefinition definition,
        int expectedPinCount,
        Type[] expectedPrimitiveTypes)
    {
        SchematicSymbolRenderPreview preview = SchematicSymbolPrimitiveMapper.FromDefinition(definition);

        Assert.True(preview.Pins.Count == expectedPinCount, $"{fixtureName} expected {expectedPinCount} pins but found {preview.Pins.Count}.");
        foreach (Type primitiveType in expectedPrimitiveTypes)
        {
            Assert.Contains(preview.Primitives, primitive => primitive.GetType() == primitiveType);
        }
    }

    public static TheoryData<string, ComponentDefinition, int, Type[]> FixtureShapes => new()
    {
        {
            "resistor",
            FixtureComponent(
                "resistor",
                [
                    ComponentSymbolPrimitive.Line(new CadPoint(-2_500_000, 0), new CadPoint(-1_500_000, 0), "94", "green"),
                    ComponentSymbolPrimitive.Rectangle(new CadRectangle(-1_500_000, -500_000, 1_500_000, 500_000), "94", "green"),
                    ComponentSymbolPrimitive.Line(new CadPoint(1_500_000, 0), new CadPoint(2_500_000, 0), "94", "green"),
                    ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Reference, ">NAME", new CadPoint(0, -1_000_000), "95", "brown")
                ],
                [
                    new ComponentSymbolPin(new ComponentPinId("1"), new CadPoint(-3_000_000, 0), ComponentPinOrientation.Right),
                    new ComponentSymbolPin(new ComponentPinId("2"), new CadPoint(3_000_000, 0), ComponentPinOrientation.Left)
                ]),
            2,
            [typeof(SchematicSymbolLine), typeof(SchematicSymbolRectangle), typeof(SchematicSymbolText)]
        },
        {
            "capacitor",
            FixtureComponent(
                "capacitor",
                [
                    ComponentSymbolPrimitive.Line(new CadPoint(-700_000, -1_000_000), new CadPoint(-700_000, 1_000_000), "94", "green"),
                    ComponentSymbolPrimitive.Line(new CadPoint(700_000, -1_000_000), new CadPoint(700_000, 1_000_000), "94", "green")
                ],
                [
                    new ComponentSymbolPin(new ComponentPinId("p"), new CadPoint(-3_000_000, 0), ComponentPinOrientation.Right),
                    new ComponentSymbolPin(new ComponentPinId("n"), new CadPoint(3_000_000, 0), ComponentPinOrientation.Left)
                ]),
            2,
            [typeof(SchematicSymbolLine)]
        },
        {
            "regulator",
            FixtureComponent(
                "regulator",
                [
                    ComponentSymbolPrimitive.Rectangle(new CadRectangle(-2_000_000, -1_500_000, 2_000_000, 1_500_000), "94", "green"),
                    ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Value, "7805", new CadPoint(0, 0), "95", "brown")
                ],
                [
                    new ComponentSymbolPin(new ComponentPinId("in"), new CadPoint(-4_000_000, 0), ComponentPinOrientation.Right),
                    new ComponentSymbolPin(new ComponentPinId("gnd"), new CadPoint(0, 3_000_000), ComponentPinOrientation.Up),
                    new ComponentSymbolPin(new ComponentPinId("out"), new CadPoint(4_000_000, 0), ComponentPinOrientation.Left)
                ]),
            3,
            [typeof(SchematicSymbolRectangle), typeof(SchematicSymbolText)]
        },
        {
            "mcu",
            FixtureComponent(
                "mcu",
                [
                    ComponentSymbolPrimitive.Rectangle(new CadRectangle(-4_000_000, -5_000_000, 4_000_000, 5_000_000), "94", "green"),
                    ComponentSymbolPrimitive.Circle(new CadPoint(-3_200_000, -4_200_000), 300_000, "94", "green")
                ],
                Enumerable.Range(1, 12)
                    .Select(index => new ComponentSymbolPin(new ComponentPinId($"p{index}"), new CadPoint(index <= 6 ? -6_000_000 : 6_000_000, (index % 6) * 1_000_000), index <= 6 ? ComponentPinOrientation.Right : ComponentPinOrientation.Left))
                    .ToArray()),
            12,
            [typeof(SchematicSymbolRectangle), typeof(SchematicSymbolCircle)]
        },
        {
            "connector",
            FixtureComponent(
                "connector",
                [
                    ComponentSymbolPrimitive.Rectangle(new CadRectangle(-1_000_000, -3_000_000, 1_000_000, 3_000_000), "94", "green"),
                    ComponentSymbolPrimitive.Circle(new CadPoint(0, -2_000_000), 250_000, "94", "green")
                ],
                Enumerable.Range(1, 4)
                    .Select(index => new ComponentSymbolPin(new ComponentPinId(index.ToString(System.Globalization.CultureInfo.InvariantCulture)), new CadPoint(-3_000_000, -2_000_000 + (index * 1_000_000)), ComponentPinOrientation.Right))
                    .ToArray()),
            4,
            [typeof(SchematicSymbolRectangle), typeof(SchematicSymbolCircle)]
        },
        {
            "op-amp",
            FixtureComponent(
                "opamp",
                [
                    ComponentSymbolPrimitive.Line(new CadPoint(-2_000_000, -2_000_000), new CadPoint(2_000_000, 0), "94", "green"),
                    ComponentSymbolPrimitive.Line(new CadPoint(2_000_000, 0), new CadPoint(-2_000_000, 2_000_000), "94", "green"),
                    ComponentSymbolPrimitive.Line(new CadPoint(-2_000_000, 2_000_000), new CadPoint(-2_000_000, -2_000_000), "94", "green"),
                    ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Custom, "+", new CadPoint(-1_300_000, -700_000), "95", "brown"),
                    ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Custom, "-", new CadPoint(-1_300_000, 700_000), "95", "brown")
                ],
                [
                    new ComponentSymbolPin(new ComponentPinId("plus"), new CadPoint(-4_000_000, -1_000_000), ComponentPinOrientation.Right),
                    new ComponentSymbolPin(new ComponentPinId("minus"), new CadPoint(-4_000_000, 1_000_000), ComponentPinOrientation.Right),
                    new ComponentSymbolPin(new ComponentPinId("out"), new CadPoint(4_000_000, 0), ComponentPinOrientation.Left)
                ]),
            3,
            [typeof(SchematicSymbolLine), typeof(SchematicSymbolText)]
        }
    };

    private static ComponentDefinition FixtureComponent(
        string id,
        IReadOnlyList<ComponentSymbolPrimitive> primitives,
        IReadOnlyList<ComponentSymbolPin> symbolPins)
    {
        ComponentPin[] pins = symbolPins
            .Select(pin => new ComponentPin(pin.PinId, pin.PinId.Value.ToUpperInvariant(), pin.PinId.Value, ComponentPinElectricalType.Passive))
            .ToArray();
        ComponentSymbolId symbolId = new($"{id}:symbol");

        return new ComponentDefinition(
            new ComponentId($"hawkcad:{id}"),
            id,
            ComponentKind.Custom,
            "",
            "",
            "",
            [],
            pins,
            [new ComponentGate(new ComponentGateId($"{id}:gate"), "A", symbolId, pins.Select(pin => pin.Id).ToArray())],
            [
                new ComponentSymbol(
                    symbolId,
                    id,
                    symbolPins,
                    primitives.OfType<ComponentSymbolLinePrimitive>().Select(primitive => new ComponentLine(primitive.Start, primitive.End)).ToArray(),
                    [])
                {
                    Primitives = primitives
                }
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }
}
