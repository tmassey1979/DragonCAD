using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.App.Marketplace.Smoke;

public sealed class VendorLiveSmokeViewModel : INotifyPropertyChanged
{
    private const string DigiKeyProviderName = "Digi-Key";
    private const string MouserProviderName = "Mouser";
    private const int DefaultResultLimit = 3;

    private readonly IVendorLiveSmokeHarness harness;
    private readonly ObservableCollection<VendorLiveSmokeProviderRow> providers;
    private readonly ObservableCollection<VendorLiveSmokeDiagnosticRow> diagnostics = [];
    private string queryText = "NE555";
    private int resultLimit = DefaultResultLimit;
    private string lastRunSummary = "Live smoke has not run.";
    private string lastRunStatus = "Not run";
    private bool isRunning;

    public VendorLiveSmokeViewModel(IVendorLiveSmokeHarness harness)
    {
        this.harness = harness ?? throw new ArgumentNullException(nameof(harness));
        providers =
        [
            VendorLiveSmokeProviderRow.NotRun(DigiKeyProviderName, IsGateEnabled),
            VendorLiveSmokeProviderRow.NotRun(MouserProviderName, IsGateEnabled)
        ];

        RunDigiKeyCommand = new AsyncDelegateCommand(() => RunDigiKeyAsync(), CanRunSmoke);
        RunMouserCommand = new AsyncDelegateCommand(() => RunMouserAsync(), CanRunSmoke);
        RunAllCommand = new AsyncDelegateCommand(() => RunAllAsync(), CanRunSmoke);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GateEnvironmentVariable => VendorLiveSmokeHarness.GateEnvironmentVariable;

    public bool IsGateEnabled => harness.IsEnabled();

    public string GateStatus => IsGateEnabled ? "Live vendor smoke is enabled" : "Live vendor smoke is disabled";

    public string DisabledMessage => IsGateEnabled
        ? string.Empty
        : $"Set {VendorLiveSmokeHarness.GateEnvironmentVariable}=1 to enable real Digi-Key and Mouser calls.";

    public string RunAllActionLabel => IsRunning
        ? "Running live smoke..."
        : IsGateEnabled ? "Run all live smoke" : "Enable live smoke gate";

    public string CommandStatusLabel => IsGateEnabled
        ? $"Ready: {RunnableProviderCount:N0} of {ProviderCount:N0} providers runnable"
        : $"Disabled: {RunnableProviderCount:N0} of {ProviderCount:N0} providers runnable";

    public int ProviderCount => providers.Count;

    public int RunnableProviderCount => IsGateEnabled && !IsRunning
        ? providers.Count(row => row.CanRun)
        : 0;

    public int DisabledProviderCount => ProviderCount - RunnableProviderCount;

    public IReadOnlyList<VendorLiveSmokeProviderRow> Providers => providers;

    public IReadOnlyList<VendorLiveSmokeDiagnosticRow> Diagnostics => diagnostics;

    public ICommand RunDigiKeyCommand { get; }

    public ICommand RunMouserCommand { get; }

    public ICommand RunAllCommand { get; }

    public string QueryText
    {
        get => queryText;
        set
        {
            string nextValue = value ?? string.Empty;
            if (queryText == nextValue)
            {
                return;
            }

            queryText = nextValue;
            OnPropertyChanged();
        }
    }

    public int ResultLimit
    {
        get => resultLimit;
        set
        {
            int nextValue = Math.Max(1, value);
            if (resultLimit == nextValue)
            {
                return;
            }

            resultLimit = nextValue;
            OnPropertyChanged();
        }
    }

    public string LastRunSummary
    {
        get => lastRunSummary;
        private set
        {
            if (lastRunSummary == value)
            {
                return;
            }

            lastRunSummary = value;
            OnPropertyChanged();
        }
    }

    public string LastRunStatus
    {
        get => lastRunStatus;
        private set
        {
            if (lastRunStatus == value)
            {
                return;
            }

            lastRunStatus = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (isRunning == value)
            {
                return;
            }

            isRunning = value;
            RaiseCommandStateChanged();
            OnPropertyChanged();
            RaiseCommandDisplayChanged();
        }
    }

    public Task RunDigiKeyAsync(CancellationToken cancellationToken = default) =>
        RunProviderAsync(DigiKeyProviderName, harness.RunDigiKeyKeywordSearchAsync, cancellationToken);

    public Task RunMouserAsync(CancellationToken cancellationToken = default) =>
        RunProviderAsync(MouserProviderName, harness.RunMouserKeywordSearchAsync, cancellationToken);

    public void RefreshStatus()
    {
        bool isGateEnabled = IsGateEnabled;
        for (int index = 0; index < providers.Count; index++)
        {
            providers[index] = providers[index].WithGateStatus(isGateEnabled);
        }

        RaiseCommandStateChanged();
        OnPropertyChanged(nameof(IsGateEnabled));
        OnPropertyChanged(nameof(GateStatus));
        OnPropertyChanged(nameof(DisabledMessage));
        OnPropertyChanged(nameof(Providers));
        RaiseCommandDisplayChanged();
    }

    public async Task RunAllAsync(CancellationToken cancellationToken = default)
    {
        if (!TryStartRun())
        {
            return;
        }

        try
        {
            string keyword = QueryText.Trim();
            int limit = ResultLimit;
            VendorLiveSmokeRunResult[] results =
            [
                await harness.RunDigiKeyKeywordSearchAsync(keyword, limit, cancellationToken).ConfigureAwait(false),
                await harness.RunMouserKeywordSearchAsync(keyword, limit, cancellationToken).ConfigureAwait(false)
            ];

            ApplyResults(results);
            int listings = results.Sum(result => result.ListingCount);
            int diagnosticCount = results.Sum(result => result.Diagnostics.Count);
            LastRunStatus = results.Any(result => result.Status == VendorLiveSmokeRunStatus.Failed) ? "Failed" : "Succeeded";
            LastRunSummary = $"Live smoke completed: {results.Length:N0} providers, {listings:N0} {Pluralize(listings, "listing")}, {diagnosticCount:N0} {Pluralize(diagnosticCount, "diagnostic")}.";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task RunProviderAsync(
        string providerName,
        Func<string, int, CancellationToken, Task<VendorLiveSmokeRunResult>> runProviderAsync,
        CancellationToken cancellationToken)
    {
        if (!TryStartRun())
        {
            return;
        }

        try
        {
            VendorLiveSmokeRunResult result = await runProviderAsync(QueryText.Trim(), ResultLimit, cancellationToken).ConfigureAwait(false);
            ApplyResults([result]);
            LastRunStatus = result.Status.ToString();
            LastRunSummary = $"{providerName} live smoke {FormatStatusPhrase(result.Status)}: {result.ListingCount:N0} {Pluralize(result.ListingCount, "listing")}, {result.Diagnostics.Count:N0} {Pluralize(result.Diagnostics.Count, "diagnostic")}.";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool TryStartRun()
    {
        if (!IsGateEnabled)
        {
            LastRunStatus = "Disabled";
            LastRunSummary = DisabledMessage;
            return false;
        }

        if (string.IsNullOrWhiteSpace(QueryText))
        {
            LastRunStatus = "Blocked";
            LastRunSummary = "Enter a keyword before running live vendor smoke.";
            ReplaceDiagnostics(
            [
                new VendorLiveSmokeDiagnosticRow(
                    "Blocked",
                    "DragonCAD.LiveSmoke.QueryRequired",
                    "Enter a keyword before running live vendor smoke.",
                    string.Empty,
                    string.Empty)
            ]);
            return false;
        }

        IsRunning = true;
        return true;
    }

    private void ApplyResults(IReadOnlyList<VendorLiveSmokeRunResult> results)
    {
        foreach (VendorLiveSmokeRunResult result in results)
        {
            int index = FindProviderIndex(result.ProviderName);
            if (index >= 0)
            {
                providers[index] = VendorLiveSmokeProviderRow.FromResult(result, IsGateEnabled);
            }
        }

        ReplaceDiagnostics(results.SelectMany(result => result.Diagnostics).Select(VendorLiveSmokeDiagnosticRow.FromDiagnostic).ToArray());
        OnPropertyChanged(nameof(Providers));
        RaiseCommandDisplayChanged();
    }

    private void ReplaceDiagnostics(IEnumerable<VendorLiveSmokeDiagnosticRow> rows)
    {
        diagnostics.Clear();
        foreach (VendorLiveSmokeDiagnosticRow row in rows)
        {
            diagnostics.Add(row);
        }

        OnPropertyChanged(nameof(Diagnostics));
    }

    private int FindProviderIndex(string providerName)
    {
        for (int index = 0; index < providers.Count; index++)
        {
            if (string.Equals(providers[index].ProviderName, providerName, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private bool CanRunSmoke() => IsGateEnabled && !IsRunning;

    private void RaiseCommandStateChanged()
    {
        (RunDigiKeyCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
        (RunMouserCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
        (RunAllCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
    }

    private void RaiseCommandDisplayChanged()
    {
        OnPropertyChanged(nameof(RunAllActionLabel));
        OnPropertyChanged(nameof(CommandStatusLabel));
        OnPropertyChanged(nameof(ProviderCount));
        OnPropertyChanged(nameof(RunnableProviderCount));
        OnPropertyChanged(nameof(DisabledProviderCount));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatStatusPhrase(VendorLiveSmokeRunStatus status) =>
        status switch
        {
            VendorLiveSmokeRunStatus.Succeeded => "succeeded",
            VendorLiveSmokeRunStatus.Failed => "failed",
            VendorLiveSmokeRunStatus.Disabled => "is disabled",
            _ => status.ToString().ToLowerInvariant()
        };

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record VendorLiveSmokeProviderRow(
    string ProviderName,
    string Status,
    string ActionLabel,
    bool CanRun,
    string LastResultSummary)
{
    public static VendorLiveSmokeProviderRow NotRun(string providerName, bool isGateEnabled) =>
        new(
            providerName,
            isGateEnabled ? "Not run" : "Disabled",
            isGateEnabled ? $"Run {providerName} smoke" : "Enable live smoke gate",
            isGateEnabled,
            "Not run");

    public static VendorLiveSmokeProviderRow FromResult(VendorLiveSmokeRunResult result, bool isGateEnabled)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new VendorLiveSmokeProviderRow(
            result.ProviderName,
            result.Status.ToString(),
            isGateEnabled ? $"Run {result.ProviderName} smoke" : "Enable live smoke gate",
            isGateEnabled,
            $"{result.ListingCount:N0} {Pluralize(result.ListingCount, "listing")}, {result.Diagnostics.Count:N0} {Pluralize(result.Diagnostics.Count, "diagnostic")}");
    }

    public VendorLiveSmokeProviderRow WithGateStatus(bool isGateEnabled) =>
        this with
        {
            Status = isGateEnabled ? StatusWhenEnabled() : "Disabled",
            ActionLabel = isGateEnabled ? $"Run {ProviderName} smoke" : "Enable live smoke gate",
            CanRun = isGateEnabled
        };

    private string StatusWhenEnabled() => Status == "Disabled" ? "Not run" : Status;

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record VendorLiveSmokeDiagnosticRow(
    string Severity,
    string Code,
    string Message,
    string ProviderName,
    string VendorSku)
{
    public static VendorLiveSmokeDiagnosticRow FromDiagnostic(CatalogImportDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new VendorLiveSmokeDiagnosticRow(
            diagnostic.Severity.ToString(),
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.ProviderName,
            diagnostic.VendorSku ?? string.Empty);
    }
}
