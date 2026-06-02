namespace DragonCAD.Core.Timeline;

public readonly record struct RevisionTimelineEventId(string Value) : IComparable<RevisionTimelineEventId>
{
    public static RevisionTimelineEventId From(string value) => new(Required(value, nameof(value)));

    public int CompareTo(RevisionTimelineEventId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;

    internal static string Required(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);

        string normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return normalized;
    }
}

public readonly record struct GitCommitId(string Value) : IComparable<GitCommitId>
{
    public static GitCommitId From(string value) => new(RevisionTimelineEventId.Required(value, nameof(value)));

    public int CompareTo(GitCommitId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public override string ToString() => Value;
}

public enum RevisionObjectRefKind
{
    Project,
    LibraryComponent,
    Board,
    Order,
}

public readonly record struct RevisionObjectRef(RevisionObjectRefKind Kind, string Value) : IComparable<RevisionObjectRef>
{
    public static RevisionObjectRef Project(string value) => Create(RevisionObjectRefKind.Project, value);

    public static RevisionObjectRef LibraryComponent(string value) => Create(RevisionObjectRefKind.LibraryComponent, value);

    public static RevisionObjectRef Board(string value) => Create(RevisionObjectRefKind.Board, value);

    public static RevisionObjectRef Order(string value) => Create(RevisionObjectRefKind.Order, value);

    public int CompareTo(RevisionObjectRef other)
    {
        int kindComparison = Kind.CompareTo(other.Kind);
        return kindComparison != 0
            ? kindComparison
            : string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    public override string ToString() => $"{Kind}:{Value}";

    private static RevisionObjectRef Create(RevisionObjectRefKind kind, string value) =>
        new(kind, RevisionTimelineEventId.Required(value, nameof(value)));
}

public enum RevisionArtifactRefKind
{
    Document,
    ImportSource,
    PromotionRecord,
    FabricationPackage,
    OrderReview,
}

public readonly record struct RevisionArtifactRef(RevisionArtifactRefKind Kind, string Value) : IComparable<RevisionArtifactRef>
{
    public static RevisionArtifactRef Document(string value) => Create(RevisionArtifactRefKind.Document, value);

    public static RevisionArtifactRef ImportSource(string value) => Create(RevisionArtifactRefKind.ImportSource, value);

    public static RevisionArtifactRef PromotionRecord(string value) => Create(RevisionArtifactRefKind.PromotionRecord, value);

    public static RevisionArtifactRef FabricationPackage(string value) => Create(RevisionArtifactRefKind.FabricationPackage, value);

    public static RevisionArtifactRef OrderReview(string value) => Create(RevisionArtifactRefKind.OrderReview, value);

    public int CompareTo(RevisionArtifactRef other)
    {
        int kindComparison = Kind.CompareTo(other.Kind);
        return kindComparison != 0
            ? kindComparison
            : string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    public override string ToString() => $"{Kind}:{Value}";

    private static RevisionArtifactRef Create(RevisionArtifactRefKind kind, string value) =>
        new(kind, RevisionTimelineEventId.Required(value, nameof(value)));
}
