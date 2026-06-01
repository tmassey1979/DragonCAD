namespace DragonCAD.Sourcing.Credentials;

public static class DragonCadCredentialEnvironment
{
    public static string Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return Get(
            name,
            processReader: key => Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process),
            userReader: key => Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User));
    }

    public static string Get(
        string name,
        Func<string, string?> processReader,
        Func<string, string?> userReader)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(processReader);
        ArgumentNullException.ThrowIfNull(userReader);

        return processReader(name) ?? userReader(name) ?? string.Empty;
    }
}
