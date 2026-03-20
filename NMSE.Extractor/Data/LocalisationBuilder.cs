using NMSE.Extractor.Config;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace NMSE.Extractor.Data;

/// <summary>
/// Parses localisation MXML files into per-language JSON dictionaries.
/// Each NMS language has its own set of MXML files (e.g. nms_loc1_japanese.MXML)
/// where only the matching language property contains values.
/// Produces one JSON file per supported language in Resources/json/lang/{bcp47}.json,
/// plus the legacy Localisation-EN.json.tbc for backward compatibility with the extractor pipeline.
/// </summary>
public static class LocalisationBuilder
{
    /// <summary>
    /// Builds localisation JSON for all supported languages.
    /// For each language, reads its own set of locale MXML files and extracts
    /// the language-specific property values.
    /// Outputs: json/lang/{tag}.json for each language, plus json/Localisation-EN.json.tbc for English.
    /// Returns the number of English translations produced.
    /// </summary>
    public static int BuildLocalisationJson(string resourcesDir)
    {
        string mbinDir = Path.Combine(resourcesDir, ExtractorConfig.MbinSubfolder);
        string jsonDir = Path.Combine(resourcesDir, ExtractorConfig.JsonSubfolder);
        string langDir = Path.Combine(jsonDir, ExtractorConfig.LangSubfolder);
        Directory.CreateDirectory(jsonDir);
        Directory.CreateDirectory(langDir);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        int englishCount = 0;

        foreach (var (nmsName, tag) in ExtractorConfig.SupportedLanguages)
        {
            var langMxmlFiles = ExtractorConfig.GetLocaleMxmlFiles(nmsName);
            var translations = new Dictionary<string, string>();

            foreach (string mxmlFile in langMxmlFiles)
            {
                string mxmlPath = Path.Combine(mbinDir, mxmlFile);
                if (!File.Exists(mxmlPath))
                    continue;

                var parsed = ParseLocalisation(mxmlPath, nmsName);
                foreach (var kv in parsed)
                    translations[kv.Key] = kv.Value;
            }

            if (translations.Count == 0) continue;

            string outputPath = Path.Combine(langDir, $"{tag}.json");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(translations, options));
            Console.WriteLine($"[OK] {tag}: {translations.Count} translations");

            if (nmsName.Equals("English", StringComparison.OrdinalIgnoreCase))
                englishCount = translations.Count;
        }

        Console.WriteLine($"\n[OK] Localisation: {englishCount} English translations, " +
            $"{ExtractorConfig.SupportedLanguages.Count} languages -> lang/\n");
        return englishCount;
    }

    /// <summary>
    /// Parses a single localisation MXML file for a specific language.
    /// Each language MXML file (e.g. nms_loc1_japanese.MXML) has entries with
    /// all language properties, but only the target language has non-empty values.
    /// </summary>
    /// <param name="mxmlPath">Path to the locale MXML file.</param>
    /// <param name="languageName">NMS language property name (e.g. "Japanese", "English").</param>
    public static Dictionary<string, string> ParseLocalisation(string mxmlPath, string languageName = "English")
    {
        var translations = new Dictionary<string, string>();
        var doc = XDocument.Load(mxmlPath);
        var root = doc.Root;
        if (root == null) return translations;

        var tableProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null)
        {
            Console.WriteLine("Warning: Could not find Table property in localisation MXML");
            return translations;
        }

        bool titleCaseNames = languageName.Equals("English", StringComparison.OrdinalIgnoreCase);

        foreach (var entry in tableProp.Elements("Property")
            .Where(e => e.Attribute("name")?.Value == "Table"))
        {
            string locId = entry.Attribute("_id")?.Value ?? "";
            if (string.IsNullOrEmpty(locId))
            {
                var idProp = entry.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Id");
                locId = idProp?.Attribute("value")?.Value ?? "";
            }

            var langProp = entry.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == languageName);
            if (langProp != null)
            {
                string text = langProp.Attribute("value")?.Value ?? "";
                if (!string.IsNullOrEmpty(locId) && !string.IsNullOrEmpty(text))
                {
                    text = MxmlParser.StripMarkupTags(text);
                    if (titleCaseNames && locId.EndsWith("_NAME", StringComparison.Ordinal))
                        text = MxmlParser.TitleCaseName(text);
                    translations[locId] = text;
                }
            }
        }

        return translations;
    }
}
