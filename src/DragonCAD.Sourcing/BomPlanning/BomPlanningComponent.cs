namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningComponent
{
    public BomPlanningComponent(
        string designator,
        string canonicalIdentity,
        string selectedValue,
        string package,
        int quantityPerBuild,
        string selectedManufacturerPartNumber,
        bool doNotSubstitute,
        IReadOnlyList<string> alternates)
    {
        if (string.IsNullOrWhiteSpace(designator))
        {
            throw new ArgumentException("Designator is required.", nameof(designator));
        }

        if (string.IsNullOrWhiteSpace(canonicalIdentity))
        {
            throw new ArgumentException("Canonical identity is required.", nameof(canonicalIdentity));
        }

        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            throw new ArgumentException("Selected value is required.", nameof(selectedValue));
        }

        if (string.IsNullOrWhiteSpace(package))
        {
            throw new ArgumentException("Package is required.", nameof(package));
        }

        if (quantityPerBuild <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityPerBuild), quantityPerBuild, "Quantity per build must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(selectedManufacturerPartNumber))
        {
            throw new ArgumentException("Selected manufacturer part number is required.", nameof(selectedManufacturerPartNumber));
        }

        ArgumentNullException.ThrowIfNull(alternates);

        Designator = designator.Trim();
        CanonicalIdentity = canonicalIdentity.Trim();
        SelectedValue = selectedValue.Trim();
        Package = package.Trim();
        QuantityPerBuild = quantityPerBuild;
        SelectedManufacturerPartNumber = selectedManufacturerPartNumber.Trim();
        DoNotSubstitute = doNotSubstitute;
        Alternates = alternates
            .Where(alternate => !string.IsNullOrWhiteSpace(alternate))
            .Select(alternate => alternate.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string Designator { get; }

    public string CanonicalIdentity { get; }

    public string SelectedValue { get; }

    public string Package { get; }

    public int QuantityPerBuild { get; }

    public string SelectedManufacturerPartNumber { get; }

    public bool DoNotSubstitute { get; }

    public IReadOnlyList<string> Alternates { get; }
}
