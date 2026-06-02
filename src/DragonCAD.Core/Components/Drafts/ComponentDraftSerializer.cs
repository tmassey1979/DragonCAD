using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Components.Drafts;

public static class ComponentDraftSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(ComponentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        ComponentDraftValidationResult validation = ComponentDraftValidator.Validate(draft);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Component draft '{draft.Id}' is invalid: {string.Join("; ", validation.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        }

        return JsonSerializer.Serialize(ToDto(draft), Options);
    }

    public static ComponentDraft Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ComponentDraftDto dto = JsonSerializer.Deserialize<ComponentDraftDto>(json, Options)
            ?? throw new InvalidOperationException("Component draft JSON was empty.");

        ComponentDraft draft = FromDto(dto);
        ComponentDraftValidationResult validation = ComponentDraftValidator.Validate(draft);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Component draft '{draft.Id}' is invalid: {string.Join("; ", validation.Diagnostics.Select(diagnostic => diagnostic.Message))}");
        }

        return draft;
    }

    private static ComponentDraftDto ToDto(ComponentDraft draft) =>
        new(
            draft.Id.Value,
            draft.DisplayName,
            draft.Package,
            draft.Attributes.ToArray(),
            draft.Pins.Select(pin => new ComponentDraftPinDto(pin.Id.Value, pin.Name, pin.Number, pin.ElectricalType)).ToArray(),
            draft.Symbols.Select(symbol => new ComponentDraftSymbolDto(
                symbol.Id.Value,
                symbol.Name,
                symbol.Pins.Select(pin => new ComponentDraftSymbolPinDto(pin.PinId.Value, pin.Start, pin.End, pin.Orientation)).ToArray(),
                symbol.Primitives.ToArray())).ToArray(),
            draft.Footprints.Select(footprint => new ComponentDraftFootprintDto(
                footprint.Id.Value,
                footprint.Name,
                footprint.Pads.Select(pad => new ComponentDraftPadDto(pad.Id.Value, pad.Name, pad.Position, pad.Size, pad.Technology, pad.Shape, pad.DrillSize)).ToArray(),
                footprint.Silkscreen.ToArray(),
                footprint.Courtyard.ToArray())).ToArray(),
            draft.DeviceMappings.Select(mapping => new ComponentDraftDeviceMappingDto(mapping.PinId.Value, mapping.FootprintId.Value, mapping.PadId.Value)).ToArray());

    private static ComponentDraft FromDto(ComponentDraftDto dto) =>
        new(
            new ComponentId(dto.Id),
            dto.DisplayName,
            dto.Package,
            dto.Attributes,
            dto.Pins.Select(pin => new ComponentDraftPin(new ComponentPinId(pin.Id), pin.Name, pin.Number, pin.ElectricalType)).ToArray(),
            dto.Symbols.Select(symbol => new ComponentDraftSymbol(
                new ComponentSymbolId(symbol.Id),
                symbol.Name,
                symbol.Pins.Select(pin => new ComponentDraftSymbolPin(new ComponentPinId(pin.PinId), pin.Start, pin.End, pin.Orientation)).ToArray(),
                symbol.Primitives)).ToArray(),
            dto.Footprints.Select(footprint => new ComponentDraftFootprint(
                new ComponentFootprintId(footprint.Id),
                footprint.Name,
                footprint.Pads.Select(pad => new ComponentDraftPad(new ComponentPadId(pad.Id), pad.Name, pad.Position, pad.Size, pad.Technology, pad.Shape, pad.DrillSize)).ToArray(),
                footprint.Silkscreen,
                footprint.Courtyard)).ToArray(),
            dto.DeviceMappings.Select(mapping => new ComponentDraftDeviceMapping(new ComponentPinId(mapping.PinId), new ComponentFootprintId(mapping.FootprintId), new ComponentPadId(mapping.PadId))).ToArray());

    private sealed record ComponentDraftDto(
        string Id,
        string DisplayName,
        ComponentDraftPackage Package,
        IReadOnlyList<ComponentDraftAttribute> Attributes,
        IReadOnlyList<ComponentDraftPinDto> Pins,
        IReadOnlyList<ComponentDraftSymbolDto> Symbols,
        IReadOnlyList<ComponentDraftFootprintDto> Footprints,
        IReadOnlyList<ComponentDraftDeviceMappingDto> DeviceMappings);

    private sealed record ComponentDraftPinDto(
        string Id,
        string Name,
        string Number,
        ComponentDraftPinElectricalType ElectricalType);

    private sealed record ComponentDraftSymbolDto(
        string Id,
        string Name,
        IReadOnlyList<ComponentDraftSymbolPinDto> Pins,
        IReadOnlyList<ComponentDraftSymbolPrimitive> Primitives);

    private sealed record ComponentDraftSymbolPinDto(
        string PinId,
        CadPoint Start,
        CadPoint End,
        ComponentDraftPinOrientation Orientation);

    private sealed record ComponentDraftFootprintDto(
        string Id,
        string Name,
        IReadOnlyList<ComponentDraftPadDto> Pads,
        IReadOnlyList<ComponentDraftFootprintPrimitive> Silkscreen,
        IReadOnlyList<ComponentDraftFootprintPrimitive> Courtyard);

    private sealed record ComponentDraftPadDto(
        string Id,
        string Name,
        CadPoint Position,
        CadVector Size,
        ComponentDraftPadTechnology Technology,
        ComponentDraftPadShape Shape,
        long? DrillSize);

    private sealed record ComponentDraftDeviceMappingDto(
        string PinId,
        string FootprintId,
        string PadId);
}
