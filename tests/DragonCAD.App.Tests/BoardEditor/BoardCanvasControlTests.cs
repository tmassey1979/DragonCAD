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

    [Fact]
    public void ThroughHolePadRenderStateIncludesCopperRingAndDrillHole()
    {
        BoardEditorViewModel board = new();
        BoardComponentInstance component = new(
            "sync-j1",
            "J1",
            "fixture:header",
            "Header",
            Position: default,
            FootprintPreview: DragonCAD.App.ComponentManager.ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Pad("1", default, new CadVector(1_400_000, 1_400_000), "Round", 800_000, "Top")
            ]);
        BoardFootprintPadPrimitive pad = Assert.IsType<BoardFootprintPadPrimitive>(component.FootprintPrimitives[0]);

        BoardFootprintPrimitiveRenderState renderState = BoardCanvasControl.CreateFootprintPrimitiveRenderState(board, component, pad);

        Assert.Equal("Top", renderState.LayerName);
        Assert.Equal(17.5, renderState.PadRadiusX);
        Assert.Equal(17.5, renderState.PadRadiusY);
        Assert.Equal(10.0, renderState.DrillRadius);
        Assert.True(renderState.HasCopperRing);
    }

    [Fact]
    public void MirroredSmdPadRenderStateUsesOppositeCopperLayer()
    {
        BoardEditorViewModel board = new();
        BoardComponentInstance component = new(
            "sync-u1",
            "U1",
            "fixture:soic",
            "SOIC",
            Position: default,
            FootprintPreview: DragonCAD.App.ComponentManager.ComponentFootprintPreview.Empty,
            IsMirrored: true,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Smd("1", default, new CadVector(600_000, 1_500_000), "Rectangle", "Top")
            ]);
        BoardFootprintSmdPrimitive smd = Assert.IsType<BoardFootprintSmdPrimitive>(component.FootprintPrimitives[0]);

        BoardFootprintPrimitiveRenderState renderState = BoardCanvasControl.CreateFootprintPrimitiveRenderState(board, component, smd);

        Assert.Equal("Bottom", renderState.LayerName);
        Assert.False(renderState.HasCopperRing);
        Assert.Equal(0, renderState.DrillRadius);
    }
}
