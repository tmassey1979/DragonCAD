using System.Text.Json;
using System.Text.Json.Serialization;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Libraries;

public static class HawkCadComponentLibraryImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static HawkCadComponentLibraryImportResult Import(string json, int maxDevices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (maxDevices <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDevices), "Import limit must be positive.");
        }

        HawkCadLibraryDto library = JsonSerializer.Deserialize<HawkCadLibraryDto>(json, Options)
            ?? throw new InvalidOperationException("HawkCAD library JSON was empty.");

        Dictionary<string, HawkCadSymbolDto> symbols = library.Symbols.ToDictionary(symbol => symbol.Name, StringComparer.Ordinal);
        Dictionary<string, HawkCadPackageDto> packages = library.Packages.ToDictionary(package => package.Name, StringComparer.Ordinal);
        List<ComponentDefinition> components = [];
        List<HawkCadComponentLibraryDiagnostic> diagnostics = [];

        foreach (HawkCadDeviceDto device in library.Devices)
        {
            if (components.Count == maxDevices)
            {
                diagnostics.Add(new HawkCadComponentLibraryDiagnostic(
                    HawkCadComponentLibraryDiagnosticSeverity.Info,
                    HawkCadComponentLibraryDiagnosticCodes.DeviceLimitReached,
                    device.Name,
                    $"Stopped after importing {maxDevices} HawkCAD library devices."));
                break;
            }

            HawkCadDeviceTranslation? translation = TryTranslateDevice(library.Name, device, symbols, packages, diagnostics);
            if (translation is not null)
            {
                components.Add(translation.Component);
            }
        }

        return new HawkCadComponentLibraryImportResult(components, diagnostics);
    }

    public static HawkCadComponentLibraryImportResult ImportDevices(
        string json,
        IReadOnlyCollection<string> deviceNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(deviceNames);

        HashSet<string> requestedNames = deviceNames.ToHashSet(StringComparer.Ordinal);
        if (requestedNames.Count == 0)
        {
            return new HawkCadComponentLibraryImportResult([], []);
        }

        HawkCadLibraryDto library = JsonSerializer.Deserialize<HawkCadLibraryDto>(json, Options)
            ?? throw new InvalidOperationException("HawkCAD library JSON was empty.");

        Dictionary<string, HawkCadSymbolDto> symbols = library.Symbols.ToDictionary(symbol => symbol.Name, StringComparer.Ordinal);
        Dictionary<string, HawkCadPackageDto> packages = library.Packages.ToDictionary(package => package.Name, StringComparer.Ordinal);
        List<ComponentDefinition> components = [];
        List<HawkCadComponentLibraryDiagnostic> diagnostics = [];

        foreach (HawkCadDeviceDto device in library.Devices.Where(device => requestedNames.Contains(device.Name)))
        {
            HawkCadDeviceTranslation? translation = TryTranslateDevice(library.Name, device, symbols, packages, diagnostics);
            if (translation is not null)
            {
                components.Add(translation.Component);
            }
        }

        return new HawkCadComponentLibraryImportResult(components, diagnostics);
    }

    private static HawkCadDeviceTranslation? TryTranslateDevice(
        string libraryName,
        HawkCadDeviceDto device,
        IReadOnlyDictionary<string, HawkCadSymbolDto> symbols,
        IReadOnlyDictionary<string, HawkCadPackageDto> packages,
        ICollection<HawkCadComponentLibraryDiagnostic> diagnostics)
    {
        HawkCadDeviceGateDto? gate = device.Gates.FirstOrDefault(gate => symbols.ContainsKey(gate.SymbolName));
        HawkCadDeviceVariantDto? variant = device.Variants.FirstOrDefault(variant => packages.ContainsKey(variant.PackageName));
        if (gate is null || variant is null)
        {
            diagnostics.Add(new HawkCadComponentLibraryDiagnostic(
                HawkCadComponentLibraryDiagnosticSeverity.Warning,
                HawkCadComponentLibraryDiagnosticCodes.DeviceMissingAssets,
                device.Name,
                "Skipped HawkCAD device because its referenced symbol or package was not present."));
            return null;
        }

        HawkCadSymbolDto symbol = symbols[gate.SymbolName];
        HawkCadPackageDto package = packages[variant.PackageName];
        string idPrefix = $"hawkcad:{device.Name.ToLowerInvariant()}";
        ComponentSymbolId symbolId = new($"{idPrefix}:symbol:{symbol.Name}");
        ComponentFootprintId footprintId = new($"{idPrefix}:footprint:{package.Name}");
        ComponentVariantId variantId = new($"{idPrefix}:variant:{variant.Name}");

        ComponentPin[] pins = symbol.Pins
            .DistinctBy(pin => pin.Name)
            .Select(pin => new ComponentPin(
                new ComponentPinId($"{idPrefix}:pin:{pin.Name}"),
                pin.Name,
                pin.Name,
                ConvertPinElectricalType(pin.ElectricalType)))
            .ToArray();
        Dictionary<string, ComponentPinId> pinIdsByName = pins.ToDictionary(pin => pin.Name, pin => pin.Id, StringComparer.Ordinal);

        ComponentFootprintPad[] pads = package.Pads
            .DistinctBy(pad => pad.Name)
            .Select(pad => new ComponentFootprintPad(
                new ComponentPadId($"{idPrefix}:pad:{pad.Name}"),
                pad.Name,
                pad.Position.ToCadPoint(),
                pad.Size.ToCadVector(),
                ConvertPadTechnology(pad.Technology),
                ConvertPadShape(pad.Shape),
                pad.DrillSize?.X))
            .ToArray();
        Dictionary<string, ComponentPadId> padIdsByName = pads.ToDictionary(pad => pad.Name, pad => pad.Id, StringComparer.Ordinal);

        ComponentPinPadMapping[] mappings = device.Mappings
            .Where(mapping => pinIdsByName.ContainsKey(mapping.PinName) && padIdsByName.ContainsKey(mapping.PadName))
            .DistinctBy(mapping => $"{mapping.PinName}\u001F{mapping.PadName}")
            .Select(mapping => new ComponentPinPadMapping(variantId, pinIdsByName[mapping.PinName], padIdsByName[mapping.PadName]))
            .ToArray();

        ComponentDefinition component = new(
            new ComponentId(idPrefix),
            device.Name,
            InferKind(device.Name),
            AttributeValue(device.Attributes, "Manufacturer"),
            AttributeValue(device.Attributes, "PartNumber"),
            AttributeValue(device.Attributes, "Description"),
            device.Attributes.Select(attribute => new ComponentAttribute(attribute.Name, attribute.Value)).ToArray(),
            pins,
            [new ComponentGate(new ComponentGateId($"{idPrefix}:gate:{gate.Name}"), gate.Name, symbolId, pins.Select(pin => pin.Id).ToArray())],
            [
                new ComponentSymbol(
                    symbolId,
                    symbol.Name,
                    symbol.Pins
                        .Where(pin => pinIdsByName.ContainsKey(pin.Name))
                        .Select(pin => new ComponentSymbolPin(pinIdsByName[pin.Name], pin.Position.ToCadPoint(), InferPinOrientation(pin.Position.ToCadPoint())))
                        .ToArray(),
                    symbol.Outlines.Select(line => new ComponentLine(line.Start.ToCadPoint(), line.End.ToCadPoint())).ToArray(),
                    symbol.Texts.Select(text => new ComponentSymbolText(ConvertSymbolTextKind(text.Kind), text.Value, text.Position.ToCadPoint())).ToArray())
            ],
            [
                new ComponentFootprint(
                    footprintId,
                    package.Name,
                    pads,
                    package.Silkscreen.Select(line => new ComponentLine(line.Start.ToCadPoint(), line.End.ToCadPoint())).ToArray(),
                    package.Courtyard.Select(line => new ComponentLine(line.Start.ToCadPoint(), line.End.ToCadPoint())).ToArray())
            ],
            [new ComponentVariant(variantId, variant.Name, footprintId, variant.Attributes.Select(attribute => new ComponentAttribute(attribute.Name, attribute.Value)).ToArray())],
            mappings,
            DatasheetsFromAttributes(device),
            Sourcing: [],
            PackageModels3D: [],
            [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, libraryName, "Imported from HawkCAD .hclib.json")]);

        component.Validate();
        return new HawkCadDeviceTranslation(component);
    }

    private static IReadOnlyList<ComponentDatasheetReference> DatasheetsFromAttributes(HawkCadDeviceDto device)
    {
        string datasheet = AttributeValue(device.Attributes, "Datasheet");
        if (!Uri.TryCreate(datasheet, UriKind.Absolute, out _))
        {
            return [];
        }

        return
        [
            new ComponentDatasheetReference(
                device.Name,
                ComponentDatasheetLocationKind.Url,
                datasheet,
                AttributeValue(device.Attributes, "Manufacturer"),
                AttributeValue(device.Attributes, "PartNumber"))
        ];
    }

    private static ComponentKind InferKind(string deviceName)
    {
        if (deviceName.Contains("res", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Contains("cap", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentKind.Passive;
        }

        if (deviceName.Contains("usb", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Contains("header", StringComparison.OrdinalIgnoreCase) ||
            deviceName.Contains("conn", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentKind.Connector;
        }

        return ComponentKind.Custom;
    }

    private static string AttributeValue(IEnumerable<HawkCadAttributeDto> attributes, string name) =>
        attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static ComponentPinElectricalType ConvertPinElectricalType(string? electricalType) =>
        electricalType switch
        {
            "Input" => ComponentPinElectricalType.Input,
            "Output" => ComponentPinElectricalType.Output,
            "Bidirectional" => ComponentPinElectricalType.Bidirectional,
            "Power" => ComponentPinElectricalType.Power,
            "NoConnect" => ComponentPinElectricalType.NoConnect,
            _ => ComponentPinElectricalType.Passive
        };

    private static ComponentPadTechnology ConvertPadTechnology(string? technology) =>
        technology == "SurfaceMount" ? ComponentPadTechnology.SurfaceMount : ComponentPadTechnology.ThroughHole;

    private static ComponentPadShape ConvertPadShape(string? shape) =>
        shape switch
        {
            "Round" => ComponentPadShape.Round,
            "RoundedRectangle" => ComponentPadShape.RoundedRectangle,
            "Oval" => ComponentPadShape.Oval,
            _ => ComponentPadShape.Rectangle
        };

    private static ComponentSymbolTextKind ConvertSymbolTextKind(string? kind) =>
        kind switch
        {
            "Reference" or "Name" => ComponentSymbolTextKind.Reference,
            "Value" => ComponentSymbolTextKind.Value,
            _ => ComponentSymbolTextKind.Custom
        };

    private static ComponentPinOrientation InferPinOrientation(CadPoint point)
    {
        if (Math.Abs(point.X) >= Math.Abs(point.Y))
        {
            return point.X <= 0 ? ComponentPinOrientation.Right : ComponentPinOrientation.Left;
        }

        return point.Y <= 0 ? ComponentPinOrientation.Down : ComponentPinOrientation.Up;
    }

    private sealed record HawkCadDeviceTranslation(ComponentDefinition Component);
}

public sealed record HawkCadComponentLibraryImportResult(
    IReadOnlyList<ComponentDefinition> Components,
    IReadOnlyList<HawkCadComponentLibraryDiagnostic> Diagnostics);

public sealed record HawkCadComponentLibraryDiagnostic(
    HawkCadComponentLibraryDiagnosticSeverity Severity,
    string Code,
    string AssetName,
    string Message);

public enum HawkCadComponentLibraryDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public static class HawkCadComponentLibraryDiagnosticCodes
{
    public const string DeviceLimitReached = "HawkCadLibrary.DeviceLimitReached";
    public const string DeviceMissingAssets = "HawkCadLibrary.DeviceMissingAssets";
}

internal sealed record HawkCadLibraryDto(
    IReadOnlyList<HawkCadAttributeDto> Attributes,
    IReadOnlyList<HawkCadDeviceDto> Devices,
    string Name,
    IReadOnlyList<HawkCadPackageDto> Packages,
    IReadOnlyList<HawkCadSymbolDto> Symbols,
    int Version);

internal sealed record HawkCadAttributeDto(string Name, string Value);

internal sealed record HawkCadDeviceDto(
    IReadOnlyList<HawkCadAttributeDto> Attributes,
    IReadOnlyList<HawkCadDeviceGateDto> Gates,
    IReadOnlyList<HawkCadDeviceMappingDto> Mappings,
    string Name,
    IReadOnlyList<HawkCadDeviceVariantDto> Variants);

internal sealed record HawkCadDeviceGateDto(string Name, string SymbolName, string VariantName);

internal sealed record HawkCadDeviceMappingDto(string GateName, string PinName, string PadName);

internal sealed record HawkCadDeviceVariantDto(
    string Name,
    string PackageName,
    IReadOnlyList<HawkCadAttributeDto>? Attributes)
{
    public IReadOnlyList<HawkCadAttributeDto> Attributes { get; init; } = Attributes?.ToArray() ?? [];
}

internal sealed record HawkCadPackageDto(
    string Name,
    IReadOnlyList<HawkCadPadDto> Pads,
    IReadOnlyList<HawkCadLineDto>? Silkscreen,
    IReadOnlyList<HawkCadLineDto>? Courtyard)
{
    public IReadOnlyList<HawkCadLineDto> Silkscreen { get; init; } = Silkscreen?.ToArray() ?? [];
    public IReadOnlyList<HawkCadLineDto> Courtyard { get; init; } = Courtyard?.ToArray() ?? [];
}

internal sealed record HawkCadPadDto(
    string Name,
    HawkCadPointDto Position,
    HawkCadVectorDto Size,
    string? Technology,
    string? Shape,
    HawkCadVectorDto? DrillSize);

internal sealed record HawkCadSymbolDto(
    string Name,
    IReadOnlyList<HawkCadPinDto> Pins,
    IReadOnlyList<HawkCadSymbolTextDto>? Texts,
    IReadOnlyList<HawkCadLineDto>? Outlines)
{
    public IReadOnlyList<HawkCadSymbolTextDto> Texts { get; init; } = Texts?.ToArray() ?? [];
    public IReadOnlyList<HawkCadLineDto> Outlines { get; init; } = Outlines?.ToArray() ?? [];
}

internal sealed record HawkCadPinDto(
    string Name,
    HawkCadPointDto Position,
    string? ElectricalType);

internal sealed record HawkCadSymbolTextDto(string? Kind, HawkCadPointDto Position, string Value);

internal sealed record HawkCadLineDto(HawkCadPointDto Start, HawkCadPointDto End);

internal sealed record HawkCadPointDto(long X, long Y)
{
    public CadPoint ToCadPoint() => new(X, Y);
}

internal sealed record HawkCadVectorDto(long X, long Y)
{
    public CadVector ToCadVector() => new(X, Y);
}
