using DragonCAD.App.Help;

namespace DragonCAD.App.Tests.Help;

public sealed class HelpTopicRegistryTests
{
    [Fact]
    public void BuiltInRegistryExposesStableTopicIdsAndGroups()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        Assert.Equal(
            [
                "getting-started",
                "editing",
                "fabrication",
                "marketplace"
            ],
            registry.Groups.Select(group => group.Id));

        Assert.Contains(registry.Topics, topic => topic.Id == "getting-started.workspace" && topic.GroupId == "getting-started");
        Assert.Contains(registry.Topics, topic => topic.Id == "editing.board-basics" && topic.GroupId == "editing");
        Assert.Contains(registry.Topics, topic => topic.Id == "fabrication.outputs" && topic.GroupId == "fabrication");
        Assert.Contains(registry.Topics, topic => topic.Id == "marketplace.vendor-catalogs" && topic.GroupId == "marketplace");
    }

    [Fact]
    public void LookupReturnsTopicByStableIdAndFallsBackForMissingTopics()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        HelpTopic found = registry.GetTopicOrFallback("editing.board-basics");
        HelpTopic missing = registry.GetTopicOrFallback("editing.unknown-tool");

        Assert.Equal("Board editing basics", found.Title);
        Assert.Equal(HelpTopicRegistry.MissingTopicId, missing.Id);
        Assert.Equal("Help topic not found", missing.Title);
        Assert.Contains("editing.unknown-tool", missing.Summary);
    }

    [Fact]
    public void SearchMatchesTitleSummaryAndKeywordsInRegistryOrder()
    {
        HelpTopicRegistry registry = HelpTopicRegistry.CreateDefault();

        IReadOnlyList<HelpTopic> boardResults = registry.Search("routing copper traces");
        IReadOnlyList<HelpTopic> vendorResults = registry.Search("supplier catalog");

        Assert.Equal(["editing.board-basics"], boardResults.Select(topic => topic.Id));
        Assert.Equal(["marketplace.vendor-catalogs"], vendorResults.Select(topic => topic.Id));
    }
}
