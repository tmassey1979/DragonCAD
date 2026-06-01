using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Components.Definitions;

public static class ComponentDefinitionSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        return JsonSerializer.Serialize(ToDto(definition), Options);
    }

    public static ComponentDefinition Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ComponentDefinitionDto dto = JsonSerializer.Deserialize<ComponentDefinitionDto>(json, Options)
            ?? throw new InvalidOperationException("Component definition JSON was empty.");
        ComponentDefinition definition = FromDto(dto);
        definition.Validate();
        return definition;
    }

    private static ComponentDefinitionDto ToDto(ComponentDefinition definition) =>
        new(
            definition.Id.Value,
            definition.DisplayName,
            definition.Kind,
            definition.Manufacturer,
            definition.ManufacturerPartNumber,
            definition.Description,
            definition.Attributes.OrderBy(attribute => attribute.Name, StringComparer.Ordinal).ThenBy(attribute => attribute.Value, StringComparer.Ordinal).ToArray(),
            definition.Pins.OrderBy(pin => pin.Id.Value, StringComparer.Ordinal).Select(pin => new ComponentPinDto(pin.Id.Value, pin.Name, pin.Number, pin.ElectricalType)).ToArray(),
            definition.Gates.OrderBy(gate => gate.Id.Value, StringComparer.Ordinal).Select(gate => new ComponentGateDto(gate.Id.Value, gate.Name, gate.SymbolId.Value, gate.PinIds.Select(id => id.Value).Order(StringComparer.Ordinal).ToArray())).ToArray(),
            definition.Symbols.OrderBy(symbol => symbol.Id.Value, StringComparer.Ordinal).Select(symbol => new ComponentSymbolDto(
                symbol.Id.Value,
                symbol.Name,
                symbol.Pins.OrderBy(pin => pin.PinId.Value, StringComparer.Ordinal).Select(pin => new ComponentSymbolPinDto(pin.PinId.Value, pin.Position, pin.Orientation)).ToArray(),
                symbol.Lines.OrderBy(LineKey, StringComparer.Ordinal).ToArray(),
                symbol.Texts.OrderBy(text => text.Value, StringComparer.Ordinal).ThenBy(text => text.Kind).ToArray())).ToArray(),
            definition.Footprints.OrderBy(footprint => footprint.Id.Value, StringComparer.Ordinal).Select(footprint => new ComponentFootprintDto(
                footprint.Id.Value,
                footprint.Name,
                footprint.Pads.OrderBy(pad => pad.Id.Value, StringComparer.Ordinal).Select(pad => new ComponentFootprintPadDto(pad.Id.Value, pad.Name, pad.Position, pad.Size, pad.Technology, pad.Shape, pad.DrillSize)).ToArray(),
                footprint.Silkscreen.OrderBy(LineKey, StringComparer.Ordinal).ToArray(),
                footprint.Courtyard.OrderBy(LineKey, StringComparer.Ordinal).ToArray())).ToArray(),
            definition.Variants.OrderBy(variant => variant.Id.Value, StringComparer.Ordinal).Select(variant => new ComponentVariantDto(variant.Id.Value, variant.Name, variant.FootprintId.Value, variant.Attributes.OrderBy(attribute => attribute.Name, StringComparer.Ordinal).ThenBy(attribute => attribute.Value, StringComparer.Ordinal).ToArray())).ToArray(),
            definition.PinPadMappings.OrderBy(mapping => mapping.VariantId.Value, StringComparer.Ordinal).ThenBy(mapping => mapping.PinId.Value, StringComparer.Ordinal).ThenBy(mapping => mapping.PadId.Value, StringComparer.Ordinal).Select(mapping => new ComponentPinPadMappingDto(mapping.VariantId.Value, mapping.PinId.Value, mapping.PadId.Value)).ToArray(),
            definition.Datasheets.OrderBy(datasheet => datasheet.Location, StringComparer.Ordinal).ToArray(),
            definition.Sourcing.OrderBy(source => source.Distributor, StringComparer.Ordinal).ThenBy(source => source.DistributorPartNumber, StringComparer.Ordinal).ToArray(),
            definition.PackageModels3D.OrderBy(model => model.Id, StringComparer.Ordinal).Select(model => new ComponentPackageModel3DDto(model.Id, model.Format, model.Location, model.VariantId.Value)).ToArray(),
            definition.Provenance.OrderBy(record => record.Kind).ThenBy(record => record.Source, StringComparer.Ordinal).ThenBy(record => record.Detail, StringComparer.Ordinal).ToArray());

    private static ComponentDefinition FromDto(ComponentDefinitionDto dto) =>
        new(
            new ComponentId(dto.Id),
            dto.DisplayName,
            dto.Kind,
            dto.Manufacturer,
            dto.ManufacturerPartNumber,
            dto.Description,
            dto.Attributes,
            dto.Pins.Select(pin => new ComponentPin(new ComponentPinId(pin.Id), pin.Name, pin.Number, pin.ElectricalType)).ToArray(),
            dto.Gates.Select(gate => new ComponentGate(new ComponentGateId(gate.Id), gate.Name, new ComponentSymbolId(gate.SymbolId), gate.PinIds.Select(id => new ComponentPinId(id)).ToArray())).ToArray(),
            dto.Symbols.Select(symbol => new ComponentSymbol(
                new ComponentSymbolId(symbol.Id),
                symbol.Name,
                symbol.Pins.Select(pin => new ComponentSymbolPin(new ComponentPinId(pin.PinId), pin.Position, pin.Orientation)).ToArray(),
                symbol.Lines,
                symbol.Texts)).ToArray(),
            dto.Footprints.Select(footprint => new ComponentFootprint(
                new ComponentFootprintId(footprint.Id),
                footprint.Name,
                footprint.Pads.Select(pad => new ComponentFootprintPad(new ComponentPadId(pad.Id), pad.Name, pad.Position, pad.Size, pad.Technology, pad.Shape, pad.DrillSize)).ToArray(),
                footprint.Silkscreen,
                footprint.Courtyard)).ToArray(),
            dto.Variants.Select(variant => new ComponentVariant(new ComponentVariantId(variant.Id), variant.Name, new ComponentFootprintId(variant.FootprintId), variant.Attributes)).ToArray(),
            dto.PinPadMappings.Select(mapping => new ComponentPinPadMapping(new ComponentVariantId(mapping.VariantId), new ComponentPinId(mapping.PinId), new ComponentPadId(mapping.PadId))).ToArray(),
            dto.Datasheets,
            dto.Sourcing,
            dto.PackageModels3D.Select(model => new ComponentPackageModel3D(model.Id, model.Format, model.Location, new ComponentVariantId(model.VariantId))).ToArray(),
            dto.Provenance);

    private static string LineKey(ComponentLine line) =>
        $"{line.Start.X:D20}:{line.Start.Y:D20}:{line.End.X:D20}:{line.End.Y:D20}";

    private sealed record ComponentDefinitionDto(
        string Id,
        string DisplayName,
        ComponentKind Kind,
        string Manufacturer,
        string ManufacturerPartNumber,
        string Description,
        IReadOnlyList<ComponentAttribute> Attributes,
        IReadOnlyList<ComponentPinDto> Pins,
        IReadOnlyList<ComponentGateDto> Gates,
        IReadOnlyList<ComponentSymbolDto> Symbols,
        IReadOnlyList<ComponentFootprintDto> Footprints,
        IReadOnlyList<ComponentVariantDto> Variants,
        IReadOnlyList<ComponentPinPadMappingDto> PinPadMappings,
        IReadOnlyList<ComponentDatasheetReference> Datasheets,
        IReadOnlyList<ComponentSourcingReference> Sourcing,
        [property: JsonPropertyName("packageModels3d")]
        IReadOnlyList<ComponentPackageModel3DDto> PackageModels3D,
        IReadOnlyList<ComponentProvenanceRecord> Provenance);

    private sealed record ComponentPinDto(string Id, string Name, string Number, ComponentPinElectricalType ElectricalType);

    private sealed record ComponentGateDto(string Id, string Name, string SymbolId, IReadOnlyList<string> PinIds);

    private sealed record ComponentSymbolDto(string Id, string Name, IReadOnlyList<ComponentSymbolPinDto> Pins, IReadOnlyList<ComponentLine> Lines, IReadOnlyList<ComponentSymbolText> Texts);

    private sealed record ComponentSymbolPinDto(string PinId, CadPoint Position, ComponentPinOrientation Orientation);

    private sealed record ComponentFootprintDto(string Id, string Name, IReadOnlyList<ComponentFootprintPadDto> Pads, IReadOnlyList<ComponentLine> Silkscreen, IReadOnlyList<ComponentLine> Courtyard);

    private sealed record ComponentFootprintPadDto(string Id, string Name, CadPoint Position, CadVector Size, ComponentPadTechnology Technology, ComponentPadShape Shape, long? DrillSize);

    private sealed record ComponentVariantDto(string Id, string Name, string FootprintId, IReadOnlyList<ComponentAttribute> Attributes);

    private sealed record ComponentPinPadMappingDto(string VariantId, string PinId, string PadId);

    private sealed record ComponentPackageModel3DDto(string Id, ComponentPackageModel3DFormat Format, string Location, string VariantId);
}
