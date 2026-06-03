using DragonCAD.Fabrication.Outputs;
using DragonCAD.Fabrication.Outputs.Gerber;

namespace DragonCAD.Fabrication.OshPark;

public static class OshParkPrototypeHandoffPlanner
{
    private static readonly Uri UploadUri = new("https://oshpark.com/uploads/new");

    public static OshParkPrototypeHandoffPackage Plan(OshParkPrototypeHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OshParkHandoffDiagnostic[] diagnostics = CreateDiagnostics(request).ToArray();
        bool hasBlockingDiagnostic = diagnostics.Any(diagnostic => diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Error
            || (diagnostic.Severity == OshParkHandoffDiagnosticSeverity.Warning && !request.AcceptedWarningCodes.Contains(diagnostic.Code)));
        Uri? uploadLink = hasBlockingDiagnostic ? null : UploadUri;

        return new OshParkPrototypeHandoffPackage(
            Gerbers: request.Manifest.Entries.Where(entry => entry.Role == ManufacturingFileRole.Gerber).ToArray(),
            DrillFiles: request.Manifest.Entries.Where(entry => entry.Role == ManufacturingFileRole.Drill).ToArray(),
            BoardOutline: request.LayerMappings.FirstOrDefault(mapping => mapping.LayerKind == GerberBoardLayerKind.BoardOutline),
            LayerMappings: request.LayerMappings,
            BoardDimensions: request.BoardDimensions,
            Manifest: request.Manifest,
            Diagnostics: diagnostics,
            IsReadyForUploadHandoff: uploadLink is not null,
            UploadHandoffUri: uploadLink);
    }

    private static IEnumerable<OshParkHandoffDiagnostic> CreateDiagnostics(OshParkPrototypeHandoffRequest request)
    {
        if (!request.Manifest.Entries.Any(entry => entry.Role == ManufacturingFileRole.Gerber))
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.MissingGerber,
                "OSH Park prototype handoff requires at least one Gerber file.");
        }

        if (!request.Manifest.Entries.Any(entry => entry.Role == ManufacturingFileRole.Drill))
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.MissingDrillFile,
                "OSH Park prototype handoff requires an Excellon drill file.");
        }

        if (!request.LayerMappings.Any(mapping => mapping.LayerKind == GerberBoardLayerKind.BoardOutline))
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.MissingBoardOutline,
                "OSH Park prototype handoff requires a board outline layer mapping.");
        }

        if (request.BoardDimensions.WidthMillimeters <= 0 || request.BoardDimensions.HeightMillimeters <= 0)
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.MissingBoardDimensions,
                "OSH Park prototype handoff requires positive board width and height.");
        }

        HashSet<ManufacturingRelativePath> gerberPaths = request.Manifest.Entries
            .Where(entry => entry.Role == ManufacturingFileRole.Gerber)
            .Select(entry => entry.RelativePath)
            .ToHashSet();
        HashSet<ManufacturingRelativePath> mappedPaths = request.LayerMappings
            .Select(mapping => mapping.RelativePath)
            .ToHashSet();

        foreach (OshParkLayerMapping mapping in request.LayerMappings.Where(mapping => !gerberPaths.Contains(mapping.RelativePath)))
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.LayerMismatch,
                $"Layer mapping '{mapping.DisplayName}' references '{mapping.RelativePath}', which is not a Gerber in the manifest.");
        }

        foreach (ManufacturingOutputEntry entry in request.Manifest.Entries.Where(entry => entry.Role == ManufacturingFileRole.Gerber && !mappedPaths.Contains(entry.RelativePath)))
        {
            yield return OshParkHandoffDiagnostic.Error(
                OshParkHandoffDiagnosticCodes.LayerMismatch,
                $"Gerber '{entry.RelativePath}' is not represented in the OSH Park layer mapping.");
        }

        foreach (OshParkHandoffWarning warning in request.Warnings)
        {
            yield return OshParkHandoffDiagnostic.Warning(warning.Code, warning.Message);
        }

        yield return OshParkHandoffDiagnostic.Info(
            OshParkHandoffDiagnosticCodes.UploadLimitations,
            "OSH Park upload handoff cannot currently fetch OSH Park previews or warnings, or attach this project to the user's OSH Park account through the upload path.");
    }
}

