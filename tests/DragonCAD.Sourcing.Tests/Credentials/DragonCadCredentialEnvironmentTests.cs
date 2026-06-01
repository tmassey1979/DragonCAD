using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Tests.Credentials;

public sealed class DragonCadCredentialEnvironmentTests
{
    [Fact]
    public void GetPrefersProcessEnvironmentOverUserEnvironment()
    {
        var value = DragonCadCredentialEnvironment.Get(
            "DRAGONCAD_DIGIKEY_CLIENT_ID",
            processReader: _ => "process-client-id",
            userReader: _ => "user-client-id");

        Assert.Equal("process-client-id", value);
    }

    [Fact]
    public void GetFallsBackToUserEnvironmentWhenProcessEnvironmentIsMissing()
    {
        var value = DragonCadCredentialEnvironment.Get(
            "DRAGONCAD_DIGIKEY_CLIENT_SECRET",
            processReader: _ => null,
            userReader: _ => "user-client-secret");

        Assert.Equal("user-client-secret", value);
    }
}
