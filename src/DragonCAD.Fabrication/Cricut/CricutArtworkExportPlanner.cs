using System.Globalization;
using System.Text;

namespace DragonCAD.Fabrication.Cricut;

public static class CricutArtworkExportPlanner
{
    public static CricutArtworkManifest Plan(CricutArtworkExportPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string projectName = RequireText(request.ProjectName, nameof(request.ProjectName));
        string projectSlug = CreateSlug(projectName, nameof(request.ProjectName));
        CricutArtworkSourceLayer[] sourceLayers = (request.SourceLayers ?? []).ToArray();

        if (request.Scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Scale), request.Scale, "Scale must be greater than zero.");
        }

        List<CricutArtworkManifestEntry> entries = [];

        if (request.IncludeCopperVinyl)
        {
            entries.AddRange(sourceLayers
                .Where(layer => layer.Kind == CricutArtworkSourceLayerKind.Copper)
                .Select(layer => CreateLayerEntry(
                    CricutArtworkOutputKind.CopperVinyl,
                    layer,
                    $"{projectSlug}-{SideSlug(layer.Side)}-copper-vinyl.svg",
                    request.Units,
                    request.Scale,
                    mirror: layer.Side == CricutArtworkBoardSide.Bottom)));
        }

        if (request.IncludeSolderPaste)
        {
            entries.AddRange(sourceLayers
                .Where(layer => layer.Kind == CricutArtworkSourceLayerKind.SolderPaste)
                .Select(layer => CreateLayerEntry(
                    CricutArtworkOutputKind.SolderPaste,
                    layer,
                    $"{projectSlug}-{SideSlug(layer.Side)}-solder-paste-stencil.svg",
                    request.Units,
                    request.Scale,
                    mirror: false)));
        }

        entries.Add(CreateBoardOutlineEntry(sourceLayers, projectSlug, request.Units, request.Scale));

        if (request.IncludeRegistrationMarks)
        {
            entries.Add(new CricutArtworkManifestEntry(
                CricutArtworkOutputKind.RegistrationMarks,
                SourceLayerName: null,
                $"{projectSlug}-registration-marks.svg",
                request.Units,
                request.Scale,
                Mirror: false,
                Blockers: []));
        }

        return new CricutArtworkManifest(projectName, SortEntries(entries));
    }

    private static CricutArtworkManifestEntry CreateLayerEntry(
        CricutArtworkOutputKind outputKind,
        CricutArtworkSourceLayer layer,
        string outputFileName,
        CricutArtworkUnits units,
        decimal scale,
        bool mirror)
    {
        return new CricutArtworkManifestEntry(
            outputKind,
            layer.Name,
            outputFileName,
            units,
            scale,
            mirror,
            CreateGeometryBlockers(layer, MissingGeometryCode(outputKind)));
    }

    private static CricutArtworkManifestEntry CreateBoardOutlineEntry(
        IReadOnlyList<CricutArtworkSourceLayer> sourceLayers,
        string projectSlug,
        CricutArtworkUnits units,
        decimal scale)
    {
        CricutArtworkSourceLayer? outline = sourceLayers
            .Where(layer => layer.Kind == CricutArtworkSourceLayerKind.BoardOutline)
            .OrderBy(layer => layer.Name, StringComparer.Ordinal)
            .FirstOrDefault();

        IReadOnlyList<CricutArtworkBlocker> blockers = outline is null
            ? [new CricutArtworkBlocker(
                CricutArtworkBlockerCodes.MissingBoardOutlineGeometry,
                "Board outline geometry is required for Cricut artwork alignment.",
                SourceLayerName: null)]
            : CreateGeometryBlockers(outline, CricutArtworkBlockerCodes.MissingBoardOutlineGeometry);

        return new CricutArtworkManifestEntry(
            CricutArtworkOutputKind.BoardOutline,
            outline?.Name,
            $"{projectSlug}-board-outline.svg",
            units,
            scale,
            Mirror: false,
            blockers);
    }

    private static IReadOnlyList<CricutArtworkBlocker> CreateGeometryBlockers(
        CricutArtworkSourceLayer layer,
        string code)
    {
        if (layer.HasGeometry)
        {
            return [];
        }

        return
        [
            new CricutArtworkBlocker(
                code,
                $"Source layer '{layer.Name}' does not contain exportable artwork geometry.",
                layer.Name)
        ];
    }

    private static CricutArtworkManifestEntry[] SortEntries(IEnumerable<CricutArtworkManifestEntry> entries)
    {
        return entries
            .OrderBy(entry => OutputSortGroup(entry.OutputKind))
            .ThenBy(entry => SideSortGroup(entry.SourceLayerName, entry.OutputFileName))
            .ThenBy(entry => entry.OutputFileName, StringComparer.Ordinal)
            .ToArray();
    }

    private static int OutputSortGroup(CricutArtworkOutputKind outputKind)
    {
        return outputKind switch
        {
            CricutArtworkOutputKind.CopperVinyl => 100,
            CricutArtworkOutputKind.SolderPaste => 200,
            CricutArtworkOutputKind.BoardOutline => 300,
            CricutArtworkOutputKind.RegistrationMarks => 400,
            _ => 900
        };
    }

    private static int SideSortGroup(string? sourceLayerName, string outputFileName)
    {
        string text = $"{sourceLayerName} {outputFileName}";
        if (text.Contains("top", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (text.Contains("bottom", StringComparison.OrdinalIgnoreCase))
        {
            return 200;
        }

        return 300;
    }

    private static string MissingGeometryCode(CricutArtworkOutputKind outputKind)
    {
        return outputKind switch
        {
            CricutArtworkOutputKind.CopperVinyl => CricutArtworkBlockerCodes.MissingCopperGeometry,
            CricutArtworkOutputKind.SolderPaste => CricutArtworkBlockerCodes.MissingSolderPasteGeometry,
            _ => CricutArtworkBlockerCodes.MissingArtworkGeometry
        };
    }

    private static string SideSlug(CricutArtworkBoardSide side)
    {
        return side switch
        {
            CricutArtworkBoardSide.Top => "top",
            CricutArtworkBoardSide.Bottom => "bottom",
            CricutArtworkBoardSide.Board => "board",
            _ => CreateSlug(side.ToString(), nameof(side))
        };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Project name must not be empty.", parameterName);
        }

        return value.Trim();
    }

    private static string CreateSlug(string value, string parameterName)
    {
        string text = RequireText(value, parameterName).ToLower(CultureInfo.InvariantCulture);
        StringBuilder builder = new(text.Length);
        bool previousWasSeparator = false;

        foreach (char character in text)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        string slug = builder.ToString().Trim('-');
        if (slug.Length == 0)
        {
            throw new ArgumentException("Project name must contain at least one letter or digit.", parameterName);
        }

        return slug;
    }
}
