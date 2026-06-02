using DragonCAD.Core.Capsules;

namespace DragonCAD.Core.Tests.Capsules;

public sealed class CapsuleDefinitionTests
{
    [Fact]
    public void CapsuleCreationStoresReusableHardwareReferences()
    {
        CapsuleDefinition capsule = CreatePowerCapsule();

        capsule.Validate();

        Assert.Equal("capsule:power/regulator", capsule.Id.Value);
        Assert.Equal("1.2.0", capsule.Version);
        Assert.Equal(["component:capacitor", "component:regulator"], capsule.ComponentRefs.Select(reference => reference.Id));
        Assert.Equal(["schematic:enable", "schematic:power-path"], capsule.SchematicBlockRefs.Select(reference => reference.Id));
        Assert.Equal(["board:input", "board:thermal"], capsule.BoardRegionRefs.Select(reference => reference.Id));
        Assert.Equal(["firmware:driver", "firmware:init"], capsule.FirmwareTemplates.Select(template => template.Id));
        Assert.Equal(["constraints:clearance", "constraints:voltage"], capsule.Constraints.Select(constraint => constraint.Id));
        Assert.Equal(["docs:datasheet", "docs:integration"], capsule.Docs.Select(doc => doc.Id));
        Assert.Equal(["rule:required-input-cap", "rule:thermal-region"], capsule.ValidationRules.Select(rule => rule.Id));
    }

