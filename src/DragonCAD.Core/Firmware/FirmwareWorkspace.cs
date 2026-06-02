namespace DragonCAD.Core.Firmware;

public sealed record FirmwareWorkspace
{
    public FirmwareWorkspace(
        IReadOnlyList<FirmwareSourceFile> SourceFiles,
        FirmwareTargetMetadata Target,
        FirmwareBoardProfile Board,
        FirmwareCommand BuildCommand,
        FirmwareCommand FlashCommand,
        IReadOnlyList<FirmwarePinBindingReference> PinBindings)
    {
        this.SourceFiles = Sort(SourceFiles, file => file.Path);
        this.Target = Target;
        this.Board = Board;
        this.BuildCommand = BuildCommand;
        this.FlashCommand = FlashCommand;
        this.PinBindings = Sort(PinBindings, binding => binding.BindingId);
    }

    public IReadOnlyList<FirmwareSourceFile> SourceFiles { get; init; }

    public FirmwareTargetMetadata Target { get; init; }

    public FirmwareBoardProfile Board { get; init; }

    public FirmwareCommand BuildCommand { get; init; }

    public FirmwareCommand FlashCommand { get; init; }

    public IReadOnlyList<FirmwarePinBindingReference> PinBindings { get; init; }

    private static IReadOnlyList<T> Sort<T>(IReadOnlyList<T> values, Func<T, string> keySelector) =>
        values.OrderBy(keySelector, StringComparer.Ordinal).ToArray();
}

public sealed record FirmwareSourceFile
{
    public FirmwareSourceFile(string path, FirmwareSourceKind kind)
    {
        Path = FirmwareValue.NormalizeRequired(path, nameof(path));
        Kind = kind;
    }

    public string Path { get; }

    public FirmwareSourceKind Kind { get; }
}

public enum FirmwareSourceKind
{
    C,
    Cpp,
    Header,
    Assembly,
    Rust,
    LinkerScript,
    Other
}

public sealed record FirmwareTargetMetadata
{
    public FirmwareTargetMetadata(string family, string chip, string toolchain)
    {
        Family = FirmwareValue.NormalizeRequired(family, nameof(family));
        Chip = FirmwareValue.NormalizeRequired(chip, nameof(chip));
        Toolchain = FirmwareValue.NormalizeRequired(toolchain, nameof(toolchain));
    }

    public string Family { get; }

    public string Chip { get; }

    public string Toolchain { get; }
}

public static class FirmwareTargets
{
    public static FirmwareTargetMetadata Avr { get; } = new("avr", "ATmega328P", "avr-gcc");

    public static FirmwareTargetMetadata Esp32 { get; } = new("esp32", "ESP32-WROOM-32", "xtensa-esp32-elf-gcc");

    public static FirmwareTargetMetadata Rp2040 { get; } = new("rp2040", "RP2040", "arm-none-eabi-gcc");

    public static IReadOnlyList<FirmwareTargetMetadata> All { get; } = [Avr, Esp32, Rp2040];
}

public sealed record FirmwareBoardProfile
{
    public FirmwareBoardProfile(string profileId, string displayName)
    {
        ProfileId = FirmwareValue.NormalizeRequired(profileId, nameof(profileId));
        DisplayName = FirmwareValue.NormalizeRequired(displayName, nameof(displayName));
    }

    public string ProfileId { get; }

    public string DisplayName { get; }
}

public sealed record FirmwareCommand
{
    public FirmwareCommand(string commandLine)
    {
        CommandLine = FirmwareValue.NormalizeRequired(commandLine, nameof(commandLine));
    }

    public string CommandLine { get; }
}

public sealed record FirmwarePinBindingReference
{
    public FirmwarePinBindingReference(string bindingId, string schematicPin, string targetPin)
    {
        BindingId = FirmwareValue.NormalizeRequired(bindingId, nameof(bindingId));
        SchematicPin = FirmwareValue.NormalizeRequired(schematicPin, nameof(schematicPin));
        TargetPin = FirmwareValue.NormalizeRequired(targetPin, nameof(targetPin));
    }

    public string BindingId { get; }

    public string SchematicPin { get; }

    public string TargetPin { get; }
}

