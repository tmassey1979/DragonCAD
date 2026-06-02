namespace DragonCAD.Core.Capsules;

public sealed record CapsuleDefinition(
    CapsuleId Id,
    string DisplayName,
    string Version,
    IReadOnlyList<CapsuleParameterDefinition> Parameters,
    IReadOnlyList<CapsuleComponentReference> ComponentRefs,
    IReadOnlyList<CapsuleSchematicBlockReference> SchematicBlockRefs,
    IReadOnlyList<CapsuleBoardRegionReference> BoardRegionRefs,
    IReadOnlyList<CapsuleFirmwareTemplateReference> FirmwareTemplates,
    IReadOnlyList<CapsuleConstraintReference> Constraints,
    IReadOnlyList<CapsuleDocumentReference> Docs,
    IReadOnlyList<CapsuleValidationRule> ValidationRules)
{
    public bool Equals(CapsuleDefinition? other) =>
        other is not null &&
        CapsuleDefinitionSerializer.Serialize(this) == CapsuleDefinitionSerializer.Serialize(other);

    public override int GetHashCode() => Id.GetHashCode();

    public void Validate()
    {
        EnsureText(DisplayName, nameof(DisplayName));
        EnsureText(Version, nameof(Version));
        EnsureUnique("parameter", Parameters.Select(parameter => parameter.Name));
        EnsureUnique("component reference", ComponentRefs.Select(reference => reference.Id));
        EnsureUnique("schematic block reference", SchematicBlockRefs.Select(reference => reference.Id));
        EnsureUnique("board region reference", BoardRegionRefs.Select(reference => reference.Id));
        EnsureUnique("firmware template", FirmwareTemplates.Select(template => template.Id));
        EnsureUnique("constraint", Constraints.Select(constraint => constraint.Id));
        EnsureUnique("document", Docs.Select(doc => doc.Id));
        EnsureUnique("validation rule", ValidationRules.Select(rule => rule.Id));

        foreach (CapsuleParameterDefinition parameter in Parameters)
        {
            parameter.Validate();
        }

        foreach (CapsuleReference reference in ComponentRefs.Cast<CapsuleReference>()
            .Concat(SchematicBlockRefs)
            .Concat(BoardRegionRefs)
            .Concat(FirmwareTemplates)
            .Concat(Constraints)
            .Concat(Docs)
            .Concat(ValidationRules))
        {
            reference.Validate();
        }
    }

    public void ValidateParameters(IReadOnlyDictionary<string, CapsuleParameterValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Validate();

        Dictionary<string, CapsuleParameterValue> valuesByName = new(values, StringComparer.Ordinal);
        foreach (CapsuleParameterDefinition parameter in Parameters)
        {
            if (!valuesByName.TryGetValue(parameter.Name, out CapsuleParameterValue? value))
            {
                if (parameter.Required)
                {
                    throw new InvalidOperationException($"Parameter '{parameter.Name}' is required.");
                }

                continue;
            }

            parameter.ValidateValue(value);
        }

        HashSet<string> knownParameters = Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
        foreach (string name in valuesByName.Keys)
        {
            if (!knownParameters.Contains(name))
            {
                throw new InvalidOperationException($"Unknown parameter '{name}'.");
            }
        }
    }

    public IReadOnlyList<CapsuleDependency> ListDependencies() =>
    [
        .. ComponentRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).Select(reference => new CapsuleDependency(CapsuleDependencyKind.Component, reference.Id)),
        .. SchematicBlockRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).Select(reference => new CapsuleDependency(CapsuleDependencyKind.SchematicBlock, reference.Id)),
        .. BoardRegionRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).Select(reference => new CapsuleDependency(CapsuleDependencyKind.BoardRegion, reference.Id)),
        .. FirmwareTemplates.OrderBy(template => template.Id, StringComparer.Ordinal).Select(template => new CapsuleDependency(CapsuleDependencyKind.FirmwareTemplate, template.Id)),
        .. Constraints.OrderBy(constraint => constraint.Id, StringComparer.Ordinal).Select(constraint => new CapsuleDependency(CapsuleDependencyKind.Constraint, constraint.Id)),
        .. Docs.OrderBy(doc => doc.Id, StringComparer.Ordinal).Select(doc => new CapsuleDependency(CapsuleDependencyKind.Documentation, doc.Id))
    ];

    internal static string NormalizeText(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);
        string trimmed = value.Trim();
        EnsureText(trimmed, parameterName);
        if (trimmed.Any(char.IsControl))
        {
            throw new ArgumentException("Capsule text values cannot contain control characters.", parameterName);
        }

        return trimmed;
    }

    private static void EnsureText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Capsule text values cannot be empty.", parameterName);
        }
    }

    private static void EnsureUnique(string collectionName, IEnumerable<string> values)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string value in values.Select(value => NormalizeText(value, collectionName)))
        {
            if (!seen.Add(value))
            {
                throw new InvalidOperationException($"Duplicate {collectionName} id '{value}'.");
            }
        }
    }
}

