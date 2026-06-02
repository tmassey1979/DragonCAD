using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Tests.Components.Drafts;

public sealed class ComponentDraftTests
{
    [Fact]
    public void ValidDraftStoresComponentAssetsAndMappings()
    {
        ComponentDraft draft = CreateTimerDraft();

        ComponentDraftValidationResult result = ComponentDraftValidator.Validate(draft);

        Assert.True(result.IsValid);
        Assert.Equal(new ComponentId("draft:fx555"), draft.Id);
        Assert.Equal("U", draft.Package.ReferencePrefix);
        Assert.Equal("Timer Symbol", draft.Symbols[0].Name);
        Assert.Equal(new ComponentPinId("trig"), draft.Symbols[0].Pins[1].PinId);
        Assert.Equal(new CadPoint(-2_540_000, 2_540_000), draft.Symbols[0].Pins[1].Start);
        Assert.Equal(new CadPoint(-1_270_000, 2_540_000), draft.Symbols[0].Pins[1].End);
        Assert.Equal(new ComponentPadId("2"), draft.Footprints[0].Pads[1].Id);
        Assert.Equal("SOIC-8", draft.Package.Metadata[0].Value);
        Assert.Equal(new ComponentPadId("2"), draft.DeviceMappings[1].PadId);
    }

