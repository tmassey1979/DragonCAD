namespace DragonCAD.Fabrication.Outputs.Gerber;

public sealed record GerberBoardLayer(
    string Name,
    GerberBoardLayerKind Kind,
    GerberBoardSide Side,
    int? CopperLayerNumber = null)
{
    public static GerberBoardLayer TopCopper(string name) =>
        new(name, GerberBoardLayerKind.Copper, GerberBoardSide.Top, CopperLayerNumber: 1);

    public static GerberBoardLayer BottomCopper(string name) =>
        new(name, GerberBoardLayerKind.Copper, GerberBoardSide.Bottom);

    public static GerberBoardLayer InnerCopper(string name, int copperLayerNumber) =>
        new(name, GerberBoardLayerKind.Copper, GerberBoardSide.Inner, copperLayerNumber);

    public static GerberBoardLayer TopSolderMask(string name) =>
        new(name, GerberBoardLayerKind.SolderMask, GerberBoardSide.Top);

    public static GerberBoardLayer BottomSolderMask(string name) =>
        new(name, GerberBoardLayerKind.SolderMask, GerberBoardSide.Bottom);

    public static GerberBoardLayer TopSilkscreen(string name) =>
        new(name, GerberBoardLayerKind.Silkscreen, GerberBoardSide.Top);

    public static GerberBoardLayer BottomSilkscreen(string name) =>
        new(name, GerberBoardLayerKind.Silkscreen, GerberBoardSide.Bottom);
}
