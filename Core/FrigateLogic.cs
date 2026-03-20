using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles frigate data operations including type/grade lookups, expedition state tracking, and level-up calculations.
/// </summary>
internal static class FrigateLogic
{
    /// <summary>
    /// Known frigate type identifiers.
    /// </summary>
    internal static readonly string[] FrigateTypes =
    {
        "Combat", "Exploration", "Mining", "Diplomacy", "Support",
        "Normandy", "DeepSpace", "DeepSpaceCommon", "Pirate", "GhostShip"
    };

    /// <summary>
    /// Available frigate grade letters, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] FrigateGrades = { "C", "B", "A", "S" };

    /// <summary>
    /// Known frigate crew race types (internal NMS identifiers).
    /// </summary>
    internal static readonly string[] FrigateRaces = { "Traders", "Warriors", "Explorers" };

    /// <summary>
    /// Player-facing display names for frigate races, matching the order of <see cref="FrigateRaces"/>.
    /// </summary>
    internal static readonly string[] FrigateRaceDisplayNames = { "Gek", "Vy'keen", "Korvax" };

    internal static readonly Dictionary<string, string> FrigateTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Combat"] = "frigate.type_combat",
        ["Exploration"] = "frigate.type_exploration",
        ["Mining"] = "frigate.type_mining",
        ["Diplomacy"] = "frigate.type_diplomacy",
        ["Support"] = "frigate.type_support",
        ["Normandy"] = "frigate.type_normandy",
        ["DeepSpace"] = "frigate.type_deep_space",
        ["DeepSpaceCommon"] = "frigate.type_deep_space_common",
        ["Pirate"] = "frigate.type_pirate",
        ["GhostShip"] = "frigate.type_ghost_ship",
    };

    internal static string GetLocalisedFrigateTypeName(string internalName)
    {
        if (FrigateTypeLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    internal static readonly Dictionary<string, string> FrigateRaceLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Traders"] = "frigate.race_traders",
        ["Warriors"] = "frigate.race_warriors",
        ["Explorers"] = "frigate.race_explorers",
    };

    internal static string GetLocalisedFrigateRaceName(string internalRace)
    {
        if (FrigateRaceLocKeys.TryGetValue(internalRace, out var key))
            return UiStrings.Get(key);
        return internalRace;
    }

    /// <summary>
    /// Maps an internal NMS race name to a player-facing display name.
    /// </summary>
    internal static string RaceToDisplayName(string internalRace)
    {
        int idx = Array.IndexOf(FrigateRaces, internalRace);
        return idx >= 0 ? FrigateRaceDisplayNames[idx] : internalRace;
    }

    /// <summary>
    /// Maps a player-facing display name back to the internal NMS race name.
    /// </summary>
    internal static string DisplayNameToRace(string displayName)
    {
        int idx = Array.IndexOf(FrigateRaceDisplayNames, displayName);
        return idx >= 0 ? FrigateRaces[idx] : displayName;
    }

    /// <summary>
    /// Display labels for frigate stat categories.
    /// </summary>
    internal static readonly string[] StatNames =
    {
        "Combat", "Exploration", "Industry", "Trading",
        "Cost Per Warp", "Expedition Fuel Cost", "Expedition Duration",
        "Loot", "Repair", "Damage Reduction", "Stealth"
    };

    /// <summary>
    /// Gets the display name for a frigate at the specified index.
    /// </summary>
    /// <param name="frigate">The frigate JSON object.</param>
    /// <param name="index">The frigate's position index.</param>
    /// <returns>The custom name, or a default "Frigate N" label.</returns>
    internal static string GetFrigateName(JsonObject frigate, int index)
    {
        try { return frigate.GetString("CustomName") ?? UiStrings.Format("frigate.list_format", index + 1); }
        catch { return UiStrings.Format("frigate.list_format", index + 1); }
    }

    /// <summary>
    /// Gets the frigate's class type string (e.g. "Combat", "Exploration").
    /// </summary>
    /// <param name="frigate">The frigate JSON object.</param>
    /// <returns>The frigate class type, or an empty string if unavailable.</returns>
    internal static string GetFrigateType(JsonObject frigate)
    {
        try
        {
            return frigate.GetObject("FrigateClass")?.GetString("FrigateClass") ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Computes the frigate's grade (C/B/A/S) from its traits using net scoring.
    /// Beneficial traits count as +1, negative traits as -1.
    /// </summary>
    /// <param name="frigate">The frigate JSON object containing trait IDs.</param>
    /// <returns>The computed grade letter.</returns>
    internal static string ComputeClassFromTraits(JsonObject frigate)
    {
        int netScore = 0;
        try
        {
            var traits = frigate.GetArray("TraitIDs");
            if (traits != null)
            {
                for (int i = 0; i < traits.Length; i++)
                {
                    string id = traits.GetString(i);
                    if (string.IsNullOrEmpty(id) || id == "^") continue;

                    // If trait not in DB -> +1; if beneficial -> +1; if negative -> -1
                    if (FrigateTraitDatabase.ById.TryGetValue(id, out var trait))
                        netScore += trait.Beneficial ? 1 : -1;
                    else
                        netScore += 1; // Unknown traits fallback to count as positive
                }
            }
        }
        catch { }
        // Class from net score: 5->S, 4->A, 3->B, anything else->C
        if (netScore >= 5) return "S";
        if (netScore == 4) return "A";
        if (netScore == 3) return "B";
        return "C";
    }

    /// <summary>
    /// Converts a grade letter (C/B/A/S) to the target net trait score.
    /// </summary>
    private static int GradeToTargetScore(string grade) => grade switch
    {
        "S" => 5,
        "A" => 4,
        "B" => 3,
        _ => 0, // C or unknown: clear non-primary traits
    };

    /// <summary>
    /// Pool of generic beneficial tertiary trait IDs that apply to all normal frigate types.
    /// Used when upgrading class by adding beneficial traits.
    /// </summary>
    private static readonly string[] GenericBeneficialTraits =
    {
        "^SPEED_TER_1", "^SPEED_TER_2", "^SPEED_TER_3", "^SPEED_TER_4",
        "^FUEL_TER_1", "^FUEL_TER_2", "^INVULN_TER_1", "^INVULN_TER_2",
    };

    /// <summary>
    /// Adjusts a frigate's traits (slots 1–4) to achieve the requested class grade.
    /// <para>
    /// The in-game class is determined entirely by the net trait score
    /// (<see cref="ComputeClassFromTraits"/>), so changing class requires modifying traits.
    /// Slot 0 (the primary trait) is never altered.
    /// </para>
    /// </summary>
    /// <param name="frigate">The frigate JSON object.</param>
    /// <param name="targetGrade">The desired grade letter (C, B, A, or S).</param>
    internal static void AdjustTraitsForTargetGrade(JsonObject frigate, string targetGrade)
    {
        var traits = frigate.GetArray("TraitIDs");
        if (traits == null || traits.Length < 2) return;

        // Early exit if already at the target grade
        string currentGrade = ComputeClassFromTraits(frigate);
        if (currentGrade == targetGrade) return;

        int targetScore = GradeToTargetScore(targetGrade);
        int currentScore = 0;

        // Compute current net score (same logic as ComputeClassFromTraits)
        for (int i = 0; i < traits.Length; i++)
        {
            string id = "";
            try { id = traits.GetString(i); } catch { }
            if (string.IsNullOrEmpty(id) || id == "^") continue;
            if (FrigateTraitDatabase.ById.TryGetValue(id, out var trait))
                currentScore += trait.Beneficial ? 1 : -1;
            else
                currentScore += 1;
        }

        int delta = targetScore - currentScore;
        if (delta == 0) return;

        // Collect the set of trait IDs already in use so we avoid duplicates
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < traits.Length; i++)
        {
            try { usedIds.Add(traits.GetString(i)); } catch { }
        }

        if (delta > 0)
        {
            // Need more beneficial score.
            // Pass 1: replace negative traits with "^" (each gives +1: changes -1 to 0)
            for (int i = 1; i < 5 && i < traits.Length && delta > 0; i++)
            {
                string id = "";
                try { id = traits.GetString(i); } catch { }
                if (string.IsNullOrEmpty(id) || id == "^") continue;
                if (FrigateTraitDatabase.ById.TryGetValue(id, out var t) && !t.Beneficial)
                {
                    traits.Set(i, "^");
                    usedIds.Remove(id);
                    delta -= 1;
                }
            }

            // Pass 2: replace "^" slots with beneficial traits (each gives +1)
            int poolIdx = 0;
            for (int i = 1; i < 5 && i < traits.Length && delta > 0; i++)
            {
                string id = "";
                try { id = traits.GetString(i); } catch { }
                if (id != "^") continue;

                // Find next unused beneficial trait from pool
                string? newTrait = null;
                while (poolIdx < GenericBeneficialTraits.Length)
                {
                    string candidate = GenericBeneficialTraits[poolIdx++];
                    if (!usedIds.Contains(candidate))
                    {
                        newTrait = candidate;
                        break;
                    }
                }
                if (newTrait == null) break;

                traits.Set(i, newTrait);
                usedIds.Add(newTrait);
                delta -= 1;
            }
        }
        else // delta < 0
        {
            // Need less score: replace beneficial non-primary traits with "^"
            // Work backwards from slot 4 to preserve earlier traits when possible
            for (int i = Math.Min(4, traits.Length - 1); i >= 1 && delta < 0; i--)
            {
                string id = "";
                try { id = traits.GetString(i); } catch { }
                if (string.IsNullOrEmpty(id) || id == "^") continue;

                bool isBeneficial = true;
                if (FrigateTraitDatabase.ById.TryGetValue(id, out var t))
                    isBeneficial = t.Beneficial;

                if (isBeneficial)
                {
                    traits.Set(i, "^");
                    delta += 1;
                }
            }
        }
    }

    // Trait auto-adjustment presets

    /// <summary>Preset trait IDs for the Normandy frigate.</summary>
    private static readonly string[] NormandyTraits = { "^NORMANDY_1", "^NORMANDY_2", "^NORMANDY_3", "^NORMANDY_4", "^NORMANDY_5" };
    /// <summary>Preset trait IDs for the unique DeepSpace frigate.</summary>
    private static readonly string[] DeepSpaceTraits = { "^DEEPSPACE_1", "^DEEPSPACE_2", "^DEEPSPACE_3", "^DEEPSPACE_4", "^DEEPSPACE_5" };
    /// <summary>Preset trait IDs for the Ghost Ship frigate.</summary>
    private static readonly string[] GhostShipTraits = { "^GHOSTSHIP_1", "^GHOSTSHIP_2", "^GHOSTSHIP_3", "^GHOSTSHIP_4", "^GHOSTSHIP_5" };

    /// <summary>
    /// Types that receive non-standard trait handling on type change.
    /// Normandy/DeepSpace/GhostShip get full preset replacements; DeepSpaceCommon gets primary-only.
    /// Used in the fallback branches to distinguish special from normal type transitions.
    /// </summary>
    private static readonly HashSet<string> SpecialTypes = new(StringComparer.Ordinal) { "Normandy", "DeepSpace", "DeepSpaceCommon", "GhostShip" };

    /// <summary>Maps normal frigate type to its primary trait ID.</summary>
    private static readonly Dictionary<string, string> PrimaryTraitForType = new(StringComparer.Ordinal)
    {
        { "Combat", "^COMBAT_PRI" },
        { "Exploration", "^EXPLORE_PRI" },
        { "Mining", "^MINING_PRI" },
        { "Diplomacy", "^TRADING_PRI" },
        { "Support", "^FUEL_PRI" },
        { "Pirate", "^PIRATE_PRI" },
    };

    /// <summary>First primary trait for DeepSpaceCommon (living ship) frigates.</summary>
    private const string DeepSpaceCommonPrimaryTrait = "^LIVING_COM_PRI";

    /// <summary>
    /// Auto-adjusts trait IDs when frigate type changes.
    /// Special types (Normandy, DeepSpace, GhostShip) get full preset traits.
    /// DeepSpaceCommon gets its primary trait set and remaining slots cleared.
    /// Changing from special to normal sets the primary trait and clears the rest.
    /// Changing between normal types updates only the primary trait.
    /// </summary>
    /// <param name="frigate">The frigate JSON object.</param>
    /// <param name="oldType">The previous frigate type.</param>
    /// <param name="newType">The new frigate type.</param>
    internal static void AutoAdjustTraitsForTypeChange(JsonObject frigate, string oldType, string newType)
    {
        if (oldType == newType) return;

        var traits = frigate.GetArray("TraitIDs");
        if (traits == null || traits.Length == 0) return;

        // Changing TO Normandy
        if (oldType != "Normandy" && newType == "Normandy")
        {
            SetAllTraits(traits, NormandyTraits);
            return;
        }
        // Changing TO DeepSpace
        if (oldType != "DeepSpace" && newType == "DeepSpace")
        {
            SetAllTraits(traits, DeepSpaceTraits);
            return;
        }
        // Changing TO GhostShip
        if (oldType != "GhostShip" && newType == "GhostShip")
        {
            SetAllTraits(traits, GhostShipTraits);
            return;
        }
        // Changing TO DeepSpaceCommon: primary trait + clear rest
        if (oldType != "DeepSpaceCommon" && newType == "DeepSpaceCommon")
        {
            traits.Set(0, DeepSpaceCommonPrimaryTrait);
            for (int i = 1; i < 5 && i < traits.Length; i++)
                traits.Set(i, "^");
            return;
        }

        // From special to normal: set primary trait and clear rest
        if (SpecialTypes.Contains(oldType) && !SpecialTypes.Contains(newType))
        {
            if (PrimaryTraitForType.TryGetValue(newType, out string? primary))
            {
                traits.Set(0, primary);
                for (int i = 1; i < 5 && i < traits.Length; i++)
                    traits.Set(i, "^");
            }
            return;
        }

        // Between normal types: only update primary trait (slot 0)
        if (!SpecialTypes.Contains(oldType) && !SpecialTypes.Contains(newType))
        {
            if (PrimaryTraitForType.TryGetValue(newType, out string? primary))
                traits.Set(0, primary);
        }
    }

    private static void SetAllTraits(JsonArray traits, string[] presetTraits)
    {
        for (int i = 0; i < presetTraits.Length && i < traits.Length; i++)
            traits.Set(i, presetTraits[i]);
    }

    // Expedition milestones: number of expeditions required for each level-up
    /// <summary>
    /// Number of completed expeditions required at each level-up milestone.
    /// </summary>
    internal static readonly int[] LevelVictoriesRequired = { 2, 5, 8, 15, 25, 30, 35, 40, 45, 55 };

    /// <summary>
    /// Possible frigate operational states.
    /// </summary>
    internal static readonly string[] FrigateStates = { "Idle", "On Expedition", "Damaged", "Awaiting Debrief" };

    /// <summary>
    /// Localisation keys for frigate operational states (parallel to <see cref="FrigateStates"/>).
    /// </summary>
    internal static readonly string[] FrigateStateKeys = { "frigate.state_idle", "frigate.state_on_expedition", "frigate.state_damaged", "frigate.state_awaiting_debrief" };

    /// <summary>
    /// Known expedition mission categories.
    /// </summary>
    internal static readonly string[] ExpeditionCategories =
    {
        "Combat", "Exploration", "Mining", "Diplomacy", "Balanced"
    };

    /// <summary>
    /// Calculates the number of expeditions remaining until the next level-up.
    /// </summary>
    /// <param name="expeditions">The current number of completed expeditions.</param>
    /// <returns>The number of expeditions until next level-up, or -1 if fully leveled.</returns>
    internal static int GetLevelUpIn(int expeditions)
    {
        foreach (int threshold in LevelVictoriesRequired)
        {
            if (expeditions < threshold)
                return threshold - expeditions;
        }
        return -1; // fully leveled
    }

    /// <summary>
    /// Calculates the total number of level-ups still remaining.
    /// </summary>
    /// <param name="expeditions">The current number of completed expeditions.</param>
    /// <returns>The count of remaining level-ups, or 0 if fully leveled.</returns>
    internal static int GetLevelUpsRemaining(int expeditions)
    {
        for (int i = 0; i < LevelVictoriesRequired.Length; i++)
        {
            if (LevelVictoriesRequired[i] > expeditions)
                return LevelVictoriesRequired.Length - i;
        }
        return 0;
    }

    /// <summary>
    /// Determines the current operational state of a frigate (Idle, On Expedition, Damaged, or Awaiting Debrief).
    /// </summary>
    /// <param name="frigate">The frigate JSON object.</param>
    /// <param name="frigateIndex">The frigate's index in the fleet.</param>
    /// <param name="expeditions">The active expeditions JSON array, or <c>null</c>.</param>
    /// <returns>An index into <see cref="FrigateStates"/>.</returns>
    internal static int GetFrigateState(JsonObject frigate, int frigateIndex, JsonArray? expeditions)
    {
        bool isDamaged = false;
        try { isDamaged = frigate.GetInt("DamageTaken") > 0; } catch { }

        if (expeditions != null)
        {
            int expIdx = FindExpeditionIndex(frigateIndex, expeditions);
            if (expIdx >= 0)
            {
                try
                {
                    var exp = expeditions.GetObject(expIdx);
                    var events = exp.GetArray("Events");
                    int nextEvent = 0;
                    try { nextEvent = exp.GetInt("NextEventToTrigger"); } catch { }
                    int totalEvents = events?.Length ?? 0;
                    return nextEvent >= totalEvents ? 3 : 1; // AwaitingDebrief or OnExpedition
                }
                catch { return 1; } // OnExpedition
            }
        }

        return isDamaged ? 2 : 0; // Damaged or Idle
    }

    /// <summary>
    /// Finds the index of the expedition that includes the specified frigate.
    /// </summary>
    /// <param name="frigateIndex">The frigate's index in the fleet.</param>
    /// <param name="expeditions">The active expeditions JSON array.</param>
    /// <returns>The expedition index, or -1 if the frigate is not on any expedition.</returns>
    internal static int FindExpeditionIndex(int frigateIndex, JsonArray expeditions)
    {
        for (int i = 0; i < expeditions.Length; i++)
        {
            try
            {
                var exp = expeditions.GetObject(i);
                var allIndices = exp.GetArray("AllFrigateIndices");
                if (allIndices == null) continue;
                for (int j = 0; j < allIndices.Length; j++)
                {
                    if (allIndices.GetInt(j) == frigateIndex) return i;
                }
            }
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Gets the mission category for an expedition at the specified index.
    /// </summary>
    /// <param name="expeditions">The active expeditions JSON array.</param>
    /// <param name="expIdx">The expedition index.</param>
    /// <returns>The expedition category name, or an empty string if unavailable.</returns>
    internal static string GetExpeditionCategory(JsonArray expeditions, int expIdx)
    {
        try
        {
            var exp = expeditions.GetObject(expIdx);
            var catObj = exp.GetObject("ExpeditionCategory");
            string cat = catObj?.GetString("ExpeditionCategory") ?? "";
            int idx = Array.IndexOf(ExpeditionCategories, cat);
            return idx >= 0 ? ExpeditionCategories[idx] : cat;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Adjusts all frigate index references in expedition data after a frigate has been
    /// removed from the FleetFrigates array. Any index greater than the removed index
    /// is decremented by one.
    /// </summary>
    /// <param name="removedIndex">The index of the removed frigate.</param>
    /// <param name="expeditions">The FleetExpeditions JSON array.</param>
    internal static void AdjustExpeditionIndicesAfterRemoval(int removedIndex, JsonArray expeditions)
    {
        for (int i = 0; i < expeditions.Length; i++)
        {
            try
            {
                var exp = expeditions.GetObject(i);
                DecrementIndicesAbove(exp.GetArray("AllFrigateIndices"), removedIndex);
                DecrementIndicesAbove(exp.GetArray("ActiveFrigateIndices"), removedIndex);
                DecrementIndicesAbove(exp.GetArray("DamagedFrigateIndices"), removedIndex);
                DecrementIndicesAbove(exp.GetArray("DestroyedFrigateIndices"), removedIndex);

                var events = exp.GetArray("Events");
                if (events != null)
                {
                    for (int j = 0; j < events.Length; j++)
                    {
                        try
                        {
                            var ev = events.GetObject(j);
                            DecrementIndicesAbove(ev.GetArray("AffectedFrigateIndices"), removedIndex);
                            DecrementIndicesAbove(ev.GetArray("RepairingFrigateIndices"), removedIndex);
                            DecrementIndicesAbove(ev.GetArray("AffectedFrigateResponses"), removedIndex);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Decrements each integer element in the array that is greater than the specified threshold.
    /// </summary>
    private static void DecrementIndicesAbove(JsonArray? arr, int removedIndex)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            try
            {
                int val = arr.GetInt(i);
                if (val > removedIndex)
                    arr.Set(i, val - 1);
            }
            catch { }
        }
    }
}
