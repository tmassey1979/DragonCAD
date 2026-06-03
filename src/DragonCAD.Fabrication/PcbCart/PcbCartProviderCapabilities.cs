namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartProviderCapabilities
{
    private PcbCartProviderCapabilities(bool isFormalApiConfigured)
    {
        IsFormalApiConfigured = isFormalApiConfigured;
        HandoffMode = isFormalApiConfigured ? "formal-api-quote-order" : "quote-order-handoff";
        AllowsAutomaticProductionSubmission = false;
    }

    public string ProviderId => "pcbcart";

    public string DisplayName => "PCBCart";

    public string HandoffMode { get; }

    public bool IsFormalApiConfigured { get; }

    public bool AllowsAutomaticProductionSubmission { get; }

    public static PcbCartProviderCapabilities Create(bool isFormalApiConfigured)
    {
        return new PcbCartProviderCapabilities(isFormalApiConfigured);
    }
}
