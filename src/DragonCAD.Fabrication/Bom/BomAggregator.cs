namespace DragonCAD.Fabrication.Bom;

public static class BomAggregator
{
    public static BomLine[] Aggregate(IEnumerable<BomComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        return components
            .Select(component => new NormalizedComponent(component))
            .GroupBy(component => component.Identity)
            .OrderBy(group => group.Key.Part, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Value, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Package, StringComparer.Ordinal)
            .Select(group => new BomLine(
                group.Key,
                group
                    .Select(component => component.Reference)
                    .OrderBy(reference => reference, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
    }

    private sealed record NormalizedComponent
    {
        public NormalizedComponent(BomComponent component)
        {
            Reference = Normalize(component.Reference);
            Identity = new BomPartIdentity(
                Normalize(component.Part),
                Normalize(component.Value),
                Normalize(component.Package));
        }

        public string Reference { get; }

        public BomPartIdentity Identity { get; }
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
