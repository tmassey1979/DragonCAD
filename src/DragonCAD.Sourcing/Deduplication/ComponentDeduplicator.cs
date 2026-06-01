using System.Globalization;
using System.Text;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Deduplication;

public static class ComponentDeduplicator
{
    private static readonly char[] AliasSeparators = [',', ';', '|', '\n', '\r', '\t'];

    private static readonly IReadOnlyDictionary<string, string> ManufacturerAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TI"] = "TEXASINSTRUMENTS",
        ["TEXASINSTRUMENTS"] = "TEXASINSTRUMENTS",
        ["STMICRO"] = "STMICROELECTRONICS",
        ["STMICROELECTRONICS"] = "STMICROELECTRONICS",
    };

    public static IReadOnlyList<ComponentCandidate> GroupCandidates(IEnumerable<NormalizedCatalogListing> listings)
    {
        ArgumentNullException.ThrowIfNull(listings);

        var groups = new List<ComponentCandidateBuilder>();
        foreach (var listing in listings)
        {
            ArgumentNullException.ThrowIfNull(listing);

            var identity = ComponentListingIdentity.From(listing);
            var group = groups.FirstOrDefault(candidate => candidate.Matches(identity));
            if (group is null)
            {
                group = new ComponentCandidateBuilder();
                groups.Add(group);
            }

            group.Add(identity);
        }

        return groups
            .Select(group => group.Build())
            .OrderBy(candidate => candidate.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Manufacturer, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class ComponentCandidateBuilder
    {
        private readonly List<ComponentListingIdentity> identities = [];
        private readonly Dictionary<string, string> aliases = new(StringComparer.Ordinal);
        private readonly HashSet<string> normalizedAliases = new(StringComparer.Ordinal);

        public bool Matches(ComponentListingIdentity identity)
        {
            return identity.NormalizedAliases.Any(normalizedAliases.Contains)
                && (ManufacturerIsCompatible(identity) || SignalsAreCompatible(identity));
        }

        public void Add(ComponentListingIdentity identity)
        {
            identities.Add(identity);
            foreach (var alias in identity.Aliases)
            {
                aliases.TryAdd(NormalizeToken(alias), alias);
                normalizedAliases.Add(NormalizeToken(alias));
            }
        }

        public ComponentCandidate Build()
        {
            var representative = identities
                .OrderByDescending(identity => identity.ManufacturerPartNumber.Length)
                .ThenBy(identity => identity.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
                .First();

            return new ComponentCandidate(
                representative.ManufacturerPartNumber,
                representative.Manufacturer,
                SelectRepresentativeSignal(identity => identity.Package),
                SelectRepresentativeSignal(identity => identity.Value),
                aliases
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => pair.Value)
                    .ToArray(),
                identities.Select(identity => identity.SourceKey).OrderBy(key => key, StringComparer.Ordinal).ToArray(),
                BuildWarnings());
        }

        private bool SignalsAreCompatible(ComponentListingIdentity identity)
        {
            return SignalIsCompatible(identity.Package, identities.Select(existing => existing.Package))
                || SignalIsCompatible(identity.Value, identities.Select(existing => existing.Value));
        }

        private bool ManufacturerIsCompatible(ComponentListingIdentity identity)
        {
            return identities.Any(existing => existing.ManufacturerSignal.NormalizedValue == identity.ManufacturerSignal.NormalizedValue);
        }

        private static bool SignalIsCompatible(ComponentSignal? incoming, IEnumerable<ComponentSignal?> existingSignals)
        {
            if (incoming is null)
            {
                return false;
            }

            var existing = existingSignals.Where(signal => signal is not null).Select(signal => signal!.NormalizedValue).ToArray();
            return existing.Contains(incoming.NormalizedValue, StringComparer.Ordinal);
        }

        private string? SelectRepresentativeSignal(Func<ComponentListingIdentity, ComponentSignal?> selector)
        {
            return identities
                .Select(selector)
                .Where(signal => signal is not null)
                .Select(signal => signal!.DisplayValue)
                .FirstOrDefault();
        }

        private IReadOnlyList<ComponentMergeWarning> BuildWarnings()
        {
            return BuildWarning(
                    ComponentMergeWarningKind.ManufacturerDisagreement,
                    "Merged listings use different manufacturer names.",
                    identities.Select(identity => identity.ManufacturerSignal),
                    identities)
                .Concat(BuildWarning(
                    ComponentMergeWarningKind.PackageDisagreement,
                    "Merged listings use different package signals.",
                    identities.Select(identity => identity.Package),
                    identities))
                .Concat(BuildWarning(
                    ComponentMergeWarningKind.ValueDisagreement,
                    "Merged listings use different value signals.",
                    identities.Select(identity => identity.Value),
                    identities))
                .ToArray();
        }

        private static IReadOnlyList<ComponentMergeWarning> BuildWarning(
            ComponentMergeWarningKind kind,
            string message,
            IEnumerable<ComponentSignal?> signals,
            IReadOnlyList<ComponentListingIdentity> warningIdentities)
        {
            var values = signals
                .Where(signal => signal is not null)
                .Select(signal => signal!)
                .GroupBy(signal => signal.NormalizedValue, StringComparer.Ordinal)
                .Select(group => group.First().DisplayValue)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (values.Length <= 1)
            {
                return [];
            }

            return
            [
                new ComponentMergeWarning(
                    kind,
                    message,
                    values,
                    warningIdentities.Select(identity => identity.SourceKey).OrderBy(key => key, StringComparer.Ordinal).ToArray())
            ];
        }
    }

    private sealed record ComponentListingIdentity(
        string SourceKey,
        string ManufacturerPartNumber,
        string Manufacturer,
        ComponentSignal ManufacturerSignal,
        ComponentSignal? Package,
        ComponentSignal? Value,
        IReadOnlyList<string> Aliases,
        IReadOnlySet<string> NormalizedAliases)
    {
        public static ComponentListingIdentity From(NormalizedCatalogListing listing)
        {
            var aliases = CollectAliases(listing).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            return new ComponentListingIdentity(
                $"{listing.ProviderName}:{listing.VendorSku}",
                listing.ManufacturerPartNumber,
                listing.Manufacturer,
                new ComponentSignal(listing.Manufacturer, NormalizeManufacturer(listing.Manufacturer)),
                ReadFieldSignal(listing, "Package", "PackageType", "Footprint"),
                ReadFieldSignal(listing, "Value", "ElectricalValue", "NominalValue"),
                aliases,
                aliases.Select(NormalizeToken).Where(alias => alias.Length > 0).ToHashSet(StringComparer.Ordinal));
        }

        private static IEnumerable<string> CollectAliases(NormalizedCatalogListing listing)
        {
            yield return listing.ManufacturerPartNumber;

            foreach (var key in new[] { "Alias", "Aliases", "AlternatePartNumber", "AlternatePartNumbers" })
            {
                if (!listing.Fields.TryGetValue(key, out var value))
                {
                    continue;
                }

                foreach (var alias in value.Split(AliasSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    yield return alias;
                }
            }
        }

        private static ComponentSignal? ReadFieldSignal(NormalizedCatalogListing listing, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (listing.Fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return new ComponentSignal(value, NormalizeToken(value));
                }
            }

            return null;
        }
    }

    private sealed record ComponentSignal(string DisplayValue, string NormalizedValue);

    private static string NormalizeManufacturer(string value)
    {
        var normalized = NormalizeToken(value);
        return ManufacturerAliases.TryGetValue(normalized, out var alias)
            ? alias
            : normalized;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpper(character, CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }
}
