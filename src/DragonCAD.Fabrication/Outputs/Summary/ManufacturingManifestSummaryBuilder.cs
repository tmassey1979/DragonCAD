namespace DragonCAD.Fabrication.Outputs.Summary;

public static class ManufacturingManifestSummaryBuilder
{
    private static readonly ManufacturingManifestSummaryRole[] RoleOrder =
    [
        ManufacturingManifestSummaryRole.Gerber,
        ManufacturingManifestSummaryRole.Drill,
        ManufacturingManifestSummaryRole.Paste,
        ManufacturingManifestSummaryRole.BillOfMaterials,
        ManufacturingManifestSummaryRole.PickAndPlace,
        ManufacturingManifestSummaryRole.Assembly,
        ManufacturingManifestSummaryRole.Auxiliary
    ];

    private static readonly ManufacturingManifestSummaryRole[] RequiredRoles =
    [
        ManufacturingManifestSummaryRole.Gerber,
        ManufacturingManifestSummaryRole.Drill,
        ManufacturingManifestSummaryRole.BillOfMaterials,
        ManufacturingManifestSummaryRole.PickAndPlace
    ];

    private static readonly ManufacturingManifestSummaryRole[] SingletonRoles =
    [
        ManufacturingManifestSummaryRole.Drill,
        ManufacturingManifestSummaryRole.Paste,
        ManufacturingManifestSummaryRole.BillOfMaterials,
        ManufacturingManifestSummaryRole.PickAndPlace,
        ManufacturingManifestSummaryRole.Assembly
    ];

    public static ManufacturingManifestSummary Create(ManufacturingOutputManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Dictionary<ManufacturingManifestSummaryRole, ManufacturingOutputEntry[]> entriesByRole = manifest.Entries
            .GroupBy(entry => MapRole(entry.Role))
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping
                    .OrderBy(entry => entry.RelativePath.Value, StringComparer.Ordinal)
                    .ToArray());

        ManufacturingManifestRoleSummary[] roleSummaries = RoleOrder
            .Select(role => CreateRoleSummary(role, entriesByRole.GetValueOrDefault(role) ?? []))
            .ToArray();

        ManufacturingManifestSummaryRole[] missingRequiredRoles = RequiredRoles
            .Where(role => roleSummaries.Single(summary => summary.Role == role).FileCount == 0)
            .ToArray();

        ManufacturingManifestReviewWarning[] warnings =
        [
            .. CreateMissingRoleWarnings(missingRequiredRoles),
            .. CreateDuplicateRoleWarnings(roleSummaries),
            .. CreateMissingChecksumWarnings(entriesByRole)
        ];

        return new ManufacturingManifestSummary(
            roleSummaries,
            missingRequiredRoles,
            warnings,
            manifest.Entries.Count);
    }

    private static ManufacturingManifestRoleSummary CreateRoleSummary(
        ManufacturingManifestSummaryRole role,
        IReadOnlyList<ManufacturingOutputEntry> entries)
    {
        string[] relativePaths = entries
            .Select(entry => entry.RelativePath.Value)
            .ToArray();
        int checksumCount = entries.Count(entry => entry.Checksum is not null);

        return new ManufacturingManifestRoleSummary(
            role,
            relativePaths,
            relativePaths.Length,
            checksumCount,
            relativePaths.Length > 0 && checksumCount == relativePaths.Length);
    }

    private static IEnumerable<ManufacturingManifestReviewWarning> CreateMissingRoleWarnings(
        IEnumerable<ManufacturingManifestSummaryRole> missingRequiredRoles) =>
        missingRequiredRoles.Select(role => new ManufacturingManifestReviewWarning(
            ManufacturingManifestReviewWarningCodes.MissingRequiredRole,
            role,
            $"{DisplayName(role)} output is missing from the manufacturing package."));

    private static IEnumerable<ManufacturingManifestReviewWarning> CreateDuplicateRoleWarnings(
        IEnumerable<ManufacturingManifestRoleSummary> roleSummaries) =>
        roleSummaries
            .Where(summary => SingletonRoles.Contains(summary.Role) && summary.FileCount > 1)
            .Select(summary => new ManufacturingManifestReviewWarning(
                ManufacturingManifestReviewWarningCodes.DuplicateRole,
                summary.Role,
                $"{summary.FileCount} {DisplayName(summary.Role)} files are present; review which one should be used."));

    private static IEnumerable<ManufacturingManifestReviewWarning> CreateMissingChecksumWarnings(
        IReadOnlyDictionary<ManufacturingManifestSummaryRole, ManufacturingOutputEntry[]> entriesByRole) =>
        RoleOrder
            .SelectMany(role => entriesByRole.GetValueOrDefault(role) ?? [])
            .Where(entry => entry.Checksum is null)
            .Select(entry =>
            {
                ManufacturingManifestSummaryRole role = MapRole(entry.Role);
                return new ManufacturingManifestReviewWarning(
                    ManufacturingManifestReviewWarningCodes.MissingChecksum,
                    role,
                    $"{entry.RelativePath.Value} is missing a checksum.",
                    entry.RelativePath.Value);
            });

    private static ManufacturingManifestSummaryRole MapRole(ManufacturingFileRole role) =>
        role switch
        {
            ManufacturingFileRole.Gerber => ManufacturingManifestSummaryRole.Gerber,
            ManufacturingFileRole.Drill => ManufacturingManifestSummaryRole.Drill,
            ManufacturingFileRole.SolderPaste => ManufacturingManifestSummaryRole.Paste,
            ManufacturingFileRole.BillOfMaterials => ManufacturingManifestSummaryRole.BillOfMaterials,
            ManufacturingFileRole.PickAndPlace => ManufacturingManifestSummaryRole.PickAndPlace,
            ManufacturingFileRole.AssemblyDrawing => ManufacturingManifestSummaryRole.Assembly,
            _ => ManufacturingManifestSummaryRole.Auxiliary
        };

    private static string DisplayName(ManufacturingManifestSummaryRole role) =>
        role switch
        {
            ManufacturingManifestSummaryRole.Gerber => "Gerber",
            ManufacturingManifestSummaryRole.Drill => "Drill",
            ManufacturingManifestSummaryRole.Paste => "Paste",
            ManufacturingManifestSummaryRole.BillOfMaterials => "Bill of materials",
            ManufacturingManifestSummaryRole.PickAndPlace => "Pick-and-place",
            ManufacturingManifestSummaryRole.Assembly => "Assembly",
            ManufacturingManifestSummaryRole.Auxiliary => "Auxiliary",
            _ => role.ToString()
        };
}
