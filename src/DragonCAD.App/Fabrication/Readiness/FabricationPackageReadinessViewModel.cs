using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.App.Fabrication.Handoff;

namespace DragonCAD.App.Fabrication.Readiness;

public sealed class FabricationPackageReadinessViewModel : INotifyPropertyChanged
{
    private readonly string providerId;
    private readonly string providerName;
    private readonly string packageName;
    private readonly FabricationHandoffActionKind actionKind;
    private readonly string target;
    private FabricationHandoffActionPlan actionPlan;

    private FabricationPackageReadinessViewModel(FabricationHandoffPackageOption option)
    {
        providerId = option.ProviderId;
        providerName = option.ProviderName;
        packageName = option.PackageName;
        actionKind = option.ActionKind;
        target = option.Target;
        Files = new ObservableCollection<FabricationFileReadinessRow>(
            option.Files.Select(FabricationFileReadinessRow.FromPackageFile));
        actionPlan = FabricationHandoffActionPlanner.Plan(option);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FabricationFileReadinessRow> Files { get; }

    public FabricationHandoffActionPlan ActionPlan
    {
        get => actionPlan;
        private set
        {
            if (actionPlan == value)
            {
                return;
            }

            actionPlan = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(Diagnostics));
        }
    }

    public bool IsReady => ActionPlan.IsReady;

    public string StatusText
    {
        get
        {
            int missingCount = Files.Count(file => !file.IsReady);
            return missingCount switch
            {
                0 => "Ready for handoff",
                1 => "1 required file missing",
                _ => $"{missingCount} required files missing"
            };
        }
    }

    public IReadOnlyList<string> Diagnostics => ActionPlan.Diagnostics;

    public static FabricationPackageReadinessViewModel FromPackageOption(FabricationHandoffPackageOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        return new FabricationPackageReadinessViewModel(option);
    }

    public void MarkFileReady(string displayName, string relativePath)
    {
        FabricationFileReadinessRow file = FindFile(displayName);
        file.MarkReady(relativePath);
        RegenerateActionPlan();
    }

    public void MarkFileMissing(string displayName)
    {
        FabricationFileReadinessRow file = FindFile(displayName);
        file.MarkMissing();
        RegenerateActionPlan();
    }

    private FabricationFileReadinessRow FindFile(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Value must not be empty.", nameof(displayName));
        }

        return Files.Single(file => string.Equals(file.DisplayName, displayName.Trim(), StringComparison.Ordinal));
    }

    private void RegenerateActionPlan()
    {
        FabricationHandoffPackageFile[] files = Files
            .Select(file => file.IsReady
                ? FabricationHandoffPackageFile.Present(file.DisplayName, file.RelativePath)
                : FabricationHandoffPackageFile.Missing(file.DisplayName))
            .ToArray();

        FabricationHandoffPackageOption option = actionKind switch
        {
            FabricationHandoffActionKind.OpenUploadPage => FabricationHandoffPackageOption.CreateUploadPage(
                providerId,
                providerName,
                packageName,
                target,
                files),
            FabricationHandoffActionKind.OpenQuotePage => FabricationHandoffPackageOption.CreateQuotePage(
                providerId,
                providerName,
                packageName,
                target,
                files),
            FabricationHandoffActionKind.ExportPackage => FabricationHandoffPackageOption.CreateExportPackage(
                providerId,
                providerName,
                packageName,
                target,
                files),
            _ => throw new InvalidOperationException($"Unknown fabrication handoff action kind {actionKind}.")
        };

        ActionPlan = FabricationHandoffActionPlanner.Plan(option);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class FabricationFileReadinessRow : INotifyPropertyChanged
{
    private bool isReady;
    private string relativePath;

    private FabricationFileReadinessRow(string displayName, bool isReady, string relativePath)
    {
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        this.isReady = isReady;
        this.relativePath = isReady ? NormalizeRequired(relativePath, nameof(relativePath)) : string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName { get; }

    public bool IsReady
    {
        get => isReady;
        private set
        {
            if (isReady == value)
            {
                return;
            }

            isReady = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabel));
        }
    }

    public string RelativePath
    {
        get => relativePath;
        private set
        {
            string nextValue = value.Replace('\\', '/');
            if (relativePath == nextValue)
            {
                return;
            }

            relativePath = nextValue;
            OnPropertyChanged();
        }
    }

    public string StatusLabel => IsReady ? "Ready" : "Missing";

    public static FabricationFileReadinessRow FromPackageFile(FabricationHandoffPackageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new FabricationFileReadinessRow(file.DisplayName, file.IsPresent, file.RelativePath);
    }

    internal void MarkReady(string relativePath)
    {
        RelativePath = NormalizeRequired(relativePath, nameof(relativePath));
        IsReady = true;
    }

    internal void MarkMissing()
    {
        RelativePath = string.Empty;
        IsReady = false;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", parameterName);
        }

        return value.Trim().Replace('\\', '/');
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
