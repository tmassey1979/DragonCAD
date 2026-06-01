using DragonCAD.App.Fabrication;
using DragonCAD.App.Fabrication.Ordering;

namespace DragonCAD.App.Tests.Fabrication.Ordering;

public sealed class FabricationOrderingReadinessViewModelTests
{
    [Fact]
    public void FromSelectedHandoffOptionBuildsBlockedPcbCartReadinessFromMissingRequiredFiles()
    {
        FabricationHandoffViewModel handoff = FabricationHandoffViewModel.CreateSample();
        handoff.SelectedOption = handoff.Options.Single(option => option.ProviderName == "PCBCart");

        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSelectedHandoffOption(handoff);

        FabricationOrderingReadinessRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("PCBCart", row.ProviderName);
        Assert.Equal("Production", row.ProviderKind);
        Assert.Equal("Production / assembly", row.Mode);
        Assert.Equal("1, 2, 4, 6, 8, 10, 12 layers", row.LayerSupport);
        Assert.Equal("5-10000 boards", row.QuantitySupport);
        Assert.Equal("Blocked", row.PackageReadiness);
        Assert.Equal(["BOM", "Gerbers"], row.MissingFiles);
        Assert.Equal("Checkout/submission is disabled: package is blocked by 2 missing required files.", row.CheckoutSubmissionDisabledExplanation);
        Assert.Equal("Fix 2 missing files", viewModel.NextActionStatusLabel);
    }

    [Fact]
    public void FromSelectedHandoffOptionBuildsReadyOshParkReadinessFromCompletePackage()
    {
        FabricationHandoffViewModel handoff = FabricationHandoffViewModel.CreateSample();
        handoff.SelectedOption = handoff.Options.Single(option => option.ProviderName == "OSH Park");

        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSelectedHandoffOption(handoff);

        FabricationOrderingReadinessRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("OSH Park", row.ProviderName);
        Assert.Equal("Prototype", row.ProviderKind);
        Assert.Equal("Prototype board", row.Mode);
        Assert.Equal("2, 4 layers", row.LayerSupport);
        Assert.Equal("3 boards", row.QuantitySupport);
        Assert.Equal("Ready", row.PackageReadiness);
        Assert.Empty(row.MissingFiles);
        Assert.Equal(
            "Prototype package ready for OSH Park: export package only; checkout/submission stays disabled.",
            row.PackageActionSummary);
        Assert.Equal("Ready to package", viewModel.NextActionStatusLabel);
    }

    [Fact]
    public void FromSelectedHandoffOptionKeepsCheckoutSubmissionDisabledForCompletePackage()
    {
        FabricationHandoffViewModel handoff = FabricationHandoffViewModel.CreateSample();
        handoff.SelectedOption = handoff.Options.Single(option => option.ProviderName == "OSH Park");

        FabricationOrderingReadinessRow row = Assert.Single(
            FabricationOrderingReadinessViewModel.FromSelectedHandoffOption(handoff).Rows);

        Assert.False(row.IsCheckoutSubmissionEnabled);
        Assert.Equal(
            "Checkout/submission is disabled: DragonCAD prepares the package only and does not place fabrication orders.",
            row.CheckoutSubmissionDisabledExplanation);
    }

    [Fact]
    public void FromSelectedHandoffOptionExposesEmptyPanelStateWhenNoProviderIsSelected()
    {
        FabricationHandoffViewModel handoff = FabricationHandoffViewModel.CreateSample();
        handoff.SelectedOption = null;

        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSelectedHandoffOption(handoff);

        Assert.False(viewModel.HasRows);
        Assert.Empty(viewModel.Rows);
        Assert.Equal(0, viewModel.ProviderCount);
        Assert.Equal(0, viewModel.ReadyProviderCount);
        Assert.Equal(0, viewModel.BlockedProviderCount);
        Assert.Equal(0, viewModel.WarningCount);
        Assert.Equal(0, viewModel.MissingFileCount);
        Assert.Equal("No fabrication provider selected.", viewModel.SummaryText);
        Assert.Equal("Select provider", viewModel.NextActionStatusLabel);
        Assert.Equal(
            "Select a marketplace or manufacturing provider to review package readiness.",
            viewModel.EmptyStateText);
    }

