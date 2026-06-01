using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Graph;

public readonly record struct GraphNodeId
{
    public GraphNodeId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct GraphEdgeId
{
    public GraphEdgeId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record EngineeringGraph(
    IReadOnlyList<EngineeringGraphNode> Nodes,
    IReadOnlyList<EngineeringGraphEdge> Edges)
{
    public bool Equals(EngineeringGraph? other) =>
        other is not null &&
        EngineeringGraphSerializer.Serialize(this) == EngineeringGraphSerializer.Serialize(other);

    public override int GetHashCode() => HashCode.Combine(Nodes.Count, Edges.Count);

    public void Validate()
    {
        HashSet<string> nodeIds = Nodes.Select(node => node.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (EngineeringGraphEdge edge in Edges)
        {
            if (!nodeIds.Contains(edge.From.Value))
            {
                throw new InvalidOperationException($"Graph edge '{edge.Id}' references missing source node '{edge.From}'.");
            }

            if (!nodeIds.Contains(edge.To.Value))
            {
                throw new InvalidOperationException($"Graph edge '{edge.Id}' references missing target node '{edge.To}'.");
            }
        }
    }
}

public sealed record EngineeringGraphNode(
    GraphNodeId Id,
    EngineeringGraphNodeKind Kind,
    string Label,
    ComponentId? ComponentId = null)
{
    public static EngineeringGraphNode Component(GraphNodeId id, ComponentId componentId, string label) =>
        new(id, EngineeringGraphNodeKind.Component, label, componentId);

    public static EngineeringGraphNode Signal(GraphNodeId id, string label) =>
        new(id, EngineeringGraphNodeKind.Signal, label);
}

public enum EngineeringGraphNodeKind
{
    Component,
    Signal,
    Net,
    Pin,
    Pad,
    FirmwareSymbol,
    Document
}

public sealed record EngineeringGraphEdge(
    GraphEdgeId Id,
    GraphNodeId From,
    GraphNodeId To,
    EngineeringGraphEdgeKind Kind,
    string Label);

public enum EngineeringGraphEdgeKind
{
    Contains,
    ConnectsTo,
    PinCarriesSignal,
    Implements,
    Documents,
    DerivedFrom
}

public static class EngineeringGraphSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(EngineeringGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        graph.Validate();
        EngineeringGraphDto dto = new(
            graph.Nodes
                .OrderBy(node => node.Id.Value, StringComparer.Ordinal)
                .Select(node => new EngineeringGraphNodeDto(node.Id.Value, node.Kind, node.Label, node.ComponentId?.Value))
                .ToArray(),
            graph.Edges
                .OrderBy(edge => edge.Id.Value, StringComparer.Ordinal)
                .Select(edge => new EngineeringGraphEdgeDto(edge.Id.Value, edge.From.Value, edge.To.Value, edge.Kind, edge.Label))
                .ToArray());

        return JsonSerializer.Serialize(dto, Options);
    }

    public static EngineeringGraph Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        EngineeringGraphDto dto = JsonSerializer.Deserialize<EngineeringGraphDto>(json, Options)
            ?? throw new InvalidOperationException("Engineering graph JSON was empty.");
        EngineeringGraph graph = new(
            dto.Nodes.Select(node => new EngineeringGraphNode(
                new GraphNodeId(node.Id),
                node.Kind,
                node.Label,
                node.ComponentId is null ? null : new ComponentId(node.ComponentId))).ToArray(),
            dto.Edges.Select(edge => new EngineeringGraphEdge(
                new GraphEdgeId(edge.Id),
                new GraphNodeId(edge.From),
                new GraphNodeId(edge.To),
                edge.Kind,
                edge.Label)).ToArray());
        graph.Validate();
        return graph;
    }

    private sealed record EngineeringGraphDto(
        IReadOnlyList<EngineeringGraphNodeDto> Nodes,
        IReadOnlyList<EngineeringGraphEdgeDto> Edges);

    private sealed record EngineeringGraphNodeDto(
        string Id,
        EngineeringGraphNodeKind Kind,
        string Label,
        string? ComponentId);

    private sealed record EngineeringGraphEdgeDto(
        string Id,
        string From,
        string To,
        EngineeringGraphEdgeKind Kind,
        string Label);
}
