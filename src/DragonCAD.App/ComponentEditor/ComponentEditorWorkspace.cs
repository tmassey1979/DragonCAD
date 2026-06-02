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
        OnPropertyChanged(nameof(PinSummaries));
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

public sealed record ComponentEditorValidationIssueDisplay(
    ComponentEditorValidationIssueKind Kind,
    string DisplayText);

public enum ComponentEditorValidationIssueKind
{
    None,
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
