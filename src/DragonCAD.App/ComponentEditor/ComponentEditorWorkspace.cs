using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.ComponentEditor;

public sealed class ComponentEditorWorkspace
{
    private readonly string baselineSnapshot;

    private ComponentEditorWorkspace(
        ComponentEditorSessionKind sessionKind,
        ComponentDefinition? originalDefinition,
        ComponentEditorViewModel viewModel)
    {
        SessionKind = sessionKind;
        OriginalDefinition = originalDefinition;
        ViewModel = viewModel;
        baselineSnapshot = ComponentEditorSnapshot.FromDefinition(viewModel.ToDefinition());
    }

    public ComponentEditorSessionKind SessionKind { get; }

    public ComponentDefinition? OriginalDefinition { get; }

    public ComponentEditorViewModel ViewModel { get; }

    public bool IsDirty => ComponentEditorSnapshot.FromDefinition(ViewModel.ToDefinition()) != baselineSnapshot;

    public bool IsTrustedLibraryEntry => SessionKind == ComponentEditorSessionKind.Edit;

    public ComponentEditorValidationSummary ValidationSummary => ComponentEditorValidationSummary.FromDefinition(ViewModel.ToDefinition());

    public IReadOnlyList<ComponentEditorValidationIssueDisplay> ValidationIssueDisplay =>
        ValidationSummary.Issues.Count == 0
            ? [new ComponentEditorValidationIssueDisplay(ComponentEditorValidationIssueKind.None, "No validation issues")]
            : ValidationSummary.Issues
                .Select(issue => new ComponentEditorValidationIssueDisplay(issue.Kind, issue.DisplayText))
                .ToArray();

    public ComponentEditorSaveReadinessSummary SaveReadiness
    {
        get
        {
            ComponentEditorValidationSummary validationSummary = ValidationSummary;
            if (validationSummary.Issues.Count > 0)
            {
                int blockingIssueCount = validationSummary.Issues.Count(issue => issue.Kind != ComponentEditorValidationIssueKind.MissingPins);
                string issueText = blockingIssueCount == 1 ? "issue" : "issues";
                return new ComponentEditorSaveReadinessSummary(
                    ComponentEditorSaveReadiness.BlockedByValidation,
                    $"Resolve {blockingIssueCount} validation {issueText} before saving.");
            }

            if (!IsDirty)
            {
                return new ComponentEditorSaveReadinessSummary(
                    ComponentEditorSaveReadiness.Unchanged,
                    "No component changes to save.");
            }

            return new ComponentEditorSaveReadinessSummary(
                ComponentEditorSaveReadiness.Ready,
                "Ready to save component changes.");
        }
    }

    public ComponentEditorTrustedPromotionReadiness TrustedPromotionReadiness =>
        IsTrustedLibraryEntry
            ? new ComponentEditorTrustedPromotionReadiness(true, "Ready for trusted-library promotion.")
            : new ComponentEditorTrustedPromotionReadiness(false, "Blocked: component editor drafts must be saved as drafts before trusted-library promotion.");

    public static ComponentEditorWorkspace StartNew(string componentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentId);
        ComponentDefinition draft = new(
            new ComponentId(componentId),
            "",
            ComponentKind.Custom,
            "",
            "",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints: [],
            Variants: [],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

        return new ComponentEditorWorkspace(
            ComponentEditorSessionKind.New,
            originalDefinition: null,
            ComponentEditorViewModel.FromDefinition(draft));
    }

    public static ComponentEditorWorkspace StartEdit(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ComponentEditorWorkspace(
            ComponentEditorSessionKind.Edit,
            definition,
            ComponentEditorViewModel.FromDefinition(definition));
    }

    public static ComponentEditorWorkspace ReloadDraftJson(string json)
    {
        ComponentDraft draft = ComponentDraftSerializer.Deserialize(json);
        return new ComponentEditorWorkspace(
            ComponentEditorSessionKind.Draft,
            originalDefinition: null,
            ComponentEditorViewModel.FromDraft(draft));
    }

    public string SaveDraftJson() =>
        ComponentDraftSerializer.Serialize(ViewModel.ToDraft());
}

public enum ComponentEditorSessionKind
{
    New,
    Edit,
    Draft
}

public enum ComponentEditorSaveReadiness
{
    BlockedByValidation,
    Unchanged,
    Ready
}

public sealed record ComponentEditorSaveReadinessSummary(
    ComponentEditorSaveReadiness State,
    string Message);

public sealed record ComponentEditorTrustedPromotionReadiness(
    bool CanPromote,
    string Message);

public sealed class ComponentEditorViewModel : INotifyPropertyChanged
{
    private static readonly CadGrid DefaultCommandGrid = new(new CadVector(100_000, 100_000));

    private readonly string componentId;
    private string displayName;
    private string manufacturer;
    private string manufacturerPartNumber;
    private string description;
    private ComponentKind kind;
    private IReadOnlyList<ComponentPin> pins;
    private IReadOnlyList<ComponentGate> gates;
    private IReadOnlyList<ComponentSymbol> symbols;
    private IReadOnlyList<ComponentFootprint> footprints;
    private IReadOnlyList<ComponentVariant> variants;
    private IReadOnlyList<ComponentPinPadMapping> pinPadMappings;
    private IReadOnlyList<ComponentAttribute> attributes;
    private IReadOnlyList<ComponentDatasheetReference> datasheets;
    private IReadOnlyList<ComponentSourcingReference> sourcing;
    private IReadOnlyList<ComponentPackageModel3D> packageModels3D;
    private IReadOnlyList<ComponentProvenanceRecord> provenance;