public sealed record FirmwareWorkspaceValidationContext
{
    public FirmwareWorkspaceValidationContext(
        string ProjectRoot,
        IReadOnlyList<string> KnownTargetFamilies,
        IReadOnlyList<string> CurrentPinBindingIds)
    {
        this.ProjectRoot = FirmwareValue.NormalizeRequired(ProjectRoot, nameof(ProjectRoot));
        this.KnownTargetFamilies = KnownTargetFamilies.ToHashSet(StringComparer.Ordinal);
        this.CurrentPinBindingIds = CurrentPinBindingIds.ToHashSet(StringComparer.Ordinal);
    }

    public string ProjectRoot { get; }

    public IReadOnlySet<string> KnownTargetFamilies { get; }

    public IReadOnlySet<string> CurrentPinBindingIds { get; }
}

public sealed record FirmwareWorkspaceValidationResult(IReadOnlyList<FirmwareWorkspaceDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.Count == 0;
}

public sealed record FirmwareWorkspaceDiagnostic(
    FirmwareWorkspaceDiagnosticSeverity Severity,
    FirmwareWorkspaceDiagnosticCode Code,
    string Subject,
    string Message);

public enum FirmwareWorkspaceDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public enum FirmwareWorkspaceDiagnosticCode
{
    MissingSource,
    StalePinBinding,
    UnknownTarget
}

public static class FirmwareWorkspaceValidator
{
    public static FirmwareWorkspaceValidationResult Validate(
        FirmwareWorkspace workspace,
        FirmwareWorkspaceValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(context);

        List<FirmwareWorkspaceDiagnostic> diagnostics = [];
        AddMissingSourceDiagnostics(workspace, context, diagnostics);
        AddUnknownTargetDiagnostic(workspace, context, diagnostics);
        AddStalePinBindingDiagnostics(workspace, context, diagnostics);

        return new FirmwareWorkspaceValidationResult(
            diagnostics
                .OrderBy(diagnostic => diagnostic.Code.ToString(), StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Subject, StringComparer.Ordinal)
                .ToArray());
    }

    private static void AddMissingSourceDiagnostics(
        FirmwareWorkspace workspace,
        FirmwareWorkspaceValidationContext context,
        List<FirmwareWorkspaceDiagnostic> diagnostics)
    {
        foreach (FirmwareSourceFile sourceFile in workspace.SourceFiles)
        {
            string path = Path.Combine(context.ProjectRoot, sourceFile.Path);
            if (!File.Exists(path))
            {
                diagnostics.Add(new FirmwareWorkspaceDiagnostic(
                    FirmwareWorkspaceDiagnosticSeverity.Error,
                    FirmwareWorkspaceDiagnosticCode.MissingSource,
                    sourceFile.Path,
                    $"Firmware source file '{sourceFile.Path}' is missing."));
            }
        }
    }

    private static void AddUnknownTargetDiagnostic(
        FirmwareWorkspace workspace,
        FirmwareWorkspaceValidationContext context,
        List<FirmwareWorkspaceDiagnostic> diagnostics)
    {
        if (!context.KnownTargetFamilies.Contains(workspace.Target.Family))
        {
            diagnostics.Add(new FirmwareWorkspaceDiagnostic(
                FirmwareWorkspaceDiagnosticSeverity.Error,
                FirmwareWorkspaceDiagnosticCode.UnknownTarget,
                workspace.Target.Family,
                $"Firmware target family '{workspace.Target.Family}' is unknown."));
        }
    }

    private static void AddStalePinBindingDiagnostics(
        FirmwareWorkspace workspace,
        FirmwareWorkspaceValidationContext context,
        List<FirmwareWorkspaceDiagnostic> diagnostics)
    {
        foreach (FirmwarePinBindingReference binding in workspace.PinBindings)
        {
            if (!context.CurrentPinBindingIds.Contains(binding.BindingId))
            {
                diagnostics.Add(new FirmwareWorkspaceDiagnostic(
                    FirmwareWorkspaceDiagnosticSeverity.Warning,
                    FirmwareWorkspaceDiagnosticCode.StalePinBinding,
                    binding.BindingId,
                    $"Firmware pin binding '{binding.BindingId}' no longer exists in the project graph."));
            }
        }
    }
}

internal static class FirmwareValue
{
    public static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);

        string normalized = value.Trim().Replace('\\', '/');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Firmware values cannot be empty.", parameterName);
        }

        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException("Firmware values cannot contain control characters.", parameterName);
        }

        return normalized;
    }
}
