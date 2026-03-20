using NMSE.Data;

namespace NMSE.Tests;

/// <summary>
/// Tests for JSON-loaded database localisation round trips.
/// These tests call LoadFromFile on static databases, mutating shared state.
/// They are placed in a dedicated [Collection] to prevent parallel execution
/// with StaticDataDatabaseTests that depend on the original hardcoded data.
/// </summary>
[Collection("MutableStaticDatabases")]
public class DatabaseLocalisationTests
{
    [Fact]
    public void FrigateTraitDatabase_LoadFromFile_WithLocStr_EnablesLocalisation()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string traitsJson = """
            [
              { "Id": "^TEST_LOC_1", "Name": "Loc Trait", "Name_LocStr": "FLEET_TRAIT_TEST", "Type": "FuelCapacity", "Strength": -15, "Beneficial": true, "Primary": "SUPPORT", "Secondary": "" },
              { "Id": "^TEST_LOC_2", "Name": "No Loc Trait", "Type": "Combat", "Strength": 5, "Beneficial": true, "Primary": "COMBAT", "Secondary": "" }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "FrigateTraits.json"), traitsJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"),
                """{"FLEET_TRAIT_TEST": "支援スペシャリスト"}""");

            FrigateTraitDatabase.LoadFromFile(Path.Combine(tmpDir, "FrigateTraits.json"));
            Assert.Equal(2, FrigateTraitDatabase.Traits.Count);
            Assert.Equal("Loc Trait", FrigateTraitDatabase.Traits[0].Name);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = FrigateTraitDatabase.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("支援スペシャリスト", FrigateTraitDatabase.Traits[0].Name);
            Assert.Equal("No Loc Trait", FrigateTraitDatabase.Traits[1].Name);

            FrigateTraitDatabase.RevertLocalisation();
            Assert.Equal("Loc Trait", FrigateTraitDatabase.Traits[0].Name);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void SettlementPerkDatabase_LoadFromFile_WithLocStr_EnablesLocalisation()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string perksJson = """
            [
              { "Id": "^TEST_LOC_1", "Name": "Worm infestation", "Name_LocStr": "UI_PERK_TEST", "Description": "Increases maintenance costs", "Description_LocStr": "UI_PERK_TEST_DESC", "Beneficial": false, "Starter": true, "Procedural": false, "StatChanges": [{ "Type": "Upkeep", "Strength": "NegativeMedium" }] }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "SettlementPerks.json"), perksJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"),
                """{"UI_PERK_TEST": "ワーム蔓延", "UI_PERK_TEST_DESC": "メンテナンスコストが増加"}""");

            SettlementPerkDatabase.LoadFromFile(Path.Combine(tmpDir, "SettlementPerks.json"));
            Assert.Single(SettlementPerkDatabase.Perks);
            Assert.False(SettlementPerkDatabase.Perks[0].Beneficial);
            Assert.Single(SettlementPerkDatabase.Perks[0].StatChanges);
            Assert.Equal(PerkStatType.Upkeep, SettlementPerkDatabase.Perks[0].StatChanges[0].Type);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = SettlementPerkDatabase.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("ワーム蔓延", SettlementPerkDatabase.Perks[0].Name);
            Assert.Equal("メンテナンスコストが増加", SettlementPerkDatabase.Perks[0].Description);

            SettlementPerkDatabase.RevertLocalisation();
            Assert.Equal("Worm infestation", SettlementPerkDatabase.Perks[0].Name);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WikiGuideDatabase_LoadFromFile_WithLocStr_EnablesLocalisation()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string wikiJson = """
            [
              { "Id": "^TOPIC_LOC_1", "Name": "Survival Basics", "Name_LocStr": "UI_GUIDE_TEST", "Category": "Survival", "Category_LocStr": "UI_GUIDE_CAT_TEST", "IconKey": "SURVIVALBASICS" }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "WikiGuide.json"), wikiJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"),
                """{"UI_GUIDE_TEST": "サバイバル基本", "UI_GUIDE_CAT_TEST": "サバイバル"}""");

            WikiGuideDatabase.LoadFromFile(Path.Combine(tmpDir, "WikiGuide.json"));
            Assert.Single(WikiGuideDatabase.Topics);
            Assert.Equal("Survival Basics", WikiGuideDatabase.GetTopicName("^TOPIC_LOC_1"));

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = WikiGuideDatabase.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("サバイバル基本", WikiGuideDatabase.GetTopicName("^TOPIC_LOC_1"));
            Assert.Equal("サバイバル", WikiGuideDatabase.GetTopicCategory("^TOPIC_LOC_1"));

            WikiGuideDatabase.RevertLocalisation();
            Assert.Equal("Survival Basics", WikiGuideDatabase.GetTopicName("^TOPIC_LOC_1"));
            Assert.Equal("Survival", WikiGuideDatabase.GetTopicCategory("^TOPIC_LOC_1"));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_ApplyLocalisation_FallsBackTo_NAME_And_1_NAME()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Create items: one with direct match, one needing _NAME fallback, one needing 1_NAME fallback
            string itemsJson = """
            [
              { "Id": "DIRECT_MATCH", "Name": "Direct", "Name_LocStr": "LOC_DIRECT", "Category": "Test" },
              { "Id": "NAME_SUFFIX", "Name": "NeedsSuffix", "Name_LocStr": "LOC_SUFFIX", "Category": "Test" },
              { "Id": "ONE_NAME_SUFFIX", "Name": "Needs1Suffix", "Name_LocStr": "LOC_ONESUFFIX", "Category": "Test" },
              { "Id": "NO_MATCH", "Name": "NoMatch", "Name_LocStr": "LOC_NOMATCH", "Category": "Test" }
            ]
            """;
            string locJson = """
            {
              "LOC_DIRECT": "直接",
              "LOC_SUFFIX_NAME": "接尾語",
              "LOC_ONESUFFIX1_NAME": "一接尾語"
            }
            """;

            File.WriteAllText(Path.Combine(tmpDir, "Test.json"), itemsJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), locJson);

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);

            Assert.Equal("直接", db.GetItem("DIRECT_MATCH")?.Name);
            Assert.Equal("接尾語", db.GetItem("NAME_SUFFIX")?.Name);
            Assert.Equal("一接尾語", db.GetItem("ONE_NAME_SUFFIX")?.Name);
            Assert.Equal("NoMatch", db.GetItem("NO_MATCH")?.Name);
            Assert.Equal(3, count);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WikiGuideDatabase_Categories_MatchJsonData()
    {
        // Verify that the WikiGuide database correctly loads categories from JSON
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string wikiJson = """
            [
              { "Id": "^T1", "Name": "Topic1", "Name_LocStr": "T1", "Category": "Survival Basics", "Category_LocStr": "CAT1", "IconKey": "ICON1" },
              { "Id": "^T2", "Name": "Topic2", "Name_LocStr": "T2", "Category": "Getting Around", "Category_LocStr": "CAT2", "IconKey": "ICON2" }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "WikiGuide.json"), wikiJson);

            WikiGuideDatabase.LoadFromFile(Path.Combine(tmpDir, "WikiGuide.json"));
            Assert.Equal(2, WikiGuideDatabase.Topics.Count);
            Assert.Equal("Survival Basics", WikiGuideDatabase.Topics[0].Category);
            Assert.Equal("Getting Around", WikiGuideDatabase.Topics[1].Category);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WordDatabase_ApplyLocalisation_FallsBackToGroupKeys()
    {
        // Words should try all group keys when TextLocStr doesn't produce a real translation.
        // This mirrors the case where "TRA_ACCESS" returns "access" (English) but "BUI_ACCESS"
        // returns "アクセス" (Japanese).
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string wordsJson = """
            [
              {
                "Id": "^ACCESS",
                "Text": "access",
                "Text_LocStr": "TRA_ACCESS",
                "Groups": { "^TRA_ACCESS": 0, "^WAR_ACCESS": 1, "^BUI_ACCESS": 8 }
              },
              {
                "Id": "^ATLAS",
                "Text": "atlas",
                "Text_LocStr": "TRA_ATLAS",
                "Groups": { "^TRA_ATLAS": 0, "^ATLAS_ATLAS": 4 }
              }
            ]
            """;
            // TRA_ACCESS returns English (untranslated), BUI_ACCESS has actual Japanese
            // TRA_ATLAS has a proper translation
            string locJson = """
            {
              "TRA_ACCESS": "access",
              "WAR_ACCESS": "access",
              "BUI_ACCESS": "アクセス",
              "TRA_ATLAS": "アトラス"
            }
            """;

            File.WriteAllText(Path.Combine(tmpDir, "Words.json"), wordsJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), locJson);

            var db = new WordDatabase();
            db.LoadFromFile(Path.Combine(tmpDir, "Words.json"));
            Assert.Equal(2, db.Count);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(2, count);

            // ACCESS should use BUI_ACCESS fallback since TRA_ACCESS returned "access" (same as English)
            var access = db.Words.FirstOrDefault(w => w.Id == "^ACCESS");
            Assert.NotNull(access);
            Assert.Equal("アクセス", access.Text);

            // ATLAS should use TRA_ATLAS directly (it returned a real translation)
            var atlas = db.Words.FirstOrDefault(w => w.Id == "^ATLAS");
            Assert.NotNull(atlas);
            Assert.Equal("アトラス", atlas.Text);

            db.RevertLocalisation();
            access = db.Words.FirstOrDefault(w => w.Id == "^ACCESS");
            Assert.Equal("access", access!.Text);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_ApplyLocalisation_UpgradeModuleWithLevelSuffix()
    {
        // UP_SHLD3 with NameLocStr="UP_SHIELDBOOST" should find "UP_SHIELDBOOST3_NAME"
        // UP_BOLTX with NameLocStr="UP_BOLT" should find "UP_BOLT_X_NAME"
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string itemsJson = """
            [
              { "Id": "UP_SHLD3", "Name": "Defence A-Class", "Name_LocStr": "UP_SHIELDBOOST", "Category": "Test" },
              { "Id": "UP_BOLTX", "Name": "Illegal Bolt", "Name_LocStr": "UP_BOLT", "Category": "Test" },
              { "Id": "UP_LASER4", "Name": "Mining S-Class", "Name_LocStr": "UP_LASER", "Category": "Test" }
            ]
            """;
            string locJson = """
            {
              "UP_SHIELDBOOST3_NAME": "シールドモジュール",
              "UP_BOLT_X_NAME": "うさんくさいボルトキャスターモジュール",
              "UP_LASER4_NAME": "マインビームモジュール"
            }
            """;

            File.WriteAllText(Path.Combine(tmpDir, "Test.json"), itemsJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), locJson);

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);

            Assert.Equal("シールドモジュール", db.GetItem("UP_SHLD3")?.Name);
            Assert.Equal("うさんくさいボルトキャスターモジュール", db.GetItem("UP_BOLTX")?.Name);
            Assert.Equal("マインビームモジュール", db.GetItem("UP_LASER4")?.Name);
            Assert.Equal(3, count);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WikiGuideDatabase_GetEnglishCategory_ReturnsOriginalAfterLocalisation()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string wikiJson = """
            [
              { "Id": "^T1", "Name": "Getting Started", "Name_LocStr": "N1", "Category": "Survival Basics", "Category_LocStr": "CAT1", "IconKey": "SURV1" }
            ]
            """;
            string locJson = """{"N1": "はじめに", "CAT1": "サバイバルの基本"}""";

            File.WriteAllText(Path.Combine(tmpDir, "WikiGuide.json"), wikiJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), locJson);

            WikiGuideDatabase.LoadFromFile(Path.Combine(tmpDir, "WikiGuide.json"));

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            WikiGuideDatabase.ApplyLocalisation(svc);

            // Category should be localised
            Assert.Equal("サバイバルの基本", WikiGuideDatabase.GetTopicCategory("^T1"));
            // English category should return the original English value for grid placement
            Assert.Equal("Survival Basics", WikiGuideDatabase.GetEnglishCategory("^T1"));

            WikiGuideDatabase.RevertLocalisation();
            Assert.Equal("Survival Basics", WikiGuideDatabase.GetTopicCategory("^T1"));
            Assert.Equal("Survival Basics", WikiGuideDatabase.GetEnglishCategory("^T1"));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_SkipsNonItemJsonFiles()
    {
        // The database should skip Words.json, FrigateTraits.json, etc.
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Real item file
            File.WriteAllText(Path.Combine(tmpDir, "Products.json"), """
            [{ "Id": "CARBON", "Name": "Carbon", "Category": "Substance" }]
            """);
            // Non-item files that should be skipped
            File.WriteAllText(Path.Combine(tmpDir, "FrigateTraits.json"), """
            [{ "Id": "^FUEL_PRI", "Name": "Support Specialist", "Type": "Fuel" }]
            """);
            File.WriteAllText(Path.Combine(tmpDir, "Words.json"), """
            [{ "Id": "^ATLAS", "Text": "atlas", "Groups": {} }]
            """);
            File.WriteAllText(Path.Combine(tmpDir, "none.json"), """
            [{ "Id": "NONE", "Name": "None" }]
            """);

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            // Only CARBON should be loaded
            Assert.NotNull(db.GetItem("CARBON"));
            Assert.Null(db.GetItem("^FUEL_PRI"));
            Assert.Null(db.GetItem("^ATLAS"));
            Assert.Null(db.GetItem("NONE"));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void AppConfig_Language_DefaultsToEnGB()
    {
        var config = new NMSE.Config.AppConfig();
        Assert.Equal("en-GB", config.Language);
    }
}