    [Fact]
    public void InvalidMappingReportsMissingAssetsAndUnmappedPins()
    {
        ComponentDraft draft = CreateTimerDraft() with
        {
            Symbols = [],
            Footprints = [],
            Package = CreateTimerDraft().Package with { ReferencePrefix = "" },
            DeviceMappings =
            [
                new ComponentDraftDeviceMapping(
                    new ComponentPinId("trig"),
                    new ComponentFootprintId("missing-footprint"),
                    new ComponentPadId("missing-pad"))
            ]
        };

        ComponentDraftValidationResult result = ComponentDraftValidator.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.MissingSymbol);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.MissingFootprint);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.UnmappedPin && diagnostic.Subject == "gnd");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.MissingReferencePrefix);
    }

    [Fact]
    public void DuplicatePinsAndPadsAreReported()
    {
        ComponentDraft draft = CreateTimerDraft() with
        {
            Pins =
            [
                new ComponentDraftPin(new ComponentPinId("gnd"), "GND", "1", ComponentDraftPinElectricalType.Power),
                new ComponentDraftPin(new ComponentPinId("gnd"), "GROUND", "1", ComponentDraftPinElectricalType.Power)
            ],
            Footprints =
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("soic-8"),
                    "SOIC-8",
                    [
                        new ComponentDraftPad(new ComponentPadId("1"), "1", new CadPoint(0, 0), new CadVector(600_000, 300_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle),
                        new ComponentDraftPad(new ComponentPadId("2"), "1", new CadPoint(1_270_000, 0), new CadVector(600_000, 300_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle)
                    ],
                    [],
                    [])
            ]
        };

        ComponentDraftValidationResult result = ComponentDraftValidator.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.DuplicatePinId && diagnostic.Subject == "gnd");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == ComponentDraftDiagnosticCode.DuplicatePadName && diagnostic.Subject == "soic-8:1");
    }

    [Fact]
    public void SaveReloadPreservesIdentityAndWritesDeterministicJson()
    {
        ComponentDraft draft = CreateTimerDraft() with
        {
            Attributes =
            [
                new ComponentDraftAttribute("tolerance", "1%"),
                new ComponentDraftAttribute("category", "timer")
            ],
            DeviceMappings =
            [
                new ComponentDraftDeviceMapping(new ComponentPinId("out"), new ComponentFootprintId("soic-8"), new ComponentPadId("3")),
                new ComponentDraftDeviceMapping(new ComponentPinId("gnd"), new ComponentFootprintId("soic-8"), new ComponentPadId("1")),
                new ComponentDraftDeviceMapping(new ComponentPinId("trig"), new ComponentFootprintId("soic-8"), new ComponentPadId("2"))
            ]
        };

        string first = ComponentDraftSerializer.Serialize(draft);
        ComponentDraft reloaded = ComponentDraftSerializer.Deserialize(first);
        string second = ComponentDraftSerializer.Serialize(reloaded);

        Assert.Equal(first, second);
        Assert.Equal(draft.Id, reloaded.Id);
        Assert.Equal(draft.Pins.Select(pin => pin.Id), reloaded.Pins.Select(pin => pin.Id));
        Assert.Equal(draft.Footprints[0].Pads.Select(pad => pad.Id), reloaded.Footprints[0].Pads.Select(pad => pad.Id));
        Assert.Equal(draft.DeviceMappings.Select(mapping => mapping.PinId), reloaded.DeviceMappings.Select(mapping => mapping.PinId));
        Assert.Contains("\"referencePrefix\": \"U\"", first, StringComparison.Ordinal);
        Assert.Contains("\"deviceMappings\"", first, StringComparison.Ordinal);
    }

    private static ComponentDraft CreateTimerDraft() =>
        new(
            new ComponentId("draft:fx555"),
            "FX555 Timer Draft",
            Package: new ComponentDraftPackage(
                "SOIC-8",
                "U",
                [new ComponentDraftAttribute("package", "SOIC-8")]),
            Attributes:
            [
                new ComponentDraftAttribute("category", "timer"),
                new ComponentDraftAttribute("voltage", "5V")
            ],
            Pins:
            [
                new ComponentDraftPin(new ComponentPinId("gnd"), "GND", "1", ComponentDraftPinElectricalType.Power),
                new ComponentDraftPin(new ComponentPinId("trig"), "TRIG", "2", ComponentDraftPinElectricalType.Input),
                new ComponentDraftPin(new ComponentPinId("out"), "OUT", "3", ComponentDraftPinElectricalType.Output)
            ],
            Symbols:
            [
                new ComponentDraftSymbol(
                    new ComponentSymbolId("timer-symbol"),
                    "Timer Symbol",
                    [
                        new ComponentDraftSymbolPin(new ComponentPinId("gnd"), new CadPoint(-2_540_000, 0), new CadPoint(-1_270_000, 0), ComponentDraftPinOrientation.Left),
                        new ComponentDraftSymbolPin(new ComponentPinId("trig"), new CadPoint(-2_540_000, 2_540_000), new CadPoint(-1_270_000, 2_540_000), ComponentDraftPinOrientation.Left),
                        new ComponentDraftSymbolPin(new ComponentPinId("out"), new CadPoint(2_540_000, 0), new CadPoint(1_270_000, 0), ComponentDraftPinOrientation.Right)
                    ],
                    [
                        new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Line, new CadPoint(-1_270_000, -2_540_000), new CadPoint(1_270_000, -2_540_000)),
                        new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Rectangle, new CadPoint(-1_270_000, -2_540_000), new CadPoint(1_270_000, 2_540_000))
                    ])
            ],
            Footprints:
            [
                new ComponentDraftFootprint(
                    new ComponentFootprintId("soic-8"),
                    "SOIC-8",
                    [
                        new ComponentDraftPad(new ComponentPadId("1"), "1", new CadPoint(0, 0), new CadVector(600_000, 300_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle),
                        new ComponentDraftPad(new ComponentPadId("2"), "2", new CadPoint(1_270_000, 0), new CadVector(600_000, 300_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle),
                        new ComponentDraftPad(new ComponentPadId("3"), "3", new CadPoint(2_540_000, 0), new CadVector(600_000, 300_000), ComponentDraftPadTechnology.SurfaceMount, ComponentDraftPadShape.Rectangle)
                    ],
                    [new ComponentDraftFootprintPrimitive(ComponentDraftPrimitiveKind.Line, new CadPoint(-500_000, -500_000), new CadPoint(3_000_000, -500_000))],
                    []),
            ],
            DeviceMappings:
            [
                new ComponentDraftDeviceMapping(new ComponentPinId("gnd"), new ComponentFootprintId("soic-8"), new ComponentPadId("1")),
                new ComponentDraftDeviceMapping(new ComponentPinId("trig"), new ComponentFootprintId("soic-8"), new ComponentPadId("2")),
                new ComponentDraftDeviceMapping(new ComponentPinId("out"), new ComponentFootprintId("soic-8"), new ComponentPadId("3"))
            ]);
}
