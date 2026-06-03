namespace DragonCAD.Core.Components.Identity;

public static class CanonicalComponentDedupeSuggester
{
    public static ComponentDedupeSuggestion Suggest(CanonicalComponentIdentity canonical, VendorComponentOffer offer)
    {
        ArgumentNullException.ThrowIfNull(canonical);
        ArgumentNullException.ThrowIfNull(offer);

        bool samePartNumber = canonical.NormalizedManufacturerPartNumber == offer.NormalizedManufacturerPartNumber;
        bool sameValue = SameValue(canonical, offer);
        bool samePackage = canonical.NormalizedPackage == offer.NormalizedPackage;
        bool samePinCount = !canonical.PinCount.HasValue || !offer.PinCount.HasValue || canonical.PinCount == offer.PinCount;

        if (samePartNumber)
        {
            return sameValue && samePackage && samePinCount
                ? new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.ExactMatch, "mpn")
                : new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.Conflict, "mpn-conflict");
        }

        if (!IsFamilyCompatible(canonical, offer))
        {
            return new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.Conflict, "family-conflict");
        }

        if (!sameValue)
        {
            return new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.ValueVariant, "value");
        }

        if (!samePackage || !samePinCount)
        {
            return new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.PackageVariant, "package");
        }

        return new ComponentDedupeSuggestion(ComponentDedupeSuggestionKind.LikelyAlternate, "family-value-package");
    }

    private static bool SameValue(CanonicalComponentIdentity canonical, VendorComponentOffer offer) =>
        CanonicalIdentityText.ValueKey(canonical.ElectricalValue) == CanonicalIdentityText.ValueKey(offer.ElectricalValue);

    private static bool IsFamilyCompatible(CanonicalComponentIdentity canonical, VendorComponentOffer offer)
    {
        string family = canonical.NormalizedGenericFamily;
        string partNumber = offer.NormalizedManufacturerPartNumber;
        string value = CanonicalIdentityText.ValueKey(offer.ElectricalValue);

        if (family.Contains("7805", StringComparison.Ordinal) && partNumber.Contains("7805", StringComparison.Ordinal))
        {
            return true;
        }

        if (family.Contains("555", StringComparison.Ordinal) && partNumber.Contains("555", StringComparison.Ordinal))
        {
            return true;
        }

        if (family.Contains("esp32", StringComparison.Ordinal)
            && (partNumber.Contains("ESP32", StringComparison.Ordinal) || value.Contains("esp32", StringComparison.Ordinal)))
        {
            return true;
        }

        if (family == "resistor" && LooksLikeResistor(offer))
        {
            return true;
        }

        if (family == "capacitor" && LooksLikeCapacitor(offer))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeResistor(VendorComponentOffer offer) =>
        CanonicalIdentityText.ValueKey(offer.ElectricalValue).Contains("ohm", StringComparison.Ordinal)
        || offer.NormalizedManufacturerPartNumber.StartsWith("RC", StringComparison.Ordinal);

    private static bool LooksLikeCapacitor(VendorComponentOffer offer)
    {
        string value = CanonicalIdentityText.ValueKey(offer.ElectricalValue);

        return value.Contains("nf", StringComparison.Ordinal)
            || value.Contains("uf", StringComparison.Ordinal)
            || value.Contains("pf", StringComparison.Ordinal)
            || offer.NormalizedManufacturerPartNumber.StartsWith("GRM", StringComparison.Ordinal);
    }
}

public sealed record ComponentDedupeSuggestion(ComponentDedupeSuggestionKind Kind, string Reason);

public enum ComponentDedupeSuggestionKind
{
    ExactMatch,
    LikelyAlternate,
    ValueVariant,
    PackageVariant,
    Conflict
}
