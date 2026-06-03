namespace DragonCAD.App.Help;

public sealed class HelpTopicRegistry
{
    public const string MissingTopicId = "help.missing";

    private readonly Dictionary<string, HelpTopic> topicsById;

    public HelpTopicRegistry(IReadOnlyList<HelpTopicGroup> groups, IReadOnlyList<HelpTopic> topics)
    {
        Groups = groups;
        Topics = topics;
        topicsById = topics
            .GroupBy(topic => topic.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<HelpTopicGroup> Groups { get; }

    public IReadOnlyList<HelpTopicGroup> Categories => Groups;

    public IReadOnlyList<HelpTopic> Topics { get; }

    public static HelpTopicRegistry CreateDefault() =>
        new(
            [
                new(
                    Id: "getting-started",
                    Title: "Getting started",
                    Description: "Project setup, workspace navigation, and first-run concepts."),
                new(
                    Id: "tutorials",
                    Title: "Tutorials",
                    Description: "Step-by-step sample walkthroughs for learning DragonCAD workflows."),
                new(
                    Id: "schematic-editing",
                    Title: "Schematic editing",
                    Description: "Symbols, nets, wires, and electrical review workflows."),
                new(
                    Id: "pcb-layout",
                    Title: "PCB layout",
                    Description: "Board placement, routing, layers, and layout review workflows."),
                new(
                    Id: "component-libraries",
                    Title: "Component libraries",
                    Description: "Library browsing, component definitions, and trusted local parts."),
                new(
                    Id: "marketplace",
                    Title: "Marketplace",
                    Description: "Vendor catalogs, sourcing, carts, and purchasing support."),
                new(
                    Id: "fabrication",
                    Title: "Fabrication",
                    Description: "Manufacturing outputs and fabrication handoff checks."),
                new(
                    Id: "project-system",
                    Title: "Project system",
                    Description: "Project folders, workspace files, and local project storage."),
                new(
                    Id: "eagle-migration",
                    Title: "EAGLE migration",
                    Description: "Importing EAGLE assets and validating migrated projects."),
                new(
                    Id: "troubleshooting",
                    Title: "Troubleshooting",
                    Description: "Common failures, diagnostics, and recovery paths."),
                new(
                    Id: "command-reference",
                    Title: "Command reference",
                    Description: "Keyboard shortcuts and command behavior.")
            ],
            [
                new(
                    Id: "getting-started.workspace",
                    GroupId: "getting-started",
                    Title: "Workspace basics",
                    Summary: "Open a DragonCAD workspace, review project state, and understand the primary editor areas.",
                    DocumentPath: "docs/help/getting-started/workspace.md",
                    RelatedTopicIds: ["tutorials.7805-regulator", "tutorials.arduino-uno", "project-system.project-folders", "pcb-layout.board-basics"],
                    WikiSlug: "Workspace-Basics",
                    Keywords: ["project", "workspace", "navigation", "startup"]),
                new(
                    Id: "tutorials.7805-regulator",
                    GroupId: "tutorials",
                    Title: "7805 regulator walkthrough",
                    Summary: "Load the 7805 sample, place a regulator, connect passives, sync board footprints, route traces, and review fabrication readiness.",
                    DocumentPath: "docs/help/tutorials/7805-regulator.md",
                    RelatedTopicIds: ["getting-started.workspace", "schematic-editing.placing-wires", "pcb-layout.board-basics", "fabrication.outputs", "command-reference.shortcuts"],
                    WikiSlug: "7805-Regulator-Walkthrough",
                    Keywords: ["tutorial", "7805", "regulator", "sample", "passives", "board sync", "fabrication"]),
                new(
                    Id: "tutorials.arduino-uno",
                    GroupId: "tutorials",
                    Title: "Arduino Uno sample walkthrough",
                    Summary: "Review the Arduino Uno sample pin counts, schematic-to-board sync, routed PCB state, and current production-clone limits.",
                    DocumentPath: "docs/help/tutorials/arduino-uno.md",
                    RelatedTopicIds: ["getting-started.workspace", "pcb-layout.board-basics", "fabrication.outputs", "command-reference.shortcuts"],
                    WikiSlug: "Arduino-Uno-Sample-Walkthrough",
                    Keywords: ["tutorial", "arduino", "uno", "pin count", "sample", "board sync", "clone"]),
                new(
                    Id: "schematic-editing.placing-wires",
                    GroupId: "schematic-editing",
                    Title: "Schematic wires and nets",
                    Summary: "Place schematic symbols, draw wires, and review net connectivity before moving into board layout.",
                    DocumentPath: "docs/help/schematic-editing/placing-wires.md",
                    RelatedTopicIds: ["pcb-layout.board-basics", "troubleshooting.common-issues"],
                    WikiSlug: "Schematic-Wires-and-Nets",
                    Keywords: ["schematic", "wire", "net", "symbol", "erc"]),
                new(
                    Id: "pcb-layout.board-basics",
                    GroupId: "pcb-layout",
                    Title: "Board editing basics",
                    Summary: "Place components, route copper traces, inspect airwires, and keep board edits aligned with the schematic.",
                    DocumentPath: "docs/help/editing/board-basics.md",
                    RelatedTopicIds: ["schematic-editing.placing-wires", "fabrication.outputs"],
                    WikiSlug: "Board-Editing-Basics",
                    Keywords: ["board", "pcb", "routing", "copper", "traces", "airwire"]),
                new(
                    Id: "component-libraries.library-basics",
                    GroupId: "component-libraries",
                    Title: "Component library basics",
                    Summary: "Browse local libraries, inspect component definitions, and promote trusted project parts for reuse.",
                    DocumentPath: "docs/help/component-libraries/library-basics.md",
                    RelatedTopicIds: ["marketplace.vendor-catalogs", "schematic-editing.placing-wires"],
                    WikiSlug: "Component-Library-Basics",
                    Keywords: ["component", "library", "definitions", "trusted"]),
                new(
                    Id: "marketplace.vendor-catalogs",
                    GroupId: "marketplace",
                    Title: "Vendor catalogs",
                    Summary: "Use supplier catalog syncs, marketplace filters, and vendor search results to source project parts.",
                    DocumentPath: "docs/help/marketplace/vendor-catalogs.md",
                    RelatedTopicIds: ["component-libraries.library-basics", "fabrication.outputs"],
                    WikiSlug: "Vendor-Catalogs",
                    Keywords: ["supplier", "vendor", "catalog", "marketplace", "sourcing"]),
                new(
                    Id: "fabrication.outputs",
                    GroupId: "fabrication",
                    Title: "Fabrication outputs",
                    Summary: "Review Gerber, BOM, pick-and-place, and readiness outputs before handing a board to fabrication.",
                    DocumentPath: "docs/help/fabrication/outputs.md",
                    RelatedTopicIds: ["pcb-layout.board-basics", "marketplace.vendor-catalogs"],
                    WikiSlug: "Fabrication-Outputs",
                    Keywords: ["gerber", "bom", "pick and place", "manufacturing", "readiness"]),
                new(
                    Id: "project-system.project-folders",
                    GroupId: "project-system",
                    Title: "Project folders",
                    Summary: "Understand the files DragonCAD stores in a project folder and how workspace state is preserved.",
                    DocumentPath: "docs/help/project-system/project-folders.md",
                    RelatedTopicIds: ["getting-started.workspace", "troubleshooting.common-issues"],
                    WikiSlug: "Project-Folders",
                    Keywords: ["project", "folder", "workspace", "storage"]),
                new(
                    Id: "eagle-migration.importing-eagle-projects",
                    GroupId: "eagle-migration",
                    Title: "Importing EAGLE projects",
                    Summary: "Import EAGLE schematic and board files, then review migrated libraries, nets, and manufacturing outputs.",
                    DocumentPath: "docs/help/eagle-migration/importing-eagle-projects.md",
                    RelatedTopicIds: ["component-libraries.library-basics", "pcb-layout.board-basics"],
                    WikiSlug: "Importing-EAGLE-Projects",
                    Keywords: ["eagle", "migration", "import", "schematic", "board"]),
                new(
                    Id: "troubleshooting.common-issues",
                    GroupId: "troubleshooting",
                    Title: "Common troubleshooting",
                    Summary: "Resolve common workspace, editor, library, marketplace, and fabrication issues with targeted checks.",
                    DocumentPath: "docs/help/troubleshooting/common-issues.md",
                    RelatedTopicIds: ["getting-started.workspace", "command-reference.shortcuts"],
                    WikiSlug: "Common-Troubleshooting",
                    Keywords: ["troubleshooting", "diagnostics", "errors", "recovery"]),
                new(
                    Id: "command-reference.shortcuts",
                    GroupId: "command-reference",
                    Title: "Command and shortcut reference",
                    Summary: "Review common DragonCAD commands, shortcut conventions, and command availability by workspace context.",
                    DocumentPath: "docs/help/command-reference/shortcuts.md",
                    RelatedTopicIds: ["getting-started.workspace", "troubleshooting.common-issues", "command-reference.catalog"],
                    WikiSlug: "Command-and-Shortcut-Reference",
                    Keywords: ["command", "shortcut", "keyboard", "reference"]),
                new(
                    Id: "command-reference.catalog",
                    GroupId: "command-reference",
                    Title: "Tool and shortcut catalog",
                    Summary: "Review DragonCAD tool commands, keyboard shortcuts, aliases, implementation status, and tracked gaps.",
                    DocumentPath: "docs/help/reference/command-catalog.md",
                    RelatedTopicIds: ["command-reference.shortcuts", "getting-started.workspace", "troubleshooting.common-issues"],
                    WikiSlug: "Tool-and-Shortcut-Catalog",
                    Keywords: ["tool", "command", "shortcut", "alias", "keyboard", "catalog", "status"])
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
            RelatedTopicIds: [],
            WikiSlug: "Help-Topic-Not-Found",
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
