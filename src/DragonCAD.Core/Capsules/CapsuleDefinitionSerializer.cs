using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Core.Capsules;

public static class CapsuleDefinitionSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(CapsuleDefinition capsule)
    {
        ArgumentNullException.ThrowIfNull(capsule);
        capsule.Validate();
        return JsonSerializer.Serialize(ToDto(capsule), Options);
    }

    public static CapsuleDefinition Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        CapsuleDefinitionDto dto = JsonSerializer.Deserialize<CapsuleDefinitionDto>(json, Options)
            ?? throw new InvalidOperationException("Capsule definition JSON was empty.");
        CapsuleDefinition capsule = FromDto(dto);
        capsule.Validate();
        return capsule;
    }

    private static CapsuleDefinitionDto ToDto(CapsuleDefinition capsule) =>
        new(
            capsule.Id.Value,
            capsule.DisplayName,
            capsule.Version,
            capsule.Parameters
                .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(parameter => parameter with
                {
                    AllowedValues = parameter.AllowedValues?.Order(StringComparer.Ordinal).ToArray()
                })
                .ToArray(),
            capsule.ComponentRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).ThenBy(reference => reference.Designator, StringComparer.Ordinal).ToArray(),
            capsule.SchematicBlockRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).ThenBy(reference => reference.Name, StringComparer.Ordinal).ToArray(),
            capsule.BoardRegionRefs.OrderBy(reference => reference.Id, StringComparer.Ordinal).ThenBy(reference => reference.Name, StringComparer.Ordinal).ToArray(),
            capsule.FirmwareTemplates.OrderBy(template => template.Id, StringComparer.Ordinal).ThenBy(template => template.Purpose, StringComparer.Ordinal).ToArray(),
            capsule.Constraints.OrderBy(constraint => constraint.Id, StringComparer.Ordinal).ThenBy(constraint => constraint.Rule, StringComparer.Ordinal).ToArray(),
            capsule.Docs.OrderBy(doc => doc.Id, StringComparer.Ordinal).ThenBy(doc => doc.Location, StringComparer.Ordinal).ToArray(),
            capsule.ValidationRules.OrderBy(rule => rule.Id, StringComparer.Ordinal).ThenBy(rule => rule.Message, StringComparer.Ordinal).ToArray());

    private static CapsuleDefinition FromDto(CapsuleDefinitionDto dto) =>
        new(
            new CapsuleId(dto.Id),
            dto.DisplayName,
            dto.Version,
            dto.Parameters,
            dto.ComponentRefs,
            dto.SchematicBlockRefs,
            dto.BoardRegionRefs,
            dto.FirmwareTemplates,
            dto.Constraints,
            dto.Docs,
            dto.ValidationRules);

    private sealed record CapsuleDefinitionDto(
        string Id,
        string DisplayName,
        string Version,
        IReadOnlyList<CapsuleParameterDefinition> Parameters,
        IReadOnlyList<CapsuleComponentReference> ComponentRefs,
        IReadOnlyList<CapsuleSchematicBlockReference> SchematicBlockRefs,
        IReadOnlyList<CapsuleBoardRegionReference> BoardRegionRefs,
        IReadOnlyList<CapsuleFirmwareTemplateReference> FirmwareTemplates,
        IReadOnlyList<CapsuleConstraintReference> Constraints,
        IReadOnlyList<CapsuleDocumentReference> Docs,
        IReadOnlyList<CapsuleValidationRule> ValidationRules);
}
