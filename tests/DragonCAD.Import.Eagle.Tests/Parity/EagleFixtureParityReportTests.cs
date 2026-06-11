namespace DragonCAD.Import.Eagle.Tests.Parity;

public sealed class EagleFixtureParityReportTests
{
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void SmokeImportVerifiesExpectedPrimitiveFamiliesWhereFixtureDataExists(EagleParityFixture fixture)
    {
        EagleFixtureImportSummary summary = EagleFixtureSmokeImporter.Import(fixture);

        AssertCountsForFixtureKind(fixture.Kind, summary.ObjectCounts);
    }

    [Fact]
    public void ParityReportRecordsUnsupportedPrimitivesAndWarningsWithoutFailingKnownGaps()
    {
        EagleParityReport report = EagleParityReporter.Create(EagleFixtureRegistry.All);

        Assert.Equal(EagleFixtureRegistry.All.Count, report.Fixtures.Count);
        Assert.Contains(report.Fixtures, fixture => fixture.UnsupportedPrimitives.ContainsKey("circle"));
        Assert.Contains(report.Fixtures, fixture => fixture.WarningCount > 0);
        Assert.All(report.Fixtures, fixture => Assert.True(fixture.KnownGapCount >= fixture.UnsupportedPrimitives.Count));
    }

    public static TheoryData<EagleParityFixture> Fixtures()
    {
        var data = new TheoryData<EagleParityFixture>();

        foreach (EagleParityFixture fixture in EagleFixtureRegistry.All)
        {
            data.Add(fixture);
        }

        return data;
    }

    private static void AssertCountsForFixtureKind(EagleFixtureKind kind, ExpectedEagleObjectCounts counts)
    {
        Assert.True(counts.Layers > 0);
        Assert.True(counts.Text > 0);

        if (kind == EagleFixtureKind.Board)
        {
            Assert.True(counts.Footprints > 0);
            Assert.True(counts.Nets > 0);
            Assert.True(counts.Traces > 0);
            Assert.True(counts.Vias > 0);
            Assert.True(counts.Pads > 0);
            Assert.True(counts.Polygons > 0);
        }

        if (kind == EagleFixtureKind.Schematic)
        {
            Assert.True(counts.Symbols > 0);
            Assert.True(counts.Nets > 0);
            Assert.True(counts.Traces > 0);
        }

        if (kind == EagleFixtureKind.Library)
        {
            Assert.True(counts.Symbols > 0);
            Assert.True(counts.Footprints > 0);
            Assert.True(counts.Pads > 0);
            Assert.True(counts.Polygons > 0);
        }
    }
}
