using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Tests.Components.Identity;

public sealed class ComponentIdentityTests
{
    [Fact]
    public void ComponentIdsRejectEmptyWhitespaceAndControlCharacters()
    {
        Assert.Throws<ArgumentException>(() => new ComponentId(""));
        Assert.Throws<ArgumentException>(() => new ComponentId("   "));
        Assert.Throws<ArgumentException>(() => new ComponentId("dragon\npart"));
    }

    [Fact]
    public void ComponentIdsNormalizeTrimmedTextAndCompareByOrdinalValue()
    {
        var first = new ComponentId(" dragon:resistor/0603 ");
        var second = new ComponentId("dragon:resistor/0603");

        Assert.Equal("dragon:resistor/0603", first.Value);
        Assert.Equal(first, second);
        Assert.Equal("dragon:resistor/0603", first.ToString());
    }

    [Fact]
    public void ComponentIdsSortDeterministicallyByOrdinalValue()
    {
        ComponentId[] ids =
        [
            new("dragon:z"),
            new("dragon:A"),
            new("dragon:a")
        ];

        Array.Sort(ids);

        Assert.Equal(
            ["dragon:A", "dragon:a", "dragon:z"],
            ids.Select(id => id.Value));
    }

    [Fact]
    public void AllComponentAssetIdsShareValidationRules()
    {
        Assert.Equal("symbol:opamp", new ComponentSymbolId(" symbol:opamp ").Value);
        Assert.Equal("footprint:soic-8", new ComponentFootprintId("footprint:soic-8").Value);
        Assert.Equal("variant:ti", new ComponentVariantId("variant:ti").Value);
        Assert.Equal("pin:in+", new ComponentPinId("pin:in+").Value);
        Assert.Equal("pad:1", new ComponentPadId("pad:1").Value);

        Assert.Throws<ArgumentException>(() => new ComponentSymbolId("\t"));
        Assert.Throws<ArgumentException>(() => new ComponentFootprintId("bad\rid"));
        Assert.Throws<ArgumentException>(() => new ComponentVariantId("bad\nid"));
        Assert.Throws<ArgumentException>(() => new ComponentPinId("bad\u0000id"));
        Assert.Throws<ArgumentException>(() => new ComponentPadId(""));
    }
}
