using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using DragonCAD.ComponentIntelligence.Datasheets;
using DragonCAD.ComponentIntelligence.Datasheets.Linking;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.App.Datasheets;

public sealed class DatasheetLinkReviewRow : INotifyPropertyChanged
{
    private DatasheetLinkReviewState reviewState = DatasheetLinkReviewState.Pending;

    private DatasheetLinkReviewRow(
        string componentName,
        string manufacturerPartNumber,
        string decisionDisplay,
        string targetComponentId,
        string matchBasis,
        string warningDisplay)
    {
        ComponentName = componentName;
        ManufacturerPartNumber = manufacturerPartNumber;
        DecisionDisplay = decisionDisplay;
        TargetComponentId = targetComponentId;
        MatchBasis = matchBasis;
        WarningDisplay = warningDisplay;
        ApproveCommand = new DelegateCommand(Approve, () => CanApprove);
        RejectCommand = new DelegateCommand(Reject, () => CanReject);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ComponentName { get; }

    public string ManufacturerPartNumber { get; }

    public string DecisionDisplay { get; }

    public string TargetComponentId { get; }

    public string MatchBasis { get; }

    public string WarningDisplay { get; }

    public DelegateCommand ApproveCommand { get; }

    public DelegateCommand RejectCommand { get; }

    public bool CanApprove => reviewState == DatasheetLinkReviewState.Pending;

    public bool CanReject => reviewState == DatasheetLinkReviewState.Pending;

    public string ApproveLabel => "Approve Link";

    public string RejectLabel => "Reject";

    public string ReviewStateDisplay =>
        reviewState switch
        {
            DatasheetLinkReviewState.Pending => "Pending Review",
            DatasheetLinkReviewState.Approved => "Approved for Promotion",
            DatasheetLinkReviewState.Staged => "Staged for Promotion",
            DatasheetLinkReviewState.Rejected => "Rejected",
            _ => throw new InvalidOperationException($"Unsupported datasheet link review state {reviewState}.")
        };

    public string ReviewNote =>
        reviewState switch
        {
            DatasheetLinkReviewState.Pending => "Waiting for human review before trusted library promotion.",
            DatasheetLinkReviewState.Approved => "Approved locally; trusted library promotion is still pending.",
            DatasheetLinkReviewState.Staged => "Staged in a local promotion record; trusted library write is still pending.",
            DatasheetLinkReviewState.Rejected => "Rejected before trusted library promotion.",
            _ => throw new InvalidOperationException($"Unsupported datasheet link review state {reviewState}.")
        };

    public bool IsApprovedForPromotion => reviewState == DatasheetLinkReviewState.Approved;

    public bool IsSafeExistingComponentLink =>
        DecisionDisplay == "Link Existing Component" &&
        WarningDisplay == "No review warnings" &&
        TargetComponentId != "New component required";

    public static DatasheetLinkReviewRow FromPlan(
        string componentName,
        ExtractedDatasheetFacts facts,
        DatasheetComponentLinkPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(plan);

        string targetComponentId = plan.ComponentId?.Value
            ?? (plan.CandidateComponentIds.Count == 0
                ? "New component required"
                : string.Join(", ", plan.CandidateComponentIds.Select(id => id.Value)));

        string matchBasis = plan.Decision switch
        {
            DatasheetComponentLinkDecision.LinkExistingComponent => $"Matched MPN {facts.ManufacturerPartNumber}",
            DatasheetComponentLinkDecision.DuplicateExistingComponents => $"Multiple matches for MPN {facts.ManufacturerPartNumber}",
            DatasheetComponentLinkDecision.NeedsNewComponent => "No trusted existing component matched",
            _ => throw new InvalidOperationException($"Unsupported datasheet link decision {plan.Decision}.")
        };

        return new DatasheetLinkReviewRow(
            componentName,
            facts.ManufacturerPartNumber,
            FormatDecision(plan.Decision),
            targetComponentId,
            matchBasis,
            plan.ReviewWarnings.Count == 0
                ? "No review warnings"
                : string.Join("; ", plan.ReviewWarnings.Select(warning => warning.Message)));
    }

    public void MarkStaged()
    {
        if (reviewState == DatasheetLinkReviewState.Approved)
        {
            SetReviewState(DatasheetLinkReviewState.Staged);
        }
    }

    private void Approve()
    {
        if (!CanApprove)
        {
            return;
        }

        SetReviewState(DatasheetLinkReviewState.Approved);
    }

    private void Reject()
    {
        if (!CanReject)
        {
            return;
        }

        SetReviewState(DatasheetLinkReviewState.Rejected);
    }

    private void SetReviewState(DatasheetLinkReviewState value)
    {
        if (reviewState == value)
        {
            return;
        }

        reviewState = value;
        OnPropertyChanged(nameof(CanApprove));
        OnPropertyChanged(nameof(CanReject));
        OnPropertyChanged(nameof(ReviewStateDisplay));
        OnPropertyChanged(nameof(ReviewNote));
        OnPropertyChanged(nameof(IsApprovedForPromotion));
        ApproveCommand.RaiseCanExecuteChanged();
        RejectCommand.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatDecision(DatasheetComponentLinkDecision decision) =>
        decision switch
        {
            DatasheetComponentLinkDecision.LinkExistingComponent => "Link Existing Component",
            DatasheetComponentLinkDecision.DuplicateExistingComponents => "Duplicate Existing Components",
            DatasheetComponentLinkDecision.NeedsNewComponent => "Needs New Component",
            _ => throw new InvalidOperationException($"Unsupported datasheet link decision {decision}.")
        };
}

public enum DatasheetLinkReviewState
{
    Pending,
    Approved,
    Staged,
    Rejected,
}

public sealed record DatasheetLinkPromotionQueueRow(
    string ComponentName,
    string TargetComponentId,
    string DecisionDisplay,
    string PromotionStatus)
{
    public static DatasheetLinkPromotionQueueRow FromReviewRow(DatasheetLinkReviewRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new DatasheetLinkPromotionQueueRow(
            row.ComponentName,
            row.TargetComponentId,
            row.DecisionDisplay,
            "Ready for trusted-library promotion");
    }
}

public sealed record DatasheetLinkPromotionRecordViewModel(
    string RecordId,
    string Status,
    IReadOnlyList<DatasheetLinkPromotionQueueRow> Rows)
{
    public string RowSummary =>
        Rows.Count == 1 ? "1 approved link staged" : $"{Rows.Count} approved links staged";

    public string ExportFileName => $"datasheet-promotion-{RecordId}.json";

    public string ExportManifestFileName => $"datasheet-promotion-{RecordId}.manifest.json";

    public string ExportAuditFileName => $"datasheet-promotion-{RecordId}.audit.json";

    public string ReadinessStatus => "Blocked: trusted-library write pending";

    public IReadOnlyList<DatasheetPromotionChecklistRow> Checklist =>
    [
        new("Promotion JSON artifact", "Preview only"),
        new("Trusted library write", "Pending implementation"),
        new("Audit entry", "Pending implementation")
    ];

    public string ExportJsonPreview =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{RecordId}\",",
                $"  \"status\": \"{Status}\",",
                "  \"trustedLibraryWrite\": \"pending\",",
                "  \"rows\": [",
                .. Rows.SelectMany((row, index) => FormatRow(row, index == Rows.Count - 1)),
                "  ]",
                "}"
            ]);

