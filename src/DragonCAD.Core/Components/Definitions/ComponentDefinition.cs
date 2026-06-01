using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Components.Definitions;

public sealed record ComponentDefinition(
    ComponentId Id,
    string DisplayName,
    ComponentKind Kind,
    string Manufacturer,
    string ManufacturerPartNumber,
    string Description,
    IReadOnlyList<ComponentAttribute> Attributes,
    IReadOnlyList<ComponentPin> Pins,
    IReadOnlyList<ComponentGate> Gates,
    IReadOnlyList<ComponentSymbol> Symbols,
    IReadOnlyList<ComponentFootprint> Footprints,
    IReadOnlyList<ComponentVariant> Variants,
    IReadOnlyList<ComponentPinPadMapping> PinPadMappings,
    IReadOnlyList<ComponentDatasheetReference> Datasheets,
    IReadOnlyList<ComponentSourcingReference> Sourcing,
    IReadOnlyList<ComponentPackageModel3D> PackageModels3D,
    IReadOnlyList<ComponentProvenanceRecord> Provenance)
{
    public bool Equals(ComponentDefinition? other) =>
        other is not null &&
        ComponentDefinitionSerializer.Serialize(this) == ComponentDefinitionSerializer.Serialize(other);

    public override int GetHashCode() => Id.GetHashCode();

    public void Validate()
    {
        EnsureUnique("pins", Pins.Select(pin => pin.Id.Value));
        EnsureUnique("gates", Gates.Select(gate => gate.Id.Value));
        EnsureUnique("symbols", Symbols.Select(symbol => symbol.Id.Value));
        EnsureUnique("footprints", Footprints.Select(footprint => footprint.Id.Value));
        EnsureUnique("variants", Variants.Select(variant => variant.Id.Value));

        HashSet<string> pinIds = Pins.Select(pin => pin.Id.Value).ToHashSet(StringComparer.Ordinal);
        HashSet<string> symbolIds = Symbols.Select(symbol => symbol.Id.Value).ToHashSet(StringComparer.Ordinal);
        HashSet<string> footprintIds = Footprints.Select(footprint => footprint.Id.Value).ToHashSet(StringComparer.Ordinal);
        Dictionary<string, ComponentFootprint> footprintsById = Footprints.ToDictionary(footprint => footprint.Id.Value, StringComparer.Ordinal);
        Dictionary<string, ComponentVariant> variantsById = Variants.ToDictionary(variant => variant.Id.Value, StringComparer.Ordinal);

        foreach (ComponentGate gate in Gates)
        {
            if (!symbolIds.Contains(gate.SymbolId.Value))
            {
                throw new InvalidOperationException($"Gate '{gate.Id}' references missing symbol '{gate.SymbolId}'.");
            }

            foreach (ComponentPinId pinId in gate.PinIds)
            {
                if (!pinIds.Contains(pinId.Value))
                {
                    throw new InvalidOperationException($"Gate '{gate.Id}' references missing pin '{pinId}'.");
                }
            }
        }

        foreach (ComponentSymbol symbol in Symbols)
        {
            foreach (ComponentSymbolPin pin in symbol.Pins)
            {
                if (!pinIds.Contains(pin.PinId.Value))
                {
                    throw new InvalidOperationException($"Symbol '{symbol.Id}' references missing pin '{pin.PinId}'.");
                }
            }
        }

        foreach (ComponentVariant variant in Variants)
        {
            if (!footprintIds.Contains(variant.FootprintId.Value))
            {
                throw new InvalidOperationException($"Variant '{variant.Id}' references missing footprint '{variant.FootprintId}'.");
            }
        }

        foreach (ComponentPinPadMapping mapping in PinPadMappings)
        {
            if (!variantsById.TryGetValue(mapping.VariantId.Value, out ComponentVariant? variant))
            {
                throw new InvalidOperationException($"Pin-pad mapping references missing variant '{mapping.VariantId}'.");
            }

            if (!pinIds.Contains(mapping.PinId.Value))
            {
                throw new InvalidOperationException($"Pin-pad mapping references missing pin '{mapping.PinId}'.");
            }

            ComponentFootprint footprint = footprintsById[variant.FootprintId.Value];
            if (footprint.Pads.All(pad => pad.Id != mapping.PadId))
            {
                throw new InvalidOperationException($"Pin-pad mapping references missing pad '{mapping.PadId}'.");
            }
        }
    }

    private static void EnsureUnique(string collectionName, IEnumerable<string> values)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (!seen.Add(value))
            {
                throw new InvalidOperationException($"Duplicate {collectionName} id '{value}'.");
            }
        }
    }
}

