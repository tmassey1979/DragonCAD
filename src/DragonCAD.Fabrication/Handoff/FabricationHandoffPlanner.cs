using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Handoff;

public static class FabricationHandoffPlanner
{
    public static FabricationHandoffPlan Plan(FabricationHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        HandoffProviderDefinition provider = HandoffProviderDefinition.For(request.Provider);
        FabricationHandoffArtifact[] requiredArtifacts = provider.RequiredRoles
            .Select(role => CreateArtifact(role, request.Manifest))
            .ToArray();
        FabricationHandoffPlanWarning[] warnings = CreateWarnings(request);
        FabricationHandoffBlocker[] blockers = CreateBlockers(provider, request, requiredArtifacts, warnings).ToArray();

        return new FabricationHandoffPlan(
            request.Provider,
            provider.DisplayName,
            requiredArtifacts,
            warnings,
            blockers,
            blockers.Length == 0,
            FabricationHandoffAction.ManualOnly(provider.DisplayName),
            CreatePackageHash(provider, request, requiredArtifacts, warnings),
            request.ProductionDetails?.Stackup,
            request.ProductionDetails?.Quantity,
            request.ProductionDetails?.AssemblySide);
    }

    private static FabricationHandoffArtifact CreateArtifact(
        ManufacturingFileRole role,
        ManufacturingOutputManifest manifest)
    {
        FabricationHandoffArtifactFile[] files = manifest.Entries
            .Where(entry => entry.Role == role)
            .OrderBy(entry => entry.RelativePath.Value, StringComparer.Ordinal)
            .Select(entry => new FabricationHandoffArtifactFile(entry.RelativePath, entry.Checksum))
            .ToArray();

        return new FabricationHandoffArtifact(role, files.Length > 0, files);
    }

    private static FabricationHandoffPlanWarning[] CreateWarnings(FabricationHandoffRequest request)
    {
        HashSet<string> acceptedCodes = request.AcceptedWarningCodes.ToHashSet(StringComparer.Ordinal);

        return request.Warnings
            .OrderBy(warning => warning.Code, StringComparer.Ordinal)
            .Select(warning => new FabricationHandoffPlanWarning(
                warning.Code,
                warning.Message,
                acceptedCodes.Contains(warning.Code)))
            .ToArray();
    }

