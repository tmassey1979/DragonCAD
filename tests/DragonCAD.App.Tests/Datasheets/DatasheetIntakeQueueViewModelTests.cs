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
}
