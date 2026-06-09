using DragonCAD.App.Capsules;
using DragonCAD.Core.Capsules;

namespace DragonCAD.App.Tests.Capsules;

public sealed class CapsuleManagerWorkspaceViewModelTests
{
    [Fact]
    public void ListsCapsulesVersionsCategoriesDependenciesParametersDocsAndValidationState()
    {
        CapsuleManagerWorkspaceViewModel viewModel = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready),
            new CapsuleCatalogItem("Connectivity", UsbCapsule(), CapsuleValidationState.Warning)
        ]);

        Assert.Equal(2, viewModel.VisibleCapsuleCount);
        Assert.Equal(["All", "Connectivity", "Power"], viewModel.CategoryFilterOptions);

        CapsuleRowViewModel power = viewModel.Capsules.Single(row => row.Id == "capsule:power/regulator");
        Assert.Equal("Point-of-load regulator", power.DisplayName);
        Assert.Equal("1.2.0", power.Version);
        Assert.Equal("Power", power.Category);
        Assert.Equal(CapsuleValidationState.Ready, power.ValidationState);
        Assert.Equal("Ready", power.ValidationStateText);
        Assert.Contains(power.Dependencies, dependency => dependency.Kind == "Component" && dependency.Id == "component:regulator");
        Assert.Contains(power.Parameters, parameter => parameter.Name == "outputVoltage" && parameter.Kind == CapsuleParameterKind.Number);
        Assert.Contains(power.Documents, doc => doc.Kind == "Datasheet" && doc.Location == "https://example.invalid/regulator.pdf");
    }

    [Fact]
    public void FiltersCapsulesByCategoryAndSearchText()
    {
        CapsuleManagerWorkspaceViewModel viewModel = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready),
            new CapsuleCatalogItem("Connectivity", UsbCapsule(), CapsuleValidationState.Warning)
        ]);

        viewModel.SelectedCategoryFilter = "Power";
        viewModel.SearchText = "regulator";

        CapsuleRowViewModel row = Assert.Single(viewModel.Capsules);
        Assert.Equal("capsule:power/regulator", row.Id);
        Assert.Equal("Showing 1 of 2 capsules", viewModel.SearchSummary);
    }

    [Fact]
    public void ParameterEditorSupportsStringNumberBooleanAndEnumValues()
    {
        CapsuleRowViewModel row = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready)
        ]).SelectedCapsule!;

        row.Parameters.Single(parameter => parameter.Name == "controller").TextValue = "boost";
        row.Parameters.Single(parameter => parameter.Name == "outputVoltage").NumberValue = 12;
        row.Parameters.Single(parameter => parameter.Name == "enableSoftStart").BooleanValue = false;
        row.Parameters.Single(parameter => parameter.Name == "mode").SelectedEnumValue = "pfm";

        IReadOnlyDictionary<string, CapsuleParameterValue> values = row.CreateParameterValues();

        Assert.Equal("boost", values["controller"].TextValue);
        Assert.Equal(12, values["outputVoltage"].NumberValue);
        Assert.False(values["enableSoftStart"].BooleanValue);
        Assert.Equal("pfm", values["mode"].TextValue);
        Assert.Empty(row.ParameterDiagnostics);
    }

    [Fact]
    public void InvalidParameterValueExposesDiagnosticWithoutCreatingInsertionContent()
    {
        CapsuleRowViewModel row = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready)
        ]).SelectedCapsule!;

        row.Parameters.Single(parameter => parameter.Name == "controller").TextValue = "buck";
        row.Parameters.Single(parameter => parameter.Name == "enableSoftStart").BooleanValue = true;
        row.Parameters.Single(parameter => parameter.Name == "mode").SelectedEnumValue = "pwm";
        row.Parameters.Single(parameter => parameter.Name == "outputVoltage").NumberValue = 99;

        Assert.False(row.HasValidParameters);
        CapsuleParameterDiagnostic diagnostic = Assert.Single(row.ParameterDiagnostics);
        Assert.Equal("outputVoltage", diagnostic.ParameterName);
        Assert.Contains("between 1.8 and 24", diagnostic.Message, StringComparison.Ordinal);
        Assert.False(row.InsertOrApplyCommand.CanExecute(null));
    }

    [Fact]
    public void DependencyDisplayIncludesEveryReviewDependencyKind()
    {
        CapsuleRowViewModel row = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready)
        ]).SelectedCapsule!;

        Assert.Equal(
            [
                "Component: component:regulator",
                "SchematicBlock: schematic:power-path",
                "BoardRegion: board:thermal",
                "FirmwareTemplate: firmware:init",
                "Constraint: constraints:voltage",
                "Documentation: docs:datasheet"
            ],
            row.Dependencies.Select(dependency => dependency.DisplayText));
    }

    [Fact]
    public void InsertApplyCommandIsReviewOnlyAndReportsDisabledReason()
    {
        CapsuleRowViewModel row = CapsuleManagerWorkspaceViewModel.FromItems(
        [
            new CapsuleCatalogItem("Power", PowerCapsule(), CapsuleValidationState.Ready)
        ]).SelectedCapsule!;

        Assert.False(row.InsertOrApplyCommand.CanExecute(null));
        Assert.Equal("Insert/apply is disabled until the capsule insertion story is implemented.", row.DisabledInsertionReason);
        Assert.Equal("Review only", row.InsertOrApplyState);
    }

    private static CapsuleDefinition PowerCapsule() =>
        new(
            new CapsuleId("capsule:power/regulator"),
            "Point-of-load regulator",
            "1.2.0",
            Parameters:
            [
                CapsuleParameterDefinition.Number("outputVoltage", "Output voltage", required: true, min: 1.8, max: 24),
                CapsuleParameterDefinition.String("controller", "Controller family", required: true),
                CapsuleParameterDefinition.Boolean("enableSoftStart", "Enable soft-start", required: true),
                CapsuleParameterDefinition.Enum("mode", "Switching mode", ["pfm", "pwm"], required: true)
            ],
            ComponentRefs: [new CapsuleComponentReference("component:regulator", "U1")],
            SchematicBlockRefs: [new CapsuleSchematicBlockReference("schematic:power-path", "Power path")],
            BoardRegionRefs: [new CapsuleBoardRegionReference("board:thermal", "Thermal copper")],
            FirmwareTemplates: [new CapsuleFirmwareTemplateReference("firmware:init", "Initialize regulator")],
            Constraints: [new CapsuleConstraintReference("constraints:voltage", "VIN <= 24V")],
            Docs: [new CapsuleDocumentReference("docs:datasheet", CapsuleDocumentKind.Datasheet, "https://example.invalid/regulator.pdf")],
            ValidationRules: [new CapsuleValidationRule("rule:thermal-region", CapsuleValidationSeverity.Warning, "Board region must include thermal copper.")]);

    private static CapsuleDefinition UsbCapsule() =>
        new(
            new CapsuleId("capsule:usb-c/pd"),
            "USB-C Power Delivery",
            "0.9.0",
            Parameters: [CapsuleParameterDefinition.Enum("role", "Port role", ["sink", "source"], required: true)],
            ComponentRefs: [new CapsuleComponentReference("component:usb-c-controller", "U2")],
            SchematicBlockRefs: [],
            BoardRegionRefs: [],
            FirmwareTemplates: [],
            Constraints: [],
            Docs: [new CapsuleDocumentReference("docs:pd-guide", CapsuleDocumentKind.DesignGuide, "docs/pd.md")],
            ValidationRules: [new CapsuleValidationRule("rule:role-review", CapsuleValidationSeverity.Warning, "Review requested role.")]);
}
