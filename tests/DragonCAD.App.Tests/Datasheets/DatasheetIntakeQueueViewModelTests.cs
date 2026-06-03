using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Tests.Datasheets;

public sealed class DatasheetIntakeQueueViewModelTests
{
    [Fact]
    public void SubmitLocalPdfAddsReviewRequiredIntakeItemWithProvenance()
    {
        string pdfPath = Path.Combine(Path.GetTempPath(), $"lm7805-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.7");
        var clock = new FixedDatasheetIntakeClock(new DateTimeOffset(2026, 6, 1, 8, 30, 0, TimeSpan.Zero));
        DatasheetIntakeQueueViewModel queue = new(clock);

        DatasheetIntakeSubmissionResult result = queue.Submit(new DatasheetIntakeRequest(
            SourceIdentifier: pdfPath,
            SubmittedActor: "Alex Dragon",
            ManufacturerPartNumber: "LM7805CT",
            VendorProductId: "DigiKey-296-1389-5-ND",
            PackageName: "TO-220-3",
            SourceNotes: "TI datasheet from local archive"));

        Assert.True(result.Accepted);
        Assert.Empty(result.Diagnostics);
        DatasheetIntakeItem item = Assert.Single(queue.Items);
        Assert.Equal(DatasheetIntakeSourceType.LocalPdf, item.SourceType);
        Assert.Equal(pdfPath, item.SourceIdentifier);
        Assert.Equal("Alex Dragon", item.SubmittedActor);
        Assert.Equal("LM7805CT", item.ManufacturerPartNumber);
        Assert.Equal("DigiKey-296-1389-5-ND", item.VendorProductId);
        Assert.Equal("TO-220-3", item.PackageName);
        Assert.Equal("TI datasheet from local archive", item.SourceNotes);
        Assert.Equal(clock.UtcNow, item.SubmittedAt);
        Assert.Equal(DatasheetIntakeReviewState.ReviewRequired, item.ReviewState);
        Assert.Equal("Local PDF", item.SourceTypeDisplay);
        Assert.Equal("Review required", item.ReviewStateDisplay);
        Assert.Equal("1 datasheet intake item pending review", queue.Summary);
    }

    [Fact]
    public void SubmitUrlAddsReviewRequiredIntakeItemWithoutNetworkFetch()
    {
        var clock = new FixedDatasheetIntakeClock(new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero));
        DatasheetIntakeQueueViewModel queue = new(clock);

        DatasheetIntakeSubmissionResult result = queue.Submit(new DatasheetIntakeRequest(
            SourceIdentifier: "https://www.ti.com/lit/ds/symlink/lm7805.pdf",
            SubmittedActor: "Alex Dragon",
            ManufacturerPartNumber: "LM7805CT",
            VendorProductId: "",
            PackageName: "TO-220-3",
            SourceNotes: "Vendor URL only"));

