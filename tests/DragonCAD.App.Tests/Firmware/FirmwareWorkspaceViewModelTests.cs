using DragonCAD.App.Firmware;
using DragonCAD.Core.Firmware;

namespace DragonCAD.App.Tests.Firmware;

public sealed class FirmwareWorkspaceViewModelTests
{
    [Fact]
    public void FromWorkspaceListsFirmwareFilesTargetCommandsAndPinBindings()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();
        temp.WriteFile("firmware/blink/main.c");
        temp.WriteFile("firmware/blink/timer.c");

        FirmwareWorkspaceViewModel viewModel = FirmwareWorkspaceViewModel.FromWorkspace(
            Workspace(),
            temp.Path,
            availableBuildTools: ["make"]);

        Assert.Equal(["firmware/blink/main.c", "firmware/blink/timer.c"], viewModel.SourceFiles.Select(file => file.Path));
        Assert.Equal(["C", "C"], viewModel.SourceFiles.Select(file => file.KindLabel));
        Assert.Equal("avr / ATmega328P", viewModel.TargetSummary);
        Assert.Equal("Arduino Uno Rev3", viewModel.BoardDisplayName);
        Assert.Equal("make firmware", viewModel.BuildCommandText);
        Assert.Equal("avrdude -p atmega328p", viewModel.FlashCommandText);
        Assert.Equal(["D1.A -> PB5", "R1.1 -> PB0"], viewModel.PinBindings.Select(binding => binding.DisplayText));
        Assert.Empty(viewModel.Diagnostics);
    }

    [Fact]
    public void SelectingFirmwareFileExposesEditorMetadataWithoutExternalIdeAction()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();
        temp.WriteFile("firmware/blink/main.c");
        FirmwareWorkspaceViewModel viewModel = FirmwareWorkspaceViewModel.FromWorkspace(
            Workspace() with
            {
                SourceFiles = [new FirmwareSourceFile("firmware/blink/main.c", FirmwareSourceKind.C)]
            },
            temp.Path,
            availableBuildTools: ["make"]);

        viewModel.SelectedSourceFile = Assert.Single(viewModel.SourceFiles);

        Assert.NotNull(viewModel.SelectedEditor);
        Assert.Equal("firmware/blink/main.c", viewModel.SelectedEditor.Path);
        Assert.Equal("C source", viewModel.SelectedEditor.LanguageLabel);
        Assert.Equal(Path.Combine(temp.Path, "firmware/blink/main.c"), viewModel.SelectedEditor.AbsolutePath);
        Assert.False(viewModel.SelectedEditor.OpensExternalIde);
    }

    [Fact]
    public void MissingSourceDiagnosticIsVisibleAndNonFatal()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();

        FirmwareWorkspaceViewModel viewModel = FirmwareWorkspaceViewModel.FromWorkspace(
            Workspace(),
            temp.Path,
            availableBuildTools: ["make"]);

        Assert.Equal(FirmwareWorkspaceSeverity.Attention, viewModel.OverallSeverity);
        Assert.Equal(["MissingSource", "MissingSource"], viewModel.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(viewModel.Diagnostics, diagnostic => Assert.Equal(FirmwareWorkspaceDiagnosticSeverity.Error, diagnostic.Severity));
        Assert.Equal(2, viewModel.SourceFiles.Count);
        Assert.False(viewModel.CanBuild);
    }

    [Fact]
    public void MissingBuildToolDiagnosticDisablesBuildActionWithoutRemovingWorkspaceMetadata()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();
        temp.WriteFile("firmware/blink/main.c");
        temp.WriteFile("firmware/blink/timer.c");

        FirmwareWorkspaceViewModel viewModel = FirmwareWorkspaceViewModel.FromWorkspace(
            Workspace(),
            temp.Path,
            availableBuildTools: []);

        FirmwareWorkspaceDiagnosticViewModel diagnostic = Assert.Single(
            viewModel.Diagnostics,
            diagnostic => diagnostic.Code == "MissingBuildTool");
        Assert.Equal(FirmwareWorkspaceDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("make", diagnostic.Subject);
        Assert.False(viewModel.CanBuild);
        Assert.Equal("Build disabled: make is unavailable.", viewModel.BuildActionText);
        Assert.Equal(2, viewModel.SourceFiles.Count);
        Assert.Equal(2, viewModel.PinBindings.Count);
    }

    private static FirmwareWorkspace Workspace() =>
        new(
            SourceFiles:
            [
                new FirmwareSourceFile("firmware/blink/timer.c", FirmwareSourceKind.C),
                new FirmwareSourceFile("firmware/blink/main.c", FirmwareSourceKind.C)
            ],
            Target: FirmwareTargets.Avr,
            Board: new FirmwareBoardProfile("arduino-uno", "Arduino Uno Rev3"),
            BuildCommand: new FirmwareCommand("make firmware"),
            FlashCommand: new FirmwareCommand("avrdude -p atmega328p"),
            PinBindings:
            [
                new FirmwarePinBindingReference("R1.1:PB0", "R1.1", "PB0"),
                new FirmwarePinBindingReference("D1.A:PB5", "D1.A", "PB5")
            ]);

    private sealed class TempFirmwareDirectory : IDisposable
    {
        private TempFirmwareDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFirmwareDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DragonCAD.App.Firmware.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempFirmwareDirectory(path);
        }

        public void WriteFile(string relativePath)
        {
            string path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? Path);
            File.WriteAllText(path, string.Empty);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
