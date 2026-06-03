namespace DragonCAD.Sourcing.ProjectPlanning;

public static class ProjectDerivedBomPlanner
{
    private static readonly ProjectFabricationArtifactRole[] RequiredFabricationRoles =
    [
        ProjectFabricationArtifactRole.Gerber,
        ProjectFabricationArtifactRole.Drill,
        ProjectFabricationArtifactRole.BillOfMaterials,
        ProjectFabricationArtifactRole.PickAndPlace
    ];

    public static ProjectPlanningResult Plan(ProjectPlanningRequest request, DateTimeOffset plannedAt)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<ProjectPlanningDiagnostic>();
        ProjectBom bom = BuildBom(request, plannedAt, diagnostics);
        ProjectFabricationReadiness readiness = BuildFabricationReadiness(request);

        return new ProjectPlanningResult(bom, readiness, diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Designator, StringComparer.OrdinalIgnoreCase)
            .ThenBy(diagnostic => diagnostic.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    private static ProjectBom BuildBom(
        ProjectPlanningRequest request,
        DateTimeOffset plannedAt,
        List<ProjectPlanningDiagnostic> diagnostics)
    {
        Dictionary<string, ProjectPackageSelection> packagesByDesignator = request.PackageSelections
            .GroupBy(package => package.Designator, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        ProjectBomRow[] rows = request.Components
            .Where(component => component.IsPlaced)
            .OrderBy(component => component.Designator, StringComparer.OrdinalIgnoreCase)
            .Select(component => TryCreateComponentBomEntry(component, packagesByDesignator, request.VendorOffers, plannedAt, diagnostics))
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .GroupBy(entry => entry.RowKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateBomRow)
            .ToArray();

        return new ProjectBom(rows);
    }

    private static ProjectComponentBomEntry? TryCreateComponentBomEntry(
        ProjectDesignComponent component,
        IReadOnlyDictionary<string, ProjectPackageSelection> packagesByDesignator,
        IReadOnlyList<ProjectVendorOffer> offers,
        DateTimeOffset plannedAt,
        List<ProjectPlanningDiagnostic> diagnostics)
    {
        if (!packagesByDesignator.TryGetValue(component.Designator, out ProjectPackageSelection? package))
        {
            diagnostics.Add(ProjectPlanningDiagnostic.MissingPackageSelection(component.Designator));
            return null;
        }

        string[] allowedPartNumbers = package.DoNotSubstitute
            ? [NormalizeKey(package.SelectedManufacturerPartNumber)]
            : package.Alternates
                .Append(package.SelectedManufacturerPartNumber)
                .Select(NormalizeKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        ProjectVendorOfferRef[] currentOffers = offers
            .Where(offer => allowedPartNumbers.Contains(NormalizeKey(offer.ManufacturerPartNumber), StringComparer.OrdinalIgnoreCase))
            .OrderBy(offer => offer.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(offer => offer.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .Select(offer => CreateOfferRef(component.Designator, offer, plannedAt, diagnostics))
            .Where(offer => offer is not null)
            .Select(offer => offer!)
            .ToArray();

        string rowKey = CreateRowKey(
            component.CanonicalIdentity,
            component.Value,
            package.PackageName,
            package.SelectedManufacturerPartNumber);

        return new ProjectComponentBomEntry(
            rowKey,
            component.Designator,
            component.CanonicalIdentity,
            component.Value,
            package.PackageName,
            package.SelectedManufacturerPartNumber,
            package.Alternates,
            package.DoNotSubstitute,
            currentOffers);
    }

    private static ProjectVendorOfferRef? CreateOfferRef(
        string designator,
        ProjectVendorOffer offer,
        DateTimeOffset plannedAt,
        List<ProjectPlanningDiagnostic> diagnostics)
    {
        if (offer.ExpiresAt <= plannedAt)
        {
            diagnostics.Add(ProjectPlanningDiagnostic.StaleVendorOffer(
                designator,
                offer.ManufacturerPartNumber,
                offer.VendorName,
                offer.VendorPartNumber));
            return null;
        }

        return new ProjectVendorOfferRef(
            offer.ManufacturerPartNumber,
            offer.VendorName,
            offer.VendorPartNumber,
            offer.Stock,
            offer.UnitPrice,
            offer.CapturedAt,
            offer.ExpiresAt);
    }

    private static ProjectBomRow CreateBomRow(IGrouping<string, ProjectComponentBomEntry> group)
    {
        ProjectComponentBomEntry first = group.First();
        ProjectVendorOfferRef[] offers = group
            .SelectMany(entry => entry.CurrentVendorOffers)
            .DistinctBy(offer => $"{NormalizeKey(offer.ManufacturerPartNumber)}|{NormalizeKey(offer.VendorName)}|{NormalizeKey(offer.VendorPartNumber)}")
            .OrderBy(offer => offer.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(offer => offer.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ProjectBomRow(
            first.RowKey,
            group.Select(entry => entry.Designator).OrderBy(designator => designator, StringComparer.OrdinalIgnoreCase).ToArray(),
            group.Count(),
            first.CanonicalIdentity,
            first.Value,
            first.PackageName,
            first.SelectedManufacturerPartNumber,
            first.AlternateManufacturerPartNumbers,
            first.DoNotSubstitute,
            offers);
    }

    private static ProjectFabricationReadiness BuildFabricationReadiness(ProjectPlanningRequest request)
    {
        ProjectFabricationRequiredArtifact[] requiredArtifacts = RequiredFabricationRoles
            .Select(role => CreateRequiredArtifact(role, request.Artifacts))
            .ToArray();

        ProjectFabricationReadinessBlocker[] blockers = requiredArtifacts
            .SelectMany(artifact => CreateBlockers(artifact, request.DesignRevision))
            .OrderBy(blocker => blocker.Role)
            .ThenBy(blocker => blocker.Code, StringComparer.Ordinal)
            .ToArray();

        return new ProjectFabricationReadiness(blockers.Length == 0, requiredArtifacts, blockers);
    }

    private static ProjectFabricationRequiredArtifact CreateRequiredArtifact(
        ProjectFabricationArtifactRole role,
        IReadOnlyList<ProjectFabricationArtifact> artifacts)
    {
        ProjectFabricationArtifactFile[] files = artifacts
            .Where(artifact => artifact.Role == role)
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .Select(artifact => new ProjectFabricationArtifactFile(
                artifact.RelativePath,
                artifact.SourceDesignRevision,
                artifact.GeneratedAt))
            .ToArray();

        return new ProjectFabricationRequiredArtifact(role, files);
    }

    private static IEnumerable<ProjectFabricationReadinessBlocker> CreateBlockers(
        ProjectFabricationRequiredArtifact artifact,
        string activeDesignRevision)
    {
        if (artifact.Files.Count == 0)
        {
            yield return ProjectFabricationReadinessBlocker.Missing(artifact.Role);
            yield break;
        }

        foreach (ProjectFabricationArtifactFile file in artifact.Files)
        {
            if (!string.Equals(file.SourceDesignRevision, activeDesignRevision, StringComparison.Ordinal))
            {
                yield return ProjectFabricationReadinessBlocker.Stale(artifact.Role, file.RelativePath);
            }
        }
    }

    private static string CreateRowKey(
        string canonicalIdentity,
        string value,
        string packageName,
        string manufacturerPartNumber)
    {
        return $"{NormalizeKey(canonicalIdentity)}|{NormalizeKey(value)}|{NormalizeKey(packageName)}|{NormalizeKey(manufacturerPartNumber)}";
    }

    private static string NormalizeKey(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private sealed record ProjectComponentBomEntry(
        string RowKey,
        string Designator,
        string CanonicalIdentity,
        string Value,
        string PackageName,
        string SelectedManufacturerPartNumber,
        IReadOnlyList<string> AlternateManufacturerPartNumbers,
        bool DoNotSubstitute,
        IReadOnlyList<ProjectVendorOfferRef> CurrentVendorOffers);
}

public sealed record ProjectPlanningRequest(
    string DesignRevision,
    IReadOnlyList<ProjectDesignComponent> Components,
    IReadOnlyList<ProjectPackageSelection> PackageSelections,
    IReadOnlyList<ProjectVendorOffer> VendorOffers,
    IReadOnlyList<ProjectFabricationArtifact> Artifacts)
{
    public string DesignRevision { get; init; } = ProjectPlanningGuard.RequireText(DesignRevision, nameof(DesignRevision));

    public IReadOnlyList<ProjectDesignComponent> Components { get; init; } = Components ?? throw new ArgumentNullException(nameof(Components));

    public IReadOnlyList<ProjectPackageSelection> PackageSelections { get; init; } = PackageSelections ?? throw new ArgumentNullException(nameof(PackageSelections));

    public IReadOnlyList<ProjectVendorOffer> VendorOffers { get; init; } = VendorOffers ?? throw new ArgumentNullException(nameof(VendorOffers));

    public IReadOnlyList<ProjectFabricationArtifact> Artifacts { get; init; } = Artifacts ?? throw new ArgumentNullException(nameof(Artifacts));
}

public sealed record ProjectDesignComponent(
    string Designator,
    string CanonicalIdentity,
    string Value,
    bool IsPlaced)
{
    public string Designator { get; init; } = ProjectPlanningGuard.RequireText(Designator, nameof(Designator));

    public string CanonicalIdentity { get; init; } = ProjectPlanningGuard.RequireText(CanonicalIdentity, nameof(CanonicalIdentity));

    public string Value { get; init; } = ProjectPlanningGuard.RequireText(Value, nameof(Value));
}

public sealed record ProjectPackageSelection(
    string Designator,
    string PackageName,
    string SelectedManufacturerPartNumber,
    bool DoNotSubstitute,
    IReadOnlyList<string> Alternates)
{
    public string Designator { get; init; } = ProjectPlanningGuard.RequireText(Designator, nameof(Designator));

    public string PackageName { get; init; } = ProjectPlanningGuard.RequireText(PackageName, nameof(PackageName));

    public string SelectedManufacturerPartNumber { get; init; } = ProjectPlanningGuard.RequireText(SelectedManufacturerPartNumber, nameof(SelectedManufacturerPartNumber));

    public IReadOnlyList<string> Alternates { get; init; } = Alternates
        .Where(alternate => !string.IsNullOrWhiteSpace(alternate))
        .Select(alternate => alternate.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(alternate => alternate, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

public sealed record ProjectVendorOffer(
    string ManufacturerPartNumber,
    string VendorName,
    string VendorPartNumber,
    int Stock,
    Money UnitPrice,
    DateTimeOffset CapturedAt,
    DateTimeOffset ExpiresAt)
{
    public string ManufacturerPartNumber { get; init; } = ProjectPlanningGuard.RequireText(ManufacturerPartNumber, nameof(ManufacturerPartNumber));

    public string VendorName { get; init; } = ProjectPlanningGuard.RequireText(VendorName, nameof(VendorName));

    public string VendorPartNumber { get; init; } = ProjectPlanningGuard.RequireText(VendorPartNumber, nameof(VendorPartNumber));

    public int Stock { get; init; } = Stock < 0
        ? throw new ArgumentOutOfRangeException(nameof(Stock), Stock, "Stock cannot be negative.")
        : Stock;
}

public sealed record ProjectFabricationArtifact(
    ProjectFabricationArtifactRole Role,
    string RelativePath,
    string SourceDesignRevision,
    DateTimeOffset GeneratedAt)
{
    public string RelativePath { get; init; } = ProjectPlanningGuard.RequireText(RelativePath, nameof(RelativePath));

    public string SourceDesignRevision { get; init; } = ProjectPlanningGuard.RequireText(SourceDesignRevision, nameof(SourceDesignRevision));
}

public sealed record ProjectPlanningResult(
    ProjectBom Bom,
    ProjectFabricationReadiness FabricationReadiness,
    IReadOnlyList<ProjectPlanningDiagnostic> Diagnostics);

public sealed record ProjectBom(IReadOnlyList<ProjectBomRow> Rows);

public sealed record ProjectBomRow(
    string RowKey,
    IReadOnlyList<string> Designators,
    int Quantity,
    string CanonicalIdentity,
    string Value,
    string PackageName,
    string SelectedManufacturerPartNumber,
    IReadOnlyList<string> AlternateManufacturerPartNumbers,
    bool DoNotSubstitute,
    IReadOnlyList<ProjectVendorOfferRef> CurrentVendorOffers);

public sealed record ProjectVendorOfferRef(
    string ManufacturerPartNumber,
    string VendorName,
    string VendorPartNumber,
    int Stock,
    Money UnitPrice,
    DateTimeOffset CapturedAt,
    DateTimeOffset ExpiresAt);

public sealed record ProjectPlanningDiagnostic(
    string Code,
    string Message,
    string? Designator,
    string? ManufacturerPartNumber,
    string? VendorName,
    string? VendorPartNumber)
{
    public static ProjectPlanningDiagnostic MissingPackageSelection(string designator)
    {
        return new ProjectPlanningDiagnostic(
            ProjectPlanningDiagnosticCodes.MissingPackageSelection,
            $"Placed component '{designator}' does not have an active package selection.",
            designator,
            ManufacturerPartNumber: null,
            VendorName: null,
            VendorPartNumber: null);
    }

    public static ProjectPlanningDiagnostic StaleVendorOffer(
        string designator,
        string manufacturerPartNumber,
        string vendorName,
        string vendorPartNumber)
    {
        return new ProjectPlanningDiagnostic(
            ProjectPlanningDiagnosticCodes.StaleVendorOffer,
            $"Vendor offer '{vendorPartNumber}' for '{manufacturerPartNumber}' is stale and was not used.",
            designator,
            manufacturerPartNumber,
            vendorName,
            vendorPartNumber);
    }
}

public static class ProjectPlanningDiagnosticCodes
{
    public const string MissingPackageSelection = "missing-package-selection";
    public const string StaleVendorOffer = "stale-vendor-offer";
}

public sealed record ProjectFabricationReadiness(
    bool IsReady,
    IReadOnlyList<ProjectFabricationRequiredArtifact> RequiredArtifacts,
    IReadOnlyList<ProjectFabricationReadinessBlocker> Blockers);

public sealed record ProjectFabricationRequiredArtifact(
    ProjectFabricationArtifactRole Role,
    IReadOnlyList<ProjectFabricationArtifactFile> Files);

public sealed record ProjectFabricationArtifactFile(
    string RelativePath,
    string SourceDesignRevision,
    DateTimeOffset GeneratedAt);

public sealed record ProjectFabricationReadinessBlocker(
    string Code,
    string Message,
    ProjectFabricationArtifactRole Role,
    string? RelativePath)
{
    public static ProjectFabricationReadinessBlocker Missing(ProjectFabricationArtifactRole role)
    {
        return new ProjectFabricationReadinessBlocker(
            ProjectFabricationReadinessBlockerCodes.ForMissingRole(role),
            $"Fabrication packet requires a current {role} artifact.",
            role,
            RelativePath: null);
    }

    public static ProjectFabricationReadinessBlocker Stale(ProjectFabricationArtifactRole role, string relativePath)
    {
        return new ProjectFabricationReadinessBlocker(
            ProjectFabricationReadinessBlockerCodes.StaleArtifact,
            $"Fabrication artifact '{relativePath}' was generated from a stale design revision.",
            role,
            relativePath);
    }
}

public static class ProjectFabricationReadinessBlockerCodes
{
    public const string MissingGerber = "missing-gerber";
    public const string MissingDrill = "missing-drill";
    public const string MissingBillOfMaterials = "missing-bill-of-materials";
    public const string MissingPickAndPlace = "missing-pick-and-place";
    public const string StaleArtifact = "stale-artifact";

    public static string ForMissingRole(ProjectFabricationArtifactRole role)
    {
        return role switch
        {
            ProjectFabricationArtifactRole.Gerber => MissingGerber,
            ProjectFabricationArtifactRole.Drill => MissingDrill,
            ProjectFabricationArtifactRole.BillOfMaterials => MissingBillOfMaterials,
            ProjectFabricationArtifactRole.PickAndPlace => MissingPickAndPlace,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported fabrication artifact role.")
        };
    }
}

public enum ProjectFabricationArtifactRole
{
    Gerber = 100,
    Drill = 110,
    BillOfMaterials = 200,
    PickAndPlace = 300
}

internal static class ProjectPlanningGuard
{
    public static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }
}
