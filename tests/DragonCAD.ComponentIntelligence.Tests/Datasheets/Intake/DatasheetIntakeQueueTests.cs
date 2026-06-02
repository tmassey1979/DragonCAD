using DragonCAD.ComponentIntelligence.Datasheets.Intake;

namespace DragonCAD.ComponentIntelligence.Tests.Datasheets.Intake;

public sealed class DatasheetIntakeQueueTests
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 06, 02, 14, 30, 00, TimeSpan.Zero);

    [Fact]
    public void SubmitLocalPdfRecordsSourceMetadataStatusAndStableIdentity()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        string pdfPath = directory.WriteFile("lm7805.pdf", "%PDF-1.7");
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);

        DatasheetIntakeSubmissionResult result = queue.SubmitLocalPdf(
            pdfPath,
            new DatasheetRequestedComponent(
                ManufacturerPartNumber: "LM7805CT",
                Manufacturer: "Texas Instruments",
                VendorProductId: "296-1415-5-ND",
                PackageName: "TO-220-3",
                Notes: "Prefer through-hole footprint."),
            submittedBy: "librarian@example.test");

        DatasheetIntakeRequest request = AssertAccepted(result);
        Assert.Equal(new DatasheetIntakeRequestId("intake-0001"), request.Id);
        Assert.Equal(DatasheetIntakeSourceType.LocalPdf, request.Source.Type);
        Assert.Equal(Path.GetFullPath(pdfPath), request.Source.Identifier);
        Assert.Equal("LM7805CT", request.RequestedComponent.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", request.RequestedComponent.Manufacturer);
        Assert.Equal("296-1415-5-ND", request.RequestedComponent.VendorProductId);
        Assert.Equal("TO-220-3", request.RequestedComponent.PackageName);
        Assert.Equal("Prefer through-hole footprint.", request.RequestedComponent.Notes);
        Assert.Equal("librarian@example.test", request.SubmittedBy);
        Assert.Equal(SubmittedAt, request.SubmittedAt);
        Assert.Equal(DatasheetIntakeStatus.ReviewRequired, request.Status);
        Assert.Empty(request.ReviewNotes);
        Assert.Same(request, Assert.Single(queue.List()));
    }

    [Fact]
    public void SubmitUrlRecordsUrlSourceWithoutDownloadingIt()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);

        DatasheetIntakeSubmissionResult result = queue.SubmitUrl(
            new Uri("https://vendor.example.test/datasheets/ne555.pdf"),
            new DatasheetRequestedComponent(
                ManufacturerPartNumber: "NE555P",
                Manufacturer: "Texas Instruments",
                VendorProductId: null,
                PackageName: "DIP-8",
                Notes: null),
            submittedBy: "component-librarian");

        DatasheetIntakeRequest request = AssertAccepted(result);
        Assert.Equal(DatasheetIntakeSourceType.Url, request.Source.Type);
        Assert.Equal("https://vendor.example.test/datasheets/ne555.pdf", request.Source.Identifier);
        Assert.Equal(DatasheetIntakeStatus.ReviewRequired, request.Status);
        Assert.Equal("NE555P", request.RequestedComponent.ManufacturerPartNumber);
    }

    [Fact]
    public void PersistenceRoundTripsQueueDeterministically()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        string pdfPath = directory.WriteFile("tl431.pdf", "%PDF-1.7");
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);
        DatasheetIntakeRequest request = AssertAccepted(queue.SubmitLocalPdf(
            pdfPath,
            new DatasheetRequestedComponent("TL431AIDBZR", "Texas Instruments", null, "SOT-23-3", "Needs symbol review."),
            submittedBy: "librarian"));

        queue.UpdateStatus(request.Id, DatasheetIntakeStatus.InReview);
        queue.AddReviewNote(
            request.Id,
            DatasheetIntakeReviewNoteSeverity.Warning,
            "Package drawing is ambiguous.");

        string firstWrite = File.ReadAllText(directory.QueuePath);

        DatasheetIntakeQueue reloaded = DatasheetIntakeQueue.Load(
            directory.QueuePath,
            new SequentialDatasheetIntakeRequestIdSource(startAt: 99),
            new FixedDatasheetIntakeClock(SubmittedAt.AddDays(1)));

        string secondWrite = reloaded.Save();
        DatasheetIntakeRequest roundTripped = Assert.Single(reloaded.List());
        Assert.Equal(request.Id, roundTripped.Id);
        Assert.Equal(DatasheetIntakeStatus.InReview, roundTripped.Status);
        Assert.Equal(firstWrite, secondWrite);
        DatasheetIntakeReviewNote note = Assert.Single(roundTripped.ReviewNotes);
        Assert.Equal(DatasheetIntakeReviewNoteSeverity.Warning, note.Severity);
        Assert.Equal("Package drawing is ambiguous.", note.Message);
        Assert.Equal(SubmittedAt, note.RecordedAt);
    }

    [Fact]
    public void SubmissionRejectsDuplicateRequests()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        string pdfPath = directory.WriteFile("lm358.pdf", "%PDF-1.7");
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);
        DatasheetRequestedComponent component = new("LM358P", "Texas Instruments", null, "DIP-8", null);

        AssertAccepted(queue.SubmitLocalPdf(pdfPath, component, submittedBy: "librarian"));

        DatasheetIntakeSubmissionResult duplicate = queue.SubmitLocalPdf(pdfPath, component, submittedBy: "librarian");

        Assert.False(duplicate.Accepted);
        DatasheetIntakeDiagnostic diagnostic = Assert.Single(duplicate.Diagnostics);
        Assert.Equal(DatasheetIntakeDiagnosticCode.DuplicateRequest, diagnostic.Code);
        Assert.Null(duplicate.Request);
        Assert.Single(queue.List());
    }

    [Fact]
    public void SubmissionRejectsInvalidSourcesAndMissingIdentifiers()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        string txtPath = directory.WriteFile("readme.txt", "not a datasheet");
        string missingPdfPath = Path.Combine(directory.Path, "missing.pdf");
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);

        DatasheetIntakeSubmissionResult unsupported = queue.SubmitLocalPdf(
            txtPath,
            new DatasheetRequestedComponent("LM1117", null, null, "SOT-223", null),
            submittedBy: "librarian");
        DatasheetIntakeSubmissionResult missingFile = queue.SubmitLocalPdf(
            missingPdfPath,
            new DatasheetRequestedComponent("LM1117", null, null, "SOT-223", null),
            submittedBy: "librarian");
        DatasheetIntakeSubmissionResult missingIdentifier = queue.SubmitUrl(
            new Uri("https://vendor.example.test/lm1117.pdf"),
            new DatasheetRequestedComponent(null, null, null, "SOT-223", null),
            submittedBy: "librarian");

        AssertDiagnostic(unsupported, DatasheetIntakeDiagnosticCode.UnsupportedLocalFileExtension);
        AssertDiagnostic(missingFile, DatasheetIntakeDiagnosticCode.MissingLocalFile);
        AssertDiagnostic(missingIdentifier, DatasheetIntakeDiagnosticCode.MissingRequestedIdentifier);
        Assert.Empty(queue.List());
    }

    [Fact]
    public void StatusTransitionsAreValidatedAndReviewNotesAreAnnotated()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        DatasheetIntakeQueue queue = CreateQueue(directory.QueuePath);
        DatasheetIntakeRequest request = AssertAccepted(queue.SubmitUrl(
            new Uri("https://vendor.example.test/mcp1700.pdf"),
            new DatasheetRequestedComponent("MCP1700T-3302E/TT", "Microchip", null, "SOT-23-3", null),
            submittedBy: "librarian"));

        queue.UpdateStatus(request.Id, DatasheetIntakeStatus.InReview);
        queue.UpdateStatus(request.Id, DatasheetIntakeStatus.DraftGenerated);
        queue.AddReviewNote(
            request.Id,
            DatasheetIntakeReviewNoteSeverity.Error,
            "Pinout conflicts with package drawing.");

        InvalidOperationException invalidTransition = Assert.Throws<InvalidOperationException>(
            () => queue.UpdateStatus(request.Id, DatasheetIntakeStatus.ReviewRequired));
        DatasheetIntakeRequest updated = Assert.Single(queue.List());
        DatasheetIntakeReviewNote note = Assert.Single(updated.ReviewNotes);
        Assert.Contains("Cannot transition", invalidTransition.Message, StringComparison.Ordinal);
        Assert.Equal(DatasheetIntakeStatus.DraftGenerated, updated.Status);
        Assert.Equal(DatasheetIntakeReviewNoteSeverity.Error, note.Severity);
        Assert.Equal("Pinout conflicts with package drawing.", note.Message);
        Assert.Equal(SubmittedAt, note.RecordedAt);
    }

    private static DatasheetIntakeQueue CreateQueue(string queuePath) =>
        DatasheetIntakeQueue.Load(
            queuePath,
            new SequentialDatasheetIntakeRequestIdSource(startAt: 1),
            new FixedDatasheetIntakeClock(SubmittedAt));

    private static DatasheetIntakeRequest AssertAccepted(DatasheetIntakeSubmissionResult result)
    {
        Assert.True(result.Accepted);
        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Request);
        return result.Request;
    }

    private static void AssertDiagnostic(
        DatasheetIntakeSubmissionResult result,
        DatasheetIntakeDiagnosticCode expectedCode)
    {
        Assert.False(result.Accepted);
        DatasheetIntakeDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Null(result.Request);
    }

    private sealed class TemporaryIntakeDirectory : IDisposable
    {
        private TemporaryIntakeDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public string QueuePath => System.IO.Path.Combine(Path, "intake-queue.json");

        public static TemporaryIntakeDirectory Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dragoncad-intake-" + Guid.NewGuid().ToString("N")));

        public string WriteFile(string fileName, string contents)
        {
            Directory.CreateDirectory(Path);
            string filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
