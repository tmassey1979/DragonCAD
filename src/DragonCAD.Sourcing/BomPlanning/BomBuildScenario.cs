namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomBuildScenario
{
    public BomBuildScenario(string name, int buildQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Scenario name is required.", nameof(name));
        }

        if (buildQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildQuantity), buildQuantity, "Build quantity must be greater than zero.");
        }

        Name = name.Trim();
        BuildQuantity = buildQuantity;
    }

    public string Name { get; }

    public int BuildQuantity { get; }
}
