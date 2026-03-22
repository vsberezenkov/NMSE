using NMSE.Data;

namespace NMSE.Tests;

/// <summary>
/// Tests for <see cref="UiStrings"/> static UI string table localisation.
/// Each test creates its own temp directory with JSON files to avoid
/// cross-test interference on the shared static state.
/// </summary>
[Collection("MutableStaticDatabases")]
public class UiStringsTests : IDisposable
{
    private readonly string _tmpDir;

    public UiStringsTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "nmse_uistr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        UiStrings.Reset();
        try { Directory.Delete(_tmpDir, recursive: true); } catch { }
    }

    [Fact]
    public void Get_ReturnsRawKey_WhenNoDataLoaded()
    {
        UiStrings.Reset();
        Assert.Equal("menu.file", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_EnglishFallback_ReturnsEnglishStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("en-GB");

        Assert.True(result);
        Assert.Equal("&File", UiStrings.Get("menu.file"));
        Assert.Equal("Player", UiStrings.Get("tab.player"));
    }

    [Fact]
    public void Load_TranslatedLanguage_ReturnsTranslatedStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル", "tab.player": "プレイヤー"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("ja-JP");

        Assert.True(result);
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));
        Assert.Equal("プレイヤー", UiStrings.Get("tab.player"));
    }

    [Fact]
    public void Get_FallsBackToEnglish_WhenKeyMissingInTranslation()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player", "tab.exosuit": "Exosuit"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");

        // Translated key
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));
        // Missing in ja-JP, falls back to English
        Assert.Equal("Player", UiStrings.Get("tab.player"));
        Assert.Equal("Exosuit", UiStrings.Get("tab.exosuit"));
    }

    [Fact]
    public void Get_ReturnsRawKey_WhenKeyMissingEverywhere()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal("nonexistent.key", UiStrings.Get("nonexistent.key"));
    }

    [Fact]
    public void GetOrNull_ReturnsNull_WhenKeyMissing()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Null(UiStrings.GetOrNull("nonexistent.key"));
        Assert.Equal("&File", UiStrings.GetOrNull("menu.file"));
    }

    [Fact]
    public void Format_SubstitutesArguments()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"status.loaded": "Loaded {0} items, {1} mappings"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Loaded 42 items, 10 mappings", UiStrings.Format("status.loaded", 42, 10));
    }

    [Fact]
    public void Format_HandlesInvalidPlaceholders_Gracefully()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"bad.format": "Value is {999}"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        // Should not throw - returns unformatted template
        string result = UiStrings.Format("bad.format", "a");
        Assert.Equal("Value is {999}", result);
    }

    [Fact]
    public void TranslatedCount_IsZero_WhenEnglishActive()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal(0, UiStrings.TranslatedCount);
        Assert.False(UiStrings.IsActive);
    }

    [Fact]
    public void TranslatedCount_ReturnsCount_WhenOtherLanguageActive()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "de-DE.json"),
            """{"menu.file": "&Datei", "tab.player": "Spieler"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("de-DE");

        Assert.Equal(2, UiStrings.TranslatedCount);
        Assert.True(UiStrings.IsActive);
    }

    [Fact]
    public void TotalKeyCount_ReflectsEnglishFallback()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"a": "1", "b": "2", "c": "3"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal(3, UiStrings.TotalKeyCount);
    }

    [Fact]
    public void Load_ReturnsFalse_WhenLanguageFileMissing()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("xx-XX");

        Assert.False(result);
        // Should still have English fallback available
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_ReturnsFalse_WhenDirectoryNotSet()
    {
        UiStrings.Reset();
        bool result = UiStrings.Load("en-GB");
        Assert.False(result);
    }

    [Fact]
    public void Load_SwitchLanguage_ReplacesStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "de-DE.json"),
            """{"menu.file": "&Datei"}""");

        UiStrings.SetDirectory(_tmpDir);

        UiStrings.Load("ja-JP");
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));

        UiStrings.Load("de-DE");
        Assert.Equal("&Datei", UiStrings.Get("menu.file"));

        // Switch back to English
        UiStrings.Load("en-GB");
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_NullTag_RevertsToEnglish()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));

        UiStrings.Load(null);
        Assert.Equal("&File", UiStrings.Get("menu.file"));
        Assert.False(UiStrings.IsActive);
    }

    [Fact]
    public void Load_EmptyTag_RevertsToEnglish()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");
        UiStrings.Load("");
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");
        Assert.Equal("&File", UiStrings.Get("menu.file"));

        UiStrings.Reset();
        Assert.Equal(0, UiStrings.TotalKeyCount);
        Assert.Equal("menu.file", UiStrings.Get("menu.file"));
    }

    // --- Locale key existence tests using real en-GB data ---

    private static string? FindRealLangDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "Resources", "ui", "lang");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    [Fact]
    public void UiStrings_DiscoveryTabLocations_HasExpectedValue()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Teleport Destinations", UiStrings.Get("discovery.tab_locations"));
    }

    [Fact]
    public void UiStrings_CommonProceduralNoName_ExistsInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("common.procedural_no_name");
        // Should resolve to a real string, not fall back to the raw key
        Assert.NotEqual("common.procedural_no_name", value);
    }

    [Fact]
    public void UiStrings_SettlementDeleteWarning_ContainsTeleportDestinations()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("settlement.delete_warning");
        Assert.Contains("Teleport Destinations", value);
    }
}
