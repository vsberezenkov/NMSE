using NMSE.Data;
using NMSE.IO;

namespace NMSE.Tests;

public class ResolveSaveInventoryTypeTests
{
    [Theory]
    [InlineData("Technology", false, "Technology")]
    [InlineData("Upgrades", false, "Product")]
    [InlineData("Technology Module", false, "Product")]
    [InlineData("Constructed Technology", false, "Product")]
    [InlineData("Others", false, "Product")]
    [InlineData("Raw Materials", false, "Substance")]
    [InlineData("Products", false, "Product")]
    public void NonTechInventory_UsesStandardMapping(string itemType, bool isTech, string expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.ResolveSaveInventoryType(itemType, isTech));
    }

    [Theory]
    [InlineData("Technology", true, "Technology")]
    [InlineData("Upgrades", true, "Product")]
    [InlineData("Technology Module", true, "Product")]
    [InlineData("Constructed Technology", true, "Product")]
    [InlineData("Others", true, "Product")]
    [InlineData("Products", true, "Product")]
    [InlineData("Raw Materials", true, "Substance")]
    public void TechInventory_UsesNativeInventoryType(string itemType, bool isTech, string expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.ResolveSaveInventoryType(itemType, isTech));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsConstructedTechnology()
    {
        // "Constructed Technology" items are Products (e.g. ACCESS3).
        // They are NOT accepted in tech-only inventories unless they have a TechnologyCategory.
        var item = new GameItem { Id = "UT_SHIPMINI", ItemType = "Constructed Technology", Category = "AllShips", TechnologyCategory = "AllShips" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsOthersWithTechCategory()
    {
        var item = new GameItem
        {
            Id = "T_SHIP_RAINBOW",
            ItemType = "Others",
            Category = "AllShips",
            TechnologyCategory = "AllShips"
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_RejectsOthersWithoutTechCategory()
    {
        var item = new GameItem { Id = "TEST", ItemType = "Others", Category = "Curiosity" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }
}