    [Fact]
    public void FromSourcesBuildsRowsForOshParkAndPcbCartReadiness()
    {
        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSources(
        [
            new FabricationOrderingReadinessSource(
                ProviderName: "OSH Park",
                ProviderKind: "Prototype",
                Mode: "Prototype board",
                SupportedLayers: [2, 4],
                MinimumQuantity: 3,
                MaximumQuantity: 3,
                ValidationDiagnostics: []),
            new FabricationOrderingReadinessSource(
                ProviderName: "PCBCart",
                ProviderKind: "Production",
                Mode: "Assembled board",
                SupportedLayers: [1, 2, 4, 6, 8, 10, 12],
                MinimumQuantity: 5,
                MaximumQuantity: 10000,
                ValidationDiagnostics:
                [
                    FabricationOrderingDiagnostic.Error("assembly-package-missing-role", "Assembly package is missing required BillOfMaterials file for PCBCart.", "BillOfMaterials"),
                    FabricationOrderingDiagnostic.Warning("manual-review-required", "Manual fabrication review is required before provider submission.")
                ])
        ]);

        Assert.Equal(["OSH Park", "PCBCart"], viewModel.Rows.Select(row => row.ProviderName));

        FabricationOrderingReadinessRow oshPark = viewModel.Rows[0];
        Assert.Equal("Prototype", oshPark.ProviderKind);
        Assert.Equal("Prototype board", oshPark.Mode);
        Assert.Equal("2, 4 layers", oshPark.LayerSupport);
        Assert.Equal("3 boards", oshPark.QuantitySupport);
        Assert.Equal("Ready", oshPark.PackageReadiness);
        Assert.Empty(oshPark.MissingFiles);
        Assert.False(oshPark.IsCheckoutSubmissionEnabled);
        Assert.Equal("Checkout/submission is disabled: DragonCAD prepares the package only and does not place fabrication orders.", oshPark.CheckoutSubmissionDisabledExplanation);

        FabricationOrderingReadinessRow pcbCart = viewModel.Rows[1];
        Assert.Equal("Production", pcbCart.ProviderKind);
        Assert.Equal("Assembled board", pcbCart.Mode);
        Assert.Equal("1, 2, 4, 6, 8, 10, 12 layers", pcbCart.LayerSupport);
        Assert.Equal("5-10000 boards", pcbCart.QuantitySupport);
        Assert.Equal("Blocked", pcbCart.PackageReadiness);
        Assert.Equal(["BillOfMaterials"], pcbCart.MissingFiles);
        Assert.Equal(["Manual fabrication review is required before provider submission."], pcbCart.Warnings);
        Assert.Equal(
            "Production package blocked for PCBCart: add BillOfMaterials before packaging.",
            pcbCart.PackageActionSummary);
        Assert.Equal("Checkout/submission is disabled: package is blocked by 1 missing required file.", pcbCart.CheckoutSubmissionDisabledExplanation);
    }

    [Fact]
    public void FromSourcesExposesAggregatePanelCountsAndSummaryText()
    {
        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSources(
        [
            new FabricationOrderingReadinessSource(
                ProviderName: "OSH Park",
                ProviderKind: "Prototype",
                Mode: "Prototype board",
                SupportedLayers: [2, 4],
                MinimumQuantity: 3,
                MaximumQuantity: 3,
                ValidationDiagnostics: []),
            new FabricationOrderingReadinessSource(
                ProviderName: "PCBCart",
                ProviderKind: "Production",
                Mode: "Assembled board",
                SupportedLayers: [1, 2, 4, 6, 8, 10, 12],
                MinimumQuantity: 5,
                MaximumQuantity: 10000,
                ValidationDiagnostics:
                [
                    FabricationOrderingDiagnostic.Error("assembly-package-missing-bom", "Assembly package is missing required BillOfMaterials file for PCBCart.", "BillOfMaterials"),
                    FabricationOrderingDiagnostic.Error("assembly-package-missing-pnp", "Assembly package is missing required PickAndPlace file for PCBCart.", "PickAndPlace"),
                    FabricationOrderingDiagnostic.Warning("manual-review-required", "Manual fabrication review is required before provider submission.")
                ])
        ]);

        Assert.True(viewModel.HasRows);
        Assert.Equal(2, viewModel.ProviderCount);
        Assert.Equal(1, viewModel.ReadyProviderCount);
        Assert.Equal(1, viewModel.BlockedProviderCount);
        Assert.Equal(1, viewModel.WarningCount);
        Assert.Equal(2, viewModel.MissingFileCount);
        Assert.Equal("2 providers: 1 ready, 1 blocked, 1 warning, 2 missing files.", viewModel.SummaryText);
        Assert.Equal("Fix 2 missing files", viewModel.NextActionStatusLabel);
        Assert.Equal(string.Empty, viewModel.EmptyStateText);
    }

