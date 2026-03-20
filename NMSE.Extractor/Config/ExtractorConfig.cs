namespace NMSE.Extractor.Config;

public static class ExtractorConfig
{
    public const string NmsGamePath = @"steamapps\common\No Man's Sky\GAMEDATA\PCBANKS";
    
    public const string HgPakToolZipUrl = "https://github.com/monkeyman192/HGPAKtool/releases/latest/download/hgpaktool-x86_64-pc-windows.zip";
    public const string HgPakToolLatestUrl = "https://github.com/monkeyman192/HGPAKtool/releases/latest/";
    public const string MbinCompilerUrl = "https://github.com/monkeyman192/MBINCompiler/releases/latest/download/MBINCompiler.exe";
    public const string MbinCompilerLatestUrl = "https://github.com/monkeyman192/MBINCompiler/releases/latest/";
    public const string MappingJsonUrl = "https://github.com/monkeyman192/MBINCompiler/releases/latest/download/mapping.json";

    public const string ImageMagickLatestUrl = "https://github.com/ImageMagick/ImageMagick/releases/latest/";
    public const string ImageMagickDownloadPattern = "https://github.com/ImageMagick/ImageMagick/releases/download/{0}/ImageMagick-{0}-portable-Q16-HDRI-x64.7z";
    public const string SevenZipLatestUrl = "https://github.com/ip7z/7zip/releases/latest/";
    public const string SevenZipDownloadPattern = "https://github.com/ip7z/7zip/releases/download/{0}/7zr.exe";