        Assert.True(result.Accepted);
        DatasheetIntakeItem item = Assert.Single(queue.Items);
        Assert.Equal(DatasheetIntakeSourceType.Url, item.SourceType);
        Assert.Equal("www.ti.com/lit/ds/symlink/lm7805.pdf", item.SourceDisplay);
        Assert.False(item.WasFetched);
    }

    [Fact]
    public void SubmitRejectsMissingIdentifierUnsupportedTypeMissingFileAndDuplicates()
    {
        string txtPath = Path.Combine(Path.GetTempPath(), $"not-datasheet-{Guid.NewGuid():N}.txt");
        File.WriteAllText(txtPath, "not a pdf");
        string missingPdfPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pdf");
        string pdfPath = Path.Combine(Path.GetTempPath(), $"ne555-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.7");
        DatasheetIntakeQueueViewModel queue = new(new FixedDatasheetIntakeClock(DateTimeOffset.UnixEpoch));

        DatasheetIntakeSubmissionResult missing = queue.Submit(new DatasheetIntakeRequest("", "Alex", "", "", "", ""));
        DatasheetIntakeSubmissionResult unsupported = queue.Submit(new DatasheetIntakeRequest(txtPath, "Alex", "", "", "", ""));
        DatasheetIntakeSubmissionResult missingFile = queue.Submit(new DatasheetIntakeRequest(missingPdfPath, "Alex", "", "", "", ""));
        DatasheetIntakeSubmissionResult firstPdf = queue.Submit(new DatasheetIntakeRequest(pdfPath, "Alex", "", "", "", ""));
        DatasheetIntakeSubmissionResult duplicatePdf = queue.Submit(new DatasheetIntakeRequest(pdfPath, "Alex", "", "", "", ""));

        Assert.False(missing.Accepted);
        Assert.Contains(missing.Diagnostics, diagnostic => diagnostic.Code == "missing-source-identifier");
        Assert.False(unsupported.Accepted);
        Assert.Contains(unsupported.Diagnostics, diagnostic => diagnostic.Code == "unsupported-datasheet-source");
        Assert.False(missingFile.Accepted);
        Assert.Contains(missingFile.Diagnostics, diagnostic => diagnostic.Code == "local-file-not-found");
        Assert.True(firstPdf.Accepted);
        Assert.False(duplicatePdf.Accepted);
        Assert.Contains(duplicatePdf.Diagnostics, diagnostic => diagnostic.Code == "duplicate-datasheet-intake");
        Assert.Single(queue.Items);
    }

    [Fact]
    public void SubmitRejectsInvalidDatasheetUrlsWithUrlDiagnostic()
    {
        DatasheetIntakeQueueViewModel queue = new(new FixedDatasheetIntakeClock(DateTimeOffset.UnixEpoch));

        DatasheetIntakeSubmissionResult invalidUrl = queue.Submit(new DatasheetIntakeRequest(
            "ftp://vendor.example.test/lm1117.pdf",
            "Alex",
            "LM1117",
            "",
            "SOT-223",
            ""));

        Assert.False(invalidUrl.Accepted);
        DatasheetIntakeDiagnostic diagnostic = Assert.Single(invalidUrl.Diagnostics);
        Assert.Equal("invalid-datasheet-url", diagnostic.Code);
        Assert.Empty(queue.Items);
    }

    [Fact]
    public void SnapshotJsonSerializesIntakeQueueDeterministicallyWithoutCandidateOrTrustedLibraryState()
    {
        using TemporaryIntakeDirectory directory = TemporaryIntakeDirectory.Create();
        string pdfPath = directory.WriteFile("lm7805.pdf", "%PDF-1.7");
        var clock = new FixedDatasheetIntakeClock(new DateTimeOffset(2026, 6, 1, 8, 30, 0, TimeSpan.Zero));
        DatasheetIntakeQueueViewModel queue = new(clock);
        queue.Submit(new DatasheetIntakeRequest(
            "https://www.ti.com/lit/ds/symlink/ne555.pdf",
            "Alex",
            "NE555P",
            "",
            "DIP-8",
            "Vendor URL"));
        queue.Submit(new DatasheetIntakeRequest(
            pdfPath,
            "Alex",
            "LM7805CT",
            "296-1415-5-ND",
            "TO-220-3",
            "Local archive"));

        string snapshot = queue.SnapshotJson();

        Assert.Equal(
            """
            [
              {
                "sourceType": "url",
                "sourceIdentifier": "https://www.ti.com/lit/ds/symlink/ne555.pdf",
                "submittedAt": "2026-06-01T08:30:00+00:00",
                "manufacturerPartNumber": "NE555P",
                "vendorProductId": "",
                "packageName": "DIP-8",
                "sourceNotes": "Vendor URL",
                "reviewState": "reviewRequired"
              },
              {
                "sourceType": "localPdf",
                "sourceIdentifier": "__PDF_PATH__",
                "submittedAt": "2026-06-01T08:30:00+00:00",
                "manufacturerPartNumber": "LM7805CT",
                "vendorProductId": "296-1415-5-ND",
                "packageName": "TO-220-3",
                "sourceNotes": "Local archive",
                "reviewState": "reviewRequired"
              }
            ]
            """.Replace("__PDF_PATH__", pdfPath.Replace("\\", "\\\\", StringComparison.Ordinal), StringComparison.Ordinal),
            snapshot,
            ignoreLineEndingDifferences: true);
        Assert.DoesNotContain("generated", snapshot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("trusted", snapshot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FilterAndSelectionUseIntakeReviewStateWithoutMutatingTrustedLibrary()
    {
        string pdfPath = Path.Combine(Path.GetTempPath(), $"lm7805-{Guid.NewGuid():N}.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.7");
        DatasheetIntakeQueueViewModel queue = new(new FixedDatasheetIntakeClock(DateTimeOffset.UnixEpoch));
        queue.Submit(new DatasheetIntakeRequest(pdfPath, "Alex", "LM7805CT", "", "TO-220-3", ""));

        queue.SelectedReviewStateFilter = DatasheetIntakeReviewStateFilter.ReviewRequired;

        DatasheetIntakeItem item = Assert.Single(queue.Items);
        Assert.Equal(item, queue.SelectedItem);
        Assert.False(item.MutatedTrustedLibrary);
        Assert.Equal(["All", "Review Required", "Linked", "Rejected"], queue.ReviewStateFilterOptions);
    }

    private sealed class FixedDatasheetIntakeClock(DateTimeOffset utcNow) : IDatasheetIntakeClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TemporaryIntakeDirectory : IDisposable
    {
        private TemporaryIntakeDirectory(string path)
        {
            Path = path;
        }

        private string Path { get; }

        public static TemporaryIntakeDirectory Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dragoncad-app-intake-" + Guid.NewGuid().ToString("N")));

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
