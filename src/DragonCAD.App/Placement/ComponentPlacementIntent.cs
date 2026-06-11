using DragonCAD.App.ComponentManager;

namespace DragonCAD.App.Placement;

public sealed record ComponentPlacementIntent(
    string ComponentId,
    string DisplayName,
    int SymbolCount,
    int FootprintCount,
    string Source,
    ComponentSymbolPreview? SymbolPreview = null,
    ComponentFootprintPreview? FootprintPreview = null,
    IReadOnlyList<ComponentPlacementUnit>? Units = null)
{
    public IReadOnlyList<ComponentPlacementUnit> PlacementUnits { get; } = Units ?? [];
}

public sealed record ComponentPlacementUnit(
    string UnitId,
    string Name,
    bool IsRequired,
    bool CanPlaceMultiple,
    ComponentSymbolPreview SymbolPreview);
