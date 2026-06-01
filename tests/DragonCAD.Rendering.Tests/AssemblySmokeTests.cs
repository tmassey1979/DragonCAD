namespace DragonCAD.Rendering.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void RenderingTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Rendering.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
