using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles starship data operations including loading, saving, type lookups, and inventory management.
/// </summary>
internal static class StarshipLogic
{
    /// <summary>
    /// Available ship class grades, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] ShipClasses = { "C", "B", "A", "S" };

    /// <summary>
    /// Maps English ship type display names to their UI localisation keys.
    /// </summary>
    private static readonly Dictionary<string, string> ShipTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hauler"] = "starship.type_hauler",
        ["Explorer"] = "starship.type_explorer",
        ["Shuttle"] = "starship.type_shuttle",
        ["Fighter"] = "starship.type_fighter",
        ["Exotic"] = "starship.type_exotic",
        ["Living Ship"] = "starship.type_living_ship",
        ["Solar"] = "starship.type_solar",
        ["Utopia Speeder"] = "starship.type_utopia_speeder",
        ["Golden Vector"] = "starship.type_golden_vector",
        ["Horizon Vector NX (Switch)"] = "starship.type_horizon_vector",
        ["Sentinel"] = "starship.type_sentinel",
        ["Starborn Runner"] = "starship.type_starborn_runner",
        ["Starborn Phoenix"] = "starship.type_starborn_phoenix",
        ["Corvette"] = "starship.type_corvette",
        ["Boundary Herald"] = "starship.type_boundary_herald",
        ["The Wraith"] = "starship.type_the_wraith",
        ["Interceptor"] = "starship.type_interceptor",
    };

    /// <summary>
    /// Gets the localised display name for a ship type given its English display name.
    /// Falls back to the English name if no localisation key is found.
    /// </summary>
    internal static string GetLocalisedShipTypeName(string englishName)
    {
        return ShipTypeLocKeys.TryGetValue(englishName, out var key) ? UiStrings.Get(key) : englishName;
    }

    /// <summary>
    /// Gets an array of ShipTypeItem wrappers for populating combo boxes.
    /// Each item displays a localised name but carries the English name for data lookups.
    /// </summary>
    internal static ShipTypeItem[] GetShipTypeItems()
    {
        return ShipInfo.Values
            .Select(info => info.DisplayName)
            .Distinct()
            .OrderBy(n => GetLocalisedShipTypeName(n))
            .Select(n => new ShipTypeItem(n, GetLocalisedShipTypeName(n)))
            .ToArray();
    }

    /// <summary>
    /// Maps ship model resource filenames to their display info (name, keywords, cargo dimensions, tech dimensions).
    /// </summary>
    internal static readonly Dictionary<string, (string DisplayName, string[] Keywords, string CargoDimensions, string TechDimensions)> ShipInfo =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN"] = ("Hauler", new[] { "DROPSHIP" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN"] = ("Explorer", new[] { "SCIENTIFIC" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SHUTTLE/SHUTTLE_PROC.SCENE.MBIN"] = ("Shuttle", new[] { "SHUTTLE" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"] = ("Fighter", new[] { "FIGHTER" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN"] = ("Exotic", new[] { "EXOTIC" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOSHIP_PROC.SCENE.MBIN"] = ("Living Ship", new[] { "BIOSHIP" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SAILSHIP/SAILSHIP_PROC.SCENE.MBIN"] = ("Solar", new[] { "SAILSHIP" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/VRSPEEDER.SCENE.MBIN"] = ("Utopia Speeder", new[] { "VRSPEEDER" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERCLASSICGOLD.SCENE.MBIN"] = ("Golden Vector", new[] { "FIGHTERCLASSICGOLD" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERSPECIALSWITCH.SCENE.MBIN"] = ("Horizon Vector NX (Switch)", new[] { "FIGHTERSPECIALSWITCH" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SENTINELSHIP/SENTINELSHIP_PROC.SCENE.MBIN"] = ("Sentinel", new[] { "SENTINEL" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACER.SCENE.MBIN"] = ("Starborn Runner", new[] { "WRACER.SCENE" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACERSE.SCENE.MBIN"] = ("Starborn Phoenix", new[] { "WRACERSE" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/BIGGS/BIGGS.SCENE.MBIN"] = ("Corvette", new[] { "BIGGS" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/SPOOKSHIP.SCENE.MBIN"] = ("Boundary Herald", new[] { "SPOOKSHIP" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOFIGHTER.SCENE.MBIN"] = ("The Wraith", new[] { "BIOFIGHTER" }, "10x12", "10x6"),
        };

    /// <summary>
    /// Retrieves the display name, cargo label, and tech label for a ship given its resource filename.
    /// Falls back to keyword matching if an exact filename match is not found.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns>A tuple containing the display name, cargo max label, and tech max label.</returns>
    internal static (string DisplayName, string CargoLabel, string TechLabel) GetShipInfo(string filename)
    {
        if (!string.IsNullOrEmpty(filename) && ShipInfo.TryGetValue(filename, out var info))
            return (info.DisplayName, UiStrings.Format("common.max_supported", info.CargoDimensions), UiStrings.Format("common.max_supported", info.TechDimensions));

        if (!string.IsNullOrEmpty(filename))
        {
            foreach (var entry in ShipInfo.Values)
            {
                if (entry.Keywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return (entry.DisplayName, UiStrings.Format("common.max_supported", entry.CargoDimensions), UiStrings.Format("common.max_supported", entry.TechDimensions));
            }
        }
        return (UiStrings.Get("common.unknown"), UiStrings.Format("common.max_supported", "?"), UiStrings.Format("common.max_supported", "10x6"));
    }

    /// <summary>
    /// Gets the display name for a ship type given its resource filename.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns>The ship type display name, or "Unknown" if not found.</returns>
    internal static string LookupShipTypeName(string filename)
    {
        var (displayName, _, _) = GetShipInfo(filename);
        return displayName;
    }

    /// <summary>
    /// Gets a sorted, distinct list of all known ship type display names.
    /// </summary>
    /// <returns>An array of unique ship type names in alphabetical order.</returns>
    internal static string[] GetShipTypeNames()
    {
        return ShipInfo.Values.Select(info => info.DisplayName).Distinct().OrderBy(n => n).ToArray();
    }

    /// <summary>
    /// Gets the resource filename for a given ship type display name.
    /// </summary>
    /// <param name="displayName">The ship type display name to look up.</param>
    /// <returns>The corresponding resource filename, or an empty string if not found.</returns>
    internal static string LookupFilenameForType(string displayName)
    {
        return ShipInfo.FirstOrDefault(
            kvp => kvp.Value.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
        ).Key ?? "";
    }



    /// <summary>
    /// Builds a list of owned ships from the ship ownership JSON array, skipping empty slots.
    /// </summary>
    /// <param name="shipOwnership">The JSON array of ship ownership entries.</param>
    /// <returns>A list of ship items with display names and data indices.</returns>
    internal static List<ShipListItem> BuildShipList(JsonArray shipOwnership)
    {
        var list = new List<ShipListItem>();
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                bool hasSeed = false;
                try
                {
                    var seedArr = resource?.GetArray("Seed");
                    if (seedArr != null && seedArr.Length > 0)
                        hasSeed = seedArr.GetBool(0);
                }
                catch { }

                if (!hasSeed) continue;

                string name = ship.GetString("Name") ?? "";
                if (string.IsNullOrEmpty(name))
                    name = $"Ship {i + 1}";
                list.Add(new ShipListItem(name, i));
            }
            catch
            {
                list.Add(new ShipListItem($"Ship {i + 1}", i));
            }
        }
        return list;
    }

    /// <summary>
    /// Loads ship data from a JSON ship object and optional player state for display and editing.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship.</param>
    /// <param name="playerState">The player state JSON object, used for legacy colour settings.</param>
    /// <returns>A populated <see cref="ShipData"/> instance.</returns>
    internal static ShipData LoadShipData(JsonObject ship, JsonObject? playerState, int shipIndex = -1)
    {
        string name = ship.GetString("Name") ?? "";

        string filename = "";
        string seed = "";
        try
        {
            var resource = ship.GetObject("Resource");
            filename = resource?.GetString("Filename") ?? "";
            seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
        }
        catch { }

        string shipTypeName = LookupShipTypeName(filename);
        var (_, cargoLabel, techLabel) = GetShipInfo(filename);

        string cls = "";
        try
        {
            var inv = ship.GetObject("Inventory");
            var classObj = inv?.GetObject("Class");
            cls = classObj?.GetString("InventoryClass") ?? "";
        }
        catch { }
        int classIndex = Array.IndexOf(ShipClasses, cls);

        bool useOldColours = false;
        try
        {
            if (playerState != null)
            {
                // ShipUsesLegacyColours is an array indexed per-ship
                var legacyArr = playerState.GetArray("ShipUsesLegacyColours");
                if (legacyArr != null && shipIndex >= 0 && shipIndex < legacyArr.Length)
                {
                    var val = legacyArr.Get(shipIndex);
                    if (val is bool b) useOldColours = b;
                }
            }
        }
        catch { }

        var shipInv = ship.GetObject("Inventory");
        double damage = 0, shield = 0, hyperdrive = 0, maneuver = 0;
        try { damage = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_DAMAGE"); } catch { }
        try { shield = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_SHIELD"); } catch { }
        try { hyperdrive = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_HYPERDRIVE"); } catch { }
        try { maneuver = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_AGILE"); } catch { }

        string safeName = FileNameHelper.SanitizeFileName(name);
        string safeTypeName = FileNameHelper.SanitizeFileName(shipTypeName);
        string cls2 = classIndex >= 0 ? ShipClasses[classIndex] : "C";

        var cfg = ExportConfig.Instance;
        var invVars = new Dictionary<string, string> { ["ship_name"] = safeName, ["type"] = safeTypeName, ["class"] = cls2 };

        return new ShipData
        {
            Name = name,
            Filename = filename,
            ShipTypeName = shipTypeName,
            Seed = seed,
            ClassIndex = classIndex,
            UseOldColours = useOldColours,
            Damage = damage,
            Shield = shield,
            Hyperdrive = hyperdrive,
            Maneuver = maneuver,
            Inventory = shipInv,
            TechInventory = ship.GetObject("Inventory_TechOnly"),
            CargoMaxLabel = cargoLabel,
            TechMaxLabel = techLabel,
            InvExportFileName = ExportConfig.BuildFileName(cfg.StarshipCargoTemplate, cfg.StarshipCargoExt, invVars),
            TechExportFileName = ExportConfig.BuildFileName(cfg.StarshipTechTemplate, cfg.StarshipTechExt, invVars)
        };
    }

    /// <summary>
    /// Saves ship data back to the JSON ship and player state objects.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship.</param>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="values">The values to write.</param>
    internal static void SaveShipData(JsonObject ship, JsonObject playerState, ShipSaveValues values)
    {
        // Always write name (allow empty string to clear a ship name)
        ship.Set("Name", values.Name ?? "");

        if (!string.IsNullOrEmpty(values.SelectedTypeName))
        {
            string filename = LookupFilenameForType(values.SelectedTypeName);
            var resource = ship.GetObject("Resource");
            if (resource != null && !string.IsNullOrEmpty(filename))
                resource.Set("Filename", filename);
        }

        if (values.ClassIndex >= 0)
        {
            string cls = ShipClasses[values.ClassIndex];
            // Set class on all ship inventories (Inventory, Inventory_TechOnly, Inventory_Cargo)
            // Sets class on all inventory objects.
            foreach (string invKey in new[] { "Inventory", "Inventory_TechOnly", "Inventory_Cargo" })
            {
                var inventory = ship.GetObject(invKey);
                var classObj = inventory?.GetObject("Class");
                classObj?.Set("InventoryClass", cls);
            }
        }

        try
        {
            var resource = ship.GetObject("Resource");
            var seedArr = resource?.GetArray("Seed");
            var normalizedSeed = SeedHelper.NormalizeSeed(values.Seed);
            if (seedArr != null && seedArr.Length > 1 && normalizedSeed != null)
                seedArr.Set(1, normalizedSeed);
        }
        catch { }

        // Write base stats to ALL inventories to keep them in sync
        // Determine ship category for clamping: "Alien" if the selected type contains it, else "Normal"
        string shipCategory = (values.SelectedTypeName ?? "").Contains("Alien", StringComparison.OrdinalIgnoreCase) ? "Alien" : "Normal";

        double writeDamage = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_DAMAGE", values.Damage, Data.StatCategory.Ship, values.RawStatValues);
        double writeShield = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_SHIELD", values.Shield, Data.StatCategory.Ship, values.RawStatValues);
        double writeHyperdrive = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_HYPERDRIVE", values.Hyperdrive, Data.StatCategory.Ship, values.RawStatValues);
        double writeManeuver = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_AGILE", values.Maneuver, Data.StatCategory.Ship, values.RawStatValues);

        foreach (string invKey in new[] { "Inventory", "Inventory_TechOnly", "Inventory_Cargo" })
        {
            var inv = ship.GetObject(invKey);
            if (inv == null) continue;
            StatHelper.WriteBaseStatValue(inv, "^SHIP_DAMAGE", writeDamage);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_SHIELD", writeShield);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_HYPERDRIVE", writeHyperdrive);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_AGILE", writeManeuver);
        }

        // ShipUsesLegacyColours is an array indexed per-ship; update the correct element
        try
        {
            if (values.ShipIndex >= 0)
            {
                var legacyArr = playerState.GetArray("ShipUsesLegacyColours");
                if (legacyArr != null && values.ShipIndex < legacyArr.Length)
                    legacyArr.Set(values.ShipIndex, values.UseOldColours);
            }
        }
        catch { }

        try { playerState.Set("PrimaryShip", values.PrimaryShipIndex); }
        catch { }
    }

    /// <summary>
    /// Fully resets a ship slot by clearing its resource, name, inventories,
    /// and all associated data. The entry remains in the ShipOwnership array to
    /// preserve index alignment with parallel arrays such as ShipUsesLegacyColours.
    /// The slot is filtered out by BuildShipList() because Seed[0] becomes false.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship to delete.</param>
    internal static void DeleteShipData(JsonObject ship)
    {
        // Clear the resource (filename + seed) - this is what marks the slot as empty
        var resource = ship.GetObject("Resource");
        if (resource != null)
        {
            resource.Set("Filename", "");
            var seedArr = resource.GetArray("Seed");
            if (seedArr != null && seedArr.Length > 1)
            {
                seedArr.Set(0, false);
                seedArr.Set(1, "0x0");
            }
        }

        // Clear the ship name
        ship.Set("Name", "");

        // Clear all three inventory types (Slots, BaseStatValues, ValidSlotIndices)
        ResetInventoryObject(ship.GetObject("Inventory"));
        ResetInventoryObject(ship.GetObject("Inventory_TechOnly"));
        ResetInventoryObject(ship.GetObject("Inventory_Cargo"));
    }

    /// <summary>
    /// Resets an inventory JSON object by clearing its Slots, ValidSlotIndices,
    /// BaseStatValues, and SpecialSlots arrays while preserving the object structure.
    /// </summary>
    private static void ResetInventoryObject(JsonObject? inventory)
    {
        if (inventory == null) return;

        ClearJsonArray(inventory.GetArray("Slots"));
        ClearJsonArray(inventory.GetArray("ValidSlotIndices"));
        ClearJsonArray(inventory.GetArray("BaseStatValues"));
        ClearJsonArray(inventory.GetArray("SpecialSlots"));
    }

    // --- Ship Customisation Data (CharacterCustomisationData) --------

    /// <summary>
    /// The CharacterCustomisationData array contains 26 entries. Entries at indices
    /// 3–8 correspond to ship slots 0–5 and entries at indices 17–22 correspond to
    /// ship slots 6–11. This method converts a ShipOwnership index to the matching
    /// CharacterCustomisationData index.
    /// </summary>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <returns>The corresponding CharacterCustomisationData index, or -1 if out of range.</returns>
    internal static int ShipIndexToCcdIndex(int shipIndex)
    {
        if (shipIndex < 0 || shipIndex > 11) return -1;
        return shipIndex < 6 ? shipIndex + 3 : shipIndex - 6 + 17;
    }

    /// <summary>
    /// Resets the CharacterCustomisationData entry for a specific ship slot
    /// by clearing its DescriptorGroups, Colours, TextureOptions, and BoneScales
    /// arrays, resetting PaletteID/FCx to "^" and Scale to 1.0.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array (expected 26 entries).</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    internal static void ResetShipCustomisation(JsonArray? ccdArray, int shipIndex)
    {
        if (ccdArray == null) return;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

        try
        {
            var entry = ccdArray.GetObject(ccdIdx);
            entry.Set("SelectedPreset", "^");
            var cd = entry.GetObject("CustomData");
            if (cd != null)
                ResetCustomDataObject(cd);
        }
        catch { }
    }

    /// <summary>
    /// Resets a CustomData object to its empty/default state.
    /// </summary>
    private static void ResetCustomDataObject(JsonObject cd)
    {
        ClearJsonArray(cd.GetArray("DescriptorGroups"));
        cd.Set("PaletteID", "^");
        ClearJsonArray(cd.GetArray("Colours"));
        ClearJsonArray(cd.GetArray("TextureOptions"));
        ClearJsonArray(cd.GetArray("BoneScales"));
        cd.Set("Scale", 1.0);
    }

    /// <summary>
    /// Removes all elements from a JSON array (if it exists).
    /// </summary>
    private static void ClearJsonArray(JsonArray? arr)
    {
        if (arr == null) return;
        for (int i = arr.Length - 1; i >= 0; i--)
            arr.RemoveAt(i);
    }

    /// <summary>
    /// Retrieves the CharacterCustomisationData entry for a specific ship slot.
    /// Returns <c>null</c> if the array is missing or the index is out of range.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array.</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <returns>A deep-clone of the CCD entry, or <c>null</c>.</returns>
    internal static JsonObject? GetShipCustomisation(JsonArray? ccdArray, int shipIndex)
    {
        if (ccdArray == null) return null;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return null;
        try
        {
            return ccdArray.GetObject(ccdIdx).DeepClone();
        }
        catch { return null; }
    }

    /// <summary>
    /// Writes a CharacterCustomisationData entry into the CCD array for a specific
    /// ship slot. All properties from <paramref name="ccdEntry"/> are copied into
    /// the target slot. If <paramref name="ccdEntry"/> is <c>null</c>, the slot is
    /// reset to default values instead.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array.</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <param name="ccdEntry">The CCD entry to write, or <c>null</c> to reset.</param>
    internal static void SetShipCustomisation(JsonArray? ccdArray, int shipIndex, JsonObject? ccdEntry)
    {
        if (ccdArray == null) return;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

        if (ccdEntry == null)
        {
            ResetShipCustomisation(ccdArray, shipIndex);
            return;
        }

        try
        {
            var target = ccdArray.GetObject(ccdIdx);
            foreach (var name in ccdEntry.Names())
                target.Set(name, ccdEntry.Get(name));
        }
        catch { }
    }

    /// <summary>
    /// Counts the number of valid (non-invalidated) ships in the ownership array.
    /// A ship is valid when its Resource.Seed[0] is true.
    /// </summary>
    internal static int CountValidShips(JsonArray shipOwnership)
    {
        int count = 0;
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                var seedArr = resource?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    count++;
            }
            catch { }
        }
        return count;
    }

    /// <summary>
    /// Returns the array index of the first valid (non-invalidated) ship, or -1 if none.
    /// </summary>
    internal static int FindFirstValidShipIndex(JsonArray shipOwnership)
    {
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                var seedArr = resource?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    return i;
            }
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Determines whether a ship filename corresponds to a Corvette type.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns><c>true</c> if the filename indicates a Corvette; otherwise <c>false</c>.</returns>
    internal static bool IsCorvette(string filename)
    {
        return !string.IsNullOrEmpty(filename) &&
               filename.Contains("BIGGS", StringComparison.OrdinalIgnoreCase);
    }

    // Per ship type:
    //   Normal ships   -> [Ship, AllShips, AllShipsExceptAlien]
    //   Living Ship    -> [AlienShip, AllShips]
    //   Robot/Sentinel -> [RobotShip, AllShips, AllShipsExceptAlien]
    //   Corvette       -> [Corvette, AllShips, AllShipsExceptAlien]
    /// <summary>
    /// Maps a ship type display name to the Technology Category owner type
    /// used for inventory tech filtering. This determines which technology items
    /// can be installed in the ship's tech inventory.
    /// </summary>
    /// <param name="shipTypeName">The ship type display name (e.g. "Fighter", "Living Ship", "Corvette").</param>
    /// <returns>The Technology Category owner string for inventory filtering.</returns>
    internal static string GetOwnerTypeForShip(string shipTypeName)
    {
        return shipTypeName switch
        {
            "Living Ship" => "AlienShip",
            "Sentinel" => "RobotShip",
            "Corvette" => "Corvette",
            _ => "Ship" // Fighter, Hauler, Explorer, Shuttle, Exotic, Solar, etc.
        };
    }

    /// <summary>
    /// Converts a hexadecimal seed string (e.g. "0x1A2B..") to its decimal representation.
    /// </summary>
    /// <param name="hexSeed">The hex seed string, optionally prefixed with "0x".</param>
    /// <returns>The decimal value, or 0 if the string is empty or invalid.</returns>
    internal static long SeedToDecimal(string hexSeed)
    {
        if (string.IsNullOrEmpty(hexSeed)) return 0;
        var s = hexSeed.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        if (long.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out long result))
            return result;
        return 0;
    }

    /// <summary>
    /// Finds the index of a corvette's player ship base entry by matching its seed against the base owner timestamp.
    /// The game can drift the TS epoch timestamp by up to ~120 seconds relative to the seed, so this method
    /// uses a 3-tier fallback: exact match -> +/-1s -> +/-60s -> +/-120s. When multiple candidates fall within
    /// the same tolerance tier, the one with the smallest absolute delta is chosen.
    /// </summary>
    /// <param name="bases">The persistent player bases JSON array.</param>
    /// <param name="seedDecimal">The decimal seed value to match.</param>
    /// <returns>The base index, or -1 if not found.</returns>
    internal static int FindCorvetteBaseIndex(JsonArray? bases, long seedDecimal)
    {
        if (bases == null || seedDecimal == 0) return -1;

        // Tolerance tiers in seconds (ascending).
        ReadOnlySpan<long> tolerances = [0, 1, 60, 120];

        foreach (long tol in tolerances)
        {
            int bestIndex = -1;
            long bestDelta = long.MaxValue;

            for (int i = 0; i < bases.Length; i++)
            {
                try
                {
                    var b = bases.GetObject(i);
                    var owner = b.GetObject("Owner");
                    if (owner == null) continue;

                    long ts = 0;
                    try { ts = (long)owner.GetDouble("TS"); } catch { }

                    long delta = Math.Abs(ts - seedDecimal);
                    if (delta > tol) continue;

                    var baseType = b.GetObject("BaseType");
                    if (baseType == null) continue;
                    string bt = baseType.GetString("PersistentBaseTypes") ?? "";
                    if (!bt.Equals("PlayerShipBase", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestIndex = i;
                    }
                }
                catch { }
            }

            if (bestIndex >= 0) return bestIndex;
        }

        return -1;
    }

    /// <summary>
    /// Gets the display name of the primary ship from the ship ownership array.
    /// </summary>
    /// <param name="shipOwnership">The JSON array of ship ownership entries.</param>
    /// <param name="primaryIndex">The index of the primary ship.</param>
    /// <returns>The ship name, or "Unknown" if unavailable.</returns>
    internal static string GetPrimaryShipName(JsonArray? shipOwnership, int primaryIndex)
    {
        if (shipOwnership == null || primaryIndex < 0 || primaryIndex >= shipOwnership.Length)
            return "Unknown";
        try
        {
            var ship = shipOwnership.GetObject(primaryIndex);
            string name = ship.GetString("Name") ?? "";
            return string.IsNullOrEmpty(name) ? $"Ship {primaryIndex + 1}" : name;
        }
        catch { return "Unknown"; }
    }

    /// <summary>
    /// Represents an item in the ship selection list.
    /// </summary>
    internal sealed class ShipListItem
    {
        /// <summary>The display name shown for this ship.</summary>
        public string DisplayName { get; set; }
        /// <summary>The index of this ship in the ship ownership array.</summary>
        public int DataIndex { get; }

        /// <summary>
        /// Initializes a new ship list item.
        /// </summary>
        /// <param name="displayName">The display name for the ship.</param>
        /// <param name="dataIndex">The index in the ownership array.</param>
        public ShipListItem(string displayName, int dataIndex)
        {
            DisplayName = displayName;
            DataIndex = dataIndex;
        }

        /// <inheritdoc/>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Represents a ship type in the type selection combo box.
    /// Carries the English internal name for data lookups while displaying a localised name.
    /// </summary>
    internal sealed class ShipTypeItem
    {
        /// <summary>The English ship type name used for data lookups via LookupFilenameForType.</summary>
        public string InternalName { get; }
        /// <summary>The localised display name shown in the combo box.</summary>
        public string DisplayName { get; }

        public ShipTypeItem(string internalName, string displayName)
        {
            InternalName = internalName;
            DisplayName = displayName;
        }

        /// <inheritdoc/>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Holds loaded ship data for display and editing in the UI.
    /// </summary>
    internal sealed class ShipData
    {
        /// <summary>The player-assigned ship name.</summary>
        public string Name { get; set; } = "";
        /// <summary>The ship model resource filename.</summary>
        public string Filename { get; set; } = "";
        /// <summary>The resolved ship type display name (e.g. "Fighter", "Hauler").</summary>
        public string ShipTypeName { get; set; } = "";
        /// <summary>The ship's procedural generation seed as a hex string.</summary>
        public string Seed { get; set; } = "";
        /// <summary>Index into <see cref="ShipClasses"/> for the ship's class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>Whether the ship uses legacy colour rendering.</summary>
        public bool UseOldColours { get; set; }
        /// <summary>The ship's base damage stat.</summary>
        public double Damage { get; set; }
        /// <summary>The ship's base shield stat.</summary>
        public double Shield { get; set; }
        /// <summary>The ship's base hyperdrive stat.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The ship's base maneuverability stat.</summary>
        public double Maneuver { get; set; }
        /// <summary>The ship's cargo inventory JSON object.</summary>
        public JsonObject? Inventory { get; set; }
        /// <summary>The ship's tech-only inventory JSON object.</summary>
        public JsonObject? TechInventory { get; set; }
        /// <summary>Label describing the maximum supported cargo inventory size.</summary>
        public string CargoMaxLabel { get; set; } = "";
        /// <summary>Label describing the maximum supported tech inventory size.</summary>
        public string TechMaxLabel { get; set; } = "";
        /// <summary>Suggested filename for exporting the cargo inventory.</summary>
        public string InvExportFileName { get; set; } = "";
        /// <summary>Suggested filename for exporting the tech inventory.</summary>
        public string TechExportFileName { get; set; } = "";
    }

    /// <summary>
    /// Holds values to be saved back to a ship's JSON data.
    /// </summary>
    internal sealed class ShipSaveValues
    {
        /// <summary>The ship name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>The selected ship type display name, or <c>null</c> to leave unchanged.</summary>
        public string? SelectedTypeName { get; set; }
        /// <summary>Index into <see cref="ShipClasses"/> for the desired class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>The seed hex string to set.</summary>
        public string Seed { get; set; } = "";
        /// <summary>The damage stat value to write.</summary>
        public double Damage { get; set; }
        /// <summary>The shield stat value to write.</summary>
        public double Shield { get; set; }
        /// <summary>The hyperdrive stat value to write.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The maneuverability stat value to write.</summary>
        public double Maneuver { get; set; }
        /// <summary>Whether to use legacy colour rendering.</summary>
        public bool UseOldColours { get; set; }
        /// <summary>The zero-based index of this ship in the ShipOwnership array.</summary>
        public int ShipIndex { get; set; } = -1;
        /// <summary>The index of the ship to set as primary.</summary>
        public int PrimaryShipIndex { get; set; }

        /// <summary>Raw (unclamped) stat values read from JSON at load time.
        /// When set, each stat is only written if the UI value differs from
        /// the clamped raw value - preserving externally-edited values.</summary>
        public Dictionary<string, double>? RawStatValues { get; set; }
    }
}
