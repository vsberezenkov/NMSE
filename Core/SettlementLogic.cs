using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles settlement data operations including loading, saving, filtering by player ownership, and stat management.
/// </summary>
internal static class SettlementLogic
{
    // Stat order: Population=0, Happiness=1, Production=2, Upkeep=3, Sentinels=4, Debt=5, Alert=6, BugAttack=7
    // Max values from database; min values in StatMinValues
    /// <summary>Maximum values for each settlement stat (Population, Happiness, Production, Upkeep, Sentinels, Debt, Alert, BugAttack).</summary>
    internal static readonly int[] StatMaxValues = { 175, 180, 1500000, 1000000, 100, 10000000, 1000, 1000 };
    /// <summary>Minimum values for each settlement stat.</summary>
    internal static readonly int[] StatMinValues = { 0, -30, 0, 0, 0, 0, 0, 0 };
    /// <summary>Display labels for each settlement stat.</summary>
    internal static readonly string[] StatLabels = { "Population", "Happiness", "Production", "Upkeep", "Sentinels", "Debt", "Alert", "Bug Attack" };
    /// <summary>Total number of tracked settlement stats.</summary>
    internal const int StatCount = 8;
    /// <summary>Maximum allowed production output amount.</summary>
    internal const int ProductionMaxAmount = 999;
    /// <summary>Maximum number of settlement slots in the save file.</summary>
    internal const int MaxSettlementSlots = 100;

    /// <summary>
    /// Item names that are valid settlement production outputs (from game data).
    /// Used to filter the production item picker dialog.
    /// </summary>
    internal static readonly string[] AllowedProductionNames =
    {
        "Amino Chamber", "Atlantideum", "Cactus Flesh", "Carbon Nanotubes",
        "Comet Droplets", "Convergence Cube", "Cryogenic chamber", "Decomm. Circuits",
        "Dirt", "Enriched Carbon", "Faecium", "Frigate Fuel", "Frost Crystal",
        "Fungal Mould", "Fusion Accelerant", "Fusion Core", "Gamma Root",
        "Gek Relic", "GekNip", "Glass", "Heat Capacitor", "Hermetic Seal",
        "Hot Ice", "Hydraulic Wiring", "Inverted Mirror", "Ion Battery",
        "Ion Capacitor", "Kelp Sac", "Korvax Casing", "Life Support Gel",
        "Living Glass", "Magnetic Resonator", "Marrow Bulb", "Metal Plating",
        "Nanotube Crate", "Neural Duct", "Nitrogen", "Nitrogen Salt",
        "Optical Solvent", "Organic Catalyst", "Portable Reactor", "Pugneum",
        "Quantum Computer", "Quantum Processor", "Radiant Shard", "Radon",
        "Re-latticed Arc Crystal", "Rusted Metal", "Salvaged Glass",
        "Semiconductor", "Solanium", "Solar Mirror", "Star Bulb",
        "Starship Launch Fuel", "Sulphurine", "Superconduct. Fibre",
        "Teleport Coordinator", "Thermic Condensate", "Vector Compressors",
        "Vy'keen Dagger", "Vy'keen Effigy", "Warp Cell", "Warp Hypercore"
    };

    internal static readonly string[] DecisionTypes =
    {
        "None", "StrangerVisit", "Policy", "NewBuilding", "BuildingChoice",
        "Conflict", "Request", "BlessingPerkRelated", "JobPerkRelated",
        "ProcPerkRelated", "UpgradeBuilding", "UpgradeBuildingChoice"
    };

