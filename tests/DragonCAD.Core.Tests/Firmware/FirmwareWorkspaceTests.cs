using DragonCAD.Core.Firmware;

namespace DragonCAD.Core.Tests.Firmware;

public sealed class FirmwareWorkspaceTests
{
    [Fact]
    public void WorkspaceStoresFirmwareFilesTargetCommandsAndPinBindings()
    {
        FirmwareWorkspace workspace = BlinkWorkspace(FirmwareTargets.Avr);

        Assert.Equal(["firmware/blink/main.c", "firmware/blink/timer.c"], workspace.SourceFiles.Select(file => file.Path));
        Assert.Equal("avr", workspace.Target.Family);
        Assert.Equal("ATmega328P", workspace.Target.Chip);
        Assert.Equal("arduino-uno", workspace.Board.ProfileId);
        Assert.Equal("dotnet dragoncad firmware build --target avr", workspace.BuildCommand.CommandLine);
        Assert.Equal("dotnet dragoncad firmware flash --port COM3", workspace.FlashCommand.CommandLine);
        Assert.Equal(["D1.A:PB5", "R1.1:PB0"], workspace.PinBindings.Select(binding => binding.BindingId));
    }

    [Theory]
    [MemberData(nameof(TargetMetadata))]
    public void BuiltInTargetsExposeFirmwareMetadata(FirmwareTargetMetadata target, string family, string chip, string toolchain)
    {
        Assert.Equal(family, target.Family);
        Assert.Equal(chip, target.Chip);
        Assert.Equal(toolchain, target.Toolchain);
    }

    [Fact]
    public void ValidationReportsMissingSourceUnknownTargetAndStalePinBindingDeterministically()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();
        temp.WriteFile("firmware/blink/timer.c");
        FirmwareWorkspace workspace = BlinkWorkspace(new FirmwareTargetMetadata("unknown-mcu", "Part42", "custom")) with
        {
            SourceFiles =
            [
                new FirmwareSourceFile("firmware/blink/timer.c", FirmwareSourceKind.C),
                new FirmwareSourceFile("firmware/blink/main.c", FirmwareSourceKind.C)
            ],
            PinBindings =
            [
                new FirmwarePinBindingReference("R1.1:PB0", "R1.1", "PB0"),
                new FirmwarePinBindingReference("D1.A:PB5", "D1.A", "PB5")
            ]
        };
        FirmwareWorkspaceValidationContext context = new(
            temp.Path,
            KnownTargetFamilies: ["avr", "esp32", "rp2040"],
            CurrentPinBindingIds: ["R1.1:PB0"]);

        FirmwareWorkspaceValidationResult result = FirmwareWorkspaceValidator.Validate(workspace, context);

        Assert.False(result.IsValid);
        Assert.Equal(
            [
                FirmwareWorkspaceDiagnosticCode.MissingSource,
                FirmwareWorkspaceDiagnosticCode.StalePinBinding,
                FirmwareWorkspaceDiagnosticCode.UnknownTarget
            ],
            result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.Equal(
            [
                "firmware/blink/main.c",
                "D1.A:PB5",
                "unknown-mcu"
            ],
            result.Diagnostics.Select(diagnostic => diagnostic.Subject));
    }

    [Fact]
    public void ValidationDoesNotInvokeBuildOrFlashCommands()
    {
        using TempFirmwareDirectory temp = TempFirmwareDirectory.Create();
        temp.WriteFile("firmware/blink/main.c");
        temp.WriteFile("firmware/blink/timer.c");
        FirmwareWorkspace workspace = BlinkWorkspace(FirmwareTargets.Esp32);
        FirmwareWorkspaceValidationContext context = new(
            temp.Path,
            KnownTargetFamilies: ["avr", "esp32", "rp2040"],
            CurrentPinBindingIds: ["D1.A:PB5", "R1.1:PB0"]);

        FirmwareWorkspaceValidationResult result = FirmwareWorkspaceValidator.Validate(workspace, context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("dotnet dragoncad firmware build --target avr", workspace.BuildCommand.CommandLine);
        Assert.Equal("dotnet dragoncad firmware flash --port COM3", workspace.FlashCommand.CommandLine);
    }

    public static TheoryData<FirmwareTargetMetadata, string, string, string> TargetMetadata() =>
        new()
        {
            { FirmwareTargets.Avr, "avr", "ATmega328P", "avr-gcc" },
            { FirmwareTargets.Esp32, "esp32", "ESP32-WROOM-32", "xtensa-esp32-elf-gcc" },
            { FirmwareTargets.Rp2040, "rp2040", "RP2040", "arm-none-eabi-gcc" }
        };

    private static FirmwareWorkspace BlinkWorkspace(FirmwareTargetMetadata target) =>
        new(
            SourceFiles:
            [
                new FirmwareSourceFile("firmware/blink/main.c", FirmwareSourceKind.C),
                new FirmwareSourceFile("firmware/blink/timer.c", FirmwareSourceKind.C)
            ],
            Target: target,
            Board: new FirmwareBoardProfile("arduino-uno", "Arduino Uno Rev3"),
            BuildCommand: new FirmwareCommand("dotnet dragoncad firmware build --target avr"),
            FlashCommand: new FirmwareCommand("dotnet dragoncad firmware flash --port COM3"),
            PinBindings:
            [
                new FirmwarePinBindingReference("D1.A:PB5", "D1.A", "PB5"),
                new FirmwarePinBindingReference("R1.1:PB0", "R1.1", "PB0")
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
                "DragonCAD.Firmware.Tests",
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
