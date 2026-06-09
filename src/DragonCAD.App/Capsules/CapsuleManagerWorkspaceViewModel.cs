using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DragonCAD.Core.Capsules;

namespace DragonCAD.App.Capsules;

public sealed class CapsuleManagerWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<CapsuleRowViewModel> allRows;
    private string searchText = "";
    private string selectedCategoryFilter = "All";
    private CapsuleRowViewModel? selectedCapsule;

    private CapsuleManagerWorkspaceViewModel(IReadOnlyList<CapsuleRowViewModel> rows)
    {
        allRows = rows;
        Capsules = new ObservableCollection<CapsuleRowViewModel>(rows);
        CategoryFilterOptions = BuildCategoryFilterOptions(rows);
        selectedCapsule = Capsules.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CapsuleRowViewModel> Capsules { get; }

    public IReadOnlyList<string> CategoryFilterOptions { get; }

    public int VisibleCapsuleCount => Capsules.Count;

    public int TotalCapsuleCount => allRows.Count;

    public string SearchSummary =>
        VisibleCapsuleCount == 0
            ? "No capsules match the current filters"
            : $"Showing {VisibleCapsuleCount} of {TotalCapsuleCount} capsules";

    public string SearchText
    {
        get => searchText;
        set
        {
            string nextValue = value ?? "";
            if (searchText == nextValue)
            {
                return;
            }

            searchText = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string SelectedCategoryFilter
    {
        get => selectedCategoryFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedCategoryFilter == nextValue)
            {
                return;
            }

            selectedCategoryFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public CapsuleRowViewModel? SelectedCapsule
    {
        get => selectedCapsule;
        set
        {
            if (selectedCapsule == value)
            {
                return;
            }

            selectedCapsule = value;
            OnPropertyChanged();
        }
    }

    public static CapsuleManagerWorkspaceViewModel FromItems(IEnumerable<CapsuleCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        CapsuleRowViewModel[] rows = items
            .Select(item => CapsuleRowViewModel.FromItem(item))
            .OrderBy(row => row.Category, StringComparer.Ordinal)
            .ThenBy(row => row.DisplayName, StringComparer.Ordinal)
            .ToArray();

        return new CapsuleManagerWorkspaceViewModel(rows);
    }

    private void ApplyFilters()
    {
        IEnumerable<CapsuleRowViewModel> rows = allRows;

        if (selectedCategoryFilter != "All")
        {
            rows = rows.Where(row => string.Equals(row.Category, selectedCategoryFilter, StringComparison.Ordinal));
        }

        string trimmedSearchText = searchText.Trim();
        if (trimmedSearchText.Length > 0)
        {
            rows = rows.Where(row => row.Matches(trimmedSearchText));
        }

        Capsules.Clear();
        foreach (CapsuleRowViewModel row in rows)
        {
            Capsules.Add(row);
        }

        SelectedCapsule = Capsules.FirstOrDefault();
        OnPropertyChanged(nameof(VisibleCapsuleCount));
        OnPropertyChanged(nameof(TotalCapsuleCount));
        OnPropertyChanged(nameof(SearchSummary));
    }

    private static IReadOnlyList<string> BuildCategoryFilterOptions(IEnumerable<CapsuleRowViewModel> rows) =>
        new[] { "All" }
            .Concat(rows.Select(row => row.Category).Where(category => category.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(category => category, StringComparer.Ordinal))
            .ToArray();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record CapsuleCatalogItem(
    string Category,
    CapsuleDefinition Definition,
    CapsuleValidationState ValidationState);

public enum CapsuleValidationState
{
    Ready,
    Warning,
    Error
}

public sealed class CapsuleRowViewModel
{
    private readonly CapsuleDefinition definition;

    private CapsuleRowViewModel(CapsuleCatalogItem item)
    {
        definition = item.Definition;
        Id = definition.Id.Value;
        DisplayName = definition.DisplayName;
        Version = definition.Version;
        Category = item.Category;
        ValidationState = item.ValidationState;
        Dependencies = definition.ListDependencies().Select(CapsuleDependencyRow.FromDependency).ToArray();
        Parameters = definition.Parameters.Select(CapsuleParameterEditorViewModel.FromDefinition).ToArray();
        Documents = definition.Docs.Select(CapsuleDocumentRow.FromDocument).ToArray();
        ValidationRules = definition.ValidationRules.Select(CapsuleValidationRuleRow.FromRule).ToArray();
        InsertOrApplyCommand = new DelegateCommand(() => { }, () => false);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Version { get; }

    public string Category { get; }

    public CapsuleValidationState ValidationState { get; }

    public string ValidationStateText => ValidationState.ToString();

    public IReadOnlyList<CapsuleDependencyRow> Dependencies { get; }

    public IReadOnlyList<CapsuleParameterEditorViewModel> Parameters { get; }

    public IReadOnlyList<CapsuleDocumentRow> Documents { get; }

    public IReadOnlyList<CapsuleValidationRuleRow> ValidationRules { get; }

    public ICommand InsertOrApplyCommand { get; }

    public string DisabledInsertionReason => "Insert/apply is disabled until the capsule insertion story is implemented.";

    public string InsertOrApplyState => "Review only";

    public bool HasValidParameters => ParameterDiagnostics.Count == 0;

    public IReadOnlyList<CapsuleParameterDiagnostic> ParameterDiagnostics =>
        Parameters.SelectMany(parameter => parameter.Diagnostics).ToArray();

    public static CapsuleRowViewModel FromItem(CapsuleCatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Definition.Validate();

        return new CapsuleRowViewModel(item);
    }

    public IReadOnlyDictionary<string, CapsuleParameterValue> CreateParameterValues()
    {
        CapsuleParameterDiagnostic[] diagnostics = ParameterDiagnostics.ToArray();
        if (diagnostics.Length > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        Dictionary<string, CapsuleParameterValue> values = Parameters.ToDictionary(
            parameter => parameter.Name,
            parameter => parameter.CreateValue(),
            StringComparer.Ordinal);

        definition.ValidateParameters(values);
        return values;
    }

    public bool Matches(string searchText) =>
        Contains(Id, searchText) ||
        Contains(DisplayName, searchText) ||
        Contains(Version, searchText) ||
        Contains(Category, searchText);

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}

public sealed class CapsuleParameterEditorViewModel : INotifyPropertyChanged
{
    private string textValue = "";
    private double? numberValue;
    private bool? booleanValue;
    private string selectedEnumValue = "";

    private CapsuleParameterEditorViewModel(CapsuleParameterDefinition definition)
    {
        Definition = definition;
        Name = definition.Name;
        DisplayName = definition.DisplayName;
        Kind = definition.Kind;
        IsRequired = definition.Required;
        Minimum = definition.Min;
        Maximum = definition.Max;
        AllowedValues = definition.AllowedValues?.ToArray() ?? [];

        if (Kind is CapsuleParameterKind.Number)
        {
            numberValue = Minimum ?? 0;
        }

        if (Kind is CapsuleParameterKind.Boolean)
        {
            booleanValue = false;
        }

        if (Kind is CapsuleParameterKind.Enum)
        {
            selectedEnumValue = AllowedValues.FirstOrDefault() ?? "";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CapsuleParameterDefinition Definition { get; }

    public string Name { get; }

    public string DisplayName { get; }

    public CapsuleParameterKind Kind { get; }

    public bool IsRequired { get; }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public IReadOnlyList<string> AllowedValues { get; }

    public string TextValue
    {
        get => textValue;
        set
        {
            string nextValue = value ?? "";
            if (textValue == nextValue)
            {
                return;
            }

            textValue = nextValue;
            OnValueChanged();
        }
    }

    public double? NumberValue
    {
        get => numberValue;
        set
        {
            if (numberValue == value)
            {
                return;
            }

            numberValue = value;
            OnValueChanged();
        }
    }

    public bool? BooleanValue
    {
        get => booleanValue;
        set
        {
            if (booleanValue == value)
            {
                return;
            }

            booleanValue = value;
            OnValueChanged();
        }
    }

    public string SelectedEnumValue
    {
        get => selectedEnumValue;
        set
        {
            string nextValue = value ?? "";
            if (selectedEnumValue == nextValue)
            {
                return;
            }

            selectedEnumValue = nextValue;
            OnValueChanged();
        }
    }

    public IReadOnlyList<CapsuleParameterDiagnostic> Diagnostics => Validate().ToArray();

    public static CapsuleParameterEditorViewModel FromDefinition(CapsuleParameterDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();

        return new CapsuleParameterEditorViewModel(definition);
    }

    public CapsuleParameterValue CreateValue() =>
        Kind switch
        {
            CapsuleParameterKind.String => CapsuleParameterValue.String(TextValue),
            CapsuleParameterKind.Number => CapsuleParameterValue.Number(NumberValue.GetValueOrDefault()),
            CapsuleParameterKind.Boolean => CapsuleParameterValue.Boolean(BooleanValue.GetValueOrDefault()),
            CapsuleParameterKind.Enum => CapsuleParameterValue.Enum(SelectedEnumValue),
            _ => throw new InvalidOperationException($"Unsupported parameter kind '{Kind}'.")
        };

    private IEnumerable<CapsuleParameterDiagnostic> Validate()
    {
        if (Kind is CapsuleParameterKind.String)
        {
            if (IsRequired && string.IsNullOrWhiteSpace(TextValue))
            {
                yield return new CapsuleParameterDiagnostic(Name, $"{DisplayName} is required.");
            }

            yield break;
        }

        if (Kind is CapsuleParameterKind.Number)
        {
            if (NumberValue is null)
            {
                yield return new CapsuleParameterDiagnostic(Name, $"{DisplayName} is required.");
                yield break;
            }

            double value = NumberValue.Value;
            if ((Minimum is not null && value < Minimum) || (Maximum is not null && value > Maximum))
            {
                yield return new CapsuleParameterDiagnostic(Name, $"{DisplayName} must be between {Minimum} and {Maximum}.");
            }

            yield break;
        }

        if (Kind is CapsuleParameterKind.Boolean)
        {
            if (IsRequired && BooleanValue is null)
            {
                yield return new CapsuleParameterDiagnostic(Name, $"{DisplayName} is required.");
            }

            yield break;
        }

        if (Kind is CapsuleParameterKind.Enum && !AllowedValues.Contains(SelectedEnumValue, StringComparer.Ordinal))
        {
            yield return new CapsuleParameterDiagnostic(Name, $"{DisplayName} must be one of: {string.Join(", ", AllowedValues)}.");
        }
    }

    private void OnValueChanged()
    {
        OnPropertyChanged(nameof(Diagnostics));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record CapsuleParameterDiagnostic(string ParameterName, string Message);

public sealed record CapsuleDependencyRow(string Kind, string Id)
{
    public string DisplayText => $"{Kind}: {Id}";

    public static CapsuleDependencyRow FromDependency(CapsuleDependency dependency) =>
        new(dependency.Kind.ToString(), dependency.Id);
}

public sealed record CapsuleDocumentRow(string Id, string Kind, string Location)
{
    public static CapsuleDocumentRow FromDocument(CapsuleDocumentReference document) =>
        new(document.Id, document.Kind.ToString(), document.Location);
}

public sealed record CapsuleValidationRuleRow(string Id, string Severity, string Message)
{
    public static CapsuleValidationRuleRow FromRule(CapsuleValidationRule rule) =>
        new(rule.Id, rule.Severity.ToString(), rule.Message);
}