public enum ComponentKind
{
    Custom,
    Passive,
    IntegratedCircuit,
    Connector,
    Module,
    Mechanical
}

public sealed record ComponentAttribute(string Name, string Value);

public sealed record ComponentPin(
    ComponentPinId Id,
    string Name,
    string Number,
    ComponentPinElectricalType ElectricalType);

public enum ComponentPinElectricalType
{
    Passive,
    Input,
    Output,
    Bidirectional,
    Power,
    NoConnect
}

public sealed record ComponentGate(
    ComponentGateId Id,
    string Name,
    ComponentSymbolId SymbolId,
    IReadOnlyList<ComponentPinId> PinIds);

public sealed record ComponentSymbol(
    ComponentSymbolId Id,
    string Name,
    IReadOnlyList<ComponentSymbolPin> Pins,
    IReadOnlyList<ComponentLine> Lines,
    IReadOnlyList<ComponentSymbolText> Texts);

public sealed record ComponentSymbolPin(
    ComponentPinId PinId,
    CadPoint Position,
    ComponentPinOrientation Orientation);

public enum ComponentPinOrientation
{
    Left,
    Right,
    Up,
    Down
}

public sealed record ComponentLine(CadPoint Start, CadPoint End);

public sealed record ComponentSymbolText(
    ComponentSymbolTextKind Kind,
    string Value,
    CadPoint Position);

public enum ComponentSymbolTextKind
{
    Reference,
    Value,
    Custom
}

public sealed record ComponentFootprint(
    ComponentFootprintId Id,
    string Name,
    IReadOnlyList<ComponentFootprintPad> Pads,
    IReadOnlyList<ComponentLine> Silkscreen,
    IReadOnlyList<ComponentLine> Courtyard);

public sealed record ComponentFootprintPad(
    ComponentPadId Id,
    string Name,
    CadPoint Position,
    CadVector Size,
    ComponentPadTechnology Technology,
    ComponentPadShape Shape,
    long? DrillSize = null);

public enum ComponentPadTechnology
{
    ThroughHole,
    SurfaceMount
}

public enum ComponentPadShape
{
    Round,
    Rectangle,
    RoundedRectangle,
    Oval
}

public sealed record ComponentVariant(
    ComponentVariantId Id,
    string Name,
    ComponentFootprintId FootprintId,
    IReadOnlyList<ComponentAttribute> Attributes);

public sealed record ComponentPinPadMapping(
    ComponentVariantId VariantId,
    ComponentPinId PinId,
    ComponentPadId PadId);

public sealed record ComponentDatasheetReference(
    string ComponentName,
    ComponentDatasheetLocationKind LocationKind,
    string Location,
    string Manufacturer,
    string ManufacturerPartNumber);

public enum ComponentDatasheetLocationKind
{
    Url,
    LocalFile
}

public sealed record ComponentSourcingReference(
    string Distributor,
    string DistributorPartNumber,
    string Manufacturer,
    string ManufacturerPartNumber);

public sealed record ComponentPackageModel3D(
    string Id,
    ComponentPackageModel3DFormat Format,
    string Location,
    ComponentVariantId VariantId);

public enum ComponentPackageModel3DFormat
{
    Step,
    Obj,
    Glb
}

public sealed record ComponentProvenanceRecord(
    ComponentProvenanceKind Kind,
    string Source,
    string Detail);

public enum ComponentProvenanceKind
{
    Native,
    EagleImport,
    DatasheetGenerated,
    Manual
}
