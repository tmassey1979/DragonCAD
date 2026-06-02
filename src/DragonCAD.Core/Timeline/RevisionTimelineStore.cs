namespace DragonCAD.Core.Timeline;

public sealed class RevisionTimelineStore
{
    private readonly List<RevisionTimelineEvent> events = [];

    public void Append(RevisionTimelineEvent timelineEvent)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);

        events.Add(timelineEvent);
    }

    public IReadOnlyList<RevisionTimelineEvent> List() =>
        events
            .OrderBy(timelineEvent => timelineEvent.OccurredAt)
            .ThenBy(timelineEvent => timelineEvent.Id)
            .ToArray();
}