    private ComponentEditorViewModel(ComponentDefinition definition)
    {
        componentId = definition.Id.Value;
        displayName = definition.DisplayName;
        manufacturer = definition.Manufacturer;
        manufacturerPartNumber = definition.ManufacturerPartNumber;
        description = definition.Description;
        kind = definition.Kind;
        attributes = definition.Attributes.ToArray();
        pins = definition.Pins.ToArray();
        gates = definition.Gates.ToArray();
        symbols = definition.Symbols.ToArray();
        footprints = definition.Footprints.ToArray();
        variants = definition.Variants.ToArray();
        pinPadMappings = definition.PinPadMappings.ToArray();
        datasheets = definition.Datasheets.ToArray();
        sourcing = definition.Sourcing.ToArray();
        packageModels3D = definition.PackageModels3D.ToArray();
        provenance = definition.Provenance.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ComponentId => componentId;

    public string DisplayName
    {
        get => displayName;
        set
        {
            if (SetField(ref displayName, value))
            {
                OnPropertyChanged(nameof(IdentitySummary));
            }
        }
    }

    public string Manufacturer
    {
        get => manufacturer;
        set
        {
            if (SetField(ref manufacturer, value))
            {
                OnPropertyChanged(nameof(IdentitySummary));
            }
        }
    }

    public string ManufacturerPartNumber
    {
        get => manufacturerPartNumber;
        set
        {
            if (SetField(ref manufacturerPartNumber, value))
            {
                OnPropertyChanged(nameof(IdentitySummary));
            }
        }
    }

    public string Description
    {
        get => description;
        set
        {
            if (SetField(ref description, value))
            {
                OnPropertyChanged(nameof(IdentitySummary));
            }
        }
    }

    public ComponentKind Kind
    {
        get => kind;
        set
        {
            if (SetField(ref kind, value))
            {
                OnPropertyChanged(nameof(IdentitySummary));
            }
        }
    }

    public IReadOnlyList<ComponentPin> Pins => pins;

    public IReadOnlyList<ComponentSymbol> Symbols => symbols;

    public IReadOnlyList<ComponentFootprint> Footprints => footprints;

    public IReadOnlyList<ComponentVariant> Variants => variants;

    public IReadOnlyList<ComponentPinPadMapping> PinPadMappings => pinPadMappings;

    public ComponentEditorIdentitySummary IdentitySummary =>
        new(
            displayName.Length == 0 ? componentId : displayName,
            BuildManufacturerLine(manufacturer, manufacturerPartNumber),
            SplitPascalCase(kind.ToString()),
            description.Length == 0 ? "No description" : description);

    public IReadOnlyList<ComponentEditorPinSummary> PinSummaries => pins
        .OrderBy(pin => pin.Number, StringComparer.Ordinal)
        .Select(pin => new ComponentEditorPinSummary(
            pin.Number,
            pin.Name,
            pin.ElectricalType.ToString(),
            $"{pin.Number} {pin.Name} ({pin.ElectricalType})"))
        .ToArray();

    public IReadOnlyList<ComponentEditorAssetSummary> SymbolSummaries => symbols
        .OrderBy(symbol => symbol.Name, StringComparer.Ordinal)
        .Select(symbol => new ComponentEditorAssetSummary(
            symbol.Id.Value,
            symbol.Name,
            $"{symbol.Name} - {symbol.Pins.Count} {Plural(symbol.Pins.Count, "pin", "pins")}"))
        .ToArray();

    public IReadOnlyList<ComponentEditorAssetSummary> FootprintSummaries => footprints
        .OrderBy(footprint => footprint.Name, StringComparer.Ordinal)
        .Select(footprint => new ComponentEditorAssetSummary(
            footprint.Id.Value,
            footprint.Name,
            $"{footprint.Name} - {footprint.Pads.Count} {Plural(footprint.Pads.Count, "pad", "pads")}"))
        .ToArray();

    public IReadOnlyList<ComponentEditorAssetSummary> PackageSummaries => variants
        .OrderBy(variant => variant.Name, StringComparer.Ordinal)
        .Select(variant => new ComponentEditorAssetSummary(
            variant.Id.Value,
            variant.Name,
            $"{variant.Name} - {FootprintNameFor(variant.FootprintId)}"))
        .ToArray();

    public static ComponentEditorViewModel FromDefinition(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ComponentEditorViewModel(definition);
    }

    public static ComponentEditorViewModel FromDraft(ComponentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        Dictionary<ComponentFootprintId, ComponentVariantId> variantIdsByFootprintId = draft.Footprints
            .Select((footprint, index) => new
            {
                footprint.Id,
                VariantId = new ComponentVariantId($"{draft.Id.Value}:variant:{Slug(index == 0 ? draft.Package.Name : footprint.Name)}")
            })
            .ToDictionary(entry => entry.Id, entry => entry.VariantId);

        ComponentDefinition definition = new(
            draft.Id,
            draft.DisplayName,
            ComponentKind.Custom,
            "",
            "",
            Description: "",
            Attributes: draft.Attributes.Select(attribute => new ComponentAttribute(attribute.Name, attribute.Value)).ToArray(),
            Pins: draft.Pins.Select(pin => new ComponentPin(pin.Id, pin.Name, pin.Number, ToDefinitionPinElectricalType(pin.ElectricalType))).ToArray(),
            Gates: [],
            Symbols: draft.Symbols.Select(symbol => new ComponentSymbol(
                symbol.Id,
                symbol.Name,
                symbol.Pins.Select(pin => new ComponentSymbolPin(pin.PinId, pin.Start, ToDefinitionPinOrientation(pin.Orientation))).ToArray(),
                [],
                [])
            {
                Primitives = symbol.Primitives.Select(ToDefinitionPrimitive).ToArray()
            }).ToArray(),
            Footprints: draft.Footprints.Select(footprint => new ComponentFootprint(
                footprint.Id,
                footprint.Name,
                footprint.Pads.Select(pad => new ComponentFootprintPad(pad.Id, pad.Name, pad.Position, pad.Size, ToDefinitionPadTechnology(pad.Technology), ToDefinitionPadShape(pad.Shape), pad.DrillSize)).ToArray(),
                footprint.Silkscreen.Select(primitive => new ComponentLine(primitive.Start, primitive.End)).ToArray(),
                footprint.Courtyard.Select(primitive => new ComponentLine(primitive.Start, primitive.End)).ToArray())).ToArray(),
            Variants: draft.Footprints.Select((footprint, index) => new ComponentVariant(
                variantIdsByFootprintId[footprint.Id],
                index == 0 ? draft.Package.Name : footprint.Name,
                footprint.Id,
                draft.Package.Metadata.Select(attribute => new ComponentAttribute(attribute.Name, attribute.Value)).ToArray())).ToArray(),
            PinPadMappings: draft.DeviceMappings
                .Where(mapping => variantIdsByFootprintId.ContainsKey(mapping.FootprintId))
                .Select(mapping => new ComponentPinPadMapping(variantIdsByFootprintId[mapping.FootprintId], mapping.PinId, mapping.PadId))
                .ToArray(),
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

        return FromDefinition(definition);
    }

    public void SetDisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        DisplayName = value.Trim();
    }

    public void SetManufacturer(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Manufacturer = value.Trim();
    }

    public void SetManufacturerPartNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ManufacturerPartNumber = value.Trim();
    }

