namespace DragonCAD.Fabrication.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void FabricationTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Fabrication.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
