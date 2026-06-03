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
    private IReadOnlyList<ComponentTypeFilterOption> typeFilterOptions;
    private string searchText = "";
    private string valueFilter = "";
    private string packageFilter = "";
    private string selectedTypeFilterKind = "";
    private string selectedVendorAvailabilityFilter = "";
    private string selectedLifecycleFilter = "";
    private string selectedVerifiedStatusFilter = "";
    private string selectedSourceFilter = "";
    private ComponentManagerRow? selectedComponent;

    private ComponentManagerViewModel(IReadOnlyList<ComponentManagerRow> components)
    {
        allComponents = components;
        typeFilterOptions = BuildTypeFilterOptions(components);
        Components = new ObservableCollection<ComponentManagerRow>(components);
        VerifiedPlaceableComponents = new ObservableCollection<ComponentManagerRow>();
        CatalogOnlyComponents = new ObservableCollection<ComponentManagerRow>();
        DraftComponents = new ObservableCollection<ComponentManagerRow>();
        UpdateResultGroups(components);
        selectedComponent = Components.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ComponentManagerRow> Components { get; }

    public ObservableCollection<ComponentManagerRow> VerifiedPlaceableComponents { get; }

    public ObservableCollection<ComponentManagerRow> CatalogOnlyComponents { get; }

    public ObservableCollection<ComponentManagerRow> DraftComponents { get; }

    public IReadOnlyList<string> TypeFilterOptions => typeFilterOptions
        .Select(option => option.Label)
        .ToArray();

    public IReadOnlyList<string> VendorAvailabilityFilterOptions { get; } =
        ["All availability", "Vendor offers", "No vendor offers"];

    public IReadOnlyList<string> LifecycleFilterOptions => BuildDistinctFilterOptions(
        "All lifecycle states",
        allComponents.Select(row => row.Lifecycle));

    public IReadOnlyList<string> VerifiedStatusFilterOptions { get; } =
        ["All verification states", "Verified", "Needs review"];

    public IReadOnlyList<string> SourceFilterOptions => BuildDistinctFilterOptions(
        "All sources",
        allComponents.Select(row => row.Source));

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

    public string ValueFilter
    {
        get => valueFilter;
        set => SetTextFilter(ref valueFilter, value);
    }

    public string PackageFilter
    {
        get => packageFilter;
        set => SetTextFilter(ref packageFilter, value);
    }

    public string SelectedTypeFilter
    {
        get => LabelForTypeFilterKind(selectedTypeFilterKind);
        set
        {
            string nextValue = ResolveTypeFilterKind(value);
            if (selectedTypeFilterKind == nextValue)
            {
                return;
            }

            selectedTypeFilterKind = nextValue;
            ApplyFilter();
            OnPropertyChanged();
        }
    }

    public string SelectedVendorAvailabilityFilter
    {
        get => string.IsNullOrWhiteSpace(selectedVendorAvailabilityFilter) ? "All availability" : selectedVendorAvailabilityFilter;
        set => SetOptionFilter(ref selectedVendorAvailabilityFilter, NormalizeOption(value, "All availability"));
    }

    public string SelectedLifecycleFilter
    {
        get => string.IsNullOrWhiteSpace(selectedLifecycleFilter) ? "All lifecycle states" : selectedLifecycleFilter;
        set => SetOptionFilter(ref selectedLifecycleFilter, NormalizeOption(value, "All lifecycle states"));
    }

    public string SelectedVerifiedStatusFilter
    {
        get => string.IsNullOrWhiteSpace(selectedVerifiedStatusFilter) ? "All verification states" : selectedVerifiedStatusFilter;
        set => SetOptionFilter(ref selectedVerifiedStatusFilter, NormalizeOption(value, "All verification states"));
    }

    public string SelectedSourceFilter
    {
        get => string.IsNullOrWhiteSpace(selectedSourceFilter) ? "All sources" : selectedSourceFilter;
        set => SetOptionFilter(ref selectedSourceFilter, NormalizeOption(value, "All sources"));
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
        if (selectedTypeFilterKind.Length > 0 && !typeFilterOptions.Any(option => option.Kind == selectedTypeFilterKind))
        {
            selectedTypeFilterKind = "";
            OnPropertyChanged(nameof(SelectedTypeFilter));
        }

        OnPropertyChanged(nameof(TypeFilterOptions));
        OnPropertyChanged(nameof(LifecycleFilterOptions));
        OnPropertyChanged(nameof(SourceFilterOptions));
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
        ComponentManagerRow? previousSelection = selectedComponent;
        string filter = searchText.Trim();
        string value = valueFilter.Trim();
        string package = packageFilter.Trim();
        IEnumerable<ComponentManagerRow> rows = allComponents;
        if (selectedTypeFilterKind.Length > 0)
        {
            rows = rows.Where(row => row.Kind == selectedTypeFilterKind);
        }

        if (value.Length > 0)
        {
            rows = rows.Where(row => Contains(row.Value, value));
        }

        if (package.Length > 0)
        {
            rows = rows.Where(row => row.PackageOptions.Any(option => option.Matches(package)) || Contains(row.ActivePackageLabel, package));
        }

        if (selectedVendorAvailabilityFilter == "Vendor offers")
        {
            rows = rows.Where(row => row.HasVendorOffers);
        }
        else if (selectedVendorAvailabilityFilter == "No vendor offers")
        {
            rows = rows.Where(row => !row.HasVendorOffers);
        }

        if (selectedLifecycleFilter.Length > 0 && selectedLifecycleFilter != "All lifecycle states")
        {
            rows = rows.Where(row => string.Equals(row.Lifecycle, selectedLifecycleFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedVerifiedStatusFilter == "Verified")
        {
            rows = rows.Where(row => row.IsVerified);
        }
        else if (selectedVerifiedStatusFilter == "Needs review")
        {
            rows = rows.Where(row => !row.IsVerified);
        }

        if (selectedSourceFilter.Length > 0 && selectedSourceFilter != "All sources")
        {
            rows = rows.Where(row => string.Equals(row.Source, selectedSourceFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.Length > 0)
        {
            rows = rows.Where(row => row.Matches(filter));
        }

        ComponentManagerRow[] filteredRows = rows.ToArray();
        Components.Clear();
        foreach (ComponentManagerRow row in filteredRows)
        {
            Components.Add(row);
        }

        UpdateResultGroups(filteredRows);

        SelectedComponent = previousSelection is not null && Components.Contains(previousSelection)
            ? previousSelection
            : Components.FirstOrDefault();
    }

    private void SetTextFilter(ref string field, string value)
    {
        value ??= "";
        if (field == value)
        {
            return;
        }

        field = value;
        ApplyFilter();
        OnPropertyChanged();
    }

    private void SetOptionFilter(ref string field, string value)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        ApplyFilter();
        OnPropertyChanged();
    }

    private void UpdateResultGroups(IEnumerable<ComponentManagerRow> rows)
    {
        ComponentManagerRow[] rowArray = rows.ToArray();
        ReplaceRows(VerifiedPlaceableComponents, rowArray.Where(row => row.PlacementState == ComponentPlacementState.VerifiedPlaceable));
        ReplaceRows(CatalogOnlyComponents, rowArray.Where(row => row.PlacementState == ComponentPlacementState.CatalogOnly));
        ReplaceRows(DraftComponents, rowArray.Where(row => row.PlacementState == ComponentPlacementState.Draft));
    }

    private static void ReplaceRows(ObservableCollection<ComponentManagerRow> target, IEnumerable<ComponentManagerRow> rows)
    {
        target.Clear();
        foreach (ComponentManagerRow row in rows)
        {
            target.Add(row);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private string LabelForTypeFilterKind(string kind) =>
        typeFilterOptions.FirstOrDefault(option => option.Kind == kind)?.Label ??
        typeFilterOptions.First(option => option.Kind.Length == 0).Label;

    private string ResolveTypeFilterKind(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        ComponentTypeFilterOption? option = typeFilterOptions.FirstOrDefault(option =>
            string.Equals(option.Label, value, StringComparison.Ordinal) ||
            string.Equals(option.Kind, value, StringComparison.Ordinal));
        return option?.Kind ?? "";
    }

    private static IReadOnlyList<ComponentTypeFilterOption> BuildTypeFilterOptions(IReadOnlyList<ComponentManagerRow> rows) =>
        new[] { new ComponentTypeFilterOption("", $"All components ({rows.Count})") }
            .Concat(rows
                .GroupBy(row => row.Kind, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new ComponentTypeFilterOption(
                    group.Key,
                    $"{CategoryLabel(group.Key)} ({group.Count()})")))
            .ToArray();

    private static string CategoryLabel(string kind) =>
        kind switch
        {
            "Connector" => "Connectors",
            "Custom" => "Custom",
            "IntegratedCircuit" => "Integrated circuits",
            "Mechanical" => "Mechanical",
            "Module" => "Modules",
            "Passive" => "Passives",
            _ => $"{kind}s"
        };

    private static IReadOnlyList<string> BuildDistinctFilterOptions(string allLabel, IEnumerable<string> values) =>
        new[] { allLabel }
            .Concat(values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            .ToArray();

    private static string NormalizeOption(string value, string allLabel) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value, allLabel, StringComparison.OrdinalIgnoreCase)
            ? ""
            : value.Trim();

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

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
    ComponentFootprintPreview FootprintPreview,
    string Value = "",
    string Lifecycle = "Unknown",
    string DatasheetLink = "",
    IReadOnlyList<ComponentVendorOffer>? VendorOffersInput = null,
    IReadOnlyList<string>? WarningsInput = null,
    bool IsVerified = false,
    ComponentPlacementState PlacementState = ComponentPlacementState.CatalogOnly)
{
    public IReadOnlyList<ComponentVendorOffer> VendorOffers { get; init; } = VendorOffersInput ?? [];

    public IReadOnlyList<string> Warnings { get; init; } = WarningsInput ?? [];

    public bool HasVendorOffers => VendorOffers.Count > 0;

    public bool CanPlaceWithoutReview => PlacementState == ComponentPlacementState.VerifiedPlaceable;

    public bool RequiresReviewBeforePlacement => !CanPlaceWithoutReview;

    public static ComponentManagerRow FromCatalogEntry(ComponentCatalogEntry entry)
    {
        ComponentDefinition definition = entry.Definition;
        ComponentPackageOption[] packageOptions = ComponentPackageOption.FromDefinition(definition);
        string value = AttributeValue(definition, "Value");
        string lifecycle = AttributeValue(definition, "Lifecycle");
        ComponentPlacementState placementState = ResolvePlacementState(entry.Source, definition);
        bool isVerified = placementState == ComponentPlacementState.VerifiedPlaceable;
        ComponentVendorOffer[] vendorOffers = definition.Sourcing
            .Select(source => new ComponentVendorOffer(
                source.Distributor,
                source.DistributorPartNumber,
                source.Manufacturer,
                source.ManufacturerPartNumber))
            .ToArray();
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
            ComponentFootprintPreview.FromDefinition(definition),
            value,
            string.IsNullOrWhiteSpace(lifecycle) ? "Unknown" : lifecycle,
            definition.Datasheets.FirstOrDefault()?.Location ?? "",
            vendorOffers,
            BuildWarnings(definition, placementState).ToArray(),
            isVerified,
            placementState);
    }

    public bool Matches(string searchText) =>
        Contains(ComponentId, searchText) ||
        Contains(DisplayName, searchText) ||
        Contains(Manufacturer, searchText) ||
        Contains(ManufacturerPartNumber, searchText) ||
        Contains(Kind, searchText) ||
        Contains(Value, searchText) ||
        Contains(Lifecycle, searchText) ||
        Contains(Source, searchText) ||
        Contains(ActivePackageLabel, searchText) ||
        PackageOptions.Any(option => option.Matches(searchText)) ||
        VendorOffers.Any(offer => offer.Matches(searchText));

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

    private static string AttributeValue(ComponentDefinition definition, string name) =>
        definition.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

    private static ComponentPlacementState ResolvePlacementState(ComponentCatalogSource source, ComponentDefinition definition)
    {
        bool hasPlacementGeometry = definition.Symbols.Count > 0 && definition.Footprints.Count > 0;
        if (definition.Provenance.Any(record => record.Kind == ComponentProvenanceKind.DatasheetGenerated))
        {
            return ComponentPlacementState.Draft;
        }

        if (!hasPlacementGeometry)
        {
            return ComponentPlacementState.CatalogOnly;
        }

        if (source == ComponentCatalogSource.BuiltIn ||
            definition.Provenance.Any(record => record.Kind is ComponentProvenanceKind.Native or ComponentProvenanceKind.Manual))
        {
            return ComponentPlacementState.VerifiedPlaceable;
        }

        return ComponentPlacementState.Draft;
    }

    private static IEnumerable<string> BuildWarnings(ComponentDefinition definition, ComponentPlacementState placementState)
    {
        if (placementState == ComponentPlacementState.Draft)
        {
            yield return "Review required before placement.";
        }
        else if (placementState == ComponentPlacementState.CatalogOnly)
        {
            yield return "Catalog-only component is missing verified placement geometry.";
        }

        if (definition.Datasheets.Count == 0)
        {
            yield return "Datasheet link missing.";
        }

        if (definition.Sourcing.Count == 0)
        {
            yield return "Vendor offers unavailable.";
        }
    }

    private static bool PackageOptionMatches(ComponentPackageOption option, ComponentPackageOption selectedOption) =>
        string.Equals(option.VariantId, selectedOption.VariantId, StringComparison.Ordinal) &&
        string.Equals(option.FootprintId, selectedOption.FootprintId, StringComparison.Ordinal);
}

public sealed record ComponentTypeFilterOption(string Kind, string Label);

public enum ComponentPlacementState
{
    VerifiedPlaceable,
    CatalogOnly,
    Draft
}

public sealed record ComponentVendorOffer(
    string Vendor,
    string DistributorPartNumber,
    string Manufacturer,
    string ManufacturerPartNumber)
{
    public bool Matches(string searchText) =>
        Contains(Vendor, searchText) ||
        Contains(DistributorPartNumber, searchText) ||
        Contains(Manufacturer, searchText) ||
        Contains(ManufacturerPartNumber, searchText);

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
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
