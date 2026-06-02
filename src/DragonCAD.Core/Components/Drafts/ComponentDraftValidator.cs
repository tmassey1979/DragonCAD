using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Components.Drafts;

public static class ComponentDraftValidator
{
    public static ComponentDraftValidationResult Validate(ComponentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        List<ComponentDraftDiagnostic> diagnostics = [];

        if (draft.Symbols.Count == 0)
        {
            diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingSymbol, draft.Id.Value, "Draft must include at least one symbol."));
        }

        if (draft.Footprints.Count == 0)
        {
            diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingFootprint, draft.Id.Value, "Draft must include at least one footprint."));
        }

        if (string.IsNullOrWhiteSpace(draft.Package.ReferencePrefix))
        {
            diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingReferencePrefix, draft.Package.Name, "Package must include a reference prefix."));
        }

        AddDuplicatePinDiagnostics(draft.Pins, diagnostics);
        AddDuplicatePadDiagnostics(draft.Footprints, diagnostics);
        AddMappingDiagnostics(draft, diagnostics);

        return new ComponentDraftValidationResult(diagnostics);
    }

    private static void AddDuplicatePinDiagnostics(
        IReadOnlyList<ComponentDraftPin> pins,
        List<ComponentDraftDiagnostic> diagnostics)
    {
        HashSet<ComponentPinId> seen = [];

        foreach (ComponentDraftPin pin in pins)
        {
            if (!seen.Add(pin.Id))
            {
                diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.DuplicatePinId, pin.Id.Value, $"Duplicate pin id '{pin.Id}'."));
            }
        }
    }

    private static void AddDuplicatePadDiagnostics(
        IReadOnlyList<ComponentDraftFootprint> footprints,
        List<ComponentDraftDiagnostic> diagnostics)
    {
        foreach (ComponentDraftFootprint footprint in footprints)
        {
            HashSet<string> padNames = new(StringComparer.Ordinal);

            foreach (ComponentDraftPad pad in footprint.Pads)
            {
                if (!padNames.Add(pad.Name))
                {
                    diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.DuplicatePadName, $"{footprint.Id.Value}:{pad.Name}", $"Footprint '{footprint.Id}' contains duplicate pad name '{pad.Name}'."));
                }
            }
        }
    }

    private static void AddMappingDiagnostics(
        ComponentDraft draft,
        List<ComponentDraftDiagnostic> diagnostics)
    {
        HashSet<ComponentPinId> pinIds = draft.Pins.Select(pin => pin.Id).ToHashSet();
        HashSet<ComponentPinId> mappedPins = draft.DeviceMappings.Select(mapping => mapping.PinId).ToHashSet();
        Dictionary<ComponentFootprintId, ComponentDraftFootprint> footprintsById = draft.Footprints
            .GroupBy(footprint => footprint.Id)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (ComponentDraftPin pin in draft.Pins)
        {
            if (!mappedPins.Contains(pin.Id))
            {
                diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.UnmappedPin, pin.Id.Value, $"Pin '{pin.Id}' is not mapped to a footprint pad."));
            }
        }

        foreach (ComponentDraftDeviceMapping mapping in draft.DeviceMappings)
        {
            if (!pinIds.Contains(mapping.PinId))
            {
                diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingPin, mapping.PinId.Value, $"Mapping references missing pin '{mapping.PinId}'."));
            }

            if (!footprintsById.TryGetValue(mapping.FootprintId, out ComponentDraftFootprint? footprint))
            {
                diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingFootprint, mapping.FootprintId.Value, $"Mapping references missing footprint '{mapping.FootprintId}'."));
                continue;
            }

            if (footprint.Pads.All(pad => pad.Id != mapping.PadId))
            {
                diagnostics.Add(new ComponentDraftDiagnostic(ComponentDraftDiagnosticCode.MissingPad, $"{mapping.FootprintId.Value}:{mapping.PadId.Value}", $"Mapping references missing pad '{mapping.PadId}'."));
            }
        }
    }
}

public sealed record ComponentDraftValidationResult(IReadOnlyList<ComponentDraftDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

public sealed record ComponentDraftDiagnostic(
    ComponentDraftDiagnosticCode Code,
    string Subject,
    string Message);

public enum ComponentDraftDiagnosticCode
{
    MissingSymbol,
    MissingFootprint,
    MissingReferencePrefix,
    UnmappedPin,
    MissingPin,
    MissingPad,
    DuplicatePinId,
    DuplicatePadName
}
