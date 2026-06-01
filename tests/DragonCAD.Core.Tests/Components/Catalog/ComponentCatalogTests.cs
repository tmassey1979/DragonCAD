using DragonCAD.Core.Components.Catalog;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Tests.Components.Catalog;

public sealed class ComponentCatalogTests
{
    [Fact]
    public void ResolveUsesProjectThenUserThenBuiltInPrecedence()
    {
        ComponentDefinition builtIn = Component("dragon:part", "Built-in");
        ComponentDefinition user = Component("dragon:part", "User");
        ComponentDefinition project = Component("dragon:part", "Project");
        ComponentCatalog catalog = new([builtIn], [user], [project]);

        ComponentCatalogResolution resolution = catalog.Resolve(new ComponentId("dragon:part"));

        Assert.True(resolution.Found);
        Assert.Equal(ComponentCatalogSource.Project, resolution.Source);
        Assert.Equal("Project", resolution.Definition?.DisplayName);
        Assert.Empty(resolution.Diagnostics);
    }

    [Fact]
    public void ResolveReportsMissingComponentWithDiagnostic()
    {
        ComponentCatalog catalog = new([], [], []);

        ComponentCatalogResolution resolution = catalog.Resolve(new ComponentId("dragon:missing"));

        Assert.False(resolution.Found);
        ComponentCatalogDiagnostic diagnostic = Assert.Single(resolution.Diagnostics);
        Assert.Equal(ComponentCatalogDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(ComponentCatalogDiagnosticCodes.ComponentDefinitionMissing, diagnostic.Code);
        Assert.Equal(new ComponentId("dragon:missing"), diagnostic.ComponentId);
    }

    [Fact]
    public void EnumerateEffectiveDefinitionsIsDeterministicAndAppliesPrecedence()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions: [Component("dragon:z", "Z"), Component("dragon:a", "Built-in A")],
            UserDefinitions: [Component("dragon:a", "User A")],
            ProjectDefinitions: [Component("dragon:m", "Project M")]);

        IReadOnlyList<ComponentCatalogEntry> entries = catalog.EnumerateEffectiveDefinitions();

        Assert.Equal(["dragon:a", "dragon:m", "dragon:z"], entries.Select(entry => entry.Definition.Id.Value));
        Assert.Equal("User A", entries[0].Definition.DisplayName);
        Assert.Equal(ComponentCatalogSource.User, entries[0].Source);
    }

    private static ComponentDefinition Component(string id, string displayName) =>
        new(
            new ComponentId(id),
            displayName,
            ComponentKind.Custom,
            Manufacturer: "",
            ManufacturerPartNumber: "",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints: [],
            Variants: [],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
}
