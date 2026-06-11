namespace DragonCAD.Import.Eagle.Tests.Parity;

public sealed class EagleFixtureRegistryTests
{
    [Fact]
    public void RegistryIdentifiesRepresentativeBoardSchematicAndLibraryFixtures()
    {
        IReadOnlyList<EagleParityFixture> fixtures = EagleFixtureRegistry.All;

        Assert.Contains(fixtures, fixture => fixture.Vendor == "SparkFun" && fixture.Kind == EagleFixtureKind.Board);
        Assert.Contains(fixtures, fixture => fixture.Vendor == "Adafruit" && fixture.Kind == EagleFixtureKind.Schematic);
        Assert.Contains(fixtures, fixture => fixture.Vendor == "ModernDevice" && fixture.Kind == EagleFixtureKind.Library);
        Assert.All(fixtures, fixture => Assert.True(File.Exists(fixture.FullPath), fixture.FullPath));
        Assert.All(fixtures, fixture => Assert.NotEqual(ExpectedEagleObjectCounts.Empty, fixture.ExpectedCounts));
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void RegistryExpectedCountsMatchFixtureData(EagleParityFixture fixture)
    {
        EagleFixtureImportSummary summary = EagleFixtureSmokeImporter.Import(fixture);

        Assert.Equal(fixture.ExpectedCounts, summary.ObjectCounts);
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
}
