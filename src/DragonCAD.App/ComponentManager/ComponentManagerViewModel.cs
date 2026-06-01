using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Components.Catalog;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.ComponentManager;

public sealed class ComponentManagerViewModel : INotifyPropertyChanged
{
    private IReadOnlyList<ComponentManagerRow> allComponents;
    private IReadOnlyList<string> typeFilterOptions;
    private string searchText = "";
    private string selectedTypeFilter = "All";
    private ComponentManagerRow? selectedComponent;

    private ComponentManagerViewModel(IReadOnlyList<ComponentManagerRow> components)
    {
        allComponents = components;
        typeFilterOptions = BuildTypeFilterOptions(components);
        Components = new ObservableCollection<ComponentManagerRow>(components);
        selectedComponent = Components.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ComponentManagerRow> Components { get; }

    public IReadOnlyList<string> TypeFilterOptions => typeFilterOptions;

    public string SearchText
    {
        get => searchText;
        set
        {
            if (searchText == value)
            {
                return;
            }

            searchText = value;
            ApplyFilter();
            OnPropertyChanged();
        }
    }

    public string SelectedTypeFilter
    {
        get => selectedTypeFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedTypeFilter == nextValue)
            {
                return;
            }

            selectedTypeFilter = nextValue;
            ApplyFilter();
            OnPropertyChanged();
        }
    }

    public ComponentManagerRow? SelectedComponent
    {
        get => selectedComponent;
        set
        {
            if (selectedComponent == value)
            {
                return;
            }

            selectedComponent = value;
            OnPropertyChanged();
        }
    }

    public static ComponentManagerViewModel FromCatalog(ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        ComponentManagerRow[] rows = catalog.EnumerateEffectiveDefinitions()
            .Select(ComponentManagerRow.FromCatalogEntry)
            .OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ComponentId, StringComparer.Ordinal)
            .ToArray();

        return new ComponentManagerViewModel(rows);
    }

    public void ReplaceFromCatalog(ComponentCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        allComponents = catalog.EnumerateEffectiveDefinitions()
            .Select(ComponentManagerRow.FromCatalogEntry)
            .OrderBy(row => row.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ComponentId, StringComparer.Ordinal)
            .ToArray();
        typeFilterOptions = BuildTypeFilterOptions(allComponents);
        if (!typeFilterOptions.Contains(selectedTypeFilter, StringComparer.Ordinal))
        {
            selectedTypeFilter = "All";
            OnPropertyChanged(nameof(SelectedTypeFilter));
        }

        OnPropertyChanged(nameof(TypeFilterOptions));
        ApplyFilter();
    }

    public void SelectPackageOption(ComponentManagerRow row, ComponentPackageOption option)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(option);

        ComponentManagerRow updatedRow = row.WithSelectedPackageOption(option);
        ReplaceRow(allComponents, row, updatedRow);
        ReplaceRow(Components, row, updatedRow);
        if (selectedComponent == row)
        {
            SelectedComponent = updatedRow;
        }
    }

    private void ApplyFilter()
    {
        string filter = searchText.Trim();
        IEnumerable<ComponentManagerRow> rows = allComponents;
        if (selectedTypeFilter != "All")
        {
            rows = rows.Where(row => row.Kind == selectedTypeFilter);
        }

        if (filter.Length > 0)
        {
            rows = rows.Where(row => row.Matches(filter));
        }

        Components.Clear();
        foreach (ComponentManagerRow row in rows)
        {
            Components.Add(row);
        }

        SelectedComponent = Components.FirstOrDefault();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static IReadOnlyList<string> BuildTypeFilterOptions(IReadOnlyList<ComponentManagerRow> rows) =>
        new[] { "All" }
            .Concat(rows.Select(row => row.Kind).Distinct(StringComparer.Ordinal).OrderBy(kind => kind, StringComparer.Ordinal))
            .ToArray();

    private static void ReplaceRow(IReadOnlyList<ComponentManagerRow> rows, ComponentManagerRow oldRow, ComponentManagerRow newRow)
    {
        if (rows is ComponentManagerRow[] rowArray)
        {
            int index = Array.IndexOf(rowArray, oldRow);
            if (index >= 0)
            {
                rowArray[index] = newRow;
            }
        }
    }

    private static void ReplaceRow(ObservableCollection<ComponentManagerRow> rows, ComponentManagerRow oldRow, ComponentManagerRow newRow)
    {
        int index = rows.IndexOf(oldRow);
        if (index >= 0)
        {
            rows[index] = newRow;
        }
    }
}

