namespace NMSE.Data;

/// <summary>Represents a single frigate trait with its stat bonus/penalty.</summary>
public class FrigateTrait
{
    /// <summary>Unique trait identifier (e.g. "^FUEL_PRI").</summary>
    public string Id { get; set; } = "";
    /// <summary>Human-readable trait name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Localisation lookup key for Name. Null when not available.</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Stat type affected by this trait (e.g. "FUELCAPACITY", "COMBAT").</summary>
    public string Type { get; set; } = "";
    /// <summary>Numeric strength of the trait effect; negative values reduce fuel consumption.</summary>
    public int Strength { get; set; }
    /// <summary>Whether this trait is considered beneficial to the frigate.</summary>
    public bool Beneficial { get; set; }
    /// <summary>Primary fleet class this trait applies to (empty if secondary/tertiary).</summary>
    public string Primary { get; set; } = "";
    /// <summary>Comma-separated secondary fleet classes this trait applies to.</summary>
    public string Secondary { get; set; } = "";

    /// <summary>
    /// Display name including strength and type for use in ComboBox dropdowns.
    /// Format: "{Name} [{Strength} {TypeName}]" with Speed type expressed as percentage.
    /// Examples: "Support Specialist [-15 Expedition Fuel Cost]", "Quick Navigator [+3% Expedition Duration]"
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (Strength == 0) return Name;
            string typeName = FrigateTraitDatabase.GetTypeDisplayName(Type);
            bool isPercent = string.Equals(Type, "Speed", StringComparison.OrdinalIgnoreCase);
            string suffix = isPercent ? "%" : "";
            return $"{Name} [{Strength:+0;-0}{suffix} {typeName}]";
        }
    }

    /// <summary>Returns the display name including strength indicator.</summary>
    public override string ToString() => DisplayName;
}

/// <summary>Static database of all known frigate traits and their stat effects.</summary>
public static class FrigateTraitDatabase
{
    /// <summary>
    /// All known frigate traits. Populated at startup from FrigateTraits.json
    /// via <see cref="LoadFromFile"/>. Empty until loaded.
    /// </summary>
    public static readonly IReadOnlyList<FrigateTrait> Traits = new List<FrigateTrait>();

    // Trait data is now loaded from Resources/json/FrigateTraits.json at startup.
    // The JSON file is produced by the extractor's ParseFrigateTraits() method
    // from FRIGATETRAITTABLE.MXML and contains all trait IDs, names, loc keys,
    // stat types, strengths, beneficial flags, and primary/secondary assignments.
    // For reference, the original 178 traits ranged from FUEL_PRI through
    // LIVING_SPE_T_3 covering fuel, combat, exploration, mining, trade,
    // deep-space, living-ship, pirate, and normandy trait families.


    /// <summary>Lookup dictionary mapping trait ID to its entry.</summary>
    public static readonly Dictionary<string, FrigateTrait> ById =
        Traits.ToDictionary(t => t.Id, StringComparer.Ordinal);

    /// <summary>
    /// Empty/none sentinel used in trait dropdowns.
    /// The game uses "^" to represent empty/unset string values.
    /// </summary>
    public static readonly FrigateTrait None = new() { Id = "^", Name = "(None)", Beneficial = false };

    private static readonly Dictionary<string, string> _englishNameBackup = new();

    /// <summary>
    /// Maps raw trait type strings from game data to their in-game display names.
    /// Used by <see cref="FrigateTrait.DisplayName"/> to show human-readable stat types.
    /// </summary>
    private static readonly Dictionary<string, string> TypeDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FuelCapacity"]  = "Expedition Fuel Cost",
        ["FuelBurnRate"]  = "Cost per Warp",
        ["Combat"]        = "Combat",
        ["Mining"]        = "Industry",
        ["Diplomatic"]    = "Trading",
        ["Exploration"]   = "Exploration",
        ["Speed"]         = "Expedition Duration",
        ["Invulnerable"]  = "Damage Reduction",
        ["Stealth"]       = "Stealth",
    };

    /// <summary>
    /// Returns the human-readable display name for a trait stat type.
    /// Falls back to the localised UI string if available, then the raw type string.
    /// </summary>
    public static string GetTypeDisplayName(string type)
    {
        if (string.IsNullOrEmpty(type)) return "";
        string locKey = $"frigate.trait_type_{type.ToLowerInvariant()}";
        string loc = UiStrings.Get(locKey);
        if (!string.IsNullOrEmpty(loc) && loc != locKey) return loc;
        return TypeDisplayNames.TryGetValue(type, out var name) ? name : type;
    }

    /// <summary>Looks up a trait's display name by its ID.</summary>
    /// <param name="id">The trait ID to look up.</param>
    /// <returns>The trait name, or the raw ID if not found.</returns>
    public static string LookupName(string? id)
    {
        if (string.IsNullOrEmpty(id) || id == "^") return UiStrings.Get("frigate.trait_none");
        return ById.TryGetValue(id, out var entry) ? entry.Name : id;
    }

    /// <summary>
    /// Loads frigate traits from a FrigateTraits.json file.
    /// Falls back to hardcoded data if the file is missing or invalid.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!System.IO.File.Exists(jsonPath)) return false;

        try
        {
            var content = System.IO.File.ReadAllBytes(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return false;

            var loaded = new List<FrigateTrait>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                loaded.Add(new FrigateTrait
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    Name = elem.TryGetProperty("Name", out var nP) ? nP.GetString() ?? "" : "",
                    NameLocStr = elem.TryGetProperty("Name_LocStr", out var nlP) ? nlP.GetString() : null,
                    Type = elem.TryGetProperty("Type", out var tP) ? tP.GetString() ?? "" : "",
                    Strength = elem.TryGetProperty("Strength", out var sP) && sP.TryGetInt32(out int s) ? s : 0,
                    Beneficial = elem.TryGetProperty("Beneficial", out var bP) && bP.ValueKind == System.Text.Json.JsonValueKind.True,
                    Primary = elem.TryGetProperty("Primary", out var pP) ? pP.GetString() ?? "" : "",
                    Secondary = elem.TryGetProperty("Secondary", out var secP) ? secP.GetString() ?? "" : "",
                });
            }

            if (loaded.Count > 0)
            {
                // Replace list contents with loaded data
                var list = (List<FrigateTrait>)Traits;
                list.Clear();
                list.AddRange(loaded);

                // Rebuild ById lookup
                ById.Clear();
                foreach (var t in loaded) ById[t.Id] = t;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Applies localisation to trait names using the specified service.
    /// </summary>
    public static int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive) { RevertLocalisation(); return 0; }

        int count = 0;
        foreach (var trait in Traits)
        {
            if (!_englishNameBackup.ContainsKey(trait.Id))
                _englishNameBackup[trait.Id] = trait.Name;

            trait.Name = _englishNameBackup[trait.Id];

            if (!string.IsNullOrEmpty(trait.NameLocStr))
            {
                var loc = service.Lookup(trait.NameLocStr);
                if (loc != null) { trait.Name = loc; count++; }
            }
        }
        return count;
    }

    /// <summary>
    /// Reverts all trait names to their original English values.
    /// </summary>
    public static void RevertLocalisation()
    {
        foreach (var kvp in _englishNameBackup)
        {
            if (ById.TryGetValue(kvp.Key, out var trait))
                trait.Name = kvp.Value;
        }
        _englishNameBackup.Clear();
    }
}
