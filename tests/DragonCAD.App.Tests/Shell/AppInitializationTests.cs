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

    [Fact]
    public void FileMenuExposesProjectPersistenceCommands()
    {
        string mainWindowXaml = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "DragonCAD.App",
            "MainWindow.axaml"));

        Assert.Contains("New Project", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Open Project Folder", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Save", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Save As", mainWindowXaml, StringComparison.Ordinal);
        Assert.Contains("Open Recent", mainWindowXaml, StringComparison.Ordinal);
    }
}
