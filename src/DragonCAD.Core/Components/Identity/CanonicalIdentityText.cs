using System.Text;

namespace DragonCAD.Core.Components.Identity;

internal static class CanonicalIdentityText
{
    public static string TextKey(string value)
    {
        string normalized = ComponentIdentityValue.Normalize(value, nameof(value)).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        bool lastWasSeparator = false;

        foreach (char character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character is '%' or '+')
            {
                builder.Append(character);
                lastWasSeparator = false;
                continue;
            }

            if (!lastWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    public static string TextKeyOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : TextKey(value);

    public static string PartNumberKey(string value)
    {
        string normalized = ComponentIdentityValue.Normalize(value, nameof(value)).ToUpperInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (char character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static string PackageKey(string value) => PartNumberKey(value);

    public static string ValueKey(string value)
    {
        string key = TextKey(value)
            .Replace("-ohm", "ohm", StringComparison.Ordinal)
            .Replace("ohms", "ohm", StringComparison.Ordinal)
            .Replace("-v", "v", StringComparison.Ordinal)
            .Replace("-a", "a", StringComparison.Ordinal)
            .Replace("-nf", "nf", StringComparison.Ordinal)
            .Replace("-uf", "uf", StringComparison.Ordinal)
            .Replace("-pf", "pf", StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return key;
    }
}
