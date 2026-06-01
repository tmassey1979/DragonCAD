namespace DragonCAD.App.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void AppTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.App.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
