using DragonCAD.Fabrication.Ordering;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Ordering;

public sealed class FabricationOrderingProviderTests
{
    [Fact]
    public void OshParkProvider_DescribesPrototypeUploadPackage()
    {
        FabricationProviderDescriptor provider = FabricationOrderingProviders.OshParkPrototype;

        Assert.Equal("osh-park", provider.Id);
        Assert.Equal("OSH Park", provider.DisplayName);
        Assert.Contains(FabricationOrderMode.PrototypeBoard, provider.SupportedOrderModes);
        Assert.Contains(FabricationHandoffType.UploadPackage, provider.SupportedHandoffTypes);
        Assert.DoesNotContain(FabricationOrderMode.AssembledBoard, provider.SupportedOrderModes);
        Assert.Contains(ManufacturingFileRole.Gerber, provider.RequiredFileRoles);
        Assert.Contains(ManufacturingFileRole.Drill, provider.RequiredFileRoles);
        Assert.DoesNotContain(ManufacturingFileRole.BillOfMaterials, provider.RequiredFileRoles);
    }

    [Fact]
    public void PcbCartProvider_DescribesProductionAssemblyQuotePackage()
    {
        FabricationProviderDescriptor provider = FabricationOrderingProviders.PcbCartProduction;

        Assert.Equal("pcbcart", provider.Id);
        Assert.Equal("PCBCart", provider.DisplayName);
        Assert.Contains(FabricationOrderMode.ProductionBoard, provider.SupportedOrderModes);
        Assert.Contains(FabricationOrderMode.AssembledBoard, provider.SupportedOrderModes);
        Assert.Contains(FabricationHandoffType.QuoteForm, provider.SupportedHandoffTypes);
        Assert.Contains(ManufacturingFileRole.Gerber, provider.RequiredFileRoles);
        Assert.Contains(ManufacturingFileRole.Drill, provider.RequiredFileRoles);
        Assert.Contains(ManufacturingFileRole.BillOfMaterials, provider.RequiredFileRoles);
        Assert.Contains(ManufacturingFileRole.PickAndPlace, provider.RequiredFileRoles);
    }

    [Fact]
    public void ValidatePackage_ReportsMissingGerbers()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Drill, "drill/project.drl"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pnp.csv")
        ]);

        FabricationQuotePackage package = FabricationQuotePackage.Create(
            FabricationOrderingProviders.PcbCartProduction,
            FabricationOrderMode.AssembledBoard,
            FabricationHandoffType.QuoteForm,
            manifest);

        FabricationPackageValidationResult result = package.Validate();

        Assert.False(result.IsValid);
        FabricationPackageDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(FabricationPackageDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("missing-required-file-role", diagnostic.Code);
        Assert.Equal(ManufacturingFileRole.Gerber, diagnostic.FileRole);
        Assert.Contains("Gerber", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatSummary_IsDeterministicForEquivalentPackages()
    {
        ManufacturingOutputManifest firstManifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pnp.csv"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl")
        ]);
        ManufacturingOutputManifest secondManifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Drill, "drill/project.drl"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/top.gbr"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/project-pnp.csv")
        ]);

        FabricationQuotePackage first = FabricationQuotePackage.Create(
            FabricationOrderingProviders.PcbCartProduction,
            FabricationOrderMode.AssembledBoard,
            FabricationHandoffType.QuoteForm,
            firstManifest);
        FabricationQuotePackage second = FabricationQuotePackage.Create(
            FabricationOrderingProviders.PcbCartProduction,
            FabricationOrderMode.AssembledBoard,
            FabricationHandoffType.QuoteForm,
            secondManifest);

        string firstSummary = FabricationQuotePackageFormatter.FormatSummary(first);
        string secondSummary = FabricationQuotePackageFormatter.FormatSummary(second);

        Assert.Equal(firstSummary, secondSummary);
        Assert.Equal(
            """
            Provider: PCBCart (pcbcart)
            OrderMode: AssembledBoard
            Handoff: QuoteForm
            RequiredFiles: Gerber, Drill, BillOfMaterials, PickAndPlace
            PackageFiles:
            - Gerber: gerbers/top.gbr
            - Drill: drill/project.drl
            - BillOfMaterials: bom/project.csv
            - PickAndPlace: assembly/project-pnp.csv
            Diagnostics: none
            """.ReplaceLineEndings("\r\n"),
            firstSummary);
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create($"pending:{Path.GetFileNameWithoutExtension(path)}"));
    }
}