    [Fact]
    public void ParameterDefinitionsValidateStringNumberEnumAndBooleanValues()
    {
        CapsuleDefinition capsule = CreatePowerCapsule();

        capsule.ValidateParameters(new Dictionary<string, CapsuleParameterValue>
        {
            ["outputVoltage"] = CapsuleParameterValue.Number(5),
            ["controller"] = CapsuleParameterValue.String("buck"),
            ["mode"] = CapsuleParameterValue.Enum("pwm"),
            ["enableSoftStart"] = CapsuleParameterValue.Boolean(true)
        });

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(
            () => capsule.ValidateParameters(new Dictionary<string, CapsuleParameterValue>()));
        Assert.Contains("outputVoltage", missing.Message, StringComparison.Ordinal);

        InvalidOperationException wrongKind = Assert.Throws<InvalidOperationException>(
            () => capsule.ValidateParameters(new Dictionary<string, CapsuleParameterValue>
            {
                ["outputVoltage"] = CapsuleParameterValue.String("5V"),
                ["controller"] = CapsuleParameterValue.String("buck"),
                ["mode"] = CapsuleParameterValue.Enum("pwm"),
                ["enableSoftStart"] = CapsuleParameterValue.Boolean(true)
            }));
        Assert.Contains("Number", wrongKind.Message, StringComparison.Ordinal);

        InvalidOperationException outOfRange = Assert.Throws<InvalidOperationException>(
            () => capsule.ValidateParameters(new Dictionary<string, CapsuleParameterValue>
            {
                ["outputVoltage"] = CapsuleParameterValue.Number(28),
                ["controller"] = CapsuleParameterValue.String("buck"),
                ["mode"] = CapsuleParameterValue.Enum("pwm"),
                ["enableSoftStart"] = CapsuleParameterValue.Boolean(true)
            }));
        Assert.Contains("between 1.8 and 24", outOfRange.Message, StringComparison.Ordinal);

        InvalidOperationException badEnum = Assert.Throws<InvalidOperationException>(
            () => capsule.ValidateParameters(new Dictionary<string, CapsuleParameterValue>
            {
                ["outputVoltage"] = CapsuleParameterValue.Number(5),
                ["controller"] = CapsuleParameterValue.String("buck"),
                ["mode"] = CapsuleParameterValue.Enum("linear"),
                ["enableSoftStart"] = CapsuleParameterValue.Boolean(true)
            }));
        Assert.Contains("pwm", badEnum.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DependencyListingIsGroupedAndOrderedDeterministically()
    {
        CapsuleDefinition capsule = CreatePowerCapsule();

        IReadOnlyList<CapsuleDependency> dependencies = capsule.ListDependencies();

        Assert.Equal(
            [
                new CapsuleDependency(CapsuleDependencyKind.Component, "component:capacitor"),
                new CapsuleDependency(CapsuleDependencyKind.Component, "component:regulator"),
                new CapsuleDependency(CapsuleDependencyKind.SchematicBlock, "schematic:enable"),
                new CapsuleDependency(CapsuleDependencyKind.SchematicBlock, "schematic:power-path"),
                new CapsuleDependency(CapsuleDependencyKind.BoardRegion, "board:input"),
                new CapsuleDependency(CapsuleDependencyKind.BoardRegion, "board:thermal"),
                new CapsuleDependency(CapsuleDependencyKind.FirmwareTemplate, "firmware:driver"),
                new CapsuleDependency(CapsuleDependencyKind.FirmwareTemplate, "firmware:init"),
                new CapsuleDependency(CapsuleDependencyKind.Constraint, "constraints:clearance"),
                new CapsuleDependency(CapsuleDependencyKind.Constraint, "constraints:voltage"),
                new CapsuleDependency(CapsuleDependencyKind.Documentation, "docs:datasheet"),
                new CapsuleDependency(CapsuleDependencyKind.Documentation, "docs:integration")
            ],
            dependencies);
    }

    [Fact]
    public void CapsuleSerializationRoundTripsWithDeterministicDependencyOrdering()
    {
        CapsuleDefinition capsule = CreatePowerCapsule();

        string first = CapsuleDefinitionSerializer.Serialize(capsule);
        CapsuleDefinition reloaded = CapsuleDefinitionSerializer.Deserialize(first);
        string second = CapsuleDefinitionSerializer.Serialize(reloaded);

        Assert.Equal(capsule, reloaded);
        Assert.Equal(first, second);
        Assert.True(first.IndexOf("component:capacitor", StringComparison.Ordinal) < first.IndexOf("component:regulator", StringComparison.Ordinal));
        Assert.True(first.IndexOf("schematic:enable", StringComparison.Ordinal) < first.IndexOf("schematic:power-path", StringComparison.Ordinal));
        Assert.True(first.IndexOf("board:input", StringComparison.Ordinal) < first.IndexOf("board:thermal", StringComparison.Ordinal));
        Assert.True(first.IndexOf("firmware:driver", StringComparison.Ordinal) < first.IndexOf("firmware:init", StringComparison.Ordinal));
        Assert.True(first.IndexOf("constraints:clearance", StringComparison.Ordinal) < first.IndexOf("constraints:voltage", StringComparison.Ordinal));
        Assert.True(first.IndexOf("docs:datasheet", StringComparison.Ordinal) < first.IndexOf("docs:integration", StringComparison.Ordinal));
        Assert.True(first.IndexOf("rule:required-input-cap", StringComparison.Ordinal) < first.IndexOf("rule:thermal-region", StringComparison.Ordinal));
        Assert.Contains("\"kind\": \"Number\"", first, StringComparison.Ordinal);
        Assert.Contains("\"allowedValues\"", first, StringComparison.Ordinal);
    }

    private static CapsuleDefinition CreatePowerCapsule() =>
        new(
            new CapsuleId(" capsule:power/regulator "),
            "Point-of-load regulator",
            "1.2.0",
            Parameters:
            [
                CapsuleParameterDefinition.Number("outputVoltage", "Output voltage", required: true, min: 1.8, max: 24),
                CapsuleParameterDefinition.String("controller", "Controller family", required: true),
                CapsuleParameterDefinition.Enum("mode", "Switching mode", ["pfm", "pwm"], required: true),
                CapsuleParameterDefinition.Boolean("enableSoftStart", "Enable soft-start", required: true)
            ],
            ComponentRefs:
            [
                new CapsuleComponentReference("component:regulator", "U1"),
                new CapsuleComponentReference("component:capacitor", "Cout")
            ],
            SchematicBlockRefs:
            [
                new CapsuleSchematicBlockReference("schematic:power-path", "Power path"),
                new CapsuleSchematicBlockReference("schematic:enable", "Enable logic")
            ],
            BoardRegionRefs:
            [
                new CapsuleBoardRegionReference("board:thermal", "Thermal copper"),
                new CapsuleBoardRegionReference("board:input", "Input filter")
            ],
            FirmwareTemplates:
            [
                new CapsuleFirmwareTemplateReference("firmware:init", "Initialize regulator"),
                new CapsuleFirmwareTemplateReference("firmware:driver", "Runtime control")
            ],
            Constraints:
            [
                new CapsuleConstraintReference("constraints:voltage", "VIN <= 24V"),
                new CapsuleConstraintReference("constraints:clearance", "0.5mm clearance")
            ],
            Docs:
            [
                new CapsuleDocumentReference("docs:integration", CapsuleDocumentKind.DesignGuide, "docs/regulator.md"),
                new CapsuleDocumentReference("docs:datasheet", CapsuleDocumentKind.Datasheet, "https://example.invalid/regulator.pdf")
            ],
            ValidationRules:
            [
                new CapsuleValidationRule("rule:thermal-region", CapsuleValidationSeverity.Warning, "Board region must include thermal copper."),
                new CapsuleValidationRule("rule:required-input-cap", CapsuleValidationSeverity.Error, "Input capacitor is required.")
            ]);
}
