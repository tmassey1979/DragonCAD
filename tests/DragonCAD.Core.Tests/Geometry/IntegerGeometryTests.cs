using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Tests.Geometry;

public sealed class IntegerGeometryTests
{
    [Fact]
    public void PointsTranslateByVectorsAndSubtractIntoVectors()
    {
        var point = new CadPoint(10, 20);
        var vector = new CadVector(-3, 5);

        Assert.Equal(new CadPoint(7, 25), point + vector);
        Assert.Equal(new CadVector(3, -5), point - new CadPoint(7, 25));
    }

    [Fact]
    public void RectanglesNormalizeBoundsAndContainEdges()
    {
        CadRectangle bounds = CadRectangle.FromCorners(new CadPoint(20, 10), new CadPoint(5, 30));

        Assert.Equal(5, bounds.Left);
        Assert.Equal(10, bounds.Top);
        Assert.Equal(20, bounds.Right);
        Assert.Equal(30, bounds.Bottom);
        Assert.True(bounds.Contains(new CadPoint(5, 10)));
        Assert.True(bounds.Contains(new CadPoint(20, 30)));
        Assert.False(bounds.Contains(new CadPoint(21, 30)));
    }

    [Fact]
    public void GridSnappingUsesNearestIntegerGridPoint()
    {
        CadGrid grid = new(new CadVector(10, 10));

        Assert.Equal(new CadPoint(20, 30), grid.Snap(new CadPoint(16, 34)));
        Assert.Equal(new CadPoint(-20, -30), grid.Snap(new CadPoint(-16, -34)));
    }

    [Fact]
    public void CadUnitsConvertMillimetersToIntegerMicrounits()
    {
        Assert.Equal(1_000_000, CadUnit.FromMillimeters(1));
        Assert.Equal(2_540_000, CadUnit.FromInches(0.1m));
    }

    [Fact]
    public void TransformsApplyScaleAndOffsetUsingIntegerMath()
    {
        CadTransform transform = new(2, 1, new CadVector(5, -5));

        Assert.Equal(new CadPoint(25, 35), transform.Apply(new CadPoint(10, 20)));
        Assert.Equal(new CadPoint(10, 20), transform.Invert(new CadPoint(25, 35)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 90)]
    [InlineData(360, 0)]
    [InlineData(450, 90)]
    [InlineData(-90, 270)]
    [InlineData(-450, 270)]
    public void RotationDegreesNormalizeToPositiveQuarterTurns(int degrees, int expectedDegrees)
    {
        Assert.Equal(expectedDegrees, CadRotation.NormalizeDegrees(degrees));
    }

    [Theory]
    [InlineData(1, 2, 10, 20, 90, 28, 11)]
    [InlineData(1, 2, 10, 20, 180, 19, 38)]
    [InlineData(1, 2, 10, 20, 270, -8, 29)]
    [InlineData(1, 2, 10, 20, -90, -8, 29)]
    public void RotatePointAroundOriginUsesIntegerQuarterTurns(
        long pointX,
        long pointY,
        long originX,
        long originY,
        int degrees,
        long expectedX,
        long expectedY)
    {
        CadPoint rotated = CadRotation.RotatePoint(
            new CadPoint(pointX, pointY),
            new CadPoint(originX, originY),
            degrees);

        Assert.Equal(new CadPoint(expectedX, expectedY), rotated);
    }

    [Fact]
    public void RotationRejectsNonQuarterTurnDegrees()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CadRotation.NormalizeDegrees(45));
    }
}
