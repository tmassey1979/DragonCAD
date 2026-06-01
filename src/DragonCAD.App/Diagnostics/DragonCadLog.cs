namespace DragonCAD.App.Diagnostics;

public static class DragonCadLog
{
    private static readonly object Gate = new();
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, ".dragoncad-logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "dragoncad.log");

    public static string CurrentLogPath => LogPath;

    public static void Info(string message)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
    }
}
