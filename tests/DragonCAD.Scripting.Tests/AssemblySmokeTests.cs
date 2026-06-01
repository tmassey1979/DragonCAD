namespace DragonCAD.Scripting.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void ScriptingTestAssemblyLoads()
    {
        Assert.Equal("DragonCAD.Scripting.Tests", typeof(AssemblySmokeTests).Assembly.GetName().Name);
    }
}
