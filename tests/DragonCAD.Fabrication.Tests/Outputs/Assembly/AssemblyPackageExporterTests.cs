using DragonCAD.Fabrication.Outputs.Assembly;

namespace DragonCAD.Fabrication.Tests.Outputs.Assembly;

public sealed class AssemblyPackageExporterTests
{
    [Fact]
    public void Export_GroupsBomLinesByManufacturingIdentity()
    {
        AssemblyComponent[] components =
        [
            AssemblyComponent.Placed(
                reference: "R2",
                value: "10k",
                manufacturerPartNumber: "RC0603FR-0710KL",
                package: "0603",
                footprint: "Resistor_SMD:R_0603",
                sourcingStatus: "Approved",
                x: 12_700_000,
                y: 0,
                rotation: 90,
                side: AssemblyPlacementSide.Top,
                placementStatus: "Placed"),
            AssemblyComponent.Placed(
                reference: "R1",
                value: "10k",
                manufacturerPartNumber: "RC0603FR-0710KL",
                package: "0603",
                footprint: "Resistor_SMD:R_0603",
                sourcingStatus: "Approved",
                x: 0,
                y: 0,
                rotation: 0,
                side: AssemblyPlacementSide.Top,
                placementStatus: "Placed"),
            AssemblyComponent.Placed(
                reference: "C1",
                value: "100nF",
                manufacturerPartNumber: "CL10B104KB8NNNC",
                package: "0603",
                footprint: "Capacitor_SMD:C_0603",
                sourcingStatus: "Review",
                x: 2_540_000,
                y: 2_540_000,
                rotation: 180,
                side: AssemblyPlacementSide.Bottom,
                placementStatus: "Placed")
        ];

        AssemblyExportPackage package = AssemblyPackageExporter.Export(components);

        Assert.Equal(
            "References,Quantity,Value,MPN,Package,Footprint,SourcingStatus\r\n" +
            "C1,1,100nF,CL10B104KB8NNNC,0603,Capacitor_SMD:C_0603,Review\r\n" +
            "R1 R2,2,10k,RC0603FR-0710KL,0603,Resistor_SMD:R_0603,Approved\r\n",
            package.BomCsv);
        Assert.Empty(package.Diagnostics);
    }

    [Fact]
    public void Export_WritesSinglePlacementLine()
    {
        AssemblyComponent[] components =
        [
            AssemblyComponent.Placed(
                reference: "U1",
                value: "MCU",
                manufacturerPartNumber: "STM32F042K6T6",
                package: "LQFP-32",
                footprint: "Package_QFP:LQFP-32",
                sourcingStatus: "Approved",
                x: 1_270_000,
                y: -2_540_000,
                rotation: 270,
                side: AssemblyPlacementSide.Bottom,
                placementStatus: "Placed")
        ];

        AssemblyExportPackage package = AssemblyPackageExporter.Export(components);

        Assert.Equal(
            "Reference,X,Y,Rotation,Side,Package,PlacementStatus\r\n" +
            "U1,1270000,-2540000,270,Bottom,LQFP-32,Placed\r\n",
            package.PickAndPlaceCsv);
        Assert.Empty(package.Diagnostics);
    }

    [Fact]
    public void Export_ReportsMissingManufacturerPartNumberDiagnostic()
    {
        AssemblyComponent[] components =
        [
            AssemblyComponent.Placed(
                reference: "J1",
                value: "USB-C",
                manufacturerPartNumber: "",
                package: "USB-C-16P",
                footprint: "Connector_USB:USB_C_Receptacle",
                sourcingStatus: "Unresolved",
                x: 0,
                y: 0,
                rotation: 0,
                side: AssemblyPlacementSide.Top,
                placementStatus: "Placed")
        ];

        AssemblyExportPackage package = AssemblyPackageExporter.Export(components);

        AssemblyExportDiagnostic diagnostic = Assert.Single(package.Diagnostics);
        Assert.Equal(AssemblyExportDiagnosticCode.MissingManufacturerPartNumber, diagnostic.Code);
        Assert.Equal("J1", diagnostic.Reference);
        Assert.Equal("J1 is missing manufacturer part number.", diagnostic.Message);
    }

    [Fact]
    public void Export_OrdersCsvRowsDeterministically()
    {
        AssemblyComponent[] components =
        [
            AssemblyComponent.Placed("U2", "MCU", "STM32F042K6T6", "LQFP-32", "Package_QFP:LQFP-32", "Approved", 2, 0, 90, AssemblyPlacementSide.Top, "Placed"),
            AssemblyComponent.Placed("C1", "100nF", "CL10B104KB8NNNC", "0603", "Capacitor_SMD:C_0603", "Approved", 1, 0, 0, AssemblyPlacementSide.Top, "Placed"),
            AssemblyComponent.Placed("R1", "10k", "RC0603FR-0710KL", "0603", "Resistor_SMD:R_0603", "Approved", 3, 0, 180, AssemblyPlacementSide.Bottom, "Placed")
        ];

        AssemblyExportPackage package = AssemblyPackageExporter.Export(components);

        Assert.Equal(
            "References,Quantity,Value,MPN,Package,Footprint,SourcingStatus\r\n" +
            "C1,1,100nF,CL10B104KB8NNNC,0603,Capacitor_SMD:C_0603,Approved\r\n" +
            "R1,1,10k,RC0603FR-0710KL,0603,Resistor_SMD:R_0603,Approved\r\n" +
            "U2,1,MCU,STM32F042K6T6,LQFP-32,Package_QFP:LQFP-32,Approved\r\n",
            package.BomCsv);
        Assert.Equal(
            "Reference,X,Y,Rotation,Side,Package,PlacementStatus\r\n" +
            "C1,1,0,0,Top,0603,Placed\r\n" +
            "R1,3,0,180,Bottom,0603,Placed\r\n" +
            "U2,2,0,90,Top,LQFP-32,Placed\r\n",
            package.PickAndPlaceCsv);
    }
}
