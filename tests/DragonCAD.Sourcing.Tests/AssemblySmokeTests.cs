namespace DragonCAD.Sourcing.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void SourcingTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Sourcing.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
