using System.Globalization;
using System.Text;

namespace DragonCAD.Fabrication.Outputs.Gerber;

public static class GerberDrillManifestGenerator
{
    public static GerberDrillManifest Generate(GerberDrillManifestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string projectName = RequireText(request.ProjectName, nameof(request.ProjectName));
        string projectSlug = CreateSlug(projectName, nameof(request.ProjectName));
        string boardName = request.BoardName?.Trim() ?? string.Empty;
        string revision = request.Revision?.Trim() ?? string.Empty;
        GerberBoardLayer[] layers = (request.Layers ?? []).ToArray();

        if (request.ViaCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.ViaCount), request.ViaCount, "Via count must not be negative.");
        }

        if (request.ThroughHolePadCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.ThroughHolePadCount), request.ThroughHolePadCount, "Through-hole pad count must not be negative.");
        }

        List<GerberDrillManifestEntry> entries = CreateGerberEntries(layers, projectSlug);
        if (request.ViaCount > 0 || request.ThroughHolePadCount > 0)
        {
            entries.Add(CreateDrillEntry(projectSlug, request.ViaCount, request.ThroughHolePadCount));
        }

        GerberDrillManifestMetadata metadata = new(
            CopperLayerCount: layers.Count(layer => layer.Kind == GerberBoardLayerKind.Copper),
            OutputFileCount: entries.Count,
            ViaCount: request.ViaCount,
            ThroughHolePadCount: request.ThroughHolePadCount);

        return new GerberDrillManifest(projectName, boardName, revision, metadata, entries);
    }

    private static List<GerberDrillManifestEntry> CreateGerberEntries(
        IEnumerable<GerberBoardLayer> layers,
        string projectSlug)
    {
        return layers
            .Select((layer, originalIndex) => new { Layer = layer, OriginalIndex = originalIndex })
            .OrderBy(item => LayerSortGroup(item.Layer))
            .ThenBy(item => LayerSideSortGroup(item.Layer))
            .ThenBy(item => item.Layer.CopperLayerNumber ?? int.MaxValue)
            .ThenBy(item => item.Layer.Name, StringComparer.Ordinal)
            .ThenBy(item => item.OriginalIndex)
            .Select(item => CreateGerberEntry(item.Layer, projectSlug))
            .ToList();
    }

    private static GerberDrillManifestEntry CreateGerberEntry(GerberBoardLayer layer, string projectSlug)
    {
        string extension = GetGerberExtension(layer);
        string relativePath = $"gerbers/{projectSlug}.{extension}";
        string outputName = GetOutputName(layer);
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["kind"] = layer.Kind.ToString(),
            ["side"] = layer.Side.ToString(),
            ["format"] = "RS-274X"
        };

        if (layer.CopperLayerNumber is not null)
        {
            metadata["copperLayerNumber"] = layer.CopperLayerNumber.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new GerberDrillManifestEntry(
            ManufacturingFileRole.Gerber,
            ManufacturingRelativePath.Create(relativePath),
            ManufacturingChecksum.Create($"pending:gerber-{projectSlug}-{CreateSlug(outputName, nameof(layer.Name))}"),
            outputName,
            layer.Name,
            layer.Kind,
            layer.Side,
            metadata);
    }

    private static GerberDrillManifestEntry CreateDrillEntry(
        string projectSlug,
        int viaCount,
        int throughHolePadCount)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["format"] = "Excellon",
            ["plating"] = "Plated",
            ["viaCount"] = viaCount.ToString(CultureInfo.InvariantCulture),
            ["throughHolePadCount"] = throughHolePadCount.ToString(CultureInfo.InvariantCulture)
        };

        return new GerberDrillManifestEntry(
            ManufacturingFileRole.Drill,
            ManufacturingRelativePath.Create($"drill/{projectSlug}.drl"),
            ManufacturingChecksum.Create($"pending:drill-{projectSlug}-plated"),
            "Plated Drill",
            SourceLayerName: null,
            LayerKind: null,
            Side: null,
            metadata);
    }

    private static string GetGerberExtension(GerberBoardLayer layer)
    {
        return (layer.Kind, layer.Side) switch
        {
            (GerberBoardLayerKind.Copper, GerberBoardSide.Top) => "GTL",
            (GerberBoardLayerKind.Copper, GerberBoardSide.Bottom) => "GBL",
            (GerberBoardLayerKind.Copper, GerberBoardSide.Inner) => $"G{RequireCopperLayerNumber(layer)}L",
            (GerberBoardLayerKind.SolderMask, GerberBoardSide.Top) => "GTS",
            (GerberBoardLayerKind.SolderMask, GerberBoardSide.Bottom) => "GBS",
            (GerberBoardLayerKind.Silkscreen, GerberBoardSide.Top) => "GTO",
            (GerberBoardLayerKind.Silkscreen, GerberBoardSide.Bottom) => "GBO",
            (GerberBoardLayerKind.BoardOutline, _) => "GKO",
            _ => throw new ArgumentException($"Unsupported Gerber layer mapping '{layer.Kind}' on '{layer.Side}'.", nameof(layer))
        };
    }

    private static int RequireCopperLayerNumber(GerberBoardLayer layer)
    {
        if (layer.CopperLayerNumber is not > 1)
        {
            throw new ArgumentException("Inner copper layers must include a copper layer number greater than 1.", nameof(layer));
        }

        return layer.CopperLayerNumber.Value;
    }

    private static string GetOutputName(GerberBoardLayer layer)
    {
        return (layer.Kind, layer.Side) switch
        {
            (GerberBoardLayerKind.Copper, GerberBoardSide.Top) => "Top Copper",
            (GerberBoardLayerKind.Copper, GerberBoardSide.Bottom) => "Bottom Copper",
            (GerberBoardLayerKind.Copper, GerberBoardSide.Inner) => $"Inner Copper {RequireCopperLayerNumber(layer)}",
            (GerberBoardLayerKind.SolderMask, GerberBoardSide.Top) => "Top Solder Mask",
            (GerberBoardLayerKind.SolderMask, GerberBoardSide.Bottom) => "Bottom Solder Mask",
            (GerberBoardLayerKind.Silkscreen, GerberBoardSide.Top) => "Top Silkscreen",
            (GerberBoardLayerKind.Silkscreen, GerberBoardSide.Bottom) => "Bottom Silkscreen",
            (GerberBoardLayerKind.BoardOutline, _) => "Board Outline",
            _ => layer.Name
        };
    }

    private static int LayerSortGroup(GerberBoardLayer layer)
    {
        return layer.Kind switch
        {
            GerberBoardLayerKind.Copper => 100,
            GerberBoardLayerKind.SolderMask => 200,
            GerberBoardLayerKind.Silkscreen => 300,
            GerberBoardLayerKind.BoardOutline => 400,
            _ => 900
        };
    }

    private static int LayerSideSortGroup(GerberBoardLayer layer)
    {
        return layer.Side switch
        {
            GerberBoardSide.Top => 100,
            GerberBoardSide.Inner => 200,
            GerberBoardSide.Bottom => 300,
            GerberBoardSide.Board => 400,
            _ => 900
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
        string text = RequireText(value, parameterName).ToLowerInvariant();
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
