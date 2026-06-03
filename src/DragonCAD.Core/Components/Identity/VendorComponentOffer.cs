namespace DragonCAD.Core.Components.Identity;

public sealed class VendorComponentOffer
{
    public VendorComponentOffer(
        string vendor,
        string vendorSku,
        string manufacturer,
        string manufacturerPartNumber,
        string electricalValue,
        string package,
        string? tolerance = null,
        string? voltageRating = null,
        string? currentRating = null,
        int? pinCount = null,
        string? footprintClass = null,
        ComponentLifecycle lifecycle = ComponentLifecycle.Unknown,
        ComponentSourceConfidence sourceConfidence = ComponentSourceConfidence.VendorClaim)
    {
        if (pinCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pinCount), "Pin count must be positive when supplied.");
        }

        Vendor = ComponentIdentityValue.Normalize(vendor, nameof(vendor));
        VendorSku = ComponentIdentityValue.Normalize(vendorSku, nameof(vendorSku));
        Manufacturer = ComponentIdentityValue.Normalize(manufacturer, nameof(manufacturer));
        ManufacturerPartNumber = ComponentIdentityValue.Normalize(manufacturerPartNumber, nameof(manufacturerPartNumber));
        NormalizedManufacturerPartNumber = CanonicalIdentityText.PartNumberKey(ManufacturerPartNumber);
        ElectricalValue = ComponentIdentityValue.Normalize(electricalValue, nameof(electricalValue));
        NormalizedElectricalValue = CanonicalIdentityText.TextKey(ElectricalValue);
        Tolerance = ComponentIdentityValue.NormalizeOptional(tolerance, nameof(tolerance));
        NormalizedTolerance = CanonicalIdentityText.TextKeyOrEmpty(Tolerance);
        VoltageRating = ComponentIdentityValue.NormalizeOptional(voltageRating, nameof(voltageRating));
        NormalizedVoltageRating = CanonicalIdentityText.TextKeyOrEmpty(VoltageRating);
        CurrentRating = ComponentIdentityValue.NormalizeOptional(currentRating, nameof(currentRating));
        NormalizedCurrentRating = CanonicalIdentityText.TextKeyOrEmpty(CurrentRating);
        Package = ComponentIdentityValue.Normalize(package, nameof(package));
        NormalizedPackage = CanonicalIdentityText.PackageKey(Package);
        PinCount = pinCount;
        FootprintClass = ComponentIdentityValue.NormalizeOptional(footprintClass, nameof(footprintClass));
        NormalizedFootprintClass = CanonicalIdentityText.TextKeyOrEmpty(FootprintClass);
        Lifecycle = lifecycle;
        SourceConfidence = sourceConfidence;
    }

    public string Vendor { get; }

    public string VendorSku { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string NormalizedManufacturerPartNumber { get; }

    public string ElectricalValue { get; }

    public string NormalizedElectricalValue { get; }

    public string? Tolerance { get; }

    public string NormalizedTolerance { get; }

    public string? VoltageRating { get; }

    public string NormalizedVoltageRating { get; }

    public string? CurrentRating { get; }

    public string NormalizedCurrentRating { get; }

    public string Package { get; }

    public string NormalizedPackage { get; }

    public int? PinCount { get; }

    public string? FootprintClass { get; }

    public string NormalizedFootprintClass { get; }

    public ComponentLifecycle Lifecycle { get; }

    public ComponentSourceConfidence SourceConfidence { get; }
}
