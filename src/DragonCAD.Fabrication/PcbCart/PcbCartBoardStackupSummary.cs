namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartBoardStackupSummary
{
    private PcbCartBoardStackupSummary(
        int layerCount,
        string material,
        string finishedThickness,
        string outerCopperWeight)
    {
        LayerCount = layerCount;
        Material = material;
        FinishedThickness = finishedThickness;
        OuterCopperWeight = outerCopperWeight;
    }

    public int LayerCount { get; }

    public string Material { get; }

    public string FinishedThickness { get; }

    public string OuterCopperWeight { get; }

    public static PcbCartBoardStackupSummary Create(
        int layerCount,
        string material,
        string finishedThickness,
        string outerCopperWeight)
    {
        if (layerCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(layerCount), layerCount, "Layer count must be at least 1.");
        }

        return new PcbCartBoardStackupSummary(
            layerCount,
            RequireText(material, nameof(material)),
            RequireText(finishedThickness, nameof(finishedThickness)),
            RequireText(outerCopperWeight, nameof(outerCopperWeight)));
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim();
    }
}
