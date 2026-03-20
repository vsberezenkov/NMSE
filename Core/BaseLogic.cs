using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Provides base-building operations and defines storage inventory keys for chest and special inventories.
/// </summary>
internal static class BaseLogic
{
    /// <summary>
    /// Swaps the Position, Up, and At fields between two base objects.
    /// </summary>
    /// <param name="a">The first base JSON object.</param>
    /// <param name="b">The second base JSON object.</param>
    internal static void SwapPositions(JsonObject a, JsonObject b)
    {
        foreach (string field in new[] { "Position", "Up", "At" })
        {
            var aVal = a.Get(field);
            var bVal = b.Get(field);
            a.Set(field, bVal);
            b.Set(field, aVal);
        }
    }

    /// <summary>
    /// JSON keys for the 10 standard storage container chest inventories.
    /// </summary>
    internal static readonly string[] ChestInventoryKeys =
    {
        "Chest1Inventory", "Chest2Inventory", "Chest3Inventory", "Chest4Inventory", "Chest5Inventory",
        "Chest6Inventory", "Chest7Inventory", "Chest8Inventory", "Chest9Inventory", "Chest10Inventory"
    };

    /// <summary>
    /// Definitions for special storage inventories, including their JSON key, display name, and export filename.
    /// </summary>
    internal static readonly (string Key, string DisplayName, string ExportFileName)[] StorageInventories =
    {
        ("CookingIngredientsInventory", "Ingredient Storage", "Ingredient_Storage.json"),
        ("CorvetteStorageInventory", "Corvette Parts Cache", "Corvette_Parts_Cache.json"),
        ("ChestMagicInventory", "Base Salvage Capsule", "Base_Salvage_Capsule.json"),
        ("RocketLockerInventory", "Rocket", "Rocket.json"),
        ("FishPlatformInventory", "Fishing Platform", "Fishing_Platform.json"),
        ("FishBaitBoxInventory", "Fish Bait", "Fish_Bait.json"),
        ("FoodUnitInventory", "Food Unit", "Food_Unit.json"),
        ("ChestMagic2Inventory", "Freighter Refund (unused)", "Freighter_Refund.json"),
    };

    /// <summary>
    /// The default internal name for unnamed storage containers in NMS save data.
    /// When a chest has this name, the player has not assigned a custom name.
    /// </summary>
    internal const string DefaultChestName = "BLD_STORAGE_NAME";

    /// <summary>
    /// Reads the display name from a chest inventory JSON object.
    /// Returns an empty string if the name is the default <see cref="DefaultChestName"/> or absent.
    /// </summary>
    /// <param name="chestInventory">The chest inventory JSON object (e.g. <c>Chest1Inventory</c>).</param>
    /// <returns>The custom name, or an empty string if unnamed.</returns>
    internal static string GetChestName(JsonObject? chestInventory)
    {
        if (chestInventory == null) return "";
        string? name = chestInventory.GetString("Name");
        if (string.IsNullOrEmpty(name) || name == DefaultChestName)
            return "";
        return name;
    }

    /// <summary>
    /// Sets the display name on a chest inventory JSON object.
    /// If the new name is null or empty, resets to <see cref="DefaultChestName"/>.
    /// </summary>
    /// <param name="chestInventory">The chest inventory JSON object to modify.</param>
    /// <param name="newName">The new name to set, or null/empty to reset.</param>
    internal static void SetChestName(JsonObject? chestInventory, string? newName)
    {
        if (chestInventory == null) return;
        string value = string.IsNullOrWhiteSpace(newName) ? DefaultChestName : newName.Trim();
        chestInventory.Set("Name", value);
    }

    /// <summary>
    /// Formats a chest tab title. If the chest has a custom name, appends it
    /// after the tab label (e.g. "Chest 0: Cooking Items").
    /// </summary>
    /// <param name="tabLabel">The base tab label (e.g. "Chest 0").</param>
    /// <param name="chestName">The custom name, or empty/null if unnamed.</param>
    /// <returns>The formatted tab title.</returns>
    internal static string FormatChestTabTitle(string tabLabel, string? chestName)
    {
        if (string.IsNullOrEmpty(chestName))
            return tabLabel;
        return $"{tabLabel}: {chestName}";
    }
}
