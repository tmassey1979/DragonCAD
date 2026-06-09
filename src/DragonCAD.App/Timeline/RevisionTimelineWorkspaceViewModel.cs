using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Timeline;

namespace DragonCAD.App.Timeline;

public sealed class RevisionTimelineWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<RevisionTimelineEvent> allEvents;
    private readonly IReadOnlyDictionary<string, RevisionTimelineEvent> eventsById;
    private string selectedAreaFilter = "All";
    private string selectedActorFilter = "All";
    private string affectedObjectFilter = string.Empty;
    private string artifactRefFilter = string.Empty;
    private string gitCommitIdFilter = string.Empty;
    private RevisionTimelineEventDetails? selectedEventDetails;

    private RevisionTimelineWorkspaceViewModel(IReadOnlyList<RevisionTimelineEvent> events)
    {
        allEvents = events;
        eventsById = events.ToDictionary(timelineEvent => timelineEvent.Id.Value, StringComparer.Ordinal);
        AreaFilterOptions = BuildFilterOptions(events.Select(timelineEvent => timelineEvent.Area.ToString()));
        ActorFilterOptions = BuildFilterOptions(events.Select(timelineEvent => timelineEvent.Actor));
        DateGroups = new ObservableCollection<RevisionTimelineDateGroup>();
        ApplyFilters();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<RevisionTimelineDateGroup> DateGroups { get; }

    public IReadOnlyList<string> AreaFilterOptions { get; }

    public IReadOnlyList<string> ActorFilterOptions { get; }

    public string SelectedAreaFilter
    {
        get => selectedAreaFilter;
        set
        {
            string nextValue = NormalizeOption(value);
            if (selectedAreaFilter == nextValue)
            {
                return;
            }

            selectedAreaFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string SelectedActorFilter
    {
        get => selectedActorFilter;
        set
        {
            string nextValue = NormalizeOption(value);
            if (selectedActorFilter == nextValue)
            {
                return;
            }

            selectedActorFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string AffectedObjectFilter
    {
        get => affectedObjectFilter;
        set
        {
            string nextValue = value?.Trim() ?? string.Empty;
            if (affectedObjectFilter == nextValue)
            {
                return;
            }

            affectedObjectFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string ArtifactRefFilter
    {
        get => artifactRefFilter;
        set
        {
            string nextValue = value?.Trim() ?? string.Empty;
            if (artifactRefFilter == nextValue)
            {
                return;
            }

            artifactRefFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string GitCommitIdFilter
    {
        get => gitCommitIdFilter;
        set
        {
            string nextValue = value?.Trim() ?? string.Empty;
            if (gitCommitIdFilter == nextValue)
            {
                return;
            }

            gitCommitIdFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public RevisionTimelineEventDetails? SelectedEventDetails
    {
        get => selectedEventDetails;
        private set
        {
            if (selectedEventDetails == value)
            {
                return;
            }

            selectedEventDetails = value;
            OnPropertyChanged();
        }
    }

    public bool IsEmpty => DateGroups.Count == 0;

    public string EmptyStateMessage => "No revision timeline events match the current filters.";

    public static RevisionTimelineWorkspaceViewModel FromEvents(IEnumerable<RevisionTimelineEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return new RevisionTimelineWorkspaceViewModel(OrderEvents(events).ToArray());
    }

    public void SelectEvent(string eventId)
    {
        string normalized = eventId?.Trim() ?? string.Empty;
        SelectedEventDetails = eventsById.TryGetValue(normalized, out RevisionTimelineEvent? timelineEvent)
            && MatchesFilters(timelineEvent)
                ? RevisionTimelineEventDetails.FromEvent(timelineEvent)
                : null;
    }

    private void ApplyFilters()
    {
        RevisionTimelineDateGroup[] groups = allEvents
            .Where(MatchesFilters)
            .GroupBy(timelineEvent => DateOnly.FromDateTime(timelineEvent.OccurredAt.UtcDateTime.Date))
            .OrderByDescending(group => group.Key)
            .Select(group => RevisionTimelineDateGroup.FromEvents(group.Key, group))
            .ToArray();

        DateGroups.Clear();
        foreach (RevisionTimelineDateGroup group in groups)
        {
            DateGroups.Add(group);
        }

        if (SelectedEventDetails is not null && !allEvents.Any(timelineEvent =>
                timelineEvent.Id.Value == SelectedEventDetails.Id && MatchesFilters(timelineEvent)))
        {
            SelectedEventDetails = null;
        }

        OnPropertyChanged(nameof(IsEmpty));
    }

    private bool MatchesFilters(RevisionTimelineEvent timelineEvent)
    {
        if (selectedAreaFilter != "All" && !string.Equals(timelineEvent.Area.ToString(), selectedAreaFilter, StringComparison.Ordinal))
        {
            return false;
        }

        if (selectedActorFilter != "All" && !string.Equals(timelineEvent.Actor, selectedActorFilter, StringComparison.Ordinal))
        {
            return false;
        }

        if (!MatchesAny(affectedObjectFilter, timelineEvent.ChangedObjectRefs.Select(FormatRef)))
        {
            return false;
        }

        if (!MatchesAny(artifactRefFilter, timelineEvent.ArtifactRefs.Select(FormatRef)))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(gitCommitIdFilter)
            || (timelineEvent.GitCommitId is { } gitCommitId
                && gitCommitId.Value.Contains(gitCommitIdFilter, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<RevisionTimelineEvent> OrderEvents(IEnumerable<RevisionTimelineEvent> events) =>
        events
            .OrderByDescending(timelineEvent => timelineEvent.OccurredAt)
            .ThenBy(timelineEvent => timelineEvent.Id.Value, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> BuildFilterOptions(IEnumerable<string> values) =>
        new[] { "All" }
            .Concat(values.Where(value => value.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            .ToArray();

    private static bool MatchesAny(string filter, IEnumerable<string> values) =>
        string.IsNullOrWhiteSpace(filter)
        || values.Any(value => value.Contains(filter, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeOption(string? value) => string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();

    private static string FormatRef<T>(T reference) => reference?.ToString() ?? string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record RevisionTimelineDateGroup(
    DateOnly Date,
    string DateDisplay,
    IReadOnlyList<RevisionTimelineAreaGroup> AreaGroups)
{
    public static RevisionTimelineDateGroup FromEvents(DateOnly date, IEnumerable<RevisionTimelineEvent> events) =>
        new(
            date,
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            events
                .GroupBy(timelineEvent => timelineEvent.Area)
                .OrderBy(group => group.Key)
                .Select(group => RevisionTimelineAreaGroup.FromEvents(group.Key, group))
                .ToArray());
}

public sealed record RevisionTimelineAreaGroup(
    RevisionTimelineArea Area,
    string AreaDisplay,
    IReadOnlyList<RevisionTimelineEventRow> Events)
{
    public static RevisionTimelineAreaGroup FromEvents(RevisionTimelineArea area, IEnumerable<RevisionTimelineEvent> events) =>
        new(
            area,
            area.ToString(),
            events
                .OrderByDescending(timelineEvent => timelineEvent.OccurredAt)
                .ThenBy(timelineEvent => timelineEvent.Id.Value, StringComparer.Ordinal)
                .Select(RevisionTimelineEventRow.FromEvent)
                .ToArray());
}

public sealed record RevisionTimelineEventRow(
    string Id,
    DateTimeOffset OccurredAt,
    string TimestampDisplay,
    string Actor,
    string Area,
    string Summary,
    string GitCommitId)
{
    public static RevisionTimelineEventRow FromEvent(RevisionTimelineEvent timelineEvent)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        return new RevisionTimelineEventRow(
            timelineEvent.Id.Value,
            timelineEvent.OccurredAt,
            timelineEvent.OccurredAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture),
            timelineEvent.Actor,
            timelineEvent.Area.ToString(),
            timelineEvent.Summary,
            timelineEvent.GitCommitId?.Value ?? string.Empty);
    }
}

public sealed record RevisionTimelineEventDetails(
    string Id,
    string Summary,
    IReadOnlyList<string> ChangedObjectRefs,
    IReadOnlyList<string> ArtifactRefs,
    IReadOnlyList<string> Diagnostics)
{
    public static RevisionTimelineEventDetails FromEvent(RevisionTimelineEvent timelineEvent)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        return new RevisionTimelineEventDetails(
            timelineEvent.Id.Value,
            timelineEvent.Summary,
            timelineEvent.ChangedObjectRefs.Select(reference => reference.ToString()).ToArray(),
            timelineEvent.ArtifactRefs.Select(reference => reference.ToString()).ToArray(),
            BuildDiagnostics(timelineEvent));
    }

    private static IReadOnlyList<string> BuildDiagnostics(RevisionTimelineEvent timelineEvent) =>
        timelineEvent.GitCommitId is { } gitCommitId
            ? [$"Git commit {gitCommitId.Value} is linked to this timeline event."]
            : ["No Git commit id is linked to this timeline event."];
}
