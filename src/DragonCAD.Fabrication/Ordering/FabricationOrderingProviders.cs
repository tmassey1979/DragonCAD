using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering;

public static class FabricationOrderingProviders
{
    public static FabricationProviderDescriptor OshParkPrototype { get; } = new(
        "osh-park",
        "OSH Park",
        [FabricationOrderMode.PrototypeBoard],
        [FabricationHandoffType.UploadPackage, FabricationHandoffType.ManualPackage],
        [ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill],
        new FabricationProviderProfile(
            "osh-park",
            FabricationProviderKind.Prototype,
            minimumQuantity: 3,
            maximumQuantity: 3,
            supportedLayerCounts: [2, 4],
            boardPackageRequiredRoles: [ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill],
            assemblyPackageRequiredRoles: []));

    public static FabricationProviderDescriptor PcbCartProduction { get; } = new(
        "pcbcart",
        "PCBCart",
        [FabricationOrderMode.ProductionBoard, FabricationOrderMode.AssembledBoard],
        [FabricationHandoffType.QuoteForm, FabricationHandoffType.ManualPackage],
        [
            ManufacturingFileRole.Gerber,
            ManufacturingFileRole.Drill,
            ManufacturingFileRole.BillOfMaterials,
            ManufacturingFileRole.PickAndPlace
        ],
        new FabricationProviderProfile(
            "pcbcart",
            FabricationProviderKind.Production,
            minimumQuantity: 5,
            maximumQuantity: 10000,
            supportedLayerCounts: [1, 2, 4, 6, 8, 10, 12],
            boardPackageRequiredRoles: [ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill],
            assemblyPackageRequiredRoles: [ManufacturingFileRole.BillOfMaterials, ManufacturingFileRole.PickAndPlace]));
}
