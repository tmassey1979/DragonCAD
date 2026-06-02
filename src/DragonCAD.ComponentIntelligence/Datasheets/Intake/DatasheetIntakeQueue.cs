using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.ComponentIntelligence.Datasheets.Intake;

public readonly record struct DatasheetIntakeRequestId
{
    [JsonConstructor]
    public DatasheetIntakeRequestId(string value)
    {
        Value = Require(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Datasheet intake request id is required.", parameterName);
        }

        return value.Trim();
    }
}

public interface IDatasheetIntakeRequestIdSource
{
    DatasheetIntakeRequestId Next();
}

public sealed class SequentialDatasheetIntakeRequestIdSource : IDatasheetIntakeRequestIdSource
{
    private int next;

    public SequentialDatasheetIntakeRequestIdSource(int startAt)
    {
        if (startAt < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startAt), "Start value must be positive.");
        }

        next = startAt;
    }

    public DatasheetIntakeRequestId Next() =>
        new($"intake-{next++:0000}");
}

public interface IDatasheetIntakeClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class FixedDatasheetIntakeClock(DateTimeOffset utcNow) : IDatasheetIntakeClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

public sealed class SystemDatasheetIntakeClock : IDatasheetIntakeClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public enum DatasheetIntakeSourceType
{
    LocalPdf,
    Url,
}

public sealed record DatasheetIntakeSource
{
    public DatasheetIntakeSource(DatasheetIntakeSourceType type, string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Datasheet source identifier is required.", nameof(identifier));
        }

        Type = type;
        Identifier = identifier.Trim();
    }

    public DatasheetIntakeSourceType Type { get; }

    public string Identifier { get; }
}

public sealed record DatasheetRequestedComponent
{
    public DatasheetRequestedComponent(
        string? ManufacturerPartNumber,
        string? Manufacturer,
        string? VendorProductId,
        string? PackageName,
        string? Notes)
    {
        this.ManufacturerPartNumber = Normalize(ManufacturerPartNumber);
        this.Manufacturer = Normalize(Manufacturer);
        this.VendorProductId = Normalize(VendorProductId);
        this.PackageName = Normalize(PackageName);
        this.Notes = Normalize(Notes);
    }

    public string? ManufacturerPartNumber { get; }

    public string? Manufacturer { get; }

    public string? VendorProductId { get; }

    public string? PackageName { get; }

    public string? Notes { get; }

    internal bool HasRequestedIdentifier =>
        !string.IsNullOrWhiteSpace(ManufacturerPartNumber) ||
        !string.IsNullOrWhiteSpace(VendorProductId);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum DatasheetIntakeStatus
{
    ReviewRequired,
    InReview,
    DraftGenerated,
    Rejected,
}

public enum DatasheetIntakeReviewNoteSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record DatasheetIntakeReviewNote(
    DatasheetIntakeReviewNoteSeverity Severity,
    string Message,
    DateTimeOffset RecordedAt);

public sealed record DatasheetIntakeRequest(
    DatasheetIntakeRequestId Id,
    DatasheetIntakeSource Source,
    DatasheetRequestedComponent RequestedComponent,
    string SubmittedBy,
    DateTimeOffset SubmittedAt,
    DatasheetIntakeStatus Status,
    IReadOnlyList<DatasheetIntakeReviewNote> ReviewNotes);

public enum DatasheetIntakeDiagnosticCode
{
    MissingLocalFile,
    UnsupportedLocalFileExtension,
    MissingRequestedIdentifier,
    DuplicateRequest,
    UnsupportedUrlScheme,
}

public sealed record DatasheetIntakeDiagnostic(DatasheetIntakeDiagnosticCode Code, string Message);

public sealed record DatasheetIntakeSubmissionResult(
    DatasheetIntakeRequest? Request,
    IReadOnlyList<DatasheetIntakeDiagnostic> Diagnostics)
{
    public bool Accepted => Request is not null && Diagnostics.Count == 0;
}

public sealed class DatasheetIntakeQueue
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly string queuePath;
    private readonly IDatasheetIntakeRequestIdSource idSource;
    private readonly IDatasheetIntakeClock clock;
    private readonly List<DatasheetIntakeRequest> requests;

