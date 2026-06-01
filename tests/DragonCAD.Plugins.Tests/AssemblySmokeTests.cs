namespace DragonCAD.Plugins.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void PluginsTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Plugins.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
