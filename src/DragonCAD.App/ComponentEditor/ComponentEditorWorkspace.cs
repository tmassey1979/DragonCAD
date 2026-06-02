using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public ComponentEditorValidationSummary ValidationSummary => ComponentEditorValidationSummary.FromDefinition(ViewModel.ToDefinition());

    public ComponentEditorSaveReadinessSummary SaveReadiness
    {
        get
        {
            ComponentEditorValidationSummary validationSummary = ValidationSummary;
            if (validationSummary.Issues.Count > 0)
            {
                string issueText = validationSummary.Issues.Count == 1 ? "issue" : "issues";
                return new ComponentEditorSaveReadinessSummary(
                    ComponentEditorSaveReadiness.BlockedByValidation,
                    $"Resolve {validationSummary.Issues.Count} validation {issueText} before saving.");
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
}

public enum ComponentEditorSessionKind
{
    New,
    Edit
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

public sealed class ComponentEditorViewModel : INotifyPropertyChanged
{
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
        set => SetField(ref displayName, value);
    }

    public string Manufacturer
    {
        get => manufacturer;
        set => SetField(ref manufacturer, value);
    }

    public string ManufacturerPartNumber
    {
        get => manufacturerPartNumber;
        set => SetField(ref manufacturerPartNumber, value);
    }

    public string Description
    {
        get => description;
        set => SetField(ref description, value);
    }

    public ComponentKind Kind
    {
        get => kind;
        set => SetField(ref kind, value);
    }

    public IReadOnlyList<ComponentPin> Pins => pins;

    public IReadOnlyList<ComponentSymbol> Symbols => symbols;

    public IReadOnlyList<ComponentFootprint> Footprints => footprints;

    public IReadOnlyList<ComponentVariant> Variants => variants;

    public IReadOnlyList<ComponentPinPadMapping> PinPadMappings => pinPadMappings;

    public static ComponentEditorViewModel FromDefinition(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new ComponentEditorViewModel(definition);
    }

    public void AddPin(string number, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        pins = pins
            .Append(new ComponentPin(
                new ComponentPinId($"{componentId}:pin:{Slug(number)}"),
                name,
                number,
                ComponentPinElectricalType.Bidirectional))
            .ToArray();
        OnPropertyChanged(nameof(Pins));
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
    }

    public void MapPinToPad(string pinNumberOrName, string padName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinNumberOrName);
        ArgumentException.ThrowIfNullOrWhiteSpace(padName);
        ComponentVariant variant = variants.First();
        ComponentPin pin = pins.First(pin =>
            string.Equals(pin.Number, pinNumberOrName, StringComparison.Ordinal) ||
            string.Equals(pin.Name, pinNumberOrName, StringComparison.Ordinal));
        ComponentFootprint footprint = footprints.First(footprint => footprint.Id == variant.FootprintId);
        ComponentFootprintPad pad = footprint.Pads.First(pad => string.Equals(pad.Name, padName, StringComparison.Ordinal));

        pinPadMappings = pinPadMappings
            .Append(new ComponentPinPadMapping(variant.Id, pin.Id, pad.Id))
            .ToArray();
        OnPropertyChanged(nameof(PinPadMappings));
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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string Slug(string value) =>
        string.Join(
            "-",
            value.Trim()
                .ToLowerInvariant()
                .Split([' ', '_', ':', '/', '\\', '.'], StringSplitOptions.RemoveEmptyEntries));
}

public sealed record ComponentEditorPadDraft(
    string Name,
    CadPoint Position,
    CadVector Size);

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

        if (definition.PinPadMappings.Count == 0)
        {
            yield return new ComponentEditorValidationIssue(
                ComponentEditorValidationIssueKind.MissingMapping,
                "Missing pin-pad mapping");
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

public enum ComponentEditorValidationIssueKind
{
    MissingSymbol,
    MissingFootprint,
    MissingPackage,
    MissingMapping
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
