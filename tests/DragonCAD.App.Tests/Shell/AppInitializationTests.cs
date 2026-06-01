using Avalonia;

namespace DragonCAD.App.Tests.Shell;

public sealed class AppInitializationTests
{
    [Fact]
    public void InitializeConfiguresThemeResourcesWithoutCompiledAppXaml()
    {
        App app = new();

        app.Initialize();

        Assert.True(app.Resources.ContainsKey("DragonShellBackground"));
        Assert.True(app.Resources.ContainsKey("DragonAccent"));
    }
}