    private static IEnumerable<FabricationHandoffBlocker> CreateBlockers(
        HandoffProviderDefinition provider,
        FabricationHandoffRequest request,
        IReadOnlyList<FabricationHandoffArtifact> requiredArtifacts,
        IReadOnlyList<FabricationHandoffPlanWarning> warnings)
    {
        foreach (FabricationHandoffArtifact artifact in requiredArtifacts.Where(artifact => !artifact.IsPresent))
        {
            yield return MissingArtifactBlocker(artifact.Role, provider.DisplayName);
        }

        foreach (FabricationHandoffPlanWarning warning in warnings.Where(warning => !warning.IsAccepted))
        {
            yield return new FabricationHandoffBlocker(
                FabricationHandoffBlockerCodes.UnacceptedWarning,
                $"Warning '{warning.Code}' must be accepted before preparing the {provider.DisplayName} handoff.",
                FileRole: null,
                WarningCode: warning.Code);
        }

        if (provider.RequiresBoardOutline && !request.BoardDetails.BoardOutlinePresent)
        {
            yield return new FabricationHandoffBlocker(
                FabricationHandoffBlockerCodes.MissingBoardOutline,
                $"{provider.DisplayName} handoff requires an exported board outline.",
                FileRole: null,
                WarningCode: null);
        }

        if (provider.RequiresBoardDimensions
            && (request.BoardDetails.WidthMillimeters <= 0 || request.BoardDetails.HeightMillimeters <= 0))
        {
            yield return new FabricationHandoffBlocker(
                FabricationHandoffBlockerCodes.MissingBoardDimensions,
                $"{provider.DisplayName} handoff requires positive board width and height.",
                FileRole: null,
                WarningCode: null);
        }

        if (provider.RequiresProductionDetails)
        {
            if (request.ProductionDetails is null)
            {
                yield return new FabricationHandoffBlocker(
                    FabricationHandoffBlockerCodes.MissingProductionDetails,
                    $"{provider.DisplayName} handoff requires stackup, quantity, and assembly side.",
                    FileRole: null,
                    WarningCode: null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(request.ProductionDetails.Stackup))
            {
                yield return new FabricationHandoffBlocker(
                    FabricationHandoffBlockerCodes.MissingStackup,
                    $"{provider.DisplayName} handoff requires a stackup description.",
                    FileRole: null,
                    WarningCode: null);
            }

            if (request.ProductionDetails.Quantity <= 0)
            {
                yield return new FabricationHandoffBlocker(
                    FabricationHandoffBlockerCodes.MissingQuantity,
                    $"{provider.DisplayName} handoff requires a positive quantity.",
                    FileRole: null,
                    WarningCode: null);
            }

            if (request.ProductionDetails.AssemblySide == FabricationAssemblySide.Unspecified)
            {
                yield return new FabricationHandoffBlocker(
                    FabricationHandoffBlockerCodes.MissingAssemblySide,
                    $"{provider.DisplayName} handoff requires an assembly side.",
                    FileRole: null,
                    WarningCode: null);
            }
        }
    }

    private static FabricationHandoffBlocker MissingArtifactBlocker(
        ManufacturingFileRole role,
        string providerDisplayName)
    {
        string code = role == ManufacturingFileRole.Drill
            ? FabricationHandoffBlockerCodes.MissingDrill
            : FabricationHandoffBlockerCodes.MissingRequiredArtifact;

        return new FabricationHandoffBlocker(
            code,
            $"{providerDisplayName} handoff requires a {role} artifact.",
            role,
            WarningCode: null);
    }

    private static ManufacturingChecksum CreatePackageHash(
        HandoffProviderDefinition provider,
        FabricationHandoffRequest request,
        IReadOnlyList<FabricationHandoffArtifact> requiredArtifacts,
        IReadOnlyList<FabricationHandoffPlanWarning> warnings)
    {
        StringBuilder builder = new();
        builder.Append("provider=").Append(provider.Id).Append('\n');
        builder.Append("board-outline=").Append(request.BoardDetails.BoardOutlinePresent ? "true" : "false").Append('\n');
        builder.Append("width-mm=").Append(request.BoardDetails.WidthMillimeters.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("height-mm=").Append(request.BoardDetails.HeightMillimeters.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("stackup=").Append(request.ProductionDetails?.Stackup?.Trim() ?? string.Empty).Append('\n');
        builder.Append("quantity=").Append(request.ProductionDetails?.Quantity.ToString(CultureInfo.InvariantCulture) ?? string.Empty).Append('\n');
        builder.Append("assembly-side=").Append(request.ProductionDetails?.AssemblySide.ToString() ?? string.Empty).Append('\n');

        foreach (FabricationHandoffArtifact artifact in requiredArtifacts.OrderBy(artifact => artifact.Role))
        {
            foreach (FabricationHandoffArtifactFile file in artifact.Files.OrderBy(file => file.RelativePath.Value, StringComparer.Ordinal))
            {
                builder.Append("artifact=")
                    .Append(artifact.Role)
                    .Append('|')
                    .Append(file.RelativePath.Value)
                    .Append('|')
                    .Append(file.Checksum.Value)
                    .Append('\n');
            }
        }

        foreach (FabricationHandoffPlanWarning warning in warnings.OrderBy(warning => warning.Code, StringComparer.Ordinal))
        {
            builder.Append("warning=")
                .Append(warning.Code)
                .Append('|')
                .Append(warning.IsAccepted ? "accepted" : "blocked")
                .Append('\n');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return ManufacturingChecksum.Create("sha256:" + Convert.ToHexString(hash).ToLowerInvariant());
    }

    private sealed record HandoffProviderDefinition(
        FabricationHandoffProvider Provider,
        string Id,
        string DisplayName,
        IReadOnlyList<ManufacturingFileRole> RequiredRoles,
        bool RequiresBoardOutline,
        bool RequiresBoardDimensions,
        bool RequiresProductionDetails)
    {
        public static HandoffProviderDefinition For(FabricationHandoffProvider provider)
        {
            return provider switch
            {
                FabricationHandoffProvider.OshPark => new HandoffProviderDefinition(
                    provider,
                    "osh-park",
                    "OSH Park",
                    [ManufacturingFileRole.Gerber, ManufacturingFileRole.Drill],
                    RequiresBoardOutline: true,
                    RequiresBoardDimensions: true,
                    RequiresProductionDetails: false),
                FabricationHandoffProvider.PcbCart => new HandoffProviderDefinition(
                    provider,
                    "pcbcart",
                    "PCBCart",
                    [
                        ManufacturingFileRole.Gerber,
                        ManufacturingFileRole.Drill,
                        ManufacturingFileRole.BillOfMaterials,
                        ManufacturingFileRole.PickAndPlace
                    ],
                    RequiresBoardOutline: false,
                    RequiresBoardDimensions: false,
                    RequiresProductionDetails: true),
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported fabrication handoff provider.")
            };
        }
    }
}

public sealed record FabricationHandoffRequest
{
    public FabricationHandoffRequest(
        FabricationHandoffProvider provider,
        ManufacturingOutputManifest manifest,
        FabricationBoardHandoffDetails boardDetails,
        FabricationProductionHandoffDetails? productionDetails,
        IReadOnlyList<FabricationHandoffWarning> warnings,
        IReadOnlyList<string> acceptedWarningCodes)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(boardDetails);
        ArgumentNullException.ThrowIfNull(warnings);
        ArgumentNullException.ThrowIfNull(acceptedWarningCodes);

        Provider = provider;
        Manifest = manifest;
        BoardDetails = boardDetails;
        ProductionDetails = productionDetails;
        Warnings = warnings
            .OrderBy(warning => warning.Code, StringComparer.Ordinal)
            .ToArray();
        AcceptedWarningCodes = acceptedWarningCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
    }

    public FabricationHandoffProvider Provider { get; init; }

    public ManufacturingOutputManifest Manifest { get; init; }

    public FabricationBoardHandoffDetails BoardDetails { get; init; }

    public FabricationProductionHandoffDetails? ProductionDetails { get; init; }

    public IReadOnlyList<FabricationHandoffWarning> Warnings { get; init; }

    public IReadOnlyList<string> AcceptedWarningCodes { get; init; }
}

public sealed record FabricationBoardHandoffDetails(
    bool BoardOutlinePresent,
    decimal WidthMillimeters,
    decimal HeightMillimeters);

public sealed record FabricationProductionHandoffDetails(
    string Stackup,
    int Quantity,
    FabricationAssemblySide AssemblySide);

public sealed record FabricationHandoffWarning
{
    public FabricationHandoffWarning(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Warning code must not be empty.", nameof(code));
        }

        Code = code.Trim();
        Message = string.IsNullOrWhiteSpace(message)
            ? Code
            : message.Trim();
    }

    public string Code { get; init; }

    public string Message { get; init; }
}

public sealed record FabricationHandoffPlan(
    FabricationHandoffProvider Provider,
    string ProviderDisplayName,
    IReadOnlyList<FabricationHandoffArtifact> RequiredArtifacts,
    IReadOnlyList<FabricationHandoffPlanWarning> Warnings,
    IReadOnlyList<FabricationHandoffBlocker> Blockers,
    bool IsActionEnabled,
    FabricationHandoffAction Action,
    ManufacturingChecksum PackageHash,
    string? Stackup,
    int? Quantity,
    FabricationAssemblySide? AssemblySide);

public sealed record FabricationHandoffArtifact(
    ManufacturingFileRole Role,
    bool IsPresent,
    IReadOnlyList<FabricationHandoffArtifactFile> Files);

public sealed record FabricationHandoffArtifactFile(
    ManufacturingRelativePath RelativePath,
    ManufacturingChecksum Checksum);

public sealed record FabricationHandoffPlanWarning(
    string Code,
    string Message,
    bool IsAccepted);

public sealed record FabricationHandoffBlocker(
    string Code,
    string Message,
    ManufacturingFileRole? FileRole,
    string? WarningCode);

public sealed record FabricationHandoffAction(
    FabricationHandoffActionKind Kind,
    string Label,
    bool AllowsUpload,
    Uri? UploadEndpoint)
{
    public static FabricationHandoffAction ManualOnly(string providerDisplayName)
    {
        return new FabricationHandoffAction(
            FabricationHandoffActionKind.PrepareManualHandoff,
            $"Prepare {providerDisplayName} handoff plan",
            AllowsUpload: false,
            UploadEndpoint: null);
    }
}

public static class FabricationHandoffBlockerCodes
{
    public const string MissingRequiredArtifact = "missing-required-artifact";
    public const string MissingDrill = "missing-drill";
    public const string MissingBoardOutline = "missing-board-outline";
    public const string MissingBoardDimensions = "missing-board-dimensions";
    public const string UnacceptedWarning = "unaccepted-warning";
    public const string MissingProductionDetails = "missing-production-details";
    public const string MissingStackup = "missing-stackup";
    public const string MissingQuantity = "missing-quantity";
    public const string MissingAssemblySide = "missing-assembly-side";
}

public enum FabricationHandoffProvider
{
    OshPark = 100,
    PcbCart = 200
}

public enum FabricationAssemblySide
{
    Unspecified = 0,
    Top = 100,
    Bottom = 200,
    Both = 300
}

public enum FabricationHandoffActionKind
{
    PrepareManualHandoff = 100
}
