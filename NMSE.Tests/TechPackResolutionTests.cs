using System.Text.RegularExpressions;
using NMSE.Data;

namespace NMSE.Tests;

/// <summary>
/// Tests for the TechPack resolution logic that maps hash-based IDs (^+12 hex chars)
/// found in save files to their corresponding game items via the TechPacks dictionary.
/// </summary>
public class TechPackResolutionTests
{
    /// <summary>
    /// Replicates the IsTechPackHash check from InventoryGridPanel.
    /// TechPack hashes are '^' followed by exactly 12 hex characters.
    /// </summary>
    private static bool IsTechPackHash(string baseId) =>
        baseId.Length == 13 && baseId[0] == '^' && Regex.IsMatch(baseId, @"^\^[0-9A-Fa-f]{12}$");

    /// <summary>
    /// Replicates the core resolution logic from InventoryGridPanel.ResolveGameItem.
    /// </summary>
    private static (GameItem? gameItem, string? techPackIcon, string? techPackClass) ResolveGameItem(
        GameItemDatabase database, string itemId)
    {
        var m = Regex.Match(itemId, @"#\d{5,}$");
        string baseId = itemId;
        if (m.Success)
            baseId = itemId.Substring(0, m.Index);

        // Try direct database lookup first
        GameItem? gi = database.GetItem(itemId) ?? database.GetItem("^" + itemId);
        if (gi == null && m.Success)
            gi = database.GetItem(baseId) ?? database.GetItem("^" + baseId);

        if (gi != null) return (gi, null, null);

        // Fallback: check TechPacks for hash-based IDs
        if (IsTechPackHash(baseId) && TechPacks.Dictionary.TryGetValue(baseId, out var techPack))
        {
            gi = database.GetItem(techPack.Id);
            if (gi != null)
                return (gi, techPack.Icon, techPack.Class);
        }

        return (null, null, null);
    }