public sealed record OshParkPrototypeHandoffRequest
{
    public OshParkPrototypeHandoffRequest(
        ManufacturingOutputManifest manifest,
        OshParkBoardDimensions boardDimensions,
        IReadOnlyList<OshParkLayerMapping> layerMappings,
        IReadOnlyList<OshParkHandoffWarning>? warnings = null,
        IReadOnlyList<string>? acceptedWarningCodes = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(layerMappings);

        Manifest = manifest;
        BoardDimensions = boardDimensions;
        LayerMappings = layerMappings
            .OrderBy(mapping => mapping.LayerKind)
            .ThenBy(mapping => mapping.Side)
            .ThenBy(mapping => mapping.DisplayName, StringComparer.Ordinal)
            .ToArray();
        Warnings = (warnings ?? [])
            .OrderBy(warning => warning.Code, StringComparer.Ordinal)
            .ToArray();
        AcceptedWarningCodes = (acceptedWarningCodes ?? [])
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public ManufacturingOutputManifest Manifest { get; }

    public OshParkBoardDimensions BoardDimensions { get; }

    public IReadOnlyList<OshParkLayerMapping> LayerMappings { get; }

    public IReadOnlyList<OshParkHandoffWarning> Warnings { get; }

    public IReadOnlyList<string> AcceptedWarningCodes { get; }
}

public sealed record OshParkPrototypeHandoffPackage(
    IReadOnlyList<ManufacturingOutputEntry> Gerbers,
    IReadOnlyList<ManufacturingOutputEntry> DrillFiles,
    OshParkLayerMapping? BoardOutline,
    IReadOnlyList<OshParkLayerMapping> LayerMappings,
    OshParkBoardDimensions BoardDimensions,
    ManufacturingOutputManifest Manifest,
    IReadOnlyList<OshParkHandoffDiagnostic> Diagnostics,
    bool IsReadyForUploadHandoff,
    Uri? UploadHandoffUri);

public sealed record OshParkBoardDimensions(decimal WidthMillimeters, decimal HeightMillimeters);

public sealed record OshParkLayerMapping
{
    public OshParkLayerMapping(
        ManufacturingRelativePath relativePath,
        GerberBoardLayerKind layerKind,
        GerberBoardSide side,
        string displayName)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        RelativePath = relativePath;
        LayerKind = layerKind;
        Side = side;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? layerKind.ToString() : displayName.Trim();
    }

    public ManufacturingRelativePath RelativePath { get; }

    public GerberBoardLayerKind LayerKind { get; }

    public GerberBoardSide Side { get; }

    public string DisplayName { get; }
}

public sealed record OshParkHandoffWarning
{
    public OshParkHandoffWarning(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("OSH Park warning code must not be empty.", nameof(code));
        }

        Code = code.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? Code : message.Trim();
    }

    public string Code { get; }

    public string Message { get; }
}

public sealed record OshParkHandoffDiagnostic(
    string Code,
    OshParkHandoffDiagnosticSeverity Severity,
    string Message)
{
    public static OshParkHandoffDiagnostic Error(string code, string message)
    {
        return new OshParkHandoffDiagnostic(code, OshParkHandoffDiagnosticSeverity.Error, message);
    }

    public static OshParkHandoffDiagnostic Warning(string code, string message)
    {
        return new OshParkHandoffDiagnostic(code, OshParkHandoffDiagnosticSeverity.Warning, message);
    }

    public static OshParkHandoffDiagnostic Info(string code, string message)
    {
        return new OshParkHandoffDiagnostic(code, OshParkHandoffDiagnosticSeverity.Info, message);
    }
}

public enum OshParkHandoffDiagnosticSeverity
{
    Info = 100,
    Warning = 200,
    Error = 300
}

public static class OshParkHandoffDiagnosticCodes
{
    public const string MissingGerber = "missing-gerber";
    public const string MissingDrillFile = "missing-drill-file";
    public const string MissingBoardOutline = "missing-board-outline";
    public const string MissingBoardDimensions = "missing-board-dimensions";
    public const string LayerMismatch = "layer-mismatch";
    public const string UploadLimitations = "upload-limitations";
}