    [Fact]
    public void FromSourcesExposesWarningNextActionStatusLabelWhenPackageHasWarningsOnly()
    {
        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromSources(
        [
            new FabricationOrderingReadinessSource(
                ProviderName: "Local Fab",
                ProviderKind: "Production",
                Mode: "Production board",
                SupportedLayers: [],
                MinimumQuantity: 1,
                MaximumQuantity: int.MaxValue,
                ValidationDiagnostics:
                [
                    FabricationOrderingDiagnostic.Warning("manual-review-required", "Manual fabrication review is required before provider submission.")
                ])
        ]);

        Assert.Equal("Review 1 warning", viewModel.NextActionStatusLabel);
    }

    [Fact]
    public void FromDomainPackagesAdaptsProviderProfilesAndValidationResultsByShape()
    {
        ShapeProviderProfile profile = new(
            ProviderKind: ShapeProviderKind.Production,
            MinimumQuantity: 5,
            MaximumQuantity: 10000,
            SupportedLayerCounts: [2, 4, 6]);
        ShapeProvider provider = new("PCBCart", profile);
        ShapePackage package = new(provider, ShapeOrderMode.AssembledBoard);
        ShapeValidation validation = new(
        [
            new ShapeDiagnostic(ShapeSeverity.Error, "assembly-package-missing-role", "Assembly package is missing required PickAndPlace file for PCBCart.", ShapeFileRole.PickAndPlace),
            new ShapeDiagnostic(ShapeSeverity.Warning, "manual-review-required", "Manual fabrication review is required before provider submission.", null)
        ]);

        FabricationOrderingReadinessViewModel viewModel = FabricationOrderingReadinessViewModel.FromDomainPackages(
        [
            new FabricationOrderingDomainPackage(package, validation)
        ]);

        FabricationOrderingReadinessRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("PCBCart", row.ProviderName);
        Assert.Equal("Production", row.ProviderKind);
        Assert.Equal("Assembled board", row.Mode);
        Assert.Equal("2, 4, 6 layers", row.LayerSupport);
        Assert.Equal("5-10000 boards", row.QuantitySupport);
        Assert.Equal("Blocked", row.PackageReadiness);
        Assert.Equal(["PickAndPlace"], row.MissingFiles);
        Assert.Equal(["Manual fabrication review is required before provider submission."], row.Warnings);
    }

    private sealed record ShapePackage(ShapeProvider Provider, ShapeOrderMode OrderMode);

    private sealed record ShapeProvider(string DisplayName, ShapeProviderProfile Profile);

    private sealed record ShapeProviderProfile(
        ShapeProviderKind ProviderKind,
        int MinimumQuantity,
        int MaximumQuantity,
        IReadOnlyList<int> SupportedLayerCounts);

    private sealed record ShapeValidation(IReadOnlyList<ShapeDiagnostic> Diagnostics);

    private sealed record ShapeDiagnostic(ShapeSeverity Severity, string Code, string Message, ShapeFileRole? FileRole);

    private enum ShapeProviderKind
    {
        Prototype,
        Production
    }

    private enum ShapeOrderMode
    {
        PrototypeBoard,
        ProductionBoard,
        AssembledBoard
    }

    private enum ShapeSeverity
    {
        Information,
        Warning,
        Error
    }

    private enum ShapeFileRole
    {
        BillOfMaterials,
        PickAndPlace
    }
}
