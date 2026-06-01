using DragonCAD.App.ComponentManager;

namespace DragonCAD.App.Placement;

public sealed record ComponentPlacementIntent(
    string ComponentId,
    string DisplayName,
    int SymbolCount,
    int FootprintCount,
    string Source,
    ComponentSymbolPreview? SymbolPreview = null,
    ComponentFootprintPreview? FootprintPreview = null);
