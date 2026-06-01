using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Components.Catalog;

public sealed class ComponentCatalog
{
    private readonly IReadOnlyDictionary<ComponentId, ComponentDefinition> builtInDefinitions;
    private readonly IReadOnlyDictionary<ComponentId, ComponentDefinition> projectDefinitions;
    private readonly IReadOnlyDictionary<ComponentId, ComponentDefinition> userDefinitions;

    public ComponentCatalog(
        IReadOnlyList<ComponentDefinition> BuiltInDefinitions,
        IReadOnlyList<ComponentDefinition> UserDefinitions,
        IReadOnlyList<ComponentDefinition> ProjectDefinitions)
    {
        ArgumentNullException.ThrowIfNull(BuiltInDefinitions);
        ArgumentNullException.ThrowIfNull(UserDefinitions);
        ArgumentNullException.ThrowIfNull(ProjectDefinitions);

        builtInDefinitions = CreateMap(BuiltInDefinitions);
        userDefinitions = CreateMap(UserDefinitions);
        projectDefinitions = CreateMap(ProjectDefinitions);
    }

    public ComponentCatalogResolution Resolve(ComponentId componentId)
    {
        if (projectDefinitions.TryGetValue(componentId, out ComponentDefinition? projectDefinition))
        {
            return ComponentCatalogResolution.Resolved(projectDefinition, ComponentCatalogSource.Project);
        }

        if (userDefinitions.TryGetValue(componentId, out ComponentDefinition? userDefinition))
        {
            return ComponentCatalogResolution.Resolved(userDefinition, ComponentCatalogSource.User);
        }

        if (builtInDefinitions.TryGetValue(componentId, out ComponentDefinition? builtInDefinition))
        {
            return ComponentCatalogResolution.Resolved(builtInDefinition, ComponentCatalogSource.BuiltIn);
        }

        return ComponentCatalogResolution.Missing(componentId);
    }

    public IReadOnlyList<ComponentCatalogEntry> EnumerateEffectiveDefinitions()
    {
        Dictionary<ComponentId, ComponentCatalogEntry> entries = [];
        AddEntries(entries, builtInDefinitions.Values, ComponentCatalogSource.BuiltIn);
        AddEntries(entries, userDefinitions.Values, ComponentCatalogSource.User);
        AddEntries(entries, projectDefinitions.Values, ComponentCatalogSource.Project);

        return entries.Values
            .OrderBy(entry => entry.Definition.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyDictionary<ComponentId, ComponentDefinition> CreateMap(
        IReadOnlyList<ComponentDefinition> definitions) =>
        definitions.ToDictionary(definition => definition.Id);

    private static void AddEntries(
        IDictionary<ComponentId, ComponentCatalogEntry> entries,
        IEnumerable<ComponentDefinition> definitions,
        ComponentCatalogSource source)
    {
        foreach (ComponentDefinition definition in definitions)
        {
            entries[definition.Id] = new ComponentCatalogEntry(definition, source);
        }
    }
}

public sealed record ComponentCatalogEntry(
    ComponentDefinition Definition,
    ComponentCatalogSource Source);

public sealed record ComponentCatalogResolution(
    bool Found,
    ComponentDefinition? Definition,
    ComponentCatalogSource Source,
    IReadOnlyList<ComponentCatalogDiagnostic> Diagnostics)
{
    public static ComponentCatalogResolution Resolved(
        ComponentDefinition definition,
        ComponentCatalogSource source) =>
        new(true, definition, source, []);

    public static ComponentCatalogResolution Missing(ComponentId componentId) =>
        new(
            false,
            null,
            ComponentCatalogSource.None,
            [
                new ComponentCatalogDiagnostic(
                    ComponentCatalogDiagnosticSeverity.Warning,
                    ComponentCatalogDiagnosticCodes.ComponentDefinitionMissing,
                    componentId,
                    $"Component definition '{componentId}' was not found.")
            ]);
}

public sealed record ComponentCatalogDiagnostic(
    ComponentCatalogDiagnosticSeverity Severity,
    string Code,
    ComponentId ComponentId,
    string Message);

public enum ComponentCatalogDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public enum ComponentCatalogSource
{
    None,
    BuiltIn,
    User,
    Project
}

public static class ComponentCatalogDiagnosticCodes
{
    public const string ComponentDefinitionMissing = "ComponentCatalog.ComponentDefinitionMissing";
}
