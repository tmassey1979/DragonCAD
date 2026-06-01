using System.Globalization;
using System.Text;

namespace DragonCAD.Sourcing.Catalog.Matching;

public static class VendorCatalogMatchScorer
{
    private const int ManufacturerPartNumberWeight = 50;
    private const int ManufacturerWeight = 25;
    private const int PackageWeight = 15;
    private const int DatasheetUrlWeight = 10;

    private static readonly IReadOnlyDictionary<string, string> ManufacturerAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["STMICRO"] = "STMICROELECTRONICS",
        ["TEXASINSTRUMENTS"] = "TEXASINSTRUMENTS",
        ["TI"] = "TEXASINSTRUMENTS",
    };

    public static VendorCatalogMatchScore Score(VendorCatalogMatchCandidate query, VendorCatalogMatchCandidate listing)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(listing);

        var manufacturerPartNumberMatches = NormalizeIdentity(query.ManufacturerPartNumber) == NormalizeIdentity(listing.ManufacturerPartNumber);
        if (!manufacturerPartNumberMatches)
        {
            return new VendorCatalogMatchScore(
                VendorCatalogMatchQuality.NoMatch,
                Score: 0,
                ManufacturerPartNumberMatches: false,
                ManufacturerMatches: false,
                PackageMatches: false,
                DatasheetUrlMatches: false);
        }

        var manufacturerMatches = NormalizeManufacturer(query.Manufacturer) == NormalizeManufacturer(listing.Manufacturer);
        var packageMatches = NormalizeToken(query.Package) == NormalizeToken(listing.Package);
        var datasheetUrlMatches = NormalizeUrl(query.DatasheetUrl) == NormalizeUrl(listing.DatasheetUrl)
            && query.DatasheetUrl is not null
            && listing.DatasheetUrl is not null;

        var score = ManufacturerPartNumberWeight
            + (manufacturerMatches ? ManufacturerWeight : 0)
            + (packageMatches ? PackageWeight : 0)
            + (datasheetUrlMatches ? DatasheetUrlWeight : 0);

        return new VendorCatalogMatchScore(
            DetermineQuality(score, manufacturerMatches, packageMatches, datasheetUrlMatches),
            score,
            ManufacturerPartNumberMatches: true,
            manufacturerMatches,
            packageMatches,
            datasheetUrlMatches);
    }

    private static VendorCatalogMatchQuality DetermineQuality(
        int score,
        bool manufacturerMatches,
        bool packageMatches,
        bool datasheetUrlMatches)
    {
        if (score == ManufacturerPartNumberWeight + ManufacturerWeight + PackageWeight + DatasheetUrlWeight)
        {
            return VendorCatalogMatchQuality.Exact;
        }

        if (manufacturerMatches && (packageMatches || datasheetUrlMatches || score >= ManufacturerPartNumberWeight + ManufacturerWeight))
        {
            return VendorCatalogMatchQuality.Duplicate;
        }

        return VendorCatalogMatchQuality.Weak;
    }

    private static string NormalizeManufacturer(string value)
    {
        var normalized = NormalizeToken(value);
        return ManufacturerAliases.TryGetValue(normalized, out var alias)
            ? alias
            : normalized;
    }

    private static string NormalizeIdentity(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(string.Empty, value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
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

    private static string NormalizeUrl(Uri? url)
    {
        if (url is null)
        {
            return string.Empty;
        }

        return new UriBuilder(url)
        {
            Query = string.Empty,
            Fragment = string.Empty,
        }.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToUpperInvariant();
    }
}
