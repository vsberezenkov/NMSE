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
