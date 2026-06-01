using DragonCAD.Fabrication.Ordering;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Ordering;

public sealed class FabricationProviderProfileTests
{
    [Fact]
    public void OshParkProfile_ModelsPrototypeBoardPackageRequirements()
    {
        FabricationProviderProfile profile = FabricationOrderingProviders.OshParkPrototype.Profile;

        Assert.Equal("osh-park", profile.ProviderId);
        Assert.Equal(FabricationProviderKind.Prototype, profile.ProviderKind);
        Assert.Equal(3, profile.MinimumQuantity);
        Assert.Equal(3, profile.MaximumQuantity);
        Assert.Equal([2, 4], profile.SupportedLayerCounts);
        Assert.Equal([ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill], profile.BoardPackageRequiredRoles);
        Assert.Empty(profile.AssemblyPackageRequiredRoles);
    }

    [Fact]
    public void PcbCartProfile_ModelsProductionAssemblyPackageRequirements()
    {
        FabricationProviderProfile profile = FabricationOrderingProviders.PcbCartProduction.Profile;

        Assert.Equal("pcbcart", profile.ProviderId);
        Assert.Equal(FabricationProviderKind.Production, profile.ProviderKind);
        Assert.Equal(5, profile.MinimumQuantity);
        Assert.Equal(10000, profile.MaximumQuantity);
        Assert.Equal([1, 2, 4, 6, 8, 10, 12], profile.SupportedLayerCounts);
        Assert.Equal([ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill], profile.BoardPackageRequiredRoles);
        Assert.Equal([ManufacturingFileRole.BillOfMaterials, ManufacturingFileRole.PickAndPlace], profile.AssemblyPackageRequiredRoles);
    }

    [Fact]
    public void ValidatePackage_ReportsDeterministicProfileDiagnostics()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Drill, "drill/project.drl"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr")
        ]);

        FabricationQuotePackage package = FabricationQuotePackage.Create(
            FabricationOrderingProviders.PcbCartProduction,
            FabricationOrderMode.AssembledBoard,
            FabricationHandoffType.QuoteForm,
            FabricationOrderSpecification.Create(quantity: 2, layerCount: 3),
            manifest);

        FabricationPackageValidationResult result = package.Validate();

        Assert.False(result.IsValid);
        Assert.Equal(
            [
                "assembly-package-missing-role:BillOfMaterials",
                "assembly-package-missing-role:PickAndPlace",
                "unsupported-layer-count:",
                "unsupported-quantity:"
            ],
            result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.FileRole}"));
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(FabricationPackageDiagnosticSeverity.Error, diagnostic.Severity));
    }

    [Fact]
    public void ValidatePackage_AcceptsReadyPrototypePackage()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl")
        ]);

        FabricationQuotePackage package = FabricationQuotePackage.Create(
            FabricationOrderingProviders.OshParkPrototype,
            FabricationOrderMode.PrototypeBoard,
            FabricationHandoffType.UploadPackage,
            FabricationOrderSpecification.Create(quantity: 3, layerCount: 2),
            manifest);

        FabricationPackageValidationResult result = package.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create($"pending:{Path.GetFileNameWithoutExtension(path)}"));
    }
}
