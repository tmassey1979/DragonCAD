using System.Globalization;
using System.Text.Json;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

internal static class ApiBackedJson
{
    public static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool TryGetArray(JsonElement element, string name, out JsonElement value)
    {
        if (TryGetProperty(element, name, out value) && value.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        value = default;
        return false;
    }

    public static string? GetText(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            var value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    public static string? GetNestedText(JsonElement element, string name, string nestedName)
    {
        return TryGetProperty(element, name, out var nested) && nested.ValueKind == JsonValueKind.Object
            ? GetText(nested, nestedName)
            : null;
    }

    public static int? GetInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String
                && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    public static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String
                && decimal.TryParse(property.GetString(), NumberStyles.Currency, CultureInfo.InvariantCulture, out var stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    public static Uri? GetUri(JsonElement element, params string[] names)
    {
        var value = GetText(element, names);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}
