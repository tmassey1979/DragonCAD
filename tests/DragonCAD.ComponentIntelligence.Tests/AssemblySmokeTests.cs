namespace DragonCAD.ComponentIntelligence.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ComponentIntelligenceTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.ComponentIntelligence.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
