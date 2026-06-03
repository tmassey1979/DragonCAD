using System.Text;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.PcbCart;

public static class PcbCartProductionHandoffBuilder
{
    private const string NewLine = "\r\n";

    public static PcbCartProductionHandoffPackage BuildQuotePackage(
        PcbCartProductionHandoffRequest request,
        bool isFormalApiConfigured = false)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<PcbCartDiagnostic> diagnostics = Validate(request);
        List<PcbCartHandoffArtifact> artifacts = BuildArtifacts(request);
        PcbCartProviderCapabilities capabilities = PcbCartProviderCapabilities.Create(isFormalApiConfigured);

        return new PcbCartProductionHandoffPackage(
            capabilities,
            artifacts,
            diagnostics,
            FormatReviewSummary(capabilities, request, artifacts, diagnostics));
    }

    private static List<PcbCartDiagnostic> Validate(PcbCartProductionHandoffRequest request)
    {
        List<PcbCartDiagnostic> diagnostics = [];

        AddMissingRoleDiagnostic(diagnostics, request, ManufacturingFileRole.Gerber, "missing-gerbers", "Gerber files are required for a PCBCart production quote.");
        AddMissingRoleDiagnostic(diagnostics, request, ManufacturingFileRole.Drill, "missing-drill-files", "Drill files are required for a PCBCart production quote.");

        if (request.Stackup is null)
        {
            diagnostics.Add(Error("missing-board-stackup", "Board stackup summary is required for a PCBCart production quote."));
        }

        if (!request.IncludesAssembly)
        {
            return diagnostics;
        }

        AddMissingRoleDiagnostic(diagnostics, request, ManufacturingFileRole.BillOfMaterials, "missing-bom", "Assembly quote package is missing a bill of materials.");
        AddMissingRoleDiagnostic(diagnostics, request, ManufacturingFileRole.PickAndPlace, "missing-pick-and-place", "Assembly quote package is missing pick-and-place placement data.");

        foreach (PcbCartBomItem item in request.BomItems.Where(item => string.IsNullOrWhiteSpace(item.ManufacturerPartNumber)))
        {
            diagnostics.Add(Error("missing-manufacturer-part-number", $"BOM item {item.Designator} is missing a manufacturer part number."));
        }

        HashSet<string> placedDesignators = request.Placements
            .Select(placement => placement.Designator)
            .ToHashSet(StringComparer.Ordinal);

        foreach (PcbCartBomItem item in request.BomItems.Where(item => !placedDesignators.Contains(item.Designator)))
        {
            diagnostics.Add(Error("missing-placement-data", $"BOM item {item.Designator} is missing placement data."));
        }

        return diagnostics;
    }

    private static void AddMissingRoleDiagnostic(
        List<PcbCartDiagnostic> diagnostics,
        PcbCartProductionHandoffRequest request,
        ManufacturingFileRole role,
        string code,
        string message)
    {
        if (!request.Manifest.Entries.Any(entry => entry.Role == role))
        {
            diagnostics.Add(Error(code, message));
        }
    }

    private static PcbCartDiagnostic Error(string code, string message)
    {
        return new PcbCartDiagnostic(PcbCartDiagnosticSeverity.Error, code, message);
    }

    private static List<PcbCartHandoffArtifact> BuildArtifacts(PcbCartProductionHandoffRequest request)
    {
        List<PcbCartHandoffArtifact> artifacts = request.Manifest.Entries
            .Select(entry => new PcbCartHandoffArtifact(
                entry.Role.ToString(),
                "manufacturing-file",
                entry.RelativePath.Value,
                $"{entry.Role}: {entry.RelativePath.Value} ({entry.Checksum.Value})"))
            .ToList();

        artifacts.Add(new PcbCartHandoffArtifact(
            "Board stackup summary",
            "quote-metadata",
            null,
            request.Stackup is null
                ? "Stackup: missing"
                : $"Stackup: {request.Stackup.LayerCount} layers, {request.Stackup.Material}, {request.Stackup.FinishedThickness}, {request.Stackup.OuterCopperWeight} outer copper"));

        artifacts.Add(new PcbCartHandoffArtifact("Board finish", "quote-metadata", null, $"Finish: {request.Finish}"));
        artifacts.Add(new PcbCartHandoffArtifact("Quantity", "quote-metadata", null, $"Quantity: {request.Quantity}"));
        artifacts.Add(new PcbCartHandoffArtifact("Assembly side", "quote-metadata", null, $"AssemblySide: {request.AssemblySide}"));
        artifacts.Add(new PcbCartHandoffArtifact("Notes", "quote-metadata", null, $"Notes: {FormatNotes(request.Notes)}"));

        return artifacts
            .OrderBy(artifact => artifact.Kind, StringComparer.Ordinal)
            .ThenBy(artifact => artifact.Name, StringComparer.Ordinal)
            .ThenBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    private static string FormatReviewSummary(
        PcbCartProviderCapabilities capabilities,
        PcbCartProductionHandoffRequest request,
        IReadOnlyList<PcbCartHandoffArtifact> artifacts,
        IReadOnlyList<PcbCartDiagnostic> diagnostics)
    {
        StringBuilder builder = new();

        builder.Append("Provider: ");
        builder.Append(capabilities.DisplayName);
        builder.Append(" (");
        builder.Append(capabilities.ProviderId);
        builder.Append(')');
        builder.Append(NewLine);

        builder.Append("HandoffMode: ");
        builder.Append(capabilities.HandoffMode);
        builder.Append(NewLine);

        builder.Append("AutomaticProductionSubmission: ");
        builder.Append(capabilities.AllowsAutomaticProductionSubmission ? "allowed" : "disabled");
        builder.Append(NewLine);

        builder.Append("Quantity: ");
        builder.Append(request.Quantity);
        builder.Append(NewLine);

        builder.Append("Finish: ");
        builder.Append(request.Finish);
        builder.Append(NewLine);

        builder.Append("AssemblySide: ");
        builder.Append(request.AssemblySide);
        builder.Append(NewLine);

        builder.Append("Artifacts:");
        builder.Append(NewLine);
        foreach (PcbCartHandoffArtifact artifact in artifacts)
        {
            builder.Append("- ");
            builder.Append(artifact.Name);
            builder.Append(": ");
            builder.Append(artifact.ReviewText);
            builder.Append(NewLine);
        }

        builder.Append("Diagnostics: ");
        if (diagnostics.Count == 0)
        {
            builder.Append("none");
            return builder.ToString();
        }

        builder.Append(NewLine);
        foreach (PcbCartDiagnostic diagnostic in diagnostics)
        {
            builder.Append("- ");
            builder.Append(diagnostic.Severity);
            builder.Append(' ');
            builder.Append(diagnostic.Code);
            builder.Append(": ");
            builder.Append(diagnostic.Message);
            builder.Append(NewLine);
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }

    private static string FormatNotes(string notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? "none" : notes;
    }
}
