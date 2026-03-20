using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class MxmlParserTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void ParseValue_Booleans(string input, bool expected)
    {
        var result = MxmlParser.ParseValue(input);
        Assert.IsType<bool>(result);
        Assert.Equal(expected, (bool)result);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("-1", -1)]
    [InlineData("1124", 1124)]
    public void ParseValue_Integers(string input, int expected)
    {
        var result = MxmlParser.ParseValue(input);
        Assert.IsType<int>(result);
        Assert.Equal(expected, (int)result);
    }

    [Theory]
    [InlineData("0.793", 0.793)]
    [InlineData("3.0", 3.0)]
    [InlineData("-1.5", -1.5)]
    public void ParseValue_Floats(string input, double expected)
    {
        var result = MxmlParser.ParseValue(input);
        Assert.IsType<double>(result);
        Assert.Equal(expected, (double)result, precision: 10);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData("GcRarity", "GcRarity")]
    public void ParseValue_Strings(string input, string expected)
    {
        var result = MxmlParser.ParseValue(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CAKE OF GLASS", "Cake of Glass")]
    [InlineData("THE QUICK BROWN FOX", "The Quick Brown Fox")]
    [InlineData("ITEMS FOR SALE", "Items for Sale")]
    [InlineData("A BIG THING", "A Big Thing")]
    public void TitleCaseName_LowercaseConjunctions(string input, string expected)
    {
        Assert.Equal(expected, MxmlParser.TitleCaseName(input));
    }

    [Theory]
    [InlineData("<TECHNOLOGY>Warp Drive</>", "Warp Drive")]
    [InlineData("<IMG>icon</> Text", "icon Text")]
    [InlineData("No tags here", "No tags here")]
    [InlineData("<>empty<>", "empty")]
    public void StripMarkupTags(string input, string expected)
    {
        Assert.Equal(expected, MxmlParser.StripMarkupTags(input));
    }

    [Theory]
    [InlineData(@"TEXTURES/UI/FRONTEND/ICONS/test.DDS", "textures/ui/frontend/icons/test.dds")]
    [InlineData(@"TEXTURES\UI\test.DDS", "textures/ui/test.dds")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    public void NormalizeGameIconPath(string input, string expected)
    {
        Assert.Equal(expected, MxmlParser.NormalizeGameIconPath(input));
    }

    [Theory]
    [InlineData("UP_CRUI4_SUB", true)]
    [InlineData("UI_FOO_NAME", true)]
    [InlineData("Hello World", false)]
    [InlineData("lower_case", false)]
    [InlineData("SINGLEWORD", false)]  // No underscore
    [InlineData("", false)]
    public void LooksLikeLocalizationKey(string input, bool expected)
    {
        Assert.Equal(expected, MxmlParser.LooksLikeLocalisationKey(input));
    }

    [Fact]
    public void FormatStatTypeName_SplitsCamelCase()
    {
        Assert.Equal("Weapon Projectile Burst Cap",
            MxmlParser.FormatStatTypeName("Weapon_Projectile_BurstCap"));
    }

    [Fact]
    public void FormatStatTypeName_StripsPrefixes()
    {
        string result = MxmlParser.FormatStatTypeName("Suit_Health", "Suit_");
        Assert.Equal("Health", result);
    }

    [Fact]
    public void ParseColour_ReturnsHexString()
    {
        // null element returns white
        Assert.Equal("FFFFFF", MxmlParser.ParseColour(null));
    }
}
