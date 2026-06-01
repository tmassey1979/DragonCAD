namespace DragonCAD.Core.Geometry;

public readonly record struct CadPoint(long X, long Y)
{
    public static CadPoint operator +(CadPoint point, CadVector vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    public static CadVector operator -(CadPoint left, CadPoint right) =>
        new(left.X - right.X, left.Y - right.Y);
}

public readonly record struct CadVector(long X, long Y)
{
    public static CadVector operator +(CadVector left, CadVector right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static CadVector operator -(CadVector left, CadVector right) =>
        new(left.X - right.X, left.Y - right.Y);
}

public readonly record struct CadRectangle
{
    public CadRectangle(long left, long top, long right, long bottom)
    {
        if (right < left)
        {
            throw new ArgumentException("Right must be greater than or equal to left.", nameof(right));
        }

        if (bottom < top)
        {
            throw new ArgumentException("Bottom must be greater than or equal to top.", nameof(bottom));
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public long Left { get; }

    public long Top { get; }

    public long Right { get; }

    public long Bottom { get; }

    public long Width => Right - Left;

    public long Height => Bottom - Top;

    public static CadRectangle FromCorners(CadPoint first, CadPoint second) =>
        new(
            Math.Min(first.X, second.X),
            Math.Min(first.Y, second.Y),
            Math.Max(first.X, second.X),
            Math.Max(first.Y, second.Y));

    public bool Contains(CadPoint point) =>
        point.X >= Left &&
        point.X <= Right &&
        point.Y >= Top &&
        point.Y <= Bottom;
}

public readonly record struct CadGrid
{
    public CadGrid(CadVector spacing)
    {
        if (spacing.X <= 0 || spacing.Y <= 0)
        {
            throw new ArgumentException("Grid spacing must be positive.", nameof(spacing));
        }

        Spacing = spacing;
    }

    public CadVector Spacing { get; }

    public CadPoint Snap(CadPoint point) =>
        new(SnapAxis(point.X, Spacing.X), SnapAxis(point.Y, Spacing.Y));

    private static long SnapAxis(long value, long spacing)
    {
        decimal snapped = Math.Round((decimal)value / spacing, MidpointRounding.AwayFromZero) * spacing;
        return (long)snapped;
    }
}

public readonly record struct CadTransform
{
    public CadTransform(long scaleNumerator, long scaleDenominator, CadVector offset)
    {
        if (scaleDenominator == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scaleDenominator), "Scale denominator cannot be zero.");
        }

        ScaleNumerator = scaleNumerator;
        ScaleDenominator = scaleDenominator;
        Offset = offset;
    }

    public long ScaleNumerator { get; }

    public long ScaleDenominator { get; }

    public CadVector Offset { get; }

    public CadPoint Apply(CadPoint point) =>
        new(
            checked((point.X * ScaleNumerator / ScaleDenominator) + Offset.X),
            checked((point.Y * ScaleNumerator / ScaleDenominator) + Offset.Y));

    public CadPoint Invert(CadPoint point)
    {
        if (ScaleNumerator == 0)
        {
            throw new InvalidOperationException("Cannot invert a zero-scale transform.");
        }

        return new CadPoint(
            checked((point.X - Offset.X) * ScaleDenominator / ScaleNumerator),
            checked((point.Y - Offset.Y) * ScaleDenominator / ScaleNumerator));
    }
}

public static class CadRotation
{
    public static int NormalizeDegrees(int degrees)
    {
        if (degrees % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), "Rotation must be a multiple of 90 degrees.");
        }

        return ((degrees % 360) + 360) % 360;
    }

    public static CadPoint RotatePoint(CadPoint point, CadPoint origin, int degrees)
    {
        int normalizedDegrees = NormalizeDegrees(degrees);
        CadVector relative = point - origin;

        return normalizedDegrees switch
        {
            0 => point,
            90 => new CadPoint(origin.X - relative.Y, origin.Y + relative.X),
            180 => new CadPoint(origin.X - relative.X, origin.Y - relative.Y),
            270 => new CadPoint(origin.X + relative.Y, origin.Y - relative.X),
            _ => throw new InvalidOperationException("Normalized rotation must be a quarter turn.")
        };
    }
}

public static class CadUnit
{
    public const long InternalUnitsPerMillimeter = 1_000_000;
    public const long InternalUnitsPerInch = 25_400_000;

    public static long FromMillimeters(decimal millimeters) =>
        checked((long)Math.Round(millimeters * InternalUnitsPerMillimeter, MidpointRounding.AwayFromZero));

    public static long FromInches(decimal inches) =>
        checked((long)Math.Round(inches * InternalUnitsPerInch, MidpointRounding.AwayFromZero));
}