public sealed record ComponentManagerRow(
    string ComponentId,
    string DisplayName,
    string Kind,
    string Manufacturer,
    string ManufacturerPartNumber,
    string Source,
    int SymbolCount,
    int FootprintCount,
    bool HasDatasheet,
    bool HasSourcing,
    bool HasModel3D,
    string CapabilitySummary,
    int PackageOptionCount,
    string ActivePackageLabel,
    ComponentPackageSummary SelectedPackageSummary,
    ComponentPackageOption? SelectedPackageOption,
    IReadOnlyList<ComponentPackageOption> PackageOptions,
    ComponentSymbolPreview SymbolPreview,
    ComponentFootprintPreview FootprintPreview)
{
    public static ComponentManagerRow FromCatalogEntry(ComponentCatalogEntry entry)
    {
        ComponentDefinition definition = entry.Definition;
        ComponentPackageOption[] packageOptions = ComponentPackageOption.FromDefinition(definition);
        return new ComponentManagerRow(
            definition.Id.Value,
            definition.DisplayName,
            definition.Kind.ToString(),
            definition.Manufacturer,
            definition.ManufacturerPartNumber,
            entry.Source.ToString(),
            definition.Symbols.Count,
            definition.Footprints.Count,
            definition.Datasheets.Count > 0,
            definition.Sourcing.Count > 0,
            definition.PackageModels3D.Count > 0,
            BuildCapabilitySummary(definition),
            packageOptions.Length,
            packageOptions.FirstOrDefault()?.Label ?? "No package",
            ComponentPackageSummary.FromOption(packageOptions.FirstOrDefault()),
            packageOptions.FirstOrDefault(),
            packageOptions,
            ComponentSymbolPreview.FromDefinition(definition),
            ComponentFootprintPreview.FromDefinition(definition));
    }

    public bool Matches(string searchText) =>
        Contains(ComponentId, searchText) ||
        Contains(DisplayName, searchText) ||
        Contains(Manufacturer, searchText) ||
        Contains(ManufacturerPartNumber, searchText) ||
        Contains(Kind, searchText) ||
        Contains(ActivePackageLabel, searchText) ||
        PackageOptions.Any(option => option.Matches(searchText));

    private static string BuildCapabilitySummary(ComponentDefinition definition)
    {
        List<string> capabilities =
        [
            CountText(definition.Symbols.Count, "symbol"),
            CountText(definition.Footprints.Count, "footprint")
        ];

        if (definition.Datasheets.Count > 0)
        {
            capabilities.Add("datasheet");
        }

        if (definition.Sourcing.Count > 0)
        {
            capabilities.Add("sourcing");
        }

        if (definition.PackageModels3D.Count > 0)
        {
            capabilities.Add("3D");
        }

        return string.Join(" / ", capabilities);
    }

    private static string CountText(int count, string singularName) =>
        $"{count} {singularName}{(count == 1 ? "" : "s")}";

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    public ComponentManagerRow WithSelectedPackageOption(ComponentPackageOption selectedOption)
    {
        ArgumentNullException.ThrowIfNull(selectedOption);

        ComponentPackageOption[] updatedOptions = PackageOptions
            .Select(option => option with { IsActive = PackageOptionMatches(option, selectedOption) })
            .ToArray();
        ComponentPackageOption activeOption = updatedOptions.FirstOrDefault(option => option.IsActive) ?? selectedOption;

        return this with
        {
            ActivePackageLabel = activeOption.Label,
            SelectedPackageSummary = ComponentPackageSummary.FromOption(activeOption),
            SelectedPackageOption = activeOption,
            PackageOptions = updatedOptions,
            FootprintPreview = activeOption.FootprintPreview
        };
    }

    private static bool PackageOptionMatches(ComponentPackageOption option, ComponentPackageOption selectedOption) =>
        string.Equals(option.VariantId, selectedOption.VariantId, StringComparison.Ordinal) &&
        string.Equals(option.FootprintId, selectedOption.FootprintId, StringComparison.Ordinal);
}

