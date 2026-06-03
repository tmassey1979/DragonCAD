using DragonCAD.App.BoardEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardCanvasControlTests
{
    [Fact]
    public void ViaRenderStateUsesConfiguredDiameterDrillAndLayerTransitionMetadata()
    {
        BoardVia via = new(
            "via-1",
            new CadPoint(1_000_000, 2_000_000),
            "Top",
            "Bottom",
            DiameterInternal: 1_200_000,
            DrillInternal: 500_000);

        BoardViaRenderState renderState = BoardCanvasControl.CreateViaRenderState(via);

        Assert.Equal(15.0, renderState.Radius);
        Assert.Equal(6.25, renderState.DrillRadius);
        Assert.Equal("Top", renderState.FromLayerName);
        Assert.Equal("Bottom", renderState.ToLayerName);
    }
}
