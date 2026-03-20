using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Shared helper for reading and writing BaseStatValues across inventory logic classes.
/// </summary>
internal static class StatHelper
{
    /// <summary>
    /// Read a base stat value from an inventory's BaseStatValues array.
    /// Returns 0.0 if the inventory is null, the array is missing, or the stat ID is not found.
    /// </summary>
    internal static double ReadBaseStatValue(JsonObject? inventory, string statId)
    {
        if (inventory == null) return 0.0;
        try
        {
            var baseStatValues = inventory.GetArray("BaseStatValues");
            if (baseStatValues == null) return 0.0;
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                var entry = baseStatValues.GetObject(i);
                if (entry.GetString("BaseStatID") == statId)
                    return entry.GetDouble("Value");
            }
        }
        catch { }
        return 0.0;
    }

    /// <summary>
    /// Write a base stat value in an inventory's BaseStatValues array.
    /// No-op if the inventory is null, the array is missing, or the stat ID is not found.
    /// </summary>
    internal static void WriteBaseStatValue(JsonObject? inventory, string statId, double value)
    {
        if (inventory == null) return;
        try
        {
            var baseStatValues = inventory.GetArray("BaseStatValues");
            if (baseStatValues == null) return;
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                var entry = baseStatValues.GetObject(i);
                if (entry.GetString("BaseStatID") == statId)
                {
                    entry.Set("Value", value);
                    return;
                }
            }
        }
        catch { }
    }
}
