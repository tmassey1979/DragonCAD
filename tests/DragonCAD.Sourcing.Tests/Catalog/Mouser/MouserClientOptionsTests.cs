using DragonCAD.Sourcing.Catalog.Mouser;

namespace DragonCAD.Sourcing.Tests.Catalog.Mouser;

public sealed class MouserClientOptionsTests
{
    [Fact]
    public void SearchOptionsCanBeLoadedFromDragonCadEnvironmentName()
    {
        var options = MouserSearchClientOptions.FromEnvironment(
            name => name == "DRAGONCAD_MOUSER_API_KEY" ? "mouser-key" : null);

        Assert.Equal("mouser-key", options.ApiKey);
    }
}
