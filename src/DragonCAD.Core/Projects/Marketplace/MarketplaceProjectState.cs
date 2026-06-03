using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Core.Projects.Marketplace;

public sealed record MarketplaceProjectState
{
    public MarketplaceProjectState(
        Version schemaVersion,
        IReadOnlyList<MarketplaceProviderFreshness> providerFreshness,
        IReadOnlyList<MarketplaceSelectedAlternate> selectedAlternates,
        IReadOnlyList<MarketplaceOrderDraft> orderDrafts,
        IReadOnlyList<MarketplaceLocalOrderRecord> localOrders)
    {
        SchemaVersion = schemaVersion;
        ProviderFreshness = providerFreshness
            .OrderBy(freshness => freshness.ProviderId, StringComparer.Ordinal)
            .ToArray();
        SelectedAlternates = selectedAlternates
            .OrderBy(alternate => alternate.Designator, StringComparer.Ordinal)
            .ThenBy(alternate => alternate.ProviderId, StringComparer.Ordinal)
            .ThenBy(alternate => alternate.VendorSku, StringComparer.Ordinal)
            .ToArray();
        OrderDrafts = orderDrafts
            .OrderBy(draft => draft.ProviderId, StringComparer.Ordinal)
            .ThenBy(draft => draft.CartId, StringComparer.Ordinal)
            .ThenBy(draft => draft.OrderDraftId, StringComparer.Ordinal)
            .ToArray();
        LocalOrders = localOrders
            .OrderBy(order => order.LocalOrderId, StringComparer.Ordinal)
            .ThenBy(order => order.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    public static MarketplaceProjectState Empty { get; } = new(new Version(1, 0), [], [], [], []);

    public bool Equals(MarketplaceProjectState? other) =>
        other is not null &&
        ProjectJson.Serialize(this) == ProjectJson.Serialize(other);

    public override int GetHashCode() => HashCode.Combine(SchemaVersion, ProviderFreshness.Count, SelectedAlternates.Count, OrderDrafts.Count, LocalOrders.Count);

    public Version SchemaVersion { get; }

    public IReadOnlyList<MarketplaceProviderFreshness> ProviderFreshness { get; }

    public IReadOnlyList<MarketplaceSelectedAlternate> SelectedAlternates { get; }

    public IReadOnlyList<MarketplaceOrderDraft> OrderDrafts { get; }

    public IReadOnlyList<MarketplaceLocalOrderRecord> LocalOrders { get; }
}

public sealed record MarketplaceProviderFreshness(
    string ProviderId,
    DateTimeOffset LastRefreshedAt,
    string? FreshnessToken);

public sealed record MarketplaceSelectedAlternate
{
    public MarketplaceSelectedAlternate(
        string designator,
        string canonicalComponentId,
        string providerId,
        string vendorSku,
        string? selectionReason)
    {
        Designator = designator;
        CanonicalComponentId = canonicalComponentId;
        ProviderId = providerId;
        VendorSku = vendorSku;
        SelectionReason = MarketplaceSecretScrubber.KeepSafeText(selectionReason);
    }

    public string Designator { get; }

    public string CanonicalComponentId { get; }

    public string ProviderId { get; }

    public string VendorSku { get; }

    public string? SelectionReason { get; }
}

public sealed record MarketplaceOrderDraft(
    string ProviderId,
    string? CartId,
    string? OrderDraftId);

public sealed record MarketplaceLocalOrderRecord
{
    public MarketplaceLocalOrderRecord(
        string localOrderId,
        string providerId,
        string? vendorOrderId,
        DateTimeOffset createdAt,
        MarketplaceLocalOrderStatus status,
        IReadOnlyList<string> designators,
        string? notes)
    {
        LocalOrderId = localOrderId;
        ProviderId = providerId;
        VendorOrderId = vendorOrderId;
        CreatedAt = createdAt;
        Status = status;
        Designators = designators.Order(StringComparer.Ordinal).ToArray();
        Notes = MarketplaceSecretScrubber.KeepSafeText(notes);
    }

    public string LocalOrderId { get; }

    public string ProviderId { get; }

    public string? VendorOrderId { get; }

    public DateTimeOffset CreatedAt { get; }

    public MarketplaceLocalOrderStatus Status { get; }

    public IReadOnlyList<string> Designators { get; }

    public string? Notes { get; }
}

public enum MarketplaceLocalOrderStatus
{
    Draft,
    Submitted,
    Cancelled,
    Received
}

public sealed record MarketplaceProjectStateLoadResult(
    MarketplaceProjectState? State,
    IReadOnlyList<MarketplaceProjectStateDiagnostic> Diagnostics);

public sealed record MarketplaceProjectStateImportResult(
    MarketplaceProjectState? State,
    IReadOnlyList<MarketplaceProjectStateDiagnostic> Diagnostics);

public sealed record MarketplaceProjectStateDiagnostic(
    MarketplaceProjectStateDiagnosticSeverity Severity,
    string Code,
    string Message);

public enum MarketplaceProjectStateDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public static class MarketplaceProjectStateDiagnosticCodes
{
    public const string StateFileCorrupt = "MarketplaceStateFileCorrupt";
    public const string ImportFileMissing = "MarketplaceImportFileMissing";
}

public sealed class MarketplaceProjectStateStore
{
    public const string RelativeStatePath = "marketplace/marketplace-state.json";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public void Save(string projectRoot, MarketplaceProjectState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(state);

        string path = Path.Combine(projectRoot, RelativeStatePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? projectRoot);
        File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));
    }

