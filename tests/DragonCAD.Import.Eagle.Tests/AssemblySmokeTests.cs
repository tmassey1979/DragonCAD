namespace DragonCAD.Import.Eagle.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void EagleImportTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Import.Eagle.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