    public int ExportLineCount => ExportJsonPreview.Split(Environment.NewLine, StringSplitOptions.None).Length;

    public string ExportManifestJsonPreview =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{RecordId}\",",
                $"  \"promotionArtifact\": \"{ExportFileName}\",",
                $"  \"promotionArtifactSha256\": \"{ComputeSha256(ExportJsonPreview)}\",",
                $"  \"rowCount\": {Rows.Count},",
                "  \"trustedLibraryWrite\": \"pending\",",
                "  \"auditEntry\": \"pending\",",
                $"  \"auditArtifact\": \"{ExportAuditFileName}\",",
                $"  \"auditArtifactSha256\": \"{ComputeSha256(ExportAuditJsonPreview)}\"",
                "}"
            ]);

    public string ExportAuditJsonPreview =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{RecordId}\",",
                "  \"event\": \"datasheetPromotionPreviewSaved\",",
                $"  \"promotionArtifact\": \"{ExportFileName}\",",
                $"  \"manifestArtifact\": \"{ExportManifestFileName}\",",
                $"  \"reviewedRows\": {Rows.Count},",
                "  \"trustedLibraryMutation\": \"not-performed\"",
                "}"
            ]);

    private static IEnumerable<string> FormatRow(DatasheetLinkPromotionQueueRow row, bool isLast)
    {
        yield return "    {";
        yield return $"      \"componentName\": \"{EscapeJson(row.ComponentName)}\",";
        yield return $"      \"targetComponentId\": \"{EscapeJson(row.TargetComponentId)}\",";
        yield return $"      \"decision\": \"{EscapeJson(row.DecisionDisplay)}\",";
        yield return $"      \"promotionStatus\": \"{EscapeJson(row.PromotionStatus)}\"";
        yield return isLast ? "    }" : "    },";
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string ComputeSha256(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}

public sealed record DatasheetPromotionChecklistRow(string Label, string Status);

public static class DatasheetLinkReviewSeedData
{
    public static IReadOnlyList<DatasheetLinkReviewRow> CreateRows()
    {
        var planner = new DatasheetComponentLinkPlanner();
        ExtractedDatasheetFacts lm7805Facts = CreateFacts(
            manufacturer: "Texas Instruments",
            manufacturerPartNumber: "LM7805CT",
            packageName: "TO-220-3",
            pinFacts:
            [
                new DatasheetPinFact("1", "IN", "Power input"),
                new DatasheetPinFact("2", "GND", "Ground"),
                new DatasheetPinFact("3", "OUT", "Regulated output"),
            ],
            dimensions: new PackageDimensionFacts(10_160, 4_570, 15_240));
        ExtractedDatasheetFacts esp32Facts = CreateFacts(
            manufacturer: "Espressif",
            manufacturerPartNumber: "ESP32-WROOM-32",
            packageName: "DevKit carrier",
            pinFacts:
            [
                new DatasheetPinFact("1", "3V3", "3.3 V supply"),
                new DatasheetPinFact("2", "EN", "Enable"),
                new DatasheetPinFact("3", "IO0", "Boot strapping GPIO"),
            ],
            dimensions: new PackageDimensionFacts(18_000, 25_500, 3_200));

        return
        [
            DatasheetLinkReviewRow.FromPlan(
                "LM7805 5V Linear Regulator",
                lm7805Facts,
                planner.Plan(
                    lm7805Facts,
                    [
                        new ExistingComponentLinkCandidate(
                            new ComponentId("dragon:lm7805"),
                            "Texas Instruments",
                            "LM7805CT",
                            "TO-220-3"),
                    ])),
            DatasheetLinkReviewRow.FromPlan(
                "ESP32 DevKit Module",
                esp32Facts,
                planner.Plan(
                    esp32Facts,
                    [
                        new ExistingComponentLinkCandidate(
                            new ComponentId("dragon:esp32-devkit"),
                            "Espressif",
                            "ESP32-DEVKITC",
                            "Dev board"),
                    ])),
        ];
    }

    private static ExtractedDatasheetFacts CreateFacts(
        string manufacturer,
        string manufacturerPartNumber,
        string packageName,
        IReadOnlyList<DatasheetPinFact> pinFacts,
        PackageDimensionFacts dimensions) =>
        new(
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            Description: "Datasheet extracted component",
            PackageName: packageName,
            PinFacts: pinFacts,
            PackageDimensions: dimensions);
}