    public void SetDescription(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Description = value.Trim();
    }

    public void SetKind(ComponentKind value) => Kind = value;

    public void AddBasicPinPackageAndMapping(string pinNumber, string pinName, string packageName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(pinName);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        string normalizedPinNumber = pinNumber.Trim();
        string normalizedPinName = pinName.Trim();
        string normalizedPackageName = packageName.Trim();

        AddPin(normalizedPinNumber, normalizedPinName);
        AddSymbol("Default Symbol");
        AddFootprint(
            normalizedPackageName,
            [new ComponentEditorPadDraft(normalizedPinNumber, new CadPoint(0, 0), new CadVector(80_000, 80_000))]);
        AddPackage(normalizedPackageName, normalizedPackageName);
        MapPinToPad(normalizedPinNumber, normalizedPinNumber);
    }

    public ComponentEditorCommandResult AddPin(string number, string name)
    {
        ComponentEditorCommandResult result = AddPin(number, name, new CadPoint(pins.Count * 100_000, 0));
        if (result.Diagnostics.Count > 0)
        {
            throw new InvalidOperationException(result.DisplayText);
        }

        return result;
    }

    public ComponentEditorCommandResult AddPin(string number, string name, CadPoint symbolPosition) =>
        AddPin(number, name, symbolPosition, DefaultCommandGrid);

