using System.Xml.Linq;
using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class LocalizationBuilderTests
{
    private static readonly string JpRefDir = Path.Combine(
        FindRepoRoot(), "_ref", "game_mbin_mxml", "language-jp-mxml-example", "jp");

    /// <summary>
    /// Walks up from the build output directory until the solution file is found.
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NMSE.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException(
            "Could not find repo root (NMSE.slnx) from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void ParseLocalization_ParsesValidMxml()
    {
        // Create a minimal MXML localization file
        string tempFile = Path.GetTempFileName();
        try
        {
            var doc = new XDocument(
                new XElement("Data",
                    new XElement("Property",
                        new XAttribute("name", "Table"),
                        new XElement("Property",
                            new XAttribute("name", "Table"),
                            new XAttribute("_id", "TEST_NAME"),
                            new XElement("Property",
                                new XAttribute("name", "English"),
                                new XAttribute("value", "Test Name"))))));
            doc.Save(tempFile);

            var result = LocalisationBuilder.ParseLocalisation(tempFile);
            Assert.Single(result);
            Assert.Equal("Test Name", result["TEST_NAME"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseLocalization_TitleCasesNameKeys()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            var doc = new XDocument(
                new XElement("Data",
                    new XElement("Property",
                        new XAttribute("name", "Table"),
                        new XElement("Property",
                            new XAttribute("name", "Table"),
                            new XAttribute("_id", "ITEM_NAME"),
                            new XElement("Property",
                                new XAttribute("name", "English"),
                                new XAttribute("value", "CAKE OF GLASS"))))));
            doc.Save(tempFile);

            var result = LocalisationBuilder.ParseLocalisation(tempFile);
            Assert.Equal("Cake of Glass", result["ITEM_NAME"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseLocalisation_Japanese_ReadsJapaneseProperty()
    {
        string loc4Path = Path.GetFullPath(Path.Combine(JpRefDir, "nms_loc4_japanese.MXML"));
        Assert.True(File.Exists(loc4Path), $"JP reference file not found: {loc4Path}");

        var result = LocalisationBuilder.ParseLocalisation(loc4Path, "Japanese");

        Assert.True(result.Count > 0, "Should parse at least one Japanese entry");
        Assert.Equal("炭素", result["UI_FUEL_1_NAME"]);
    }

    [Fact]
    public void ParseLocalisation_English_SkipsEmptyInJapaneseFile()
    {
        string loc1Path = Path.GetFullPath(Path.Combine(JpRefDir, "nms_loc1_japanese.MXML"));
        Assert.True(File.Exists(loc1Path), $"JP reference file not found: {loc1Path}");

        var result = LocalisationBuilder.ParseLocalisation(loc1Path, "English");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseLocalisation_JapaneseDescriptionContainsSpecialChars()
    {
        string loc4Path = Path.GetFullPath(Path.Combine(JpRefDir, "nms_loc4_japanese.MXML"));
        Assert.True(File.Exists(loc4Path), $"JP reference file not found: {loc4Path}");

        var result = LocalisationBuilder.ParseLocalisation(loc4Path, "Japanese");

        Assert.True(result.ContainsKey("UI_FUEL_1_DESC"), "UI_FUEL_1_DESC should be present");
        string desc = result["UI_FUEL_1_DESC"];
        // After StripMarkupTags the markup is removed but Japanese text and newlines are preserved
        Assert.Contains("炭素", desc);
        Assert.Contains("マインレーザー", desc);
        Assert.Contains("\n", desc);
    }

    [Fact]
    public void BuildLocalisationJson_WritesUnescapedUtf8()
    {
        // Verify that the LocalisationBuilder produces unescaped UTF-8 JSON
        // rather than escaped \uXXXX sequences for non-ASCII characters.
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_unescape_" + Guid.NewGuid().ToString("N"));
        string mbinDir = Path.Combine(tmpDir, "mbin");
        string jsonDir = Path.Combine(tmpDir, "json");
        string langDir = Path.Combine(jsonDir, "lang");
        Directory.CreateDirectory(mbinDir);
        Directory.CreateDirectory(langDir);

        try
        {
            // Create a minimal Japanese MXML with a known Japanese value
            var doc = new XDocument(
                new XElement("Data",
                    new XElement("Property",
                        new XAttribute("name", "Table"),
                        new XElement("Property",
                            new XAttribute("name", "Table"),
                            new XAttribute("_id", "TEST_KEY"),
                            new XElement("Property",
                                new XAttribute("name", "Japanese"),
                                new XAttribute("value", "炭素テスト"))))));
            doc.Save(Path.Combine(mbinDir, "nms_loc1_japanese.MXML"));

            LocalisationBuilder.BuildLocalisationJson(tmpDir);

            string outputPath = Path.Combine(langDir, "ja-JP.json");
            Assert.True(File.Exists(outputPath), "ja-JP.json should be created");

            string rawJson = File.ReadAllText(outputPath);
            // Should contain actual Japanese characters, not \uXXXX escapes
            Assert.Contains("炭素テスト", rawJson);
            Assert.DoesNotContain("\\u", rawJson);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
