using NMSE.Data;

namespace NMSE.Core;

/// <summary>
/// Defines vehicle (exocraft) types and provides helpers for building export filenames.
/// </summary>
internal static class ExocraftLogic
{
    /// <summary>
    /// Known vehicle types with their save data indices and display names.
    /// </summary>
    internal static readonly (int Index, string Name)[] VehicleTypes =
    [
        (0, "Roamer"),
        (1, "Nomad"),
        (2, "Colossus"),
        (3, "Pilgrim"),
        (5, "Nautilon"),
        (6, "Minotaur")
    ];

    internal static readonly Dictionary<string, string> VehicleTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Roamer"] = "exocraft.type_roamer",
        ["Nomad"] = "exocraft.type_nomad",
        ["Colossus"] = "exocraft.type_colossus",
        ["Pilgrim"] = "exocraft.type_pilgrim",
        ["Nautilon"] = "exocraft.type_nautilon",
        ["Minotaur"] = "exocraft.type_minotaur",
    };

    internal static string GetLocalisedVehicleTypeName(string internalName)
    {
        if (VehicleTypeLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    // Per vehicle type:
    //   Standard exocraft:
    //   (Roamer, Nomad, Pilgrim)  -> [Exocraft, AllVehicles]
    //   Colossus: (truck)         -> [Colossus, Exocraft, AllVehicles]
    //   Nautilon: (sub)           -> [Submarine, AllVehicles]
    //   Minotaur: (mech)          -> [Mech, AllVehicles]
    /// <summary>
    /// Maps a vehicle display name to the Technology Category owner type
    /// used for inventory tech filtering. This determines which technology items
    /// can be installed in the vehicle's tech inventory.
    /// </summary>
    /// <param name="vehicleName">The vehicle display name (e.g. "Roamer", "Colossus", "Nautilon").</param>
    /// <returns>The Technology Category owner string for inventory filtering.</returns>
    internal static string GetOwnerTypeForVehicle(string vehicleName)
    {
        return vehicleName switch
        {
            "Colossus" => "Colossus",
            "Nautilon" => "Submarine",
            "Minotaur" => "Mech",
            _ => "Exocraft" // Roamer, Nomad, Pilgrim
        };
    }

    /// <summary>
    /// Builds a sanitized export filename for a vehicle inventory.
    /// </summary>
    /// <param name="vehicleName">The vehicle display name.</param>
    /// <param name="suffix">A suffix describing the inventory type (e.g. "cargo", "tech").</param>
    /// <returns>A filename-safe string ending with "_inv.json".</returns>
    internal static string BuildExportFileName(string vehicleName, string suffix)
    {
        string safeName = (vehicleName ?? "vehicle").Replace(' ', '_');
        return $"{safeName}_{suffix}_inv.json";
    }

    /// <summary>
    /// Builds a sanitized export filename for a whole vehicle export.
    /// </summary>
    /// <param name="vehicleName">The vehicle display name.</param>
    /// <returns>A filename-safe string ending with "_vehicle.json".</returns>
    internal static string BuildVehicleExportFileName(string vehicleName)
    {
        string safeName = (vehicleName ?? "vehicle").Replace(' ', '_');
        return $"{safeName}_vehicle.json";
    }
}