    public ComponentEditorCommandResult AddPin(string number, string name, CadPoint symbolPosition, CadGrid grid)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pin number is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pin name is required.");
        }

        string normalizedNumber = number.Trim();
        string normalizedName = name.Trim();
        if (pins.Any(pin => string.Equals(pin.Number, normalizedNumber, StringComparison.Ordinal)))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.Duplicate, $"Pin '{normalizedNumber}' already exists.");
        }

        ComponentPin pin = new(
            new ComponentPinId($"{componentId}:pin:{Slug(normalizedNumber)}"),
            normalizedName,
            normalizedNumber,
            ComponentPinElectricalType.Bidirectional);
        CadPoint snappedPosition = grid.Snap(symbolPosition);
        pins = pins
            .Append(pin)
            .ToArray();
        symbols = symbols
            .Select(symbol => symbol with
            {
                Pins = symbol.Pins
                    .Append(new ComponentSymbolPin(pin.Id, snappedPosition, ComponentPinOrientation.Right))
                    .ToArray()
            })
            .ToArray();
        OnPropertyChanged(nameof(Pins));
        OnPropertyChanged(nameof(PinSummaries));
        OnPropertyChanged(nameof(Symbols));
        OnPropertyChanged(nameof(SymbolSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult MovePin(string pinNumberOrName, CadPoint symbolPosition) =>
        MovePin(pinNumberOrName, symbolPosition, DefaultCommandGrid);

    public ComponentEditorCommandResult MovePin(string pinNumberOrName, CadPoint symbolPosition, CadGrid grid)
    {
        ComponentPin? pin = FindPin(pinNumberOrName);
        if (pin is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pin '{pinNumberOrName}' was not found.");
        }

        CadPoint snappedPosition = grid.Snap(symbolPosition);
        symbols = symbols
            .Select(symbol => symbol with
            {
                Pins = symbol.Pins
                    .Select(symbolPin => symbolPin.PinId == pin.Id ? symbolPin with { Position = snappedPosition } : symbolPin)
                    .ToArray()
            })
            .ToArray();
        OnPropertyChanged(nameof(Symbols));
        OnPropertyChanged(nameof(SymbolSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult RenamePin(string pinNumberOrName, string newNumber, string newName)
    {
        ComponentPin? pin = FindPin(pinNumberOrName);
        if (pin is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pin '{pinNumberOrName}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(newNumber))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pin number is required.");
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pin name is required.");
        }

        string normalizedNumber = newNumber.Trim();
        string normalizedName = newName.Trim();
        if (pins.Any(existing => existing.Id != pin.Id && string.Equals(existing.Number, normalizedNumber, StringComparison.Ordinal)))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.Duplicate, $"Pin '{normalizedNumber}' already exists.");
        }

        pins = pins
            .Select(existing => existing.Id == pin.Id ? existing with { Number = normalizedNumber, Name = normalizedName } : existing)
            .ToArray();
        OnPropertyChanged(nameof(Pins));
        OnPropertyChanged(nameof(PinSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult DeletePin(string pinNumberOrName)
    {
        ComponentPin? pin = FindPin(pinNumberOrName);
        if (pin is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pin '{pinNumberOrName}' was not found.");
        }

        pins = pins.Where(existing => existing.Id != pin.Id).ToArray();
        gates = gates
            .Select(gate => gate with { PinIds = gate.PinIds.Where(pinId => pinId != pin.Id).ToArray() })
            .ToArray();
        symbols = symbols
            .Select(symbol => symbol with { Pins = symbol.Pins.Where(symbolPin => symbolPin.PinId != pin.Id).ToArray() })
            .ToArray();
        pinPadMappings = pinPadMappings.Where(mapping => mapping.PinId != pin.Id).ToArray();
        OnPropertyChanged(nameof(Pins));
        OnPropertyChanged(nameof(PinSummaries));
        OnPropertyChanged(nameof(Symbols));
        OnPropertyChanged(nameof(SymbolSummaries));
        OnPropertyChanged(nameof(PinPadMappings));
        return ComponentEditorCommandResult.Success();
    }

    public void AddSymbol(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ComponentSymbol symbol = new(
            new ComponentSymbolId($"{componentId}:symbol:{Slug(name)}"),
            name,
            pins
                .Select((pin, index) => new ComponentSymbolPin(
                    pin.Id,
                    new CadPoint(index * 100_000, 0),
                    ComponentPinOrientation.Right))
                .ToArray(),
            [],
            []);

        symbols = symbols.Append(symbol).ToArray();
        OnPropertyChanged(nameof(Symbols));
        OnPropertyChanged(nameof(SymbolSummaries));
    }

    public ComponentEditorCommandResult AddSymbolLine(string symbolNameOrId, CadPoint start, CadPoint end)
    {
        ComponentSymbol? symbol = FindSymbol(symbolNameOrId);
        if (symbol is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Symbol '{symbolNameOrId}' was not found.");
        }

        ComponentSymbolLinePrimitive primitive = ComponentSymbolPrimitive.Line(start, end, "symbol", "default");
        symbols = symbols
            .Select(existing => existing.Id == symbol.Id
                ? existing with { Primitives = existing.Primitives.Append(primitive).ToArray() }
                : existing)
            .ToArray();
        OnPropertyChanged(nameof(Symbols));
        OnPropertyChanged(nameof(SymbolSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public void AddFootprint(string name, IReadOnlyList<ComponentEditorPadDraft> pads)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(pads);
        ComponentFootprint footprint = new(
            new ComponentFootprintId($"{componentId}:footprint:{Slug(name)}"),
            name,
            pads
                .Select(pad => new ComponentFootprintPad(
                    new ComponentPadId($"{componentId}:pad:{Slug(pad.Name)}"),
                    pad.Name,
                    pad.Position,
                    pad.Size,
                    ComponentPadTechnology.SurfaceMount,
                    ComponentPadShape.Rectangle))
                .ToArray(),
            [],
            []);

        footprints = footprints.Append(footprint).ToArray();
        OnPropertyChanged(nameof(Footprints));
        OnPropertyChanged(nameof(FootprintSummaries));
    }

    public ComponentEditorCommandResult AddPad(string footprintNameOrId, string name, CadPoint position, CadVector size) =>
        AddPad(footprintNameOrId, name, position, size, DefaultCommandGrid);

    public ComponentEditorCommandResult AddPad(string footprintNameOrId, string name, CadPoint position, CadVector size, CadGrid grid)
        => AddPad(footprintNameOrId, name, position, size, ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle, drillSize: null, grid);

    public ComponentEditorCommandResult AddSmdPad(string footprintNameOrId, string name, CadPoint position, CadVector size) =>
        AddPad(footprintNameOrId, name, position, size, ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle, drillSize: null, DefaultCommandGrid);

    private ComponentEditorCommandResult AddPad(
        string footprintNameOrId,
        string name,
        CadPoint position,
        CadVector size,
        ComponentPadTechnology technology,
        ComponentPadShape shape,
        long? drillSize,
        CadGrid grid)
    {
        ComponentFootprint? footprint = FindFootprint(footprintNameOrId);
        if (footprint is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Footprint '{footprintNameOrId}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pad name is required.");
        }

        string normalizedName = name.Trim();
        if (footprint.Pads.Any(pad => string.Equals(pad.Name, normalizedName, StringComparison.Ordinal)))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.Duplicate, $"Pad '{normalizedName}' already exists.");
        }

        ComponentFootprintPad pad = new(
            new ComponentPadId($"{componentId}:pad:{Slug(normalizedName)}"),
            normalizedName,
            grid.Snap(position),
            size,
            technology,
            shape,
            drillSize);
        footprints = footprints
            .Select(existing => existing.Id == footprint.Id ? existing with { Pads = existing.Pads.Append(pad).ToArray() } : existing)
            .ToArray();
        OnPropertyChanged(nameof(Footprints));
        OnPropertyChanged(nameof(FootprintSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult MovePad(string footprintNameOrId, string padName, CadPoint position) =>
        MovePad(footprintNameOrId, padName, position, DefaultCommandGrid);

    public ComponentEditorCommandResult MovePad(string footprintNameOrId, string padName, CadPoint position, CadGrid grid)
    {
        if (!TryFindPad(footprintNameOrId, padName, out ComponentFootprint? footprint, out ComponentFootprintPad? pad, out ComponentEditorCommandResult failure))
        {
            return failure;
        }

        CadPoint snappedPosition = grid.Snap(position);
        footprints = footprints
            .Select(existing => existing.Id == footprint.Id
                ? existing with { Pads = existing.Pads.Select(existingPad => existingPad.Id == pad.Id ? existingPad with { Position = snappedPosition } : existingPad).ToArray() }
                : existing)
            .ToArray();
        OnPropertyChanged(nameof(Footprints));
        OnPropertyChanged(nameof(FootprintSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult RenamePad(string footprintNameOrId, string padName, string newName)
    {
        if (!TryFindPad(footprintNameOrId, padName, out ComponentFootprint? footprint, out ComponentFootprintPad? pad, out ComponentEditorCommandResult failure))
        {
            return failure;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pad name is required.");
        }

        string normalizedName = newName.Trim();
        if (footprint.Pads.Any(existing => existing.Id != pad.Id && string.Equals(existing.Name, normalizedName, StringComparison.Ordinal)))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.Duplicate, $"Pad '{normalizedName}' already exists.");
        }

        footprints = footprints
            .Select(existing => existing.Id == footprint.Id
                ? existing with { Pads = existing.Pads.Select(existingPad => existingPad.Id == pad.Id ? existingPad with { Name = normalizedName } : existingPad).ToArray() }
                : existing)
            .ToArray();
        OnPropertyChanged(nameof(Footprints));
        OnPropertyChanged(nameof(FootprintSummaries));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentEditorCommandResult DeletePad(string footprintNameOrId, string padName)
    {
        if (!TryFindPad(footprintNameOrId, padName, out ComponentFootprint? footprint, out ComponentFootprintPad? pad, out ComponentEditorCommandResult failure))
        {
            return failure;
        }

        footprints = footprints
            .Select(existing => existing.Id == footprint.Id
                ? existing with { Pads = existing.Pads.Where(existingPad => existingPad.Id != pad.Id).ToArray() }
                : existing)
            .ToArray();
        pinPadMappings = pinPadMappings.Where(mapping => mapping.PadId != pad.Id).ToArray();
        OnPropertyChanged(nameof(Footprints));
        OnPropertyChanged(nameof(FootprintSummaries));
        OnPropertyChanged(nameof(PinPadMappings));
        return ComponentEditorCommandResult.Success();
    }

    public void AddPackage(string name, string footprintNameOrId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(footprintNameOrId);
        ComponentFootprint footprint = footprints.First(footprint =>
            string.Equals(footprint.Name, footprintNameOrId, StringComparison.Ordinal) ||
            string.Equals(footprint.Id.Value, footprintNameOrId, StringComparison.Ordinal));
        variants = variants
            .Append(new ComponentVariant(
                new ComponentVariantId($"{componentId}:variant:{Slug(name)}"),
                name,
                footprint.Id,
                []))
            .ToArray();
        OnPropertyChanged(nameof(Variants));
        OnPropertyChanged(nameof(PackageSummaries));
    }

    public ComponentEditorCommandResult MapPinToPad(string pinNumberOrName, string padName)
    {
        ComponentVariant? variant = variants.FirstOrDefault();
        if (variant is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, "Package is required before mapping pins to pads.");
        }

        return MapPinToPad(variant.Name, pinNumberOrName, padName);
    }

    public ComponentEditorCommandResult MapPinToPad(string packageNameOrId, string pinNumberOrName, string padName)
    {
        if (string.IsNullOrWhiteSpace(pinNumberOrName))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pin number or name is required.");
        }

        if (string.IsNullOrWhiteSpace(padName))
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.InvalidInput, "Pad name is required.");
        }

        ComponentVariant? variant = FindVariant(packageNameOrId);
        if (variant is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Package '{packageNameOrId}' was not found.");
        }

        ComponentPin? pin = FindPin(pinNumberOrName);
        if (pin is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pin '{pinNumberOrName}' was not found.");
        }

        ComponentFootprint? footprint = footprints.FirstOrDefault(footprint => footprint.Id == variant.FootprintId);
        if (footprint is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Footprint '{variant.FootprintId.Value}' was not found.");
        }

        ComponentFootprintPad? pad = footprint.Pads.FirstOrDefault(pad => string.Equals(pad.Name, padName, StringComparison.Ordinal));
        if (pad is null)
        {
            return ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pad '{padName}' was not found.");
        }

        pinPadMappings = pinPadMappings
            .Where(mapping => mapping.VariantId != variant.Id || mapping.PinId != pin.Id)
            .Append(new ComponentPinPadMapping(variant.Id, pin.Id, pad.Id))
            .ToArray();
        OnPropertyChanged(nameof(PinPadMappings));
        return ComponentEditorCommandResult.Success();
    }

    public ComponentDefinition ToDefinition() =>
        new(
            new ComponentId(componentId),
            displayName,
            kind,
            manufacturer,
            manufacturerPartNumber,
            description,
            attributes,
            pins,
            gates,
            symbols,
            footprints,
            variants,
            pinPadMappings,
            datasheets,
            sourcing,
            packageModels3D,
            provenance);

    public ComponentDraft ToDraft()
    {
        ComponentVariant? primaryVariant = variants.FirstOrDefault();
        ComponentFootprint? primaryFootprint = primaryVariant is null
            ? footprints.FirstOrDefault()
            : footprints.FirstOrDefault(footprint => footprint.Id == primaryVariant.FootprintId);
        Dictionary<ComponentVariantId, ComponentVariant> variantsById = variants.ToDictionary(variant => variant.Id);

        return new ComponentDraft(
            new ComponentId(componentId),
            displayName,
            new ComponentDraftPackage(
                primaryVariant?.Name ?? primaryFootprint?.Name ?? "Draft Package",
                "U",
                primaryVariant?.Attributes.Select(attribute => new ComponentDraftAttribute(attribute.Name, attribute.Value)).ToArray() ?? []),
            attributes.Select(attribute => new ComponentDraftAttribute(attribute.Name, attribute.Value)).ToArray(),
            pins.Select(pin => new ComponentDraftPin(pin.Id, pin.Name, pin.Number, ToDraftPinElectricalType(pin.ElectricalType))).ToArray(),
            symbols.Select(symbol => new ComponentDraftSymbol(
                symbol.Id,
                symbol.Name,
                symbol.Pins.Select(pin => new ComponentDraftSymbolPin(
                    pin.PinId,
                    pin.Position,
                    PinEndPoint(pin.Position, pin.Orientation),
                    ToDraftPinOrientation(pin.Orientation))).ToArray(),
                symbol.Primitives.Select(ToDraftPrimitive).ToArray())).ToArray(),
            footprints.Select(footprint => new ComponentDraftFootprint(
                footprint.Id,
                footprint.Name,
                footprint.Pads.Select(pad => new ComponentDraftPad(pad.Id, pad.Name, pad.Position, pad.Size, ToDraftPadTechnology(pad.Technology), ToDraftPadShape(pad.Shape), pad.DrillSize)).ToArray(),
                footprint.Silkscreen.Select(line => new ComponentDraftFootprintPrimitive(ComponentDraftPrimitiveKind.Line, line.Start, line.End)).ToArray(),
                footprint.Courtyard.Select(line => new ComponentDraftFootprintPrimitive(ComponentDraftPrimitiveKind.Line, line.Start, line.End)).ToArray())).ToArray(),
            pinPadMappings
                .Where(mapping => variantsById.ContainsKey(mapping.VariantId))
                .Select(mapping => new ComponentDraftDeviceMapping(mapping.PinId, variantsById[mapping.VariantId].FootprintId, mapping.PadId))
                .ToArray());
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string Slug(string value) =>
        string.Join(
            "-",
            value.Trim()
                .ToLowerInvariant()
                .Split([' ', '_', ':', '/', '\\', '.'], StringSplitOptions.RemoveEmptyEntries));

    private ComponentPin? FindPin(string pinNumberOrName) =>
        pins.FirstOrDefault(pin =>
            string.Equals(pin.Number, pinNumberOrName, StringComparison.Ordinal) ||
            string.Equals(pin.Name, pinNumberOrName, StringComparison.Ordinal) ||
            string.Equals(pin.Id.Value, pinNumberOrName, StringComparison.Ordinal));

    private ComponentSymbol? FindSymbol(string symbolNameOrId) =>
        symbols.FirstOrDefault(symbol =>
            string.Equals(symbol.Name, symbolNameOrId, StringComparison.Ordinal) ||
            string.Equals(symbol.Id.Value, symbolNameOrId, StringComparison.Ordinal));

    private ComponentFootprint? FindFootprint(string footprintNameOrId) =>
        footprints.FirstOrDefault(footprint =>
            string.Equals(footprint.Name, footprintNameOrId, StringComparison.Ordinal) ||
            string.Equals(footprint.Id.Value, footprintNameOrId, StringComparison.Ordinal));

    private ComponentVariant? FindVariant(string packageNameOrId) =>
        variants.FirstOrDefault(variant =>
            string.Equals(variant.Name, packageNameOrId, StringComparison.Ordinal) ||
            string.Equals(variant.Id.Value, packageNameOrId, StringComparison.Ordinal));

    private bool TryFindPad(
        string footprintNameOrId,
        string padName,
        out ComponentFootprint footprint,
        out ComponentFootprintPad pad,
        out ComponentEditorCommandResult failure)
    {
        footprint = FindFootprint(footprintNameOrId)!;
        if (footprint is null)
        {
            pad = null!;
            failure = ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Footprint '{footprintNameOrId}' was not found.");
            return false;
        }

        pad = footprint.Pads.FirstOrDefault(pad =>
            string.Equals(pad.Name, padName, StringComparison.Ordinal) ||
            string.Equals(pad.Id.Value, padName, StringComparison.Ordinal))!;
        if (pad is null)
        {
            failure = ComponentEditorCommandResult.Failed(ComponentEditorCommandDiagnosticKind.NotFound, $"Pad '{padName}' was not found.");
            return false;
        }

        failure = ComponentEditorCommandResult.Success();
        return true;
    }

    private static string BuildManufacturerLine(string manufacturer, string manufacturerPartNumber)
    {
        if (manufacturer.Length == 0 && manufacturerPartNumber.Length == 0)
        {
            return "No manufacturer";
        }

        if (manufacturer.Length == 0)
        {
            return manufacturerPartNumber;
        }

        if (manufacturerPartNumber.Length == 0)
        {
            return manufacturer;
        }

        return $"{manufacturer} - {manufacturerPartNumber}";
    }

    private string FootprintNameFor(ComponentFootprintId footprintId) =>
        footprints.FirstOrDefault(footprint => footprint.Id == footprintId)?.Name ?? footprintId.Value;

    private static string SplitPascalCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        List<char> characters = [];
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsWhiteSpace(value[i - 1]))
            {
                characters.Add(' ');
            }

            characters.Add(value[i]);
        }

        return new string(characters.ToArray());
    }

    private static string Plural(int count, string singular, string plural) =>
        count == 1 ? singular : plural;

    private static CadPoint PinEndPoint(CadPoint start, ComponentPinOrientation orientation) =>
        orientation switch
        {
            ComponentPinOrientation.Left => start + new CadVector(-100_000, 0),
            ComponentPinOrientation.Right => start + new CadVector(100_000, 0),
            ComponentPinOrientation.Up => start + new CadVector(0, -100_000),
            ComponentPinOrientation.Down => start + new CadVector(0, 100_000),
            _ => start
        };

    private static ComponentDraftPinElectricalType ToDraftPinElectricalType(ComponentPinElectricalType type) =>
        type switch
        {
            ComponentPinElectricalType.Passive => ComponentDraftPinElectricalType.Passive,
            ComponentPinElectricalType.Input => ComponentDraftPinElectricalType.Input,
            ComponentPinElectricalType.Output => ComponentDraftPinElectricalType.Output,
            ComponentPinElectricalType.Bidirectional => ComponentDraftPinElectricalType.Bidirectional,
            ComponentPinElectricalType.Power => ComponentDraftPinElectricalType.Power,
            ComponentPinElectricalType.NoConnect => ComponentDraftPinElectricalType.NoConnect,
            _ => throw new InvalidOperationException($"Unsupported component pin electrical type {type}.")
        };

    private static ComponentPinElectricalType ToDefinitionPinElectricalType(ComponentDraftPinElectricalType type) =>
        type switch
        {
            ComponentDraftPinElectricalType.Passive => ComponentPinElectricalType.Passive,
            ComponentDraftPinElectricalType.Input => ComponentPinElectricalType.Input,
            ComponentDraftPinElectricalType.Output => ComponentPinElectricalType.Output,
            ComponentDraftPinElectricalType.Bidirectional => ComponentPinElectricalType.Bidirectional,
            ComponentDraftPinElectricalType.Power => ComponentPinElectricalType.Power,
            ComponentDraftPinElectricalType.NoConnect => ComponentPinElectricalType.NoConnect,
            _ => throw new InvalidOperationException($"Unsupported component draft pin electrical type {type}.")
        };

    private static ComponentDraftPinOrientation ToDraftPinOrientation(ComponentPinOrientation orientation) =>
        orientation switch
        {
            ComponentPinOrientation.Left => ComponentDraftPinOrientation.Left,
            ComponentPinOrientation.Right => ComponentDraftPinOrientation.Right,
            ComponentPinOrientation.Up => ComponentDraftPinOrientation.Up,
            ComponentPinOrientation.Down => ComponentDraftPinOrientation.Down,
            _ => throw new InvalidOperationException($"Unsupported component pin orientation {orientation}.")
        };

    private static ComponentPinOrientation ToDefinitionPinOrientation(ComponentDraftPinOrientation orientation) =>
        orientation switch
        {
            ComponentDraftPinOrientation.Left => ComponentPinOrientation.Left,
            ComponentDraftPinOrientation.Right => ComponentPinOrientation.Right,
            ComponentDraftPinOrientation.Up => ComponentPinOrientation.Up,
            ComponentDraftPinOrientation.Down => ComponentPinOrientation.Down,
            _ => throw new InvalidOperationException($"Unsupported component draft pin orientation {orientation}.")
        };

    private static ComponentDraftPadTechnology ToDraftPadTechnology(ComponentPadTechnology technology) =>
        technology switch
        {
            ComponentPadTechnology.ThroughHole => ComponentDraftPadTechnology.ThroughHole,
            ComponentPadTechnology.SurfaceMount => ComponentDraftPadTechnology.SurfaceMount,
            _ => throw new InvalidOperationException($"Unsupported component pad technology {technology}.")
        };

    private static ComponentPadTechnology ToDefinitionPadTechnology(ComponentDraftPadTechnology technology) =>
        technology switch
        {
            ComponentDraftPadTechnology.ThroughHole => ComponentPadTechnology.ThroughHole,
            ComponentDraftPadTechnology.SurfaceMount => ComponentPadTechnology.SurfaceMount,
            _ => throw new InvalidOperationException($"Unsupported component draft pad technology {technology}.")
        };

    private static ComponentDraftPadShape ToDraftPadShape(ComponentPadShape shape) =>
        shape switch
        {
            ComponentPadShape.Round => ComponentDraftPadShape.Round,
            ComponentPadShape.Rectangle => ComponentDraftPadShape.Rectangle,
            ComponentPadShape.RoundedRectangle => ComponentDraftPadShape.RoundedRectangle,
            ComponentPadShape.Oval => ComponentDraftPadShape.Oval,
            _ => throw new InvalidOperationException($"Unsupported component pad shape {shape}.")
        };

    private static ComponentPadShape ToDefinitionPadShape(ComponentDraftPadShape shape) =>
        shape switch
        {
            ComponentDraftPadShape.Round => ComponentPadShape.Round,
            ComponentDraftPadShape.Rectangle => ComponentPadShape.Rectangle,
            ComponentDraftPadShape.RoundedRectangle => ComponentPadShape.RoundedRectangle,
            ComponentDraftPadShape.Oval => ComponentPadShape.Oval,
            _ => throw new InvalidOperationException($"Unsupported component draft pad shape {shape}.")
        };

    private static ComponentDraftSymbolPrimitive ToDraftPrimitive(ComponentSymbolPrimitive primitive) =>
        primitive switch
        {
            ComponentSymbolLinePrimitive line => new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Line, line.Start, line.End),
            ComponentSymbolRectanglePrimitive rectangle => new ComponentDraftSymbolPrimitive(
                ComponentDraftPrimitiveKind.Rectangle,
                new CadPoint(rectangle.Bounds.Left, rectangle.Bounds.Top),
                new CadPoint(rectangle.Bounds.Right, rectangle.Bounds.Bottom)),
            ComponentSymbolCirclePrimitive circle => new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Circle, circle.Center, new CadPoint(circle.Center.X + circle.Radius, circle.Center.Y)),
            ComponentSymbolArcPrimitive arc => new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Arc, arc.Center, new CadPoint(arc.Center.X + arc.Radius, arc.Center.Y)),
            ComponentSymbolTextPrimitive text => new ComponentDraftSymbolPrimitive(ComponentDraftPrimitiveKind.Text, text.Position, text.Position),
            _ => throw new InvalidOperationException($"Unsupported component symbol primitive {primitive.GetType().Name}.")
        };

    private static ComponentSymbolPrimitive ToDefinitionPrimitive(ComponentDraftSymbolPrimitive primitive) =>
        primitive.Kind switch
        {
            ComponentDraftPrimitiveKind.Line => ComponentSymbolPrimitive.Line(primitive.Start, primitive.End, "symbol", "default"),
            ComponentDraftPrimitiveKind.Rectangle => ComponentSymbolPrimitive.Rectangle(CadRectangle.FromCorners(primitive.Start, primitive.End), "symbol", "default"),
            ComponentDraftPrimitiveKind.Circle => ComponentSymbolPrimitive.Circle(primitive.Start, Math.Abs(primitive.End.X - primitive.Start.X), "symbol", "default"),
            ComponentDraftPrimitiveKind.Arc => ComponentSymbolPrimitive.Arc(primitive.Start, Math.Abs(primitive.End.X - primitive.Start.X), 0, 90, "symbol", "default"),
            ComponentDraftPrimitiveKind.Text => ComponentSymbolPrimitive.Text(ComponentSymbolTextKind.Custom, "", primitive.Start, "symbol", "default"),
            _ => throw new InvalidOperationException($"Unsupported component draft primitive kind {primitive.Kind}.")
        };
}

public sealed record ComponentEditorIdentitySummary(
    string DisplayName,
    string ManufacturerLine,
    string KindText,
    string Description);

public sealed record ComponentEditorPinSummary(
    string Number,
    string Name,
    string ElectricalTypeText,
    string DisplayText);

public sealed record ComponentEditorAssetSummary(
    string Id,
    string Name,
    string DisplayText);

public sealed record ComponentEditorPadDraft(
    string Name,
    CadPoint Position,
    CadVector Size);

public sealed record ComponentEditorCommandResult(
    IReadOnlyList<ComponentEditorCommandDiagnostic> Diagnostics)
{
    public bool Succeeded => Diagnostics.Count == 0;

    public string DisplayText => Succeeded
        ? "Command completed"
        : string.Join(", ", Diagnostics.Select(diagnostic => diagnostic.Message));

    public static ComponentEditorCommandResult Success() => new([]);

    public static ComponentEditorCommandResult Failed(ComponentEditorCommandDiagnosticKind kind, string message) =>
        new([new ComponentEditorCommandDiagnostic(kind, message)]);
}

public sealed record ComponentEditorCommandDiagnostic(
    ComponentEditorCommandDiagnosticKind Kind,
    string Message);

public enum ComponentEditorCommandDiagnosticKind
{
    InvalidInput,
    NotFound,
    Duplicate
}

public sealed record ComponentEditorValidationSummary(
    IReadOnlyList<ComponentEditorValidationIssue> Issues,
    string DisplayText)
{
    public static ComponentEditorValidationSummary FromDefinition(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ComponentEditorValidationIssue[] issues =
        [
            .. MissingIssues(definition)
        ];

        return new ComponentEditorValidationSummary(
            issues,
            issues.Length == 0
                ? "Ready to save"
                : string.Join(", ", issues.Select((issue, index) => index == 0 ? issue.DisplayText : LowercaseFirst(issue.DisplayText))));
    }

    private static IEnumerable<ComponentEditorValidationIssue> MissingIssues(ComponentDefinition definition)
    {
        if (definition.Symbols.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingSymbol,
                "Missing symbol");
        }

        if (definition.Pins.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingPins,
                "Missing pins");
        }

        if (definition.Footprints.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingFootprint,
                "Missing footprint");
        }

        if (definition.Variants.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingPackage,
                "Missing package");
        }

        foreach (ComponentEditorValidationIssue issue in MissingPinReferences(definition))
        {
            yield return issue;
        }

        foreach (ComponentEditorValidationIssue issue in DuplicatePinNames(definition))
        {
            yield return issue;
        }

        foreach (ComponentEditorValidationIssue issue in IncompleteMappings(definition))
        {
            yield return issue;
        }

        if (definition.PinPadMappings.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingMapping,
                "Missing pin-pad mapping");
        }
        else
        {
            HashSet<ComponentPinId> mappedPinIds = definition.PinPadMappings
                .Select(mapping => mapping.PinId)
                .ToHashSet();

            foreach (ComponentPin pin in definition.Pins.OrderBy(pin => pin.Number, StringComparer.Ordinal))
            {
                if (!mappedPinIds.Contains(pin.Id))
                {
                    yield return new ComponentEditorValidationIssue(
                        ComponentEditorValidationIssueKind.UnmappedPin,
                        $"Unmapped pin {pin.Number}");
                }
            }
        }
    }

    private static IEnumerable<ComponentEditorValidationIssue> MissingPinReferences(ComponentDefinition definition)
    {
        HashSet<ComponentPinId> pinIds = definition.Pins.Select(pin => pin.Id).ToHashSet();

        foreach (ComponentSymbol symbol in definition.Symbols.OrderBy(symbol => symbol.Name, StringComparer.Ordinal))
        {
            foreach (ComponentSymbolPin pin in symbol.Pins.OrderBy(pin => pin.PinId.Value, StringComparer.Ordinal))
            {
                if (!pinIds.Contains(pin.PinId))
                {
                    yield return new ComponentEditorValidationIssue(
                        ComponentEditorValidationIssueKind.MissingPin,
                        $"Symbol references missing pin {pin.PinId.Value}");
                }
            }
        }
    }

    private static IEnumerable<ComponentEditorValidationIssue> DuplicatePinNames(ComponentDefinition definition) =>
        definition.Pins
            .GroupBy(pin => pin.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.DuplicatePinName,
                $"Duplicate pin name {group.Key}"));

    private static IEnumerable<ComponentEditorValidationIssue> IncompleteMappings(ComponentDefinition definition)
    {
        HashSet<ComponentPinId> pinIds = definition.Pins.Select(pin => pin.Id).ToHashSet();
        Dictionary<ComponentVariantId, ComponentVariant> variantsById = definition.Variants.ToDictionary(variant => variant.Id);
        Dictionary<ComponentFootprintId, ComponentFootprint> footprintsById = definition.Footprints.ToDictionary(footprint => footprint.Id);

        foreach (ComponentPinPadMapping mapping in definition.PinPadMappings
            .OrderBy(mapping => mapping.VariantId.Value, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.PinId.Value, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.PadId.Value, StringComparer.Ordinal))
        {
            if (!variantsById.TryGetValue(mapping.VariantId, out ComponentVariant? variant))
            {
                yield return new ComponentEditorValidationIssue(
                    ComponentEditorValidationIssueKind.IncompleteMapping,
                    $"Mapping references missing package {mapping.VariantId.Value}");
                continue;
            }

            if (!pinIds.Contains(mapping.PinId))
            {
                yield return new ComponentEditorValidationIssue(
                    ComponentEditorValidationIssueKind.IncompleteMapping,
                    $"Mapping references missing pin {mapping.PinId.Value}");
            }

            if (!footprintsById.TryGetValue(variant.FootprintId, out ComponentFootprint? footprint))
            {
                yield return new ComponentEditorValidationIssue(
                    ComponentEditorValidationIssueKind.IncompleteMapping,
                    $"Mapping references missing footprint {variant.FootprintId.Value}");
                continue;
            }

            if (footprint.Pads.All(pad => pad.Id != mapping.PadId))
            {
                yield return new ComponentEditorValidationIssue(
                    ComponentEditorValidationIssueKind.IncompleteMapping,
                    $"Mapping references missing pad {mapping.PadId.Value}");
            }
        }
    }

    private static string LowercaseFirst(string value) =>
        value.Length == 0
            ? value
            : string.Concat(char.ToLowerInvariant(value[0]), value[1..]);
}