    /// <summary>
    /// Maps NMS internal language property names to IETF BCP 47 language tags.
    /// TencentChinese is excluded as it is not needed.
    /// </summary>
    public static readonly Dictionary<string, string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"]              = "en-GB",
        ["French"]               = "fr-FR",
        ["Italian"]              = "it-IT",
        ["German"]               = "de-DE",
        ["Spanish"]              = "es-ES",
        ["Russian"]              = "ru-RU",
        ["Polish"]               = "pl-PL",
        ["Dutch"]                = "nl-NL",
        ["Portuguese"]           = "pt-PT",
        ["LatinAmericanSpanish"] = "es-419",
        ["BrazilianPortuguese"]  = "pt-BR",
        ["SimplifiedChinese"]    = "zh-CN",
        ["TraditionalChinese"]   = "zh-TW",
        ["Korean"]               = "ko-KR",
        ["Japanese"]             = "ja-JP",
        ["USEnglish"]            = "en-US",
    };

    /// <summary>
    /// Base stems for locale MXML/MBIN files. Each language has one file per stem,
    /// e.g. "nms_loc1" -> nms_loc1_english.MXML, nms_loc1_french.MXML, etc.
    /// </summary>
    public static readonly string[] LocaleFileStems =
    [
        "nms_loc1", "nms_loc4", "nms_loc5", "nms_loc6",
        "nms_loc7", "nms_loc8", "nms_loc9", "nms_update3",
    ];

    /// <summary>
    /// Returns the locale MXML file names for a given NMS language name.
    /// e.g. GetLocaleMxmlFiles("Japanese") -> ["nms_loc1_japanese.MXML", "nms_loc4_japanese.MXML", …]
    /// </summary>
    public static string[] GetLocaleMxmlFiles(string nmsLanguageName)
    {
        string suffix = nmsLanguageName.ToLowerInvariant();
        return LocaleFileStems.Select(stem => $"{stem}_{suffix}.MXML").ToArray();
    }

    /// <summary>Game data MBIN filters (non-locale).</summary>
    private static readonly string[] GameDataMbinFilters =
    [
        "*REALITY/TABLES/nms_reality_gcproducttable.mbin",
        "*REALITY/TABLES/consumableitemtable.mbin",
        "*REALITY/TABLES/nms_reality_gcrecipetable.mbin",
        "*REALITY/TABLES/nms_reality_gctechnologytable.mbin",
        "*REALITY/TABLES/basebuildingobjectstable.mbin",
        "*REALITY/TABLES/nms_reality_gcsubstancetable.mbin",
        "*REALITY/TABLES/fishdatatable.mbin",
        "*REALITY/TABLES/nms_modularcustomisationproducts.mbin",
        "*REALITY/TABLES/nms_basepartproducts.mbin",
        "*REALITY/TABLES/nms_reality_gcproceduraltechnologytable.mbin",
        "*REALITY/TABLES/rewardtable.mbin",
        "*REALITY/TABLES/nms_dialog_gcalienspeechtable.mbin",
        "*REALITY/TABLES/UNLOCKABLESEASONREWARDS.mbin",
        "*REALITY/TABLES/UNLOCKABLETWITCHREWARDS.mbin",
        "*REALITY/TABLES/UNLOCKABLEPLATFORMREWARDS.mbin",
        "*SIMULATION/ECOSYSTEM/peteggtraitmodifieroverridetable.mbin",
        "*GAMESTATE/PLAYERDATA/PLAYERTITLEDATA.mbin",
        "*REALITY/TABLES/FRIGATETRAITTABLE.mbin",
        "*REALITY/TABLES/SETTLEMENTPERKSTABLE.mbin",
        "*REALITY/WIKI.mbin",
    ];

    /// <summary>
    /// Locale MBIN filters using wildcards to capture all language variants.
    /// e.g. "*LANGUAGE/nms_loc1_*.mbin" matches nms_loc1_english, nms_loc1_french, etc.
    /// </summary>
    private static readonly string[] LocaleMbinFilters =
        LocaleFileStems.Select(stem => $"*LANGUAGE/{stem}_*.mbin").ToArray();

    /// <summary>
    /// All MBIN filters for game data extraction (game data + all locale languages).
    /// </summary>
    public static readonly string[] MbinFilters = [.. GameDataMbinFilters, .. LocaleMbinFilters];

    /// <summary>
    /// Filters for DDS texture files needed for icon extraction.
    /// </summary>
    public static readonly string[] TextureFilters =
    [
        "*TEXTURES/*.DDS",
    ];

    /// <summary>
    /// Combined MBIN + texture filters for single-pass extraction.
    /// </summary>
    public static readonly string[] AllExtractionFilters = [.. MbinFilters, .. TextureFilters];

    /// <summary>
    /// MXML files that MUST be produced by MBINCompiler. Missing any of these is fatal.
    /// English locale files are required; other languages are optional.
    /// </summary>
    public static readonly string[] ExpectedMxmlFiles =
    [
        "nms_reality_gcproducttable.MXML",
        "consumableitemtable.MXML",
        "nms_reality_gcrecipetable.MXML",
        "nms_reality_gctechnologytable.MXML",
        "basebuildingobjectstable.MXML",
        "nms_reality_gcsubstancetable.MXML",
        "fishdatatable.MXML",
        "nms_modularcustomisationproducts.MXML",
        "nms_basepartproducts.MXML",
        "nms_reality_gcproceduraltechnologytable.MXML",
        "rewardtable.MXML",
        "nms_dialog_gcalienspeechtable.MXML",
        "peteggtraitmodifieroverridetable.MXML",
        "nms_loc1_english.MXML",
        "nms_loc4_english.MXML",
        "nms_loc5_english.MXML",
        "nms_loc6_english.MXML",
        "nms_loc7_english.MXML",
        "nms_loc8_english.MXML",
        "nms_loc9_english.MXML",
        "nms_update3_english.MXML",
    ];

    /// <summary>
    /// MXML files extracted on a best-effort basis. MBINCompiler may not support these
    /// MBIN types or they may not exist in all game versions. Missing files are logged
    /// as warnings but do not abort the extraction pipeline.
    /// Non-English locale MXML files are optional since they depend on which
    /// language packs the game installation has.
    /// </summary>
    public static readonly string[] OptionalMxmlFiles =
    [
        "UNLOCKABLESEASONREWARDS.MXML",
        "UNLOCKABLETWITCHREWARDS.MXML",
        "UNLOCKABLEPLATFORMREWARDS.MXML",
        "PLAYERTITLEDATA.MXML",
        "FRIGATETRAITTABLE.MXML",
        "SETTLEMENTPERKSTABLE.MXML",
        "WIKI.MXML",
        .. SupportedLanguages.Keys
            .Where(lang => !lang.Equals("English", StringComparison.OrdinalIgnoreCase))
            .SelectMany(lang => GetLocaleMxmlFiles(lang)),
    ];

    /// <summary>English locale MXML files (kept for backward compatibility).</summary>
    public static readonly string[] LocaleMxmlFiles = GetLocaleMxmlFiles("English");

    public const string LangSubfolder = "lang";

    public const string ResourcesFolder = "Resources";
    public const string ToolsFolder = "tools";
    public const string BanksFolder = "banks";
    public const string ImageMagickSubfolder = "imagemagick";
    public const string MbinSubfolder = "mbin";
    public const string JsonSubfolder = "json";
    public const string ImagesSubfolder = "images";
    public const string MapSubfolder = "map";
    public const string ExtractedSubfolder = "extracted";
}