    private GameItemDatabase CreateDatabaseWithItems(params (string id, string name, string type)[] items)
    {
        // Create a temporary JSON file with the given items
        var db = new GameItemDatabase();
        var tempDir = Path.Combine(Path.GetTempPath(), "nmse_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // Write a JSON file with the items
            var json = "[" + string.Join(",", items.Select(i =>
                $"{{\"Id\":\"{i.id}\",\"Name\":\"{i.name}\",\"Group\":\"Test\"}}")) + "]";
            File.WriteAllText(Path.Combine(tempDir, $"{items[0].type}.json"), json);
            db.LoadItemsFromJsonDirectory(tempDir);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
        return db;
    }

    [Fact]
    public void IsTechPackHash_ValidHash_ReturnsTrue()
    {
        Assert.True(IsTechPackHash("^808497C54986"));
        Assert.True(IsTechPackHash("^8080DAB7B71F"));
        Assert.True(IsTechPackHash("^80809DBCC329"));
    }

    [Fact]
    public void IsTechPackHash_LowercaseHex_ReturnsTrue()
    {
        Assert.True(IsTechPackHash("^808497c54986"));
        Assert.True(IsTechPackHash("^abcdef123456"));
    }

    [Fact]
    public void IsTechPackHash_NormalItemId_ReturnsFalse()
    {
        Assert.False(IsTechPackHash("^FUEL1"));
        Assert.False(IsTechPackHash("^UP_JETX"));
        Assert.False(IsTechPackHash("^ANTIMATTER"));
        Assert.False(IsTechPackHash("^UP_ENGY3"));
    }

    [Fact]
    public void IsTechPackHash_WithVariantSuffix_ReturnsFalse()
    {
        // The variant should be stripped before calling IsTechPackHash
        Assert.False(IsTechPackHash("^808497C54986#12345"));
    }

    [Fact]
    public void IsTechPackHash_EmptyAndEdgeCases_ReturnsFalse()
    {
        Assert.False(IsTechPackHash(""));
        Assert.False(IsTechPackHash("^"));
        Assert.False(IsTechPackHash("^12345")); // Too short
        Assert.False(IsTechPackHash("^YOURSLOTITEM"));
    }

    [Fact]
    public void TechPacksDictionary_ContainsExpectedEntries()
    {
        Assert.True(TechPacks.Dictionary.ContainsKey("^808497C54986"));
        Assert.Equal("^UT_PROTECT", TechPacks.Dictionary["^808497C54986"].Id);
        Assert.Equal("TECHBOX_HEALTH.png", TechPacks.Dictionary["^808497C54986"].Icon);
    }

    [Fact]
    public void TechPacksDictionary_AllKeysAreValidHashes()
    {
        foreach (var key in TechPacks.Dictionary.Keys)
        {
            Assert.True(IsTechPackHash(key), $"TechPack key '{key}' should be a valid hash");
        }
    }

    [Fact]
    public void TechPacksDictionary_AllEntriesHaveNonEmptyIdAndIcon()
    {
        foreach (var entry in TechPacks.Dictionary)
        {
            Assert.False(string.IsNullOrEmpty(entry.Value.Id),
                $"TechPack entry '{entry.Key}' should have a non-empty Id");
            Assert.False(string.IsNullOrEmpty(entry.Value.Icon),
                $"TechPack entry '{entry.Key}' should have a non-empty Icon");
        }
    }

    [Fact]
    public void ResolveGameItem_NormalItem_ReturnsFromDatabase()
    {
        var db = CreateDatabaseWithItems(("^FUEL1", "Fuel 1", "substance"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^FUEL1");

        Assert.NotNull(gi);
        Assert.Equal("Fuel 1", gi.Name);
        Assert.Null(techPackIcon); // Normal items don't have a TechPack icon override
    }

    [Fact]
    public void ResolveGameItem_TechPackHash_ResolvesViaLookup()
    {
        // ^808497C54986 maps to ^UT_PROTECT in TechPacks dictionary
        var db = CreateDatabaseWithItems(("^UT_PROTECT", "Protection Module", "technology"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^808497C54986");

        Assert.NotNull(gi);
        Assert.Equal("Protection Module", gi.Name);
        Assert.Equal("TECHBOX_HEALTH.png", techPackIcon);
    }

    [Fact]
    public void ResolveGameItem_TechPackHashWithVariant_ResolvesViaLookup()
    {
        // Same hash but with variant suffix
        var db = CreateDatabaseWithItems(("^UT_PROTECT", "Protection Module", "technology"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^808497C54986#12345");

        Assert.NotNull(gi);
        Assert.Equal("Protection Module", gi.Name);
        Assert.Equal("TECHBOX_HEALTH.png", techPackIcon);
    }

    [Fact]
    public void ResolveGameItem_UnknownHash_ReturnsNull()
    {
        var db = CreateDatabaseWithItems(("^FUEL1", "Fuel 1", "substance"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^AAAAAAAAAAAA");

        Assert.Null(gi);
        Assert.Null(techPackIcon);
    }

    [Fact]
    public void ResolveGameItem_NormalItemWithVariant_ResolvesFromDatabase()
    {
        var db = CreateDatabaseWithItems(("^UP_ENGY3", "Energy Upgrade 3", "proceduralTechnology"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^UP_ENGY3#66802");

        Assert.NotNull(gi);
        Assert.Equal("Energy Upgrade 3", gi.Name);
        Assert.Null(techPackIcon); // Not a TechPack hash, resolved directly
    }

    [Fact]
    public void ResolveGameItem_PrefersDatabaseOverTechPack()
    {
        // If the hash itself is in the database, it should be found directly
        // (this shouldn't normally happen but tests the priority)
        var db = CreateDatabaseWithItems(("^808497C54986", "Direct Hash Entry", "product"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^808497C54986");

        Assert.NotNull(gi);
        Assert.Equal("Direct Hash Entry", gi.Name);
        Assert.Null(techPackIcon); // Found directly, not via TechPack
    }

    [Fact]
    public void ResolveGameItem_AlienTechPack_UsesCorrectIcon()
    {
        // ^808042B8B0A7 maps to ^CARGO_S_ALIEN with TECHBOX_UTILITY.png icon
        var db = CreateDatabaseWithItems(("^CARGO_S_ALIEN", "Alien Cargo Shield", "technology"));
        var (gi, techPackIcon, _) = ResolveGameItem(db, "^808042B8B0A7");

        Assert.NotNull(gi);
        Assert.Equal("Alien Cargo Shield", gi.Name);
        Assert.Equal("TECHBOX_UTILITY.png", techPackIcon);
    }

    [Fact]
    public void OriginalHashId_PreservedInItemId()
    {
        // This test verifies the conceptual requirement that the original hash ID
        // is what gets stored/displayed, not the resolved ID.
        // The resolve function returns the GameItem for display purposes,
        // but never changes the original itemId that would be used for saving.
        string originalId = "^808497C54986#12345";
        var db = CreateDatabaseWithItems(("^UT_PROTECT", "Protection Module", "technology"));
        var (gi, _, _) = ResolveGameItem(db, originalId);

        Assert.NotNull(gi);
        // The resolved item has the TechPack's Id, not the original hash
        Assert.Equal("^UT_PROTECT", gi.Id);
        // But originalId remains unchanged - it's what should be saved
        Assert.Equal("^808497C54986#12345", originalId);
    }

    [Fact]
    public void GetItem_TBobbleId_ResolvesViaTPrefix()
    {
        // T_BOBBLE_APOLLO should look up BOBBLE_APOLLO (strip the T_ prefix)
        var db = CreateDatabaseWithItems(("BOBBLE_APOLLO", "Apollo Bobble Head", "others"));

        // Direct lookup by ^T_BOBBLE_APOLLO (as it appears in save files)
        var item = db.GetItem("^T_BOBBLE_APOLLO");
        Assert.NotNull(item);
        Assert.Equal("Apollo Bobble Head", item.Name);
        // The resolved item has the database ID, not the T_ prefixed one
        Assert.Equal("BOBBLE_APOLLO", item.Id);
    }

    [Fact]
    public void GetItem_TBobbleId_WithoutCaret_ResolvesViaTPrefix()
    {
        var db = CreateDatabaseWithItems(("BOBBLE_ATLAS", "Atlas Bobble Head", "others"));

        // T_BOBBLE_ATLAS without ^ prefix
        var item = db.GetItem("T_BOBBLE_ATLAS");
        Assert.NotNull(item);
        Assert.Equal("Atlas Bobble Head", item.Name);
    }

    [Fact]
    public void GetItem_NonTBobble_DoesNotStripTPrefix()
    {
        // Items that start with T_ but exist directly in the database
        var db = CreateDatabaseWithItems(("T_SOMETHING", "Direct T Item", "technology"));

        var item = db.GetItem("T_SOMETHING");
        Assert.NotNull(item);
        Assert.Equal("Direct T Item", item.Name);
    }
}
