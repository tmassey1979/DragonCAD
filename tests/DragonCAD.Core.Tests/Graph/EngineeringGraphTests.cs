using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Graph;

namespace DragonCAD.Core.Tests.Graph;

public sealed class EngineeringGraphTests
{
    [Fact]
    public void GraphSerializesDeterministically()
    {
        EngineeringGraph graph = new(
            Nodes:
            [
                EngineeringGraphNode.Component(new GraphNodeId("component:u1"), new ComponentId("dragon:mcu"), "U1"),
                EngineeringGraphNode.Signal(new GraphNodeId("signal:scl"), "SCL")
            ],
            Edges:
            [
                new EngineeringGraphEdge(
                    new GraphEdgeId("edge:u1-scl"),
                    new GraphNodeId("component:u1"),
                    new GraphNodeId("signal:scl"),
                    EngineeringGraphEdgeKind.PinCarriesSignal,
                    "PA9")
            ]);

        string first = EngineeringGraphSerializer.Serialize(graph);
        EngineeringGraph reloaded = EngineeringGraphSerializer.Deserialize(first);
        string second = EngineeringGraphSerializer.Serialize(reloaded);

        Assert.Equal(first, second);
        Assert.Equal(graph, reloaded);
        Assert.Contains("\"componentId\": \"dragon:mcu\"", first, StringComparison.Ordinal);
        Assert.Contains("\"kind\": \"PinCarriesSignal\"", first, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphRejectsEdgesThatReferenceMissingNodes()
    {
        EngineeringGraph graph = new(
            Nodes: [EngineeringGraphNode.Signal(new GraphNodeId("signal:scl"), "SCL")],
            Edges:
            [
                new EngineeringGraphEdge(
                    new GraphEdgeId("bad"),
                    new GraphNodeId("component:u1"),
                    new GraphNodeId("signal:scl"),
                    EngineeringGraphEdgeKind.PinCarriesSignal,
                    "PA9")
            ]);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(graph.Validate);
        Assert.Contains("component:u1", error.Message, StringComparison.Ordinal);
    }
}
