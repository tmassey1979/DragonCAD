using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Schematics.Diagnostics;

namespace DragonCAD.Core.Tests.Schematics.Diagnostics;

public sealed class SchematicElectricalRuleCheckerTests
{
    [Fact]
    public void AnalyzeReturnsNoDiagnosticsForConnectedSchematic()
    {
        SchematicDiagnosticDocument schematic = new(
            Components:
            [
                Component("R1", [Pin("1"), Pin("2")]),
                Component("D1", [Pin("A"), Pin("K")])
            ],
            Nets:
            [
                Net("net:input", [PinRef("R1", "1"), PinRef("D1", "A")], ["IN"]),
                Net("net:output", [PinRef("R1", "2"), PinRef("D1", "K")], ["OUT"])
            ]);

        SchematicElectricalRuleResult result = SchematicElectricalRuleChecker.Analyze(schematic);

        Assert.Empty(result.Diagnostics);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void AnalyzeReportsUnconnectedPinsAndSinglePinNetsInDeterministicOrder()
    {
        SchematicDiagnosticDocument schematic = new(
            Components:
            [
                Component("U1", [Pin("OUT"), Pin("GND"), Pin("IN")])
            ],
            Nets:
            [
                Net("net:solo", [PinRef("U1", "IN")], ["SENSE"])
            ]);

        SchematicElectricalRuleResult result = SchematicElectricalRuleChecker.Analyze(schematic);

        Assert.Equal(
            [
                "ERC001|Warning|pin:U1/GND|Pin 'GND' on component 'U1' is not connected to any net.",
                "ERC001|Warning|pin:U1/OUT|Pin 'OUT' on component 'U1' is not connected to any net.",
                "ERC002|Warning|net:net:solo|Net 'net:solo' only connects one pin."
            ],
            result.Diagnostics.Select(FormatDiagnostic));
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void AnalyzeReportsDuplicateNetLabelsWithObjectReferences()
    {
        SchematicDiagnosticDocument schematic = new(
            Components:
            [
                Component("R1", [Pin("1"), Pin("2")]),
                Component("R2", [Pin("1"), Pin("2")])
            ],
            Nets:
            [
                Net("net:a", [PinRef("R1", "1"), PinRef("R2", "1")], ["CTRL"]),
                Net("net:b", [PinRef("R1", "2"), PinRef("R2", "2")], ["CTRL"])
            ]);

        SchematicElectricalRuleResult result = SchematicElectricalRuleChecker.Analyze(schematic);

        SchematicDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("ERC003", diagnostic.Id);
        Assert.Equal(SchematicDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Net label 'CTRL' appears on multiple separate nets: net:a, net:b.", diagnostic.Message);
        Assert.Equal(["net:net:a", "net:net:b"], diagnostic.ObjectReferences.Select(reference => reference.Value));
        Assert.True(result.HasErrors);
    }

    private static SchematicDiagnosticComponent Component(string reference, IReadOnlyList<SchematicDiagnosticPin> pins) =>
        new(reference, pins);

    private static SchematicDiagnosticPin Pin(string name, ComponentPinElectricalType electricalType = ComponentPinElectricalType.Passive) =>
        new(name, electricalType);

    private static SchematicDiagnosticNet Net(
        string id,
        IReadOnlyList<SchematicDiagnosticPinReference> pins,
        IReadOnlyList<string> labels) =>
        new(id, pins, labels);

    private static SchematicDiagnosticPinReference PinRef(string componentReference, string pinName) =>
        new(componentReference, pinName);

    private static string FormatDiagnostic(SchematicDiagnostic diagnostic) =>
        $"{diagnostic.Id}|{diagnostic.Severity}|{string.Join(",", diagnostic.ObjectReferences.Select(reference => reference.Value))}|{diagnostic.Message}";
}
