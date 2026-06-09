using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Firmware;

namespace DragonCAD.App.Firmware;

public sealed class FirmwareWorkspaceViewModel : INotifyPropertyChanged
{
    private FirmwareSourceFileViewModel? selectedSourceFile;

    private FirmwareWorkspaceViewModel(
        IReadOnlyList<FirmwareSourceFileViewModel> sourceFiles,
        FirmwareTargetMetadata target,
        FirmwareBoardProfile board,
        FirmwareCommand buildCommand,
        FirmwareCommand flashCommand,
        IReadOnlyList<FirmwarePinBindingViewModel> pinBindings,
        IReadOnlyList<FirmwareWorkspaceDiagnosticViewModel> diagnostics,
        string projectRoot)
    {
        SourceFiles = sourceFiles;
        TargetSummary = $"{target.Family} / {target.Chip}";
        TargetToolchain = target.Toolchain;
        BoardDisplayName = board.DisplayName;
        BuildCommandText = buildCommand.CommandLine;
        FlashCommandText = flashCommand.CommandLine;
        PinBindings = pinBindings;
        Diagnostics = diagnostics;
        ProjectRoot = projectRoot;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<FirmwareSourceFileViewModel> SourceFiles { get; }

    public string TargetSummary { get; }

    public string TargetToolchain { get; }

    public string BoardDisplayName { get; }

    public string BuildCommandText { get; }

    public string FlashCommandText { get; }

    public IReadOnlyList<FirmwarePinBindingViewModel> PinBindings { get; }

    public IReadOnlyList<FirmwareWorkspaceDiagnosticViewModel> Diagnostics { get; }

    public string ProjectRoot { get; }

    public FirmwareSourceFileViewModel? SelectedSourceFile
    {
        get => selectedSourceFile;
        set
        {
            if (selectedSourceFile == value)
            {
                return;
            }

            selectedSourceFile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedEditor));
        }
    }

    public FirmwareEditorMetadataViewModel? SelectedEditor =>
        selectedSourceFile is null
            ? null
            : new FirmwareEditorMetadataViewModel(
                selectedSourceFile.Path,
                Path.Combine(ProjectRoot, selectedSourceFile.Path),
                selectedSourceFile.LanguageLabel,
                OpensExternalIde: false);

    public bool CanBuild =>
        Diagnostics.All(diagnostic => diagnostic.Code != "MissingSource" && diagnostic.Code != "MissingBuildTool");

    public string BuildActionText
    {
        get
        {
            FirmwareWorkspaceDiagnosticViewModel? missingBuildTool = Diagnostics
                .FirstOrDefault(diagnostic => diagnostic.Code == "MissingBuildTool");
            if (missingBuildTool is not null)
            {
                return $"Build disabled: {missingBuildTool.Subject} is unavailable.";
            }

            if (Diagnostics.Any(diagnostic => diagnostic.Code == "MissingSource"))
            {
                return "Build disabled: firmware source files are missing.";
            }

            return "Build firmware";
        }
    }

    public FirmwareWorkspaceSeverity OverallSeverity =>
        Diagnostics.Count == 0 ? FirmwareWorkspaceSeverity.Ready : FirmwareWorkspaceSeverity.Attention;

    public static FirmwareWorkspaceViewModel FromWorkspace(
        FirmwareWorkspace workspace,
        string projectRoot,
        IReadOnlyList<string> availableBuildTools)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(availableBuildTools);

        FirmwareWorkspaceValidationResult validation = FirmwareWorkspaceValidator.Validate(
            workspace,
            new FirmwareWorkspaceValidationContext(
                projectRoot,
                FirmwareTargets.All.Select(target => target.Family).ToArray(),
                workspace.PinBindings.Select(binding => binding.BindingId).ToArray()));

        List<FirmwareWorkspaceDiagnosticViewModel> diagnostics = validation.Diagnostics
            .Select(FirmwareWorkspaceDiagnosticViewModel.FromDiagnostic)
            .ToList();

        AddMissingBuildToolDiagnostic(workspace.BuildCommand, availableBuildTools, diagnostics);

        return new FirmwareWorkspaceViewModel(
            workspace.SourceFiles.Select(FirmwareSourceFileViewModel.FromSourceFile).ToArray(),
            workspace.Target,
            workspace.Board,
            workspace.BuildCommand,
            workspace.FlashCommand,
            workspace.PinBindings.Select(FirmwarePinBindingViewModel.FromBinding).ToArray(),
            diagnostics,
            projectRoot);
    }

    private static void AddMissingBuildToolDiagnostic(
        FirmwareCommand buildCommand,
        IReadOnlyList<string> availableBuildTools,
        List<FirmwareWorkspaceDiagnosticViewModel> diagnostics)
    {
        string toolName = FirstCommandToken(buildCommand.CommandLine);
        bool toolAvailable = availableBuildTools.Any(tool => string.Equals(tool, toolName, StringComparison.OrdinalIgnoreCase));
        if (!toolAvailable)
        {
            diagnostics.Add(new FirmwareWorkspaceDiagnosticViewModel(
                FirmwareWorkspaceDiagnosticSeverity.Warning,
                "MissingBuildTool",
                toolName,
                $"Build tool '{toolName}' is unavailable."));
        }
    }

    private static string FirstCommandToken(string commandLine)
    {
        int separatorIndex = commandLine.IndexOf(' ', StringComparison.Ordinal);
        return separatorIndex < 0 ? commandLine : commandLine[..separatorIndex];
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public enum FirmwareWorkspaceSeverity
{
    Ready,
    Attention
}

public sealed record FirmwareSourceFileViewModel(
    string Path,
    FirmwareSourceKind Kind,
    string KindLabel,
    string LanguageLabel)
{
    public static FirmwareSourceFileViewModel FromSourceFile(FirmwareSourceFile sourceFile) =>
        new(sourceFile.Path, sourceFile.Kind, KindLabelFor(sourceFile.Kind), LanguageLabelFor(sourceFile.Kind));

    private static string KindLabelFor(FirmwareSourceKind kind) =>
        kind switch
        {
            FirmwareSourceKind.C => "C",
            FirmwareSourceKind.Cpp => "C++",
            FirmwareSourceKind.Header => "Header",
            FirmwareSourceKind.Assembly => "Assembly",
            FirmwareSourceKind.Rust => "Rust",
            FirmwareSourceKind.LinkerScript => "Linker script",
            _ => "Other"
        };

    private static string LanguageLabelFor(FirmwareSourceKind kind) =>
        kind switch
        {
            FirmwareSourceKind.C => "C source",
            FirmwareSourceKind.Cpp => "C++ source",
            FirmwareSourceKind.Header => "Header",
            FirmwareSourceKind.Assembly => "Assembly source",
            FirmwareSourceKind.Rust => "Rust source",
            FirmwareSourceKind.LinkerScript => "Linker script",
            _ => "Firmware source"
        };
}

public sealed record FirmwarePinBindingViewModel(
    string BindingId,
    string SchematicPin,
    string TargetPin,
    string DisplayText)
{
    public static FirmwarePinBindingViewModel FromBinding(FirmwarePinBindingReference binding) =>
        new(binding.BindingId, binding.SchematicPin, binding.TargetPin, $"{binding.SchematicPin} -> {binding.TargetPin}");
}

public sealed record FirmwareEditorMetadataViewModel(
    string Path,
    string AbsolutePath,
    string LanguageLabel,
    bool OpensExternalIde);

public sealed record FirmwareWorkspaceDiagnosticViewModel(
    FirmwareWorkspaceDiagnosticSeverity Severity,
    string Code,
    string Subject,
    string Message)
{
    public static FirmwareWorkspaceDiagnosticViewModel FromDiagnostic(FirmwareWorkspaceDiagnostic diagnostic) =>
        new(diagnostic.Severity, diagnostic.Code.ToString(), diagnostic.Subject, diagnostic.Message);
}
