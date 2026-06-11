using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace DragonCAD.Import.Eagle.Tests.Parity;

public enum EagleFixtureKind
{
    Board,
    Schematic,
    Library
}

public sealed record ExpectedEagleObjectCounts(
    int Layers,
    int Symbols,
    int Footprints,
    int Nets,
    int Traces,
    int Vias,
    int Pads,
    int Text,
    int Polygons)
{
    public static ExpectedEagleObjectCounts Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record EagleParityFixture(
    string Vendor,
    EagleFixtureKind Kind,
    string RelativePath,
    ExpectedEagleObjectCounts ExpectedCounts,
    IReadOnlySet<string> KnownUnsupportedPrimitives)
{
    public string FullPath => Path.Combine(EagleFixtureRegistry.FixtureRoot, RelativePath);
}

public sealed record EagleFixtureImportSummary(
    EagleParityFixture Fixture,
    ExpectedEagleObjectCounts ObjectCounts,
    IReadOnlyDictionary<string, int> UnsupportedPrimitives,
    int WarningCount);

public sealed record EagleParityFixtureReport(
    EagleParityFixture Fixture,
    ExpectedEagleObjectCounts ObjectCounts,
    IReadOnlyDictionary<string, int> UnsupportedPrimitives,
    int WarningCount)
{
    public int KnownGapCount => UnsupportedPrimitives
        .Where(pair => Fixture.KnownUnsupportedPrimitives.Contains(pair.Key))
        .Sum(pair => pair.Value);
}

public sealed record EagleParityReport(IReadOnlyList<EagleParityFixtureReport> Fixtures);

public static class EagleFixtureRegistry
{
    public static string FixtureRoot { get; } = FindFixtureRoot();

    public static IReadOnlyList<EagleParityFixture> All { get; } =
    [
        new(
            "SparkFun",
            EagleFixtureKind.Board,
            Path.Combine("SparkFun", "qwiic-environment.brd"),
            new ExpectedEagleObjectCounts(
                Layers: 2,
                Symbols: 0,
                Footprints: 2,
                Nets: 2,
                Traces: 3,
                Vias: 1,
                Pads: 3,
                Text: 1,
                Polygons: 1),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "circle" }),
        new(
            "Adafruit",
            EagleFixtureKind.Schematic,
            Path.Combine("Adafruit", "featherwing-sensor.sch"),
            new ExpectedEagleObjectCounts(
                Layers: 2,
                Symbols: 2,
                Footprints: 0,
                Nets: 2,
                Traces: 3,
                Vias: 0,
                Pads: 0,
                Text: 2,
                Polygons: 0),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bus" }),
        new(
            "ModernDevice",
            EagleFixtureKind.Library,
            Path.Combine("ModernDevice", "wind-sensor.lbr"),
            new ExpectedEagleObjectCounts(
                Layers: 1,
                Symbols: 1,
                Footprints: 1,
                Nets: 0,
                Traces: 2,
                Vias: 0,
                Pads: 3,
                Text: 2,
                Polygons: 1),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hole" })
    ];

    private static string FindFixtureRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "tests", "DragonCAD.Import.Eagle.Tests", "Fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(Path.Combine("tests", "DragonCAD.Import.Eagle.Tests", "Fixtures"));
    }
}

public static class EagleFixtureSmokeImporter
{
    private static readonly HashSet<string> UnsupportedPrimitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bus",
        "circle",
        "hole"
    };

    public static EagleFixtureImportSummary Import(EagleParityFixture fixture)
    {
        XDocument document = XDocument.Load(fixture.FullPath, LoadOptions.PreserveWhitespace);
        IReadOnlyDictionary<string, int> unsupportedPrimitives = CountUnsupportedPrimitives(document);

        return new EagleFixtureImportSummary(
            fixture,
            CountObjects(document, fixture.Kind),
            unsupportedPrimitives,
            WarningCount: unsupportedPrimitives.Values.Sum());
    }

    private static ExpectedEagleObjectCounts CountObjects(XContainer document, EagleFixtureKind kind)
    {
        int layers = Count(document, "layer");
        int symbols = kind == EagleFixtureKind.Library
            ? Count(document, "symbol")
            : Count(document, "instance");
        int footprints = kind == EagleFixtureKind.Library
            ? Count(document, "package")
            : Count(document, "element");
        int nets = Count(document, "signal") + Count(document, "net");
        int traces = Count(document, "wire");
        int vias = Count(document, "via");
        int pads = Count(document, "pad") + Count(document, "smd");
        int text = Count(document, "text");
        int polygons = Count(document, "polygon");

        return new ExpectedEagleObjectCounts(layers, symbols, footprints, nets, traces, vias, pads, text, polygons);
    }

    private static IReadOnlyDictionary<string, int> CountUnsupportedPrimitives(XContainer document)
    {
        var counts = document
            .Descendants()
            .Select(element => element.Name.LocalName)
            .Where(UnsupportedPrimitiveNames.Contains)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new ReadOnlyDictionary<string, int>(counts);
    }

    private static int Count(XContainer document, string elementName)
    {
        return document.Descendants(elementName).Count();
    }
}

public static class EagleParityReporter
{
    public static EagleParityReport Create(IReadOnlyList<EagleParityFixture> fixtures)
    {
        return new EagleParityReport(fixtures
            .Select(CreateFixtureReport)
            .ToArray());
    }

    private static EagleParityFixtureReport CreateFixtureReport(EagleParityFixture fixture)
    {
        EagleFixtureImportSummary summary = EagleFixtureSmokeImporter.Import(fixture);

        return new EagleParityFixtureReport(
            fixture,
            summary.ObjectCounts,
            summary.UnsupportedPrimitives,
            summary.WarningCount);
    }
}
