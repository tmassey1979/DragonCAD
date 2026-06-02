using DragonCAD.Core.Components.Definitions;

namespace DragonCAD.Core.Schematics.Diagnostics;

public static class SchematicElectricalRuleChecker
{
    public static SchematicElectricalRuleResult Analyze(SchematicDiagnosticDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<SchematicDiagnostic> diagnostics = [];
        HashSet<SchematicDiagnosticPinReference> connectedPins = document.Nets
            .SelectMany(net => net.PinReferences)
            .ToHashSet();

        diagnostics.AddRange(FindUnconnectedPins(document, connectedPins));
        diagnostics.AddRange(FindSinglePinNets(document));
        diagnostics.AddRange(FindDuplicateLabels(document));

        return new SchematicElectricalRuleResult(
            diagnostics
                .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.PrimaryObjectReference.Value, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToArray());
    }

    private static IEnumerable<SchematicDiagnostic> FindUnconnectedPins(
        SchematicDiagnosticDocument document,
        HashSet<SchematicDiagnosticPinReference> connectedPins)
    {
        return document.Components
            .OrderBy(component => component.Reference, StringComparer.Ordinal)
            .SelectMany(component => component.Pins
                .Where(pin => pin.ElectricalType != ComponentPinElectricalType.NoConnect)
                .OrderBy(pin => pin.Name, StringComparer.Ordinal)
                .Select(pin => new
                {
                    Component = component,
                    Pin = pin,
                    Reference = new SchematicDiagnosticPinReference(component.Reference, pin.Name)
                }))
            .Where(entry => !connectedPins.Contains(entry.Reference))
            .Select(entry => new SchematicDiagnostic(
                SchematicDiagnosticCodes.UnconnectedPin,
                SchematicDiagnosticSeverity.Warning,
                $"Pin '{entry.Pin.Name}' on component '{entry.Component.Reference}' is not connected to any net.",
                [SchematicDiagnosticObjectReference.Pin(entry.Component.Reference, entry.Pin.Name)]));
    }

    private static IEnumerable<SchematicDiagnostic> FindSinglePinNets(SchematicDiagnosticDocument document)
    {
        return document.Nets
            .OrderBy(net => net.Id, StringComparer.Ordinal)
            .Where(net => net.PinReferences.Count < 2)
            .Select(net => new SchematicDiagnostic(
                SchematicDiagnosticCodes.SinglePinNet,
                SchematicDiagnosticSeverity.Warning,
                net.PinReferences.Count == 0
                    ? $"Net '{net.Id}' does not connect any pins."
                    : $"Net '{net.Id}' only connects one pin.",
                [SchematicDiagnosticObjectReference.Net(net.Id)]));
    }

    private static IEnumerable<SchematicDiagnostic> FindDuplicateLabels(SchematicDiagnosticDocument document)
    {
        return document.Nets
            .SelectMany(net => net.Labels.Select(label => new { Label = label, Net = net }))
            .GroupBy(entry => entry.Label, StringComparer.Ordinal)
            .Select(group => new
            {
                Label = group.Key,
                NetIds = group
                    .Select(entry => entry.Net.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray()
            })
            .Where(group => group.NetIds.Length > 1)
            .OrderBy(group => group.Label, StringComparer.Ordinal)
            .Select(group => new SchematicDiagnostic(
                SchematicDiagnosticCodes.DuplicateNetLabel,
                SchematicDiagnosticSeverity.Error,
                $"Net label '{group.Label}' appears on multiple separate nets: {string.Join(", ", group.NetIds)}.",
                group.NetIds.Select(SchematicDiagnosticObjectReference.Net).ToArray()));
    }
}

public static class SchematicDiagnosticCodes
{
    public const string UnconnectedPin = "ERC001";
    public const string SinglePinNet = "ERC002";
    public const string DuplicateNetLabel = "ERC003";
}

public sealed record SchematicElectricalRuleResult(IReadOnlyList<SchematicDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == SchematicDiagnosticSeverity.Error);
}

public sealed record SchematicDiagnostic(
    string Id,
    SchematicDiagnosticSeverity Severity,
    string Message,
    IReadOnlyList<SchematicDiagnosticObjectReference> ObjectReferences)
{
    public SchematicDiagnosticObjectReference PrimaryObjectReference =>
        ObjectReferences.Count > 0
            ? ObjectReferences[0]
            : new SchematicDiagnosticObjectReference("diagnostic:unknown");
}

public enum SchematicDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record SchematicDiagnosticObjectReference(string Value)
{
    public static SchematicDiagnosticObjectReference Pin(string componentReference, string pinName) =>
        new($"pin:{componentReference}/{pinName}");

    public static SchematicDiagnosticObjectReference Net(string netId) =>
        new($"net:{netId}");
}

public sealed record SchematicDiagnosticDocument(
    IReadOnlyList<SchematicDiagnosticComponent> Components,
    IReadOnlyList<SchematicDiagnosticNet> Nets);

public sealed record SchematicDiagnosticComponent(
    string Reference,
    IReadOnlyList<SchematicDiagnosticPin> Pins);

public sealed record SchematicDiagnosticPin(
    string Name,
    ComponentPinElectricalType ElectricalType);

public sealed record SchematicDiagnosticNet(
    string Id,
    IReadOnlyList<SchematicDiagnosticPinReference> PinReferences,
    IReadOnlyList<string> Labels);

public sealed record SchematicDiagnosticPinReference(
    string ComponentReference,
    string PinName);
