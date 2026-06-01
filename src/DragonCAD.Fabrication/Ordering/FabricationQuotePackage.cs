using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationQuotePackage
{
    private FabricationQuotePackage(
        FabricationProviderDescriptor provider,
        FabricationOrderMode orderMode,
        FabricationHandoffType handoffType,
        FabricationOrderSpecification? specification,
        ManufacturingOutputManifest manifest)
    {
        Provider = provider;
        OrderMode = orderMode;
        HandoffType = handoffType;
        Specification = specification;
        Manifest = manifest;
    }

    public FabricationProviderDescriptor Provider { get; }

    public FabricationOrderMode OrderMode { get; }

    public FabricationHandoffType HandoffType { get; }

    public FabricationOrderSpecification? Specification { get; }

    public ManufacturingOutputManifest Manifest { get; }

    public static FabricationQuotePackage Create(
        FabricationProviderDescriptor provider,
        FabricationOrderMode orderMode,
        FabricationHandoffType handoffType,
        ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(manifest);

        return new FabricationQuotePackage(provider, orderMode, handoffType, null, manifest);
    }

    public static FabricationQuotePackage Create(
        FabricationProviderDescriptor provider,
        FabricationOrderMode orderMode,
        FabricationHandoffType handoffType,
        FabricationOrderSpecification specification,
        ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(manifest);

        return new FabricationQuotePackage(provider, orderMode, handoffType, specification, manifest);
    }

    public FabricationPackageValidationResult Validate()
    {
        List<FabricationPackageDiagnostic> diagnostics = [];

        if (!Provider.SupportedOrderModes.Contains(OrderMode))
        {
            diagnostics.Add(new FabricationPackageDiagnostic(
                FabricationPackageDiagnosticSeverity.Error,
                "unsupported-order-mode",
                $"{Provider.DisplayName} does not support {OrderMode} orders.",
                null));
        }

        if (!Provider.SupportedHandoffTypes.Contains(HandoffType))
        {
            diagnostics.Add(new FabricationPackageDiagnostic(
                FabricationPackageDiagnosticSeverity.Error,
                "unsupported-handoff-type",
                $"{Provider.DisplayName} does not support {HandoffType} handoff.",
                null));
        }

        HashSet<ManufacturingFileRole> providedRoles = Manifest.Entries
            .Select(entry => entry.Role)
            .ToHashSet();

        if (Specification is null)
        {
            foreach (ManufacturingFileRole role in Provider.RequiredFileRoles)
            {
                if (!providedRoles.Contains(role))
                {
                    diagnostics.Add(new FabricationPackageDiagnostic(
                        FabricationPackageDiagnosticSeverity.Error,
                        "missing-required-file-role",
                        $"Package is missing required {role} file for {Provider.DisplayName}.",
                        role));
                }
            }
        }
        else
        {
            AddProfileDiagnostics(diagnostics, providedRoles);
        }

        return FabricationPackageValidationResult.Create(diagnostics);
    }

    private void AddProfileDiagnostics(
        List<FabricationPackageDiagnostic> diagnostics,
        HashSet<ManufacturingFileRole> providedRoles)
    {
        FabricationProviderProfile profile = Provider.Profile;

        if (Specification!.Quantity < profile.MinimumQuantity || Specification.Quantity > profile.MaximumQuantity)
        {
            diagnostics.Add(new FabricationPackageDiagnostic(
                FabricationPackageDiagnosticSeverity.Error,
                "unsupported-quantity",
                $"{Provider.DisplayName} supports quantities from {profile.MinimumQuantity} to {profile.MaximumQuantity}; requested {Specification.Quantity}.",
                null));
        }

        if (profile.SupportedLayerCounts.Count > 0 && !profile.SupportedLayerCounts.Contains(Specification.LayerCount))
        {
            diagnostics.Add(new FabricationPackageDiagnostic(
                FabricationPackageDiagnosticSeverity.Error,
                "unsupported-layer-count",
                $"{Provider.DisplayName} supports layer counts {string.Join(", ", profile.SupportedLayerCounts)}; requested {Specification.LayerCount}.",
                null));
        }

        foreach (ManufacturingFileRole role in profile.BoardPackageRequiredRoles)
        {
            if (!providedRoles.Contains(role) && !HasDiagnostic(diagnostics, role))
            {
                diagnostics.Add(new FabricationPackageDiagnostic(
                    FabricationPackageDiagnosticSeverity.Error,
                    "board-package-missing-role",
                    $"Board package is missing required {role} file for {Provider.DisplayName}.",
                    role));
            }
        }

        if (OrderMode != FabricationOrderMode.AssembledBoard)
        {
            return;
        }

        foreach (ManufacturingFileRole role in profile.AssemblyPackageRequiredRoles)
        {
            if (!providedRoles.Contains(role) && !HasDiagnostic(diagnostics, role))
            {
                diagnostics.Add(new FabricationPackageDiagnostic(
                    FabricationPackageDiagnosticSeverity.Error,
                    "assembly-package-missing-role",
                    $"Assembly package is missing required {role} file for {Provider.DisplayName}.",
                    role));
            }
        }
    }

    private static bool HasDiagnostic(IEnumerable<FabricationPackageDiagnostic> diagnostics, ManufacturingFileRole role)
    {
        return diagnostics.Any(diagnostic => diagnostic.FileRole == role);
    }
}
