namespace DragonCAD.Core.Components.Identity;

public sealed class CanonicalComponentIdentity
{
    private readonly IReadOnlyList<VendorComponentOffer> vendorOffers;

    public CanonicalComponentIdentity(
        ComponentId id,
        string manufacturer,
        string manufacturerPartNumber,
        string genericFamily,
        string electricalValue,
        string? tolerance,
        string? voltageRating,
        string? currentRating,
        string package,
        int? pinCount,
        string footprintClass,
        ComponentLifecycle lifecycle,
        ComponentSourceConfidence sourceConfidence,
        VerifiedComponentGeometry? verifiedGeometry = null,
        IReadOnlyList<VendorComponentOffer>? vendorOffers = null)
    {
        if (pinCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pinCount), "Pin count must be positive when supplied.");
        }

        Id = id;
        Manufacturer = ComponentIdentityValue.Normalize(manufacturer, nameof(manufacturer));
        ManufacturerPartNumber = ComponentIdentityValue.Normalize(manufacturerPartNumber, nameof(manufacturerPartNumber));
        NormalizedManufacturerPartNumber = CanonicalIdentityText.PartNumberKey(ManufacturerPartNumber);
        GenericFamily = ComponentIdentityValue.Normalize(genericFamily, nameof(genericFamily));
        NormalizedGenericFamily = CanonicalIdentityText.TextKey(GenericFamily);
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
        FootprintClass = ComponentIdentityValue.Normalize(footprintClass, nameof(footprintClass));
        NormalizedFootprintClass = CanonicalIdentityText.TextKey(FootprintClass);
        Lifecycle = lifecycle;
        SourceConfidence = sourceConfidence;
        VerifiedGeometry = verifiedGeometry;
        this.vendorOffers = [.. vendorOffers ?? []];
    }

    public ComponentId Id { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string NormalizedManufacturerPartNumber { get; }

    public string GenericFamily { get; }

    public string NormalizedGenericFamily { get; }

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

    public string FootprintClass { get; }

    public string NormalizedFootprintClass { get; }

    public ComponentLifecycle Lifecycle { get; }

    public ComponentSourceConfidence SourceConfidence { get; }

    public VerifiedComponentGeometry? VerifiedGeometry { get; }

    public IReadOnlyList<VendorComponentOffer> VendorOffers => vendorOffers;

    public CanonicalComponentIdentity WithVerifiedGeometry(VerifiedComponentGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        return Copy(verifiedGeometry: geometry);
    }

    public CanonicalComponentIdentity AttachOffer(VendorComponentOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        return Copy(vendorOffers: [.. vendorOffers, offer]);
    }

    private CanonicalComponentIdentity Copy(
        VerifiedComponentGeometry? verifiedGeometry = null,
        IReadOnlyList<VendorComponentOffer>? vendorOffers = null) =>
        new(
            Id,
            Manufacturer,
            ManufacturerPartNumber,
            GenericFamily,
            ElectricalValue,
            Tolerance,
            VoltageRating,
            CurrentRating,
            Package,
            PinCount,
            FootprintClass,
            Lifecycle,
            SourceConfidence,
            verifiedGeometry ?? VerifiedGeometry,
            vendorOffers ?? this.vendorOffers);
}

public sealed record VerifiedComponentGeometry(
    ComponentSymbolId SymbolId,
    ComponentFootprintId FootprintId,
    string ReviewNote)
{
    public string ReviewNote { get; } = ComponentIdentityValue.Normalize(ReviewNote, nameof(ReviewNote));
}

public enum ComponentLifecycle
{
    Unknown,
    Active,
    NotRecommendedForNewDesigns,
    Obsolete
}

public enum ComponentSourceConfidence
{
    Unknown,
    VendorClaim,
    DatasheetDerived,
    Verified
}