public sealed record ComponentEditorValidationIssue(
    ComponentEditorValidationIssueKind Kind,
    string DisplayText);

public sealed record ComponentEditorValidationIssueDisplay(
    ComponentEditorValidationIssueKind Kind,
    string DisplayText);

public enum ComponentEditorValidationIssueKind
{
    None,
    MissingSymbol,
    MissingPins,
    MissingFootprint,
    MissingPackage,
    MissingMapping,
    MissingPin,
    DuplicatePinName,
    IncompleteMapping,
    UnmappedPin
}

internal static class ComponentEditorSnapshot
{
    public static string FromDefinition(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        string attributesText = string.Join("|", definition.Attributes
            .OrderBy(attribute => attribute.Name, StringComparer.Ordinal)
            .ThenBy(attribute => attribute.Value, StringComparer.Ordinal)
            .Select(attribute => $"{attribute.Name}={attribute.Value}"));
        string pinsText = string.Join("|", definition.Pins
            .OrderBy(pin => pin.Id.Value, StringComparer.Ordinal)
            .Select(pin => $"{pin.Id.Value}:{pin.Name}:{pin.Number}:{pin.ElectricalType}"));
        string symbolsText = string.Join("|", definition.Symbols
            .OrderBy(symbol => symbol.Id.Value, StringComparer.Ordinal)
            .Select(symbol => $"{symbol.Id.Value}:{symbol.Name}:{string.Join(",", symbol.Pins.Select(pin => $"{pin.PinId.Value}@{pin.Position.X}:{pin.Position.Y}:{pin.Orientation}"))}"));
        string footprintsText = string.Join("|", definition.Footprints
            .OrderBy(footprint => footprint.Id.Value, StringComparer.Ordinal)
            .Select(footprint => $"{footprint.Id.Value}:{footprint.Name}:{string.Join(",", footprint.Pads.Select(pad => $"{pad.Id.Value}:{pad.Name}@{pad.Position.X}:{pad.Position.Y}:{pad.Size.X}:{pad.Size.Y}:{pad.Technology}:{pad.Shape}:{pad.DrillSize}"))}"));
        string variantsText = string.Join("|", definition.Variants
            .OrderBy(variant => variant.Id.Value, StringComparer.Ordinal)
            .Select(variant => $"{variant.Id.Value}:{variant.Name}:{variant.FootprintId.Value}"));
        string mappingsText = string.Join("|", definition.PinPadMappings
            .OrderBy(mapping => mapping.VariantId.Value, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.PinId.Value, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.PadId.Value, StringComparer.Ordinal)
            .Select(mapping => $"{mapping.VariantId.Value}:{mapping.PinId.Value}:{mapping.PadId.Value}"));

        return string.Join(
            "\n",
            definition.Id.Value,
            definition.DisplayName,
            definition.Kind,
            definition.Manufacturer,
            definition.ManufacturerPartNumber,
            definition.Description,
            attributesText,
            pinsText,
            symbolsText,
            footprintsText,
            variantsText,
            mappingsText);
    }
}