public sealed record ComponentPackageSummary(
    string FootprintId,
    int PadCount,
    bool HasModel3D,
    string DisplayText)
{
    public static ComponentPackageSummary Empty { get; } =
        new("", 0, false, "No package selected");

    public static ComponentPackageSummary FromOption(ComponentPackageOption? option) =>
        option is null
            ? Empty
            : new ComponentPackageSummary(
                option.FootprintId,
                option.PadCount,
                option.HasModel3D,
                $"{option.Label} - {option.FootprintId} - {option.PadCount} pad{(option.PadCount == 1 ? "" : "s")}{(option.HasModel3D ? " - 3D model" : "")}");
}

public sealed record ComponentPackageOption(
    string VariantId,
    string FootprintId,
    string Label,
    string DisplayText,
    int PadCount,
    bool HasModel3D,
    bool IsActive,
    ComponentFootprintPreview FootprintPreview)
{
    public static ComponentPackageOption[] FromDefinition(ComponentDefinition definition)
    {
        Dictionary<string, ComponentFootprint> footprintsById = definition.Footprints
            .ToDictionary(footprint => footprint.Id.Value, StringComparer.Ordinal);
        HashSet<string> variantsWithModel3D = definition.PackageModels3D
            .Select(model => model.VariantId.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (definition.Variants.Count > 0)
        {
            return definition.Variants
                .OrderBy(variant => variant.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(variant => variant.Id.Value, StringComparer.Ordinal)
                .Select((variant, index) =>
                {
                    footprintsById.TryGetValue(variant.FootprintId.Value, out ComponentFootprint? footprint);
                    string footprintName = footprint?.Name ?? variant.FootprintId.Value;
                    int padCount = footprint?.Pads.Count ?? 0;
                    string label = $"{variant.Name} ({footprintName})";
                    return new ComponentPackageOption(
                        variant.Id.Value,
                        variant.FootprintId.Value,
                        label,
                        BuildDisplayText(label, padCount, variantsWithModel3D.Contains(variant.Id.Value)),
                        padCount,
                        variantsWithModel3D.Contains(variant.Id.Value),
                        index == 0,
                        footprint is null ? ComponentFootprintPreview.Empty : ComponentFootprintPreview.FromFootprint(footprint));
                })
                .ToArray();
        }

        return definition.Footprints
            .OrderBy(footprint => footprint.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(footprint => footprint.Id.Value, StringComparer.Ordinal)
            .Select((footprint, index) => new ComponentPackageOption(
                VariantId: "",
                footprint.Id.Value,
                footprint.Name,
                BuildDisplayText(footprint.Name, footprint.Pads.Count, hasModel3D: false),
                footprint.Pads.Count,
                false,
                index == 0,
                ComponentFootprintPreview.FromFootprint(footprint)))
            .ToArray();
    }

    public bool Matches(string searchText) =>
        Contains(VariantId, searchText) ||
        Contains(FootprintId, searchText) ||
        Contains(Label, searchText) ||
        Contains(DisplayText, searchText);

    private static string BuildDisplayText(string label, int padCount, bool hasModel3D)
    {
        string displayText = $"{label} - {padCount} pad{(padCount == 1 ? "" : "s")}";
        return hasModel3D ? $"{displayText} - 3D model" : displayText;
    }

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}

public sealed record ComponentSymbolPreview(
    CadRectangle Bounds,
    IReadOnlyList<ComponentPreviewLine> Lines,
    IReadOnlyList<ComponentSymbolPinPreview> Pins)
{
    public static ComponentSymbolPreview FromDefinition(ComponentDefinition definition)
    {
        ComponentSymbol? symbol = definition.Symbols.FirstOrDefault();
        if (symbol is null)
        {
            return Empty;
        }

        Dictionary<string, ComponentPin> pinsById = definition.Pins.ToDictionary(pin => pin.Id.Value, StringComparer.Ordinal);
        ComponentPreviewLine[] lines = symbol.Lines
            .Select(line => new ComponentPreviewLine(line.Start, line.End))
            .ToArray();
        ComponentSymbolPinPreview[] pins = symbol.Pins
            .Select(pin =>
            {
                pinsById.TryGetValue(pin.PinId.Value, out ComponentPin? componentPin);
                return new ComponentSymbolPinPreview(
                    componentPin?.Name ?? pin.PinId.Value,
                    pin.Position,
                    BodyPointForPin(pin.Position, pin.Orientation),
                    pin.Orientation.ToString());
            })
            .ToArray();

        return new ComponentSymbolPreview(CalculateBounds(lines, pins), lines, pins);
    }

    public static ComponentSymbolPreview Empty { get; } =
        new(new CadRectangle(0, 0, 0, 0), [], []);

    private static CadPoint BodyPointForPin(CadPoint connectPoint, ComponentPinOrientation orientation) =>
        orientation switch
        {
            ComponentPinOrientation.Left => connectPoint + new CadVector(-50_000, 0),
            ComponentPinOrientation.Right => connectPoint + new CadVector(50_000, 0),
            ComponentPinOrientation.Up => connectPoint + new CadVector(0, -50_000),
            ComponentPinOrientation.Down => connectPoint + new CadVector(0, 50_000),
            _ => connectPoint
        };

    private static CadRectangle CalculateBounds(
        IReadOnlyList<ComponentPreviewLine> lines,
        IReadOnlyList<ComponentSymbolPinPreview> pins)
    {
        List<CadPoint> points = [];
        foreach (ComponentPreviewLine line in lines)
        {
            points.Add(line.Start);
            points.Add(line.End);
        }

        foreach (ComponentSymbolPinPreview pin in pins)
        {
            points.Add(pin.ConnectPoint);
            points.Add(pin.BodyPoint);
        }

        return ComponentPreviewBounds.FromPoints(points);
    }
}

public sealed record ComponentSymbolPinPreview(
    string Name,
    CadPoint ConnectPoint,
    CadPoint BodyPoint,
    string Orientation);

public sealed record ComponentFootprintPreview(
    CadRectangle Bounds,
    IReadOnlyList<ComponentPreviewLine> Lines,
    IReadOnlyList<ComponentFootprintPadPreview> Pads)
{
    public static ComponentFootprintPreview FromDefinition(ComponentDefinition definition)
    {
        ComponentFootprint? footprint = definition.Footprints.FirstOrDefault();
        if (footprint is null)
        {
            return Empty;
        }

        return FromFootprint(footprint);
    }

    public static ComponentFootprintPreview FromFootprint(ComponentFootprint footprint)
    {
        ComponentPreviewLine[] lines = footprint.Silkscreen
            .Concat(footprint.Courtyard)
            .Select(line => new ComponentPreviewLine(line.Start, line.End))
            .ToArray();
        ComponentFootprintPadPreview[] pads = footprint.Pads
            .Select(pad => new ComponentFootprintPadPreview(pad.Name, pad.Position, pad.Size, pad.Shape.ToString(), pad.Technology.ToString()))
            .ToArray();

        return new ComponentFootprintPreview(CalculateBounds(lines, pads), lines, pads);
    }

    public static ComponentFootprintPreview Empty { get; } =
        new(new CadRectangle(0, 0, 0, 0), [], []);

    private static CadRectangle CalculateBounds(
        IReadOnlyList<ComponentPreviewLine> lines,
        IReadOnlyList<ComponentFootprintPadPreview> pads)
    {
        List<CadPoint> points = [];
        foreach (ComponentPreviewLine line in lines)
        {
            points.Add(line.Start);
            points.Add(line.End);
        }

        foreach (ComponentFootprintPadPreview pad in pads)
        {
            long halfWidth = pad.Size.X / 2;
            long halfHeight = pad.Size.Y / 2;
            points.Add(new CadPoint(pad.Position.X - halfWidth, pad.Position.Y - halfHeight));
            points.Add(new CadPoint(pad.Position.X + halfWidth, pad.Position.Y + halfHeight));
        }

        return ComponentPreviewBounds.FromPoints(points);
    }
}

public sealed record ComponentFootprintPadPreview(
    string Name,
    CadPoint Position,
    CadVector Size,
    string Shape,
    string Technology);

public sealed record ComponentPreviewLine(CadPoint Start, CadPoint End);

internal static class ComponentPreviewBounds
{
    public static CadRectangle FromPoints(IReadOnlyList<CadPoint> points)
    {
        if (points.Count == 0)
        {
            return new CadRectangle(0, 0, 0, 0);
        }

        return new CadRectangle(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }
}