    private DatasheetIntakeQueue(
        string queuePath,
        IDatasheetIntakeRequestIdSource idSource,
        IDatasheetIntakeClock clock,
        IReadOnlyList<DatasheetIntakeRequest> requests)
    {
        this.queuePath = queuePath;
        this.idSource = idSource;
        this.clock = clock;
        this.requests = requests
            .OrderBy(request => request.Id.Value, StringComparer.Ordinal)
            .ToList();
    }

    public static DatasheetIntakeQueue Load(
        string queuePath,
        IDatasheetIntakeRequestIdSource idSource,
        IDatasheetIntakeClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queuePath);
        ArgumentNullException.ThrowIfNull(idSource);
        ArgumentNullException.ThrowIfNull(clock);

        IReadOnlyList<DatasheetIntakeRequest> loadedRequests = [];
        if (File.Exists(queuePath))
        {
            string json = File.ReadAllText(queuePath);
            loadedRequests = JsonSerializer.Deserialize<List<DatasheetIntakeRequest>>(json, JsonOptions) ?? [];
        }

        return new DatasheetIntakeQueue(queuePath, idSource, clock, loadedRequests);
    }

    public IReadOnlyList<DatasheetIntakeRequest> List() =>
        requests
            .OrderBy(request => request.SubmittedAt)
            .ThenBy(request => request.Id.Value, StringComparer.Ordinal)
            .ToArray();

    public DatasheetIntakeSubmissionResult SubmitLocalPdf(
        string localPath,
        DatasheetRequestedComponent requestedComponent,
        string submittedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        ArgumentNullException.ThrowIfNull(requestedComponent);

        string normalizedPath = Path.GetFullPath(localPath);
        List<DatasheetIntakeDiagnostic> diagnostics = ValidateSubmission(
            new DatasheetIntakeSource(DatasheetIntakeSourceType.LocalPdf, normalizedPath),
            requestedComponent);

        if (!File.Exists(normalizedPath))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic(
                DatasheetIntakeDiagnosticCode.MissingLocalFile,
                "Local datasheet file does not exist."));
        }
        else if (!string.Equals(Path.GetExtension(normalizedPath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic(
                DatasheetIntakeDiagnosticCode.UnsupportedLocalFileExtension,
                "Local datasheet intake only supports PDF files."));
        }

        return SubmitIfValid(
            new DatasheetIntakeSource(DatasheetIntakeSourceType.LocalPdf, normalizedPath),
            requestedComponent,
            submittedBy,
            diagnostics);
    }

    public DatasheetIntakeSubmissionResult SubmitUrl(
        Uri datasheetUrl,
        DatasheetRequestedComponent requestedComponent,
        string submittedBy)
    {
        ArgumentNullException.ThrowIfNull(datasheetUrl);
        ArgumentNullException.ThrowIfNull(requestedComponent);

        string normalizedUrl = datasheetUrl.AbsoluteUri;
        DatasheetIntakeSource source = new(DatasheetIntakeSourceType.Url, normalizedUrl);
        List<DatasheetIntakeDiagnostic> diagnostics = ValidateSubmission(source, requestedComponent);

        if (!datasheetUrl.IsAbsoluteUri || datasheetUrl.Scheme is not ("http" or "https"))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic(
                DatasheetIntakeDiagnosticCode.UnsupportedUrlScheme,
                "Datasheet URL must be absolute HTTP or HTTPS."));
        }

        return SubmitIfValid(source, requestedComponent, submittedBy, diagnostics);
    }

    public DatasheetIntakeRequest UpdateStatus(
        DatasheetIntakeRequestId requestId,
        DatasheetIntakeStatus nextStatus)
    {
        int index = FindRequestIndex(requestId);
        DatasheetIntakeRequest request = requests[index];
        if (!CanTransition(request.Status, nextStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition datasheet intake request {request.Id} from {request.Status} to {nextStatus}.");
        }

        DatasheetIntakeRequest updated = request with { Status = nextStatus };
        requests[index] = updated;
        Save();
        return updated;
    }

    public DatasheetIntakeRequest AddReviewNote(
        DatasheetIntakeRequestId requestId,
        DatasheetIntakeReviewNoteSeverity severity,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        int index = FindRequestIndex(requestId);
        DatasheetIntakeRequest request = requests[index];
        DatasheetIntakeRequest updated = request with
        {
            ReviewNotes = request.ReviewNotes
                .Append(new DatasheetIntakeReviewNote(severity, message.Trim(), clock.UtcNow))
                .ToArray(),
        };

        requests[index] = updated;
        Save();
        return updated;
    }

    public string Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(queuePath))!);
        string json = JsonSerializer.Serialize(
            requests.OrderBy(request => request.Id.Value, StringComparer.Ordinal).ToArray(),
            JsonOptions);
        File.WriteAllText(queuePath, json);
        return json;
    }

    private DatasheetIntakeSubmissionResult SubmitIfValid(
        DatasheetIntakeSource source,
        DatasheetRequestedComponent requestedComponent,
        string submittedBy,
        IReadOnlyList<DatasheetIntakeDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(submittedBy);

        if (diagnostics.Count > 0)
        {
            return new DatasheetIntakeSubmissionResult(null, diagnostics);
        }

        DatasheetIntakeRequest request = new(
            idSource.Next(),
            source,
            requestedComponent,
            submittedBy.Trim(),
            clock.UtcNow,
            DatasheetIntakeStatus.ReviewRequired,
            ReviewNotes: []);

        requests.Add(request);
        Save();
        return new DatasheetIntakeSubmissionResult(request, Diagnostics: []);
    }

    private List<DatasheetIntakeDiagnostic> ValidateSubmission(
        DatasheetIntakeSource source,
        DatasheetRequestedComponent requestedComponent)
    {
        List<DatasheetIntakeDiagnostic> diagnostics = [];

        if (!requestedComponent.HasRequestedIdentifier)
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic(
                DatasheetIntakeDiagnosticCode.MissingRequestedIdentifier,
                "Datasheet intake requires a manufacturer part number or vendor product id."));
        }

        if (requests.Any(request => IsDuplicate(request, source, requestedComponent)))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic(
                DatasheetIntakeDiagnosticCode.DuplicateRequest,
                "A matching datasheet intake request already exists."));
        }

        return diagnostics;
    }

    private static bool IsDuplicate(
        DatasheetIntakeRequest request,
        DatasheetIntakeSource source,
        DatasheetRequestedComponent requestedComponent) =>
        request.Source.Type == source.Type &&
        string.Equals(request.Source.Identifier, source.Identifier, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            request.RequestedComponent.ManufacturerPartNumber,
            requestedComponent.ManufacturerPartNumber,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            request.RequestedComponent.VendorProductId,
            requestedComponent.VendorProductId,
            StringComparison.OrdinalIgnoreCase);

    private int FindRequestIndex(DatasheetIntakeRequestId requestId)
    {
        int index = requests.FindIndex(request => request.Id == requestId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Datasheet intake request {requestId} was not found.");
        }

        return index;
    }

    private static bool CanTransition(
        DatasheetIntakeStatus currentStatus,
        DatasheetIntakeStatus nextStatus) =>
        currentStatus == nextStatus ||
        (currentStatus, nextStatus) is
            (DatasheetIntakeStatus.ReviewRequired, DatasheetIntakeStatus.InReview) or
            (DatasheetIntakeStatus.ReviewRequired, DatasheetIntakeStatus.Rejected) or
            (DatasheetIntakeStatus.InReview, DatasheetIntakeStatus.DraftGenerated) or
            (DatasheetIntakeStatus.InReview, DatasheetIntakeStatus.Rejected);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