    public MarketplaceProjectStateLoadResult Load(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        string path = Path.Combine(projectRoot, RelativeStatePath);
        if (!File.Exists(path))
        {
            return new MarketplaceProjectStateLoadResult(MarketplaceProjectState.Empty, []);
        }

        return ReadState(path, RelativeStatePath);
    }

    public MarketplaceProjectStateImportResult ImportAppArtifactState(string artifactStatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactStatePath);

        if (!File.Exists(artifactStatePath))
        {
            return new MarketplaceProjectStateImportResult(
                null,
                [
                    new MarketplaceProjectStateDiagnostic(
                        MarketplaceProjectStateDiagnosticSeverity.Error,
                        MarketplaceProjectStateDiagnosticCodes.ImportFileMissing,
                        $"Marketplace import file '{NormalizePath(artifactStatePath)}' is missing.")
                ]);
        }

        MarketplaceProjectStateLoadResult result = ReadState(artifactStatePath, artifactStatePath);
        return new MarketplaceProjectStateImportResult(result.State, result.Diagnostics);
    }

    private static MarketplaceProjectStateLoadResult ReadState(string path, string diagnosticPath)
    {
        try
        {
            MarketplaceProjectState? state = JsonSerializer.Deserialize<MarketplaceProjectState>(
                File.ReadAllText(path),
                JsonOptions);

            return state is null
                ? new MarketplaceProjectStateLoadResult(null, [CorruptFileDiagnostic(diagnosticPath, "State file was empty.")])
                : new MarketplaceProjectStateLoadResult(state, []);
        }
        catch (JsonException exception)
        {
            return new MarketplaceProjectStateLoadResult(null, [CorruptFileDiagnostic(diagnosticPath, exception.Message)]);
        }
        catch (NotSupportedException exception)
        {
            return new MarketplaceProjectStateLoadResult(null, [CorruptFileDiagnostic(diagnosticPath, exception.Message)]);
        }
    }

    private static MarketplaceProjectStateDiagnostic CorruptFileDiagnostic(string path, string detail) =>
        new(
            MarketplaceProjectStateDiagnosticSeverity.Error,
            MarketplaceProjectStateDiagnosticCodes.StateFileCorrupt,
            $"Marketplace state file '{NormalizePath(path)}' could not be read: {detail}");

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new(ProjectJson.Options);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

internal static class MarketplaceSecretScrubber
{
    private static readonly string[] SensitiveTerms =
    [
        "address",
        "card",
        "credential",
        "oauth",
        "payment",
        "secret",
        "shipping",
        "token"
    ];

    public static string? KeepSafeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return SensitiveTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? null
            : value;
    }
}
