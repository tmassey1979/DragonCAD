using System.Globalization;
using System.Security;
using System.Text;

namespace DragonCAD.Fabrication.Cricut;

public static class CricutSvgArtworkWriter
{
    private const string EmptyManifestCode = "empty-cricut-artwork-manifest";

    public static CricutArtworkWriteResult Write(CricutArtworkManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        List<CricutArtworkSvgFile> files = [];
        List<CricutArtworkWriteDiagnostic> diagnostics = [];

        if (manifest.Entries.Count == 0)
        {
            diagnostics.Add(new CricutArtworkWriteDiagnostic(
                manifest.ProjectName,
                OutputFileName: null,
                EmptyManifestCode,
                "Cricut artwork manifest does not contain any SVG outputs.",
                SourceLayerName: null));

            return new CricutArtworkWriteResult(files, diagnostics);
        }

        foreach (CricutArtworkManifestEntry entry in SortEntries(manifest.Entries))
        {
            if (entry.Blockers.Count > 0)
            {
                diagnostics.AddRange(entry.Blockers.Select(blocker => new CricutArtworkWriteDiagnostic(
                    manifest.ProjectName,
                    entry.OutputFileName,
                    blocker.Code,
                    blocker.Message,
                    blocker.SourceLayerName)));
                continue;
            }

            files.Add(new CricutArtworkSvgFile(entry.OutputFileName, CreateSvg(manifest.ProjectName, entry)));
        }

        return new CricutArtworkWriteResult(files, diagnostics);
    }

    private static string CreateSvg(string projectName, CricutArtworkManifestEntry entry)
    {
        string unitSuffix = UnitSuffix(entry.Units);
        string documentSize = FormatMeasurement(DocumentSize(entry.Units));
        string scale = FormatDecimal(entry.Scale);
        string layerName = entry.SourceLayerName ?? string.Empty;
        string layerId = CreateLayerId(entry);
        string side = InferSide(entry);
        string mirror = entry.Mirror ? "true" : "false";

        StringBuilder builder = new();
        builder.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{documentSize}{unitSuffix}\" height=\"{documentSize}{unitSuffix}\" viewBox=\"0 0 100 100\">\n");
        builder.Append(CultureInfo.InvariantCulture, $"  <title>{Escape(projectName)} - {Escape(GetTitleLayer(entry))}</title>\n");
        builder.Append(CultureInfo.InvariantCulture, $"  <metadata>{{\"kind\":\"{entry.OutputKind}\",\"sourceLayer\":\"{Escape(layerName)}\",\"units\":\"{entry.Units}\",\"scale\":\"{scale}\",\"mirror\":{mirror},\"side\":\"{side}\"}}</metadata>\n");
        builder.Append(CultureInfo.InvariantCulture, $"  <g id=\"{Escape(layerId)}\" data-output-kind=\"{entry.OutputKind}\" data-source-layer=\"{Escape(layerName)}\" data-units=\"{entry.Units}\" data-scale=\"{scale}\" data-mirror=\"{mirror}\" data-side=\"{side}\"");

        if (entry.Mirror)
        {
            builder.Append(" transform=\"translate(100 0) scale(-1 1)\"");
        }

        builder.Append(">\n");
        AppendArtwork(builder, entry, layerId);
        builder.Append("  </g>\n");
        builder.Append("</svg>\n");

        return builder.ToString();
    }

    private static void AppendArtwork(StringBuilder builder, CricutArtworkManifestEntry entry, string layerId)
    {
        switch (entry.OutputKind)
        {
            case CricutArtworkOutputKind.CopperVinyl:
                builder.Append(CultureInfo.InvariantCulture, $"    <path id=\"{Escape(layerId)}-artwork\" d=\"M 10 10 H 90 V 90 H 10 Z\" fill=\"#b87333\" stroke=\"none\" />\n");
                break;
            case CricutArtworkOutputKind.SolderPaste:
                builder.Append(CultureInfo.InvariantCulture, $"    <path id=\"{Escape(layerId)}-artwork\" d=\"M 18 18 H 38 V 38 H 18 Z M 62 62 H 82 V 82 H 62 Z\" fill=\"#808080\" stroke=\"none\" />\n");
                break;
            case CricutArtworkOutputKind.BoardOutline:
                builder.Append(CultureInfo.InvariantCulture, $"    <path id=\"{Escape(layerId)}-artwork\" d=\"M 5 5 H 95 V 95 H 5 Z\" fill=\"none\" stroke=\"#111111\" stroke-width=\"0.5\" />\n");
                break;
            case CricutArtworkOutputKind.RegistrationMarks:
                builder.Append("    <circle id=\"registration-mark-top-left\" cx=\"8\" cy=\"8\" r=\"2\" fill=\"#111111\" />\n");
                builder.Append("    <circle id=\"registration-mark-top-right\" cx=\"92\" cy=\"8\" r=\"2\" fill=\"#111111\" />\n");
                builder.Append("    <circle id=\"registration-mark-bottom-left\" cx=\"8\" cy=\"92\" r=\"2\" fill=\"#111111\" />\n");
                builder.Append("    <circle id=\"registration-mark-bottom-right\" cx=\"92\" cy=\"92\" r=\"2\" fill=\"#111111\" />\n");
                break;
            default:
                builder.Append(CultureInfo.InvariantCulture, $"    <path id=\"{Escape(layerId)}-artwork\" d=\"M 10 10 H 90 V 90 H 10 Z\" fill=\"none\" stroke=\"#111111\" stroke-width=\"0.5\" />\n");
                break;
        }
    }

    private static IEnumerable<CricutArtworkManifestEntry> SortEntries(IEnumerable<CricutArtworkManifestEntry> entries)
    {
        return entries
            .OrderBy(entry => (int)entry.OutputKind)
            .ThenBy(entry => entry.OutputFileName, StringComparer.Ordinal);
    }

    private static string CreateLayerId(CricutArtworkManifestEntry entry)
    {
        string name = entry.OutputKind == CricutArtworkOutputKind.RegistrationMarks
            ? "registration marks"
            : entry.SourceLayerName ?? entry.OutputKind.ToString();

        StringBuilder builder = new(name.Length);
        bool previousWasSeparator = false;

        foreach (char character in name.ToLower(CultureInfo.InvariantCulture))
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

        return builder.ToString().Trim('-');
    }

    private static string InferSide(CricutArtworkManifestEntry entry)
    {
        string text = $"{entry.SourceLayerName} {entry.OutputFileName}";
        if (text.Contains("bottom", StringComparison.OrdinalIgnoreCase))
        {
            return "Bottom";
        }

        if (text.Contains("top", StringComparison.OrdinalIgnoreCase))
        {
            return "Top";
        }

        return entry.OutputKind == CricutArtworkOutputKind.BoardOutline ? "Board" : string.Empty;
    }

    private static string GetTitleLayer(CricutArtworkManifestEntry entry)
    {
        return entry.SourceLayerName ?? entry.OutputKind switch
        {
            CricutArtworkOutputKind.RegistrationMarks => "Registration Marks",
            CricutArtworkOutputKind.BoardOutline => "Board Outline",
            _ => entry.OutputKind.ToString()
        };
    }

    private static decimal DocumentSize(CricutArtworkUnits units)
    {
        return units == CricutArtworkUnits.Inches ? 100m / 25.4m : 100m;
    }

    private static string UnitSuffix(CricutArtworkUnits units)
    {
        return units == CricutArtworkUnits.Inches ? "in" : "mm";
    }

    private static string FormatMeasurement(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.#############################", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
    }
}