public readonly record struct CapsuleId : IComparable<CapsuleId>
{
    public CapsuleId(string value)
    {
        Value = CapsuleDefinition.NormalizeText(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(CapsuleId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public sealed record CapsuleParameterDefinition(
    string Name,
    string DisplayName,
    CapsuleParameterKind Kind,
    bool Required,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? AllowedValues = null)
{
    public static CapsuleParameterDefinition String(string name, string displayName, bool required = false) =>
        new(name, displayName, CapsuleParameterKind.String, required);

    public static CapsuleParameterDefinition Number(string name, string displayName, bool required = false, double? min = null, double? max = null) =>
        new(name, displayName, CapsuleParameterKind.Number, required, min, max);

    public static CapsuleParameterDefinition Enum(string name, string displayName, IReadOnlyList<string> allowedValues, bool required = false) =>
        new(name, displayName, CapsuleParameterKind.Enum, required, AllowedValues: allowedValues);

    public static CapsuleParameterDefinition Boolean(string name, string displayName, bool required = false) =>
        new(name, displayName, CapsuleParameterKind.Boolean, required);

    public void Validate()
    {
        CapsuleDefinition.NormalizeText(Name, nameof(Name));
        CapsuleDefinition.NormalizeText(DisplayName, nameof(DisplayName));

        if (Kind is not CapsuleParameterKind.Number && (Min is not null || Max is not null))
        {
            throw new InvalidOperationException($"Parameter '{Name}' can only define min and max for Number values.");
        }

        if (Min is not null && Max is not null && Min > Max)
        {
            throw new InvalidOperationException($"Parameter '{Name}' min cannot be greater than max.");
        }

        if (Kind is CapsuleParameterKind.Enum)
        {
            if (AllowedValues is null || AllowedValues.Count == 0)
            {
                throw new InvalidOperationException($"Parameter '{Name}' must define allowed enum values.");
            }

            EnsureAllowedValuesAreUnique();
            return;
        }

        if (AllowedValues is { Count: > 0 })
        {
            throw new InvalidOperationException($"Parameter '{Name}' can only define allowed values for Enum values.");
        }
    }

    public void ValidateValue(CapsuleParameterValue value)
    {
        if (value.Kind != Kind)
        {
            throw new InvalidOperationException($"Parameter '{Name}' expects {Kind} but received {value.Kind}.");
        }

        if (Kind is CapsuleParameterKind.Number)
        {
            double number = value.NumberValue ?? throw new InvalidOperationException($"Parameter '{Name}' is missing a number value.");
            if ((Min is not null && number < Min) || (Max is not null && number > Max))
            {
                throw new InvalidOperationException($"Parameter '{Name}' must be between {Min} and {Max}.");
            }
        }

        if (Kind is CapsuleParameterKind.Enum)
        {
            string enumValue = value.TextValue ?? throw new InvalidOperationException($"Parameter '{Name}' is missing an enum value.");
            if (AllowedValues is null || !AllowedValues.Contains(enumValue, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Parameter '{Name}' must be one of: {string.Join(", ", AllowedValues ?? [])}.");
            }
        }
    }

    private void EnsureAllowedValuesAreUnique()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string value in AllowedValues ?? [])
        {
            string normalized = CapsuleDefinition.NormalizeText(value, nameof(AllowedValues));
            if (!seen.Add(normalized))
            {
                throw new InvalidOperationException($"Parameter '{Name}' has duplicate enum value '{normalized}'.");
            }
        }
    }
}

public enum CapsuleParameterKind
{
    String,
    Number,
    Enum,
    Boolean
}

public sealed record CapsuleParameterValue(
    CapsuleParameterKind Kind,
    string? TextValue = null,
    double? NumberValue = null,
    bool? BooleanValue = null)
{
    public static CapsuleParameterValue String(string value) =>
        new(CapsuleParameterKind.String, TextValue: CapsuleDefinition.NormalizeText(value, nameof(value)));

    public static CapsuleParameterValue Number(double value) =>
        new(CapsuleParameterKind.Number, NumberValue: value);

    public static CapsuleParameterValue Enum(string value) =>
        new(CapsuleParameterKind.Enum, TextValue: CapsuleDefinition.NormalizeText(value, nameof(value)));

    public static CapsuleParameterValue Boolean(bool value) =>
        new(CapsuleParameterKind.Boolean, BooleanValue: value);
}

public abstract record CapsuleReference(string Id)
{
    public virtual void Validate() => CapsuleDefinition.NormalizeText(Id, nameof(Id));
}

public sealed record CapsuleComponentReference(string Id, string Designator) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Designator, nameof(Designator));
    }
}

public sealed record CapsuleSchematicBlockReference(string Id, string Name) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Name, nameof(Name));
    }
}

public sealed record CapsuleBoardRegionReference(string Id, string Name) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Name, nameof(Name));
    }
}

public sealed record CapsuleFirmwareTemplateReference(string Id, string Purpose) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Purpose, nameof(Purpose));
    }
}

public sealed record CapsuleConstraintReference(string Id, string Rule) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Rule, nameof(Rule));
    }
}

public sealed record CapsuleDocumentReference(string Id, CapsuleDocumentKind Kind, string Location) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Location, nameof(Location));
    }
}

public enum CapsuleDocumentKind
{
    Datasheet,
    DesignGuide,
    ConstraintNote,
    FirmwareGuide,
    Other
}

public sealed record CapsuleValidationRule(string Id, CapsuleValidationSeverity Severity, string Message) : CapsuleReference(Id)
{
    public override void Validate()
    {
        base.Validate();
        CapsuleDefinition.NormalizeText(Message, nameof(Message));
    }
}

public enum CapsuleValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record CapsuleDependency(CapsuleDependencyKind Kind, string Id);

public enum CapsuleDependencyKind
{
    Component,
    SchematicBlock,
    BoardRegion,
    FirmwareTemplate,
    Constraint,
    Documentation
}
