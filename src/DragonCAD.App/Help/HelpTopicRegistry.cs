namespace DragonCAD.App.Help;

public sealed class HelpTopicRegistry
{
    public const string MissingTopicId = "help.missing";

    private readonly Dictionary<string, HelpTopic> topicsById;

    private HelpTopicRegistry(IReadOnlyList<HelpTopicGroup> groups, IReadOnlyList<HelpTopic> topics)
    {
        Groups = groups;
        Topics = topics;
        topicsById = topics.ToDictionary(topic => topic.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<HelpTopicGroup> Groups { get; }

    public IReadOnlyList<HelpTopic> Topics { get; }

    public static HelpTopicRegistry CreateDefault() =>
        new(
            [
                new(
                    Id: "getting-started",
                    Title: "Getting started",
                    Description: "Project setup, workspace navigation, and first-run concepts."),
                new(
                    Id: "editing",
                    Title: "Editing",
                    Description: "Schematic and board editing workflows."),
                new(
                    Id: "fabrication",
                    Title: "Fabrication",
                    Description: "Manufacturing outputs and fabrication handoff checks."),
                new(
                    Id: "marketplace",
                    Title: "Marketplace",
                    Description: "Vendor catalogs, sourcing, carts, and purchasing support.")
            ],
            [
                new(
                    Id: "getting-started.workspace",
                    GroupId: "getting-started",
                    Title: "Workspace basics",
                    Summary: "Open a DragonCAD workspace, review project state, and understand the primary editor areas.",
                    DocumentPath: "docs/help/getting-started/workspace.md",
                    Keywords: ["project", "workspace", "navigation", "startup"]),
                new(
                    Id: "editing.board-basics",
                    GroupId: "editing",
                    Title: "Board editing basics",
                    Summary: "Place components, route copper traces, inspect airwires, and keep board edits aligned with the schematic.",
                    DocumentPath: "docs/help/editing/board-basics.md",
                    Keywords: ["board", "pcb", "routing", "copper", "traces", "airwire"]),
                new(
                    Id: "fabrication.outputs",
                    GroupId: "fabrication",
                    Title: "Fabrication outputs",
                    Summary: "Review Gerber, BOM, pick-and-place, and readiness outputs before handing a board to fabrication.",
                    DocumentPath: "docs/help/fabrication/outputs.md",
                    Keywords: ["gerber", "bom", "pick and place", "manufacturing", "readiness"]),
                new(
                    Id: "marketplace.vendor-catalogs",
                    GroupId: "marketplace",
                    Title: "Vendor catalogs",
                    Summary: "Use supplier catalog syncs, marketplace filters, and vendor search results to source project parts.",
                    DocumentPath: "docs/help/marketplace/vendor-catalogs.md",
                    Keywords: ["supplier", "vendor", "catalog", "marketplace", "sourcing"])
            ]);

    public HelpTopic GetTopicOrFallback(string? topicId)
    {
        string normalizedTopicId = NormalizeTopicId(topicId);
        if (normalizedTopicId.Length > 0 && topicsById.TryGetValue(normalizedTopicId, out HelpTopic? topic))
        {
            return topic;
        }

        string missingId = normalizedTopicId.Length > 0 ? normalizedTopicId : "(blank)";
        return new(
            Id: MissingTopicId,
            GroupId: "getting-started",
            Title: "Help topic not found",
            Summary: $"No local help topic is registered for '{missingId}'.",
            DocumentPath: "docs/help/missing-topic.md",
            Keywords: []);
    }

    public IReadOnlyList<HelpTopic> Search(string? query)
    {
        string[] terms = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
        {
            return Topics;
        }

        return Topics
            .Where(topic => terms.All(term => Matches(topic, term)))
            .ToArray();
    }

    private static string NormalizeTopicId(string? topicId) => (topicId ?? string.Empty).Trim();

    private static bool Matches(HelpTopic topic, string term) =>
        Contains(topic.Id, term) ||
        Contains(topic.GroupId, term) ||
        Contains(topic.Title, term) ||
        Contains(topic.Summary, term) ||
        topic.Keywords.Any(keyword => Contains(keyword, term));

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
