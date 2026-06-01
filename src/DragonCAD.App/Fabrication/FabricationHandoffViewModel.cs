using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Fabrication;

public sealed class FabricationHandoffViewModel : INotifyPropertyChanged
{
    private static readonly string[] Providers =
    [
        "All",
        "OSH Park",
        "PCBCart"
    ];

    private readonly IReadOnlyList<FabricationHandoffOptionViewModel> allOptions;
    private string selectedProviderFilter = "All";
    private FabricationHandoffOptionViewModel? selectedOption;

    private FabricationHandoffViewModel(IReadOnlyList<FabricationHandoffOptionViewModel> options)
    {
        allOptions = options;
        Options = new ObservableCollection<FabricationHandoffOptionViewModel>(options);
        selectedOption = Options.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FabricationHandoffOptionViewModel> Options { get; }

    public IReadOnlyList<string> ProviderFilterOptions => Providers;

    public string SelectedProviderFilter
    {
        get => selectedProviderFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedProviderFilter == nextValue)
            {
                return;
            }

            selectedProviderFilter = nextValue;
            ApplyProviderFilter();
            OnPropertyChanged();
        }
    }

    public FabricationHandoffOptionViewModel? SelectedOption
    {
        get => selectedOption;
        set
        {
            if (selectedOption == value)
            {
                return;
            }

            selectedOption = value;
            OnPropertyChanged();
        }
    }

    public static FabricationHandoffViewModel CreateSample() =>
        new(
        [
            new FabricationHandoffOptionViewModel(
                "osh-park",
                "OSH Park",
                "Prototype board",
                "Upload prototype package",
                [
                    FabricationRequiredFileViewModel.Ready("Gerbers", "manufacturing/gerbers.zip"),
                    FabricationRequiredFileViewModel.Ready("Drill files", "manufacturing/drill.zip")
                ]),
            new FabricationHandoffOptionViewModel(
                "pcbcart",
                "PCBCart",
                "Production / assembly",
                "Open production quote",
                [
                    FabricationRequiredFileViewModel.Missing("Gerbers"),
                    FabricationRequiredFileViewModel.Ready("Drill files", "manufacturing/drill.zip"),
                    FabricationRequiredFileViewModel.Missing("BOM"),
                    FabricationRequiredFileViewModel.Ready("Pick and place", "manufacturing/pick-place.csv")
                ])
        ]);

    private void ApplyProviderFilter()
    {
        IEnumerable<FabricationHandoffOptionViewModel> options = allOptions;
        if (selectedProviderFilter != "All")
        {
            options = options.Where(option => string.Equals(option.ProviderName, selectedProviderFilter, StringComparison.Ordinal));
        }

        Options.Clear();
        foreach (FabricationHandoffOptionViewModel option in options)
        {
            Options.Add(option);
        }

        SelectedOption = Options.FirstOrDefault();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record FabricationHandoffOptionViewModel(
    string ProviderId,
    string ProviderName,
    string OrderKindLabel,
    string HandoffLabel,
    IReadOnlyList<FabricationRequiredFileViewModel> RequiredFiles)
{
    public bool IsReady => RequiredFiles.All(file => file.IsReady);

    public bool CanStartHandoff => IsReady;

    public string StatusText
    {
        get
        {
            int missingCount = RequiredFiles.Count(file => !file.IsReady);
            return missingCount switch
            {
                0 => "Ready for handoff",
                1 => "1 required file missing",
                _ => $"{missingCount} required files missing"
            };
        }
    }

    public IReadOnlyList<string> Diagnostics => RequiredFiles
        .Where(file => !file.IsReady)
        .Select(file => $"Missing {file.DisplayName} for {ProviderName}.")
        .ToArray();
}

public sealed record FabricationRequiredFileViewModel(
    string DisplayName,
    bool IsReady,
    string RelativePath)
{
    public string StatusLabel => IsReady ? "Ready" : "Missing";

    public static FabricationRequiredFileViewModel Ready(string displayName, string relativePath) =>
        new(displayName, true, relativePath);

    public static FabricationRequiredFileViewModel Missing(string displayName) =>
        new(displayName, false, "");
}
