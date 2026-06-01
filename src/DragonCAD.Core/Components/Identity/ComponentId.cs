namespace DragonCAD.Core.Components.Identity;

public readonly record struct ComponentId : IComparable<ComponentId>
{
    public ComponentId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentSymbolId : IComparable<ComponentSymbolId>
{
    public ComponentSymbolId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentSymbolId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentFootprintId : IComparable<ComponentFootprintId>
{
    public ComponentFootprintId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentFootprintId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentVariantId : IComparable<ComponentVariantId>
{
    public ComponentVariantId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentVariantId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentGateId : IComparable<ComponentGateId>
{
    public ComponentGateId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentGateId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentPinId : IComparable<ComponentPinId>
{
    public ComponentPinId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentPinId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

public readonly record struct ComponentPadId : IComparable<ComponentPadId>
{
    public ComponentPadId(string value)
    {
        Value = ComponentIdentityValue.Normalize(value, nameof(value));
    }

    public string Value { get; }

    public int CompareTo(ComponentPadId other) => string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;
}

internal static class ComponentIdentityValue
{
    public static string Normalize(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Component identity values cannot be empty.", parameterName);
        }

        if (trimmed.Any(char.IsControl))
        {
            throw new ArgumentException("Component identity values cannot contain control characters.", parameterName);
        }

        return trimmed;
    }
}
