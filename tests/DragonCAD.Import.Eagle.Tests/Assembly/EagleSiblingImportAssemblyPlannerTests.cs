using DragonCAD.Import.Eagle.Assembly;

namespace DragonCAD.Import.Eagle.Tests.Assembly;

public sealed class EagleSiblingImportAssemblyPlannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dragoncad-eagle-assembly-tests", Guid.NewGuid().ToString("N"));

    public EagleSiblingImportAssemblyPlannerTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void PlanFromBoardDiscoversPrimarySchematicBoardAndNearbyLibraries()
    {
        string board = File("amp.brd");
        string schematic = File("amp.sch");
        string siblingLibrary = File("amp.lbr");
        string folderLibrary = File("libraries", "connectors.lbr");

        EagleSiblingImportAssemblyPlan plan = EagleSiblingImportAssemblyPlanner.Plan(board);

        Assert.Equal(schematic, plan.PrimarySchematicPath);
        Assert.Equal(board, plan.PrimaryBoardPath);
        Assert.Equal(new[] { siblingLibrary, folderLibrary }, plan.LibraryPaths);
        Assert.Empty(plan.MissingSiblingExtensions);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Severity == EagleImportAssemblyDiagnosticSeverity.Warning);
    }

    [Fact]
    public void PlanFromSchematicDiscoversMatchingBoardAndOrdersLibrariesDeterministically()
    {
        string schematic = File("radio.sch");
        string board = File("radio.brd");
        string zLibrary = File("libraries", "zeta.lbr");
        string aLibrary = File("libraries", "alpha.lbr");

        EagleSiblingImportAssemblyPlan plan = EagleSiblingImportAssemblyPlanner.Plan(schematic);

        Assert.Equal(schematic, plan.PrimarySchematicPath);
        Assert.Equal(board, plan.PrimaryBoardPath);
        Assert.Equal(new[] { aLibrary, zLibrary }, plan.LibraryPaths);
        Assert.Empty(plan.MissingSiblingExtensions);
    }

    [Fact]
    public void PlanFromLibraryOnlyUsesLibraryAsPrimaryInputAndReportsMissingDesignSiblings()
    {
        string library = File("opamp.lbr");

        EagleSiblingImportAssemblyPlan plan = EagleSiblingImportAssemblyPlanner.Plan(library);

        Assert.Null(plan.PrimarySchematicPath);
        Assert.Null(plan.PrimaryBoardPath);
        Assert.Equal(new[] { library }, plan.LibraryPaths);
        Assert.Equal(new[] { ".brd", ".sch" }, plan.MissingSiblingExtensions);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == EagleImportAssemblyDiagnosticCodes.MissingSibling);
    }

    [Fact]
    public void PlanReportsMissingSiblingWhenBoardHasNoMatchingSchematic()
    {
        string board = File("controller.brd");

        EagleSiblingImportAssemblyPlan plan = EagleSiblingImportAssemblyPlanner.Plan(board);

        Assert.Equal(board, plan.PrimaryBoardPath);
        Assert.Null(plan.PrimarySchematicPath);
        Assert.Equal(new[] { ".sch" }, plan.MissingSiblingExtensions);
        Assert.Contains(plan.Diagnostics, diagnostic =>
            diagnostic.Code == EagleImportAssemblyDiagnosticCodes.MissingSibling &&
            diagnostic.Message.Contains("controller.sch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlanReportsMultipleLibraryFoldersWithoutSelectingAmbiguousProjectLibrary()
    {
        string schematic = File("sensor.sch");
        string board = File("sensor.brd");
        string localLibrary = File("sensor.lbr");
        _ = File("libraries", "shared.lbr");
        _ = File("lib", "shared.lbr");

        EagleSiblingImportAssemblyPlan plan = EagleSiblingImportAssemblyPlanner.Plan(schematic);

        Assert.Equal(schematic, plan.PrimarySchematicPath);
        Assert.Equal(board, plan.PrimaryBoardPath);
        Assert.Equal(new[] { localLibrary }, plan.LibraryPaths);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == EagleImportAssemblyDiagnosticCodes.MultipleLibraryFolders);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string File(params string[] segments)
    {
        string path = Path.Combine(new[] { _root }.Concat(segments).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, "<eagle />");
        return Path.GetFullPath(path);
    }
}