    internal static readonly Dictionary<string, string> DecisionTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["None"] = "settlement.decision_none",
        ["StrangerVisit"] = "settlement.decision_stranger_visit",
        ["Policy"] = "settlement.decision_policy",
        ["NewBuilding"] = "settlement.decision_new_building",
        ["BuildingChoice"] = "settlement.decision_building_choice",
        ["Conflict"] = "settlement.decision_conflict",
        ["Request"] = "settlement.decision_request",
        ["BlessingPerkRelated"] = "settlement.decision_blessing_perk",
        ["JobPerkRelated"] = "settlement.decision_job_perk",
        ["ProcPerkRelated"] = "settlement.decision_proc_perk",
        ["UpgradeBuilding"] = "settlement.decision_upgrade_building",
        ["UpgradeBuildingChoice"] = "settlement.decision_upgrade_choice",
    };

    /// <summary>
    /// Builds a dictionary of allowed production items (ID -> Name) by matching
    /// <see cref="AllowedProductionNames"/> against the game item database.
    /// </summary>
    internal static Dictionary<string, string> BuildAllowedProductionItems(Data.GameItemDatabase database)
    {
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nameSet = new HashSet<string>(AllowedProductionNames, StringComparer.OrdinalIgnoreCase);

        foreach (var item in database.Items.Values)
        {
            if (nameSet.Contains(item.Name))
                allowed[item.Id] = item.Name;
        }

        return allowed;
    }

    /// <summary>
    /// Filters the settlements array to only include settlements owned by the current player.
    /// Matching is done by comparing LID, UID, or USN from the discovery owners.
    /// </summary>
    /// <param name="saveData">The top-level save data JSON object containing CommonStateData.</param>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="settlements">The JSON array of all settlements.</param>
    /// <returns>A list of indices into the settlements array that belong to this player.</returns>
    internal static List<int> FilterSettlements(JsonObject saveData, JsonObject playerState, JsonArray settlements)
    {
        var filteredIndices = new List<int>();

        // Get the player's owner identifiers from CommonStateData.UsedDiscoveryOwnersV2[0]
        // Note: UsedDiscoveryOwnersV2 is a direct child of CommonStateData, NOT under SeasonData
        string? playerLid = null;
        string? playerUid = null;
        string? playerUsn = null;
        try
        {
            var commonState = saveData.GetObject("CommonStateData");
            var owners = commonState?.GetArray("UsedDiscoveryOwnersV2");
            if (owners != null && owners.Length > 0)
            {
                var firstOwner = owners.GetObject(0);
                playerLid = firstOwner?.GetString("LID");
                playerUid = firstOwner?.GetString("UID");
                playerUsn = firstOwner?.GetString("USN");
            }
        }
        catch { }

        bool hasIdentifier = !string.IsNullOrEmpty(playerLid) || !string.IsNullOrEmpty(playerUid) || !string.IsNullOrEmpty(playerUsn);

        // Filter settlements by matching Owner LID, UID, or USN
        if (hasIdentifier)
        {
            for (int i = 0; i < settlements.Length; i++)
            {
                try
                {
                    var settlement = settlements.GetObject(i);
                    var owner = settlement?.GetObject("Owner");
                    if (owner == null) continue;

                    bool match = false;
                    if (!string.IsNullOrEmpty(playerLid))
                    {
                        var ownerLid = owner.GetString("LID");
                        if (playerLid.Equals(ownerLid, StringComparison.Ordinal)) match = true;
                    }
                    if (!match && !string.IsNullOrEmpty(playerUid))
                    {
                        var ownerUid = owner.GetString("UID");
                        if (playerUid.Equals(ownerUid, StringComparison.Ordinal)) match = true;
                    }
                    if (!match && !string.IsNullOrEmpty(playerUsn))
                    {
                        var ownerUsn = owner.GetString("USN");
                        if (playerUsn.Equals(ownerUsn, StringComparison.Ordinal)) match = true;
                    }

                    if (match)
                        filteredIndices.Add(i);
                }
                catch { }
            }
        }

        return filteredIndices;
    }

    /// <summary>
    /// Loads settlement data from a settlement JSON object for display and editing.
    /// </summary>
    /// <param name="settlement">The settlement JSON object.</param>
    /// <returns>A populated <see cref="SettlementData"/> instance.</returns>
    internal static SettlementData LoadSettlementData(JsonObject settlement)
    {
        var data = new SettlementData();

        data.Name = settlement.GetString("Name") ?? "";

        var ownerObj = settlement.GetObject("Owner");
        data.OwnerUsn = ownerObj?.GetString("USN") ?? "";
        data.OwnerUid = ownerObj?.GetString("UID") ?? "";

        data.SeedValue = settlement.GetString("SeedValue") ?? "";

        var statsArr = settlement.GetArray("Stats");
        if (statsArr != null)
        {
            for (int i = 0; i < StatCount && i < statsArr.Length; i++)
            {
                try { data.Stats[i] = Math.Clamp(statsArr.GetInt(i), StatMinValues[i], StatMaxValues[i]); }
                catch { data.Stats[i] = 0; }
            }
        }

        // NMS 5.70+ moved Population to its own field; prefer it over Stats[0]
        try
        {
            int pop = settlement.GetInt("Population");
            if (pop > 0 || data.Stats[0] == 0)
                data.Stats[0] = Math.Clamp(pop, StatMinValues[0], StatMaxValues[0]);
        }
        catch { }

        // Decision type
        try
        {
            var decisionObj = settlement.GetObject("PendingJudgementType");
            string decType = decisionObj?.GetString("SettlementJudgementType") ?? "None";
            data.DecisionTypeIndex = Array.IndexOf(DecisionTypes, decType);
            if (data.DecisionTypeIndex < 0) data.DecisionTypeIndex = 0;
        }
        catch { data.DecisionTypeIndex = 0; }

        // Last decision time
        try
        {
            long unixTime = settlement.GetLong("LastJudgementTime");
            if (unixTime > 0)
                data.LastDecisionTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToLocalTime().DateTime;
        }
        catch { }

        return data;
    }

    /// <summary>
    /// Saves settlement data back to the settlement JSON object.
    /// </summary>
    /// <param name="settlement">The settlement JSON object to update.</param>
    /// <param name="values">The values to write.</param>
    internal static void SaveSettlementData(JsonObject settlement, SettlementSaveValues values)
    {
        settlement.Set("Name", values.Name);

        if (!string.IsNullOrEmpty(values.SeedValue))
            settlement.Set("SeedValue", values.SeedValue);

        var statsArr = settlement.GetArray("Stats");
        if (statsArr != null)
        {
            for (int i = 0; i < StatCount && i < statsArr.Length; i++)
                statsArr.Set(i, values.Stats[i]);
        }

        // NMS 5.70+ also has a separate Population field
        try { settlement.Set("Population", values.Stats[0]); } catch { }

        // Decision type
        if (values.DecisionTypeIndex >= 0 && values.DecisionTypeIndex < DecisionTypes.Length)
        {
            try
            {
                var decisionObj = settlement.GetObject("PendingJudgementType");
                decisionObj?.Set("SettlementJudgementType", DecisionTypes[values.DecisionTypeIndex]);
            }
            catch { }
        }

        // Last decision time
        if (values.LastDecisionTime.HasValue)
        {
            try
            {
                long unixTime = ((DateTimeOffset)values.LastDecisionTime.Value.ToUniversalTime()).ToUnixTimeSeconds();
                settlement.Set("LastJudgementTime", unixTime);
            }
            catch { }
        }
    }

    /// <summary>
    /// Removes a settlement from the array by index.
    /// </summary>
    /// <param name="settlements">The SettlementStatesV2 JSON array.</param>
    /// <param name="index">The zero-based index of the settlement to remove.</param>
    internal static void RemoveSettlement(JsonArray settlements, int index)
    {
        if (index < 0 || index >= settlements.Length) return;
        settlements.RemoveAt(index);
    }

    /// <summary>
    /// Finds the best target index for importing a settlement.
    /// Returns -1 to signal a new entry should be appended, -2 if no slot is available,
    /// or a valid index to overwrite.
    /// </summary>
    /// <param name="settlements">The SettlementStatesV2 JSON array.</param>
    /// <param name="selectedDataIndex">The currently-selected settlement array index, or -1 if none.</param>
    /// <returns>
    /// > = 0 : overwrite that index.
    /// -1 : append a new entry (spare capacity exists).
    /// -2 : array full and no selection – caller must ask the user to choose.
    /// </returns>
    internal static int FindImportTargetIndex(JsonArray settlements, int selectedDataIndex)
    {
        // If there's a selected settlement, overwrite it
        if (selectedDataIndex >= 0 && selectedDataIndex < settlements.Length)
            return selectedDataIndex;

        // If spare capacity, append
        if (settlements.Length < MaxSettlementSlots)
            return -1;

        // Array full and no selection
        return -2;
    }

    /// <summary>
    /// Holds loaded settlement data for display and editing in the UI.
    /// </summary>
    internal sealed class SettlementData
    {
        /// <summary>The settlement name.</summary>
        public string Name { get; set; } = "";
        /// <summary>The owner's USN (username) identifier.</summary>
        public string OwnerUsn { get; set; } = "";
        /// <summary>The owner's UID identifier.</summary>
        public string OwnerUid { get; set; } = "";
        /// <summary>The settlement's procedural seed value.</summary>
        public string SeedValue { get; set; } = "";
        /// <summary>Array of settlement stat values indexed by stat type.</summary>
        public int[] Stats { get; set; } = new int[StatCount];
        /// <summary>Index into <see cref="DecisionTypes"/> for the pending decision.</summary>
        public int DecisionTypeIndex { get; set; }
        /// <summary>The timestamp of the last settlement judgement decision.</summary>
        public DateTime? LastDecisionTime { get; set; }
    }

    /// <summary>
    /// Holds values to be saved back to a settlement's JSON data.
    /// </summary>
    internal sealed class SettlementSaveValues
    {
        /// <summary>The settlement name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>The seed value to set.</summary>
        public string SeedValue { get; set; } = "";
        /// <summary>Array of stat values to write.</summary>
        public int[] Stats { get; set; } = new int[StatCount];
        /// <summary>Index into <see cref="DecisionTypes"/> for the desired pending decision.</summary>
        public int DecisionTypeIndex { get; set; }
        /// <summary>The last decision timestamp to set, or <c>null</c> to leave unchanged.</summary>
        public DateTime? LastDecisionTime { get; set; }
    }
}
