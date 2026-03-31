using NMSE.Data;
using NMSE.IO;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles account-level data operations including loading/saving reward unlock states.
/// </summary>
internal static class AccountLogic
{
    /// <summary>
    /// Reads a JSON array of string values into a case-insensitive hash set.
    /// </summary>
    /// <param name="array">The JSON array of string values, or <c>null</c>.</param>
    /// <returns>A hash set containing the non-empty string values from the array.</returns>
    internal static HashSet<string> GetUnlockedSet(JsonArray? array)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (array == null) return set;
        for (int i = 0; i < array.Length; i++)
        {
            var val = array.GetString(i);
            if (!string.IsNullOrEmpty(val))
                set.Add(val);
        }
        return set;
    }

    /// <summary>
    /// Loads account data from an Xbox Game Pass containers.index AccountData blob.
    /// This is the Xbox equivalent of <see cref="LoadAccountData"/> which reads accountdata.hg.
    /// </summary>
    /// <param name="accountSlot">The Xbox slot info for the AccountData entry.</param>
    /// <returns>An <see cref="AccountData"/> with loaded reward sets, or an error message if loading failed.</returns>
    internal static AccountData LoadXboxAccountData(XboxSlotInfo accountSlot)
    {
        try
        {
            string? json = ContainersIndexManager.LoadXboxSave(accountSlot);
            if (json == null)
                return new AccountData { ErrorMessage = UiStrings.Get("account.not_found") };

            var accountObj = JsonObject.Parse(json);

            var userSettings = accountObj.GetObject("UserSettingsData") ?? accountObj;

            var seasonUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedSeasonRewards"));
            var twitchUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedTwitchRewards"));
            var platformUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));

            int total = seasonUnlocked.Count + twitchUnlocked.Count + platformUnlocked.Count;

            return new AccountData
            {
                AccountObject = accountObj,
                AccountFilePath = accountSlot.DataFilePath,
                SeasonUnlocked = seasonUnlocked,
                TwitchUnlocked = twitchUnlocked,
                PlatformUnlocked = platformUnlocked,
                StatusMessage = UiStrings.Format("account.status_loaded", total),
            };
        }
        catch (Exception ex)
        {
            return new AccountData { ErrorMessage = UiStrings.Format("account.load_error", accountSlot.DataFilePath ?? "Xbox AccountData", ex.Message) };
        }
    }

    /// <summary>
    /// Loads account data from the accountdata.hg file in the specified save directory.
    /// </summary>
    /// <param name="saveDirectory">The path to the save directory containing accountdata.hg.</param>
    /// <returns>An <see cref="AccountData"/> with loaded reward sets, or an error message if loading failed.</returns>
    internal static AccountData LoadAccountData(string saveDirectory)
    {
        string accountPath = Path.Combine(saveDirectory, "accountdata.hg");
        if (!File.Exists(accountPath))
            return new AccountData { ErrorMessage = UiStrings.Get("account.not_found") };

        try
        {
            var accountObj = SaveFileManager.LoadSaveFile(accountPath);
            var userSettings = accountObj.GetObject("UserSettingsData") ?? accountObj;

            var seasonUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedSeasonRewards"));
            var twitchUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedTwitchRewards"));
            var platformUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));

            int total = seasonUnlocked.Count + twitchUnlocked.Count + platformUnlocked.Count;

            return new AccountData
            {
                AccountObject = accountObj,
                AccountFilePath = accountPath,
                SeasonUnlocked = seasonUnlocked,
                TwitchUnlocked = twitchUnlocked,
                PlatformUnlocked = platformUnlocked,
                StatusMessage = UiStrings.Format("account.status_loaded", total),
            };
        }
        catch (Exception ex)
        {
            return new AccountData { ErrorMessage = UiStrings.Format("account.load_error", accountPath, ex.Message) };
        }
    }

    /// <summary>
    /// Saves a list of rewards to a JSON array under the specified key, keeping only unlocked entries.
    /// </summary>
    /// <param name="rewards">The reward entries with their unlock states.</param>
    /// <param name="userSettings">The user settings JSON object to update.</param>
    /// <param name="key">The JSON key for the reward array (e.g. "UnlockedSeasonRewards").</param>
    internal static void SaveRewardList(List<(string Id, bool Unlocked)> rewards, JsonObject userSettings, string key)
    {
        var array = userSettings.GetArray(key);
        if (array == null)
        {
            array = new JsonArray();
            userSettings.Set(key, array);
        }

        array.Clear();
        foreach (var (id, unlocked) in rewards)
        {
            if (unlocked && !string.IsNullOrEmpty(id))
                array.Add(id);
        }
    }

    /// <summary>
    /// Reads the redeemed reward sets from the game save data.
    /// Returns sets for season and Twitch rewards that have been redeemed in
    /// this particular save slot (from <c>PlayerStateData.RedeemedSeasonRewards</c>
    /// and <c>PlayerStateData.RedeemedTwitchRewards</c>).
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <returns>A tuple of (seasonRedeemed, twitchRedeemed) hash sets.</returns>
    internal static (HashSet<string> SeasonRedeemed, HashSet<string> TwitchRedeemed) GetRedeemedSets(JsonObject? saveData)
    {
        var seasonRedeemed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var twitchRedeemed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var playerState = saveData?.GetObject("PlayerStateData");
        if (playerState == null) return (seasonRedeemed, twitchRedeemed);

        seasonRedeemed = GetUnlockedSet(playerState.GetArray("RedeemedSeasonRewards"));
        twitchRedeemed = GetUnlockedSet(playerState.GetArray("RedeemedTwitchRewards"));
        return (seasonRedeemed, twitchRedeemed);
    }

    /// <summary>
    /// Saves the redeemed rewards arrays in the game save to match the user's
    /// explicit per-save redemption choices. Unlike the old approach, this does
    /// NOT mirror the account unlock state - it writes only rewards the user
    /// has explicitly ticked as "Redeemed in Save".
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="seasonRedeemed">Season reward entries with explicit redeem states.</param>
    /// <param name="twitchRedeemed">Twitch reward entries with explicit redeem states.</param>
    internal static void SaveRedeemedRewards(JsonObject saveData,
        List<(string Id, bool Redeemed)> seasonRedeemed,
        List<(string Id, bool Redeemed)> twitchRedeemed)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        WriteRedeemedArray(playerState, "RedeemedSeasonRewards", seasonRedeemed);
        WriteRedeemedArray(playerState, "RedeemedTwitchRewards", twitchRedeemed);
    }

    private static void WriteRedeemedArray(JsonObject playerState, string key, List<(string Id, bool Redeemed)> rewards)
    {
        var array = playerState.GetArray(key);
        if (array == null)
        {
            array = new JsonArray();
            playerState.Set(key, array);
        }

        array.Clear();
        foreach (var (id, redeemed) in rewards)
        {
            if (redeemed && !string.IsNullOrEmpty(id))
                array.Add(id);
        }
    }

    /// <summary>
    /// Builds display rows from a rewards database and the currently unlocked and
    /// redeemed sets, including any unlocked rewards not found in the database.
    /// </summary>
    /// <param name="rewardsDb">The known rewards database entries with IDs, names, and metadata.</param>
    /// <param name="unlocked">The set of currently unlocked reward IDs (account level).</param>
    /// <param name="redeemed">The set of currently redeemed reward IDs (save level). May be empty.</param>
    /// <returns>A list of reward row data for display in the UI.</returns>
    internal static List<RewardRowData> BuildRewardRows(
        List<RewardDbEntry> rewardsDb,
        HashSet<string> unlocked,
        HashSet<string>? redeemed = null)
    {
        redeemed ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<RewardRowData>();

        if (rewardsDb.Count > 0)
        {
            foreach (var entry in rewardsDb)
            {
                bool isUnlocked = unlocked.Contains(entry.Id);
                bool isRedeemed = redeemed.Contains(entry.Id);
                rows.Add(new RewardRowData(entry.Id, entry.Name,
                    unlocked: isUnlocked, redeemed: isRedeemed,
                    seasonId: entry.SeasonId, stageId: entry.StageId,
                    mustBeUnlocked: entry.MustBeUnlocked));
            }

            foreach (var id in unlocked)
            {
                bool found = false;
                foreach (var entry in rewardsDb)
                {
                    if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    rows.Add(new RewardRowData(id, "(unknown)", true, redeemed.Contains(id)));
            }
        }
        else
        {
            foreach (var id in unlocked)
                rows.Add(new RewardRowData(id, "", true, redeemed.Contains(id)));
        }

        return rows;
    }

    /// <summary>
    /// Holds loaded account data including unlocked reward sets and status information.
    /// </summary>
    internal sealed class AccountData
    {
        /// <summary>The parsed account data JSON object.</summary>
        public JsonObject? AccountObject { get; set; }
        /// <summary>The file path to the account data file.</summary>
        public string? AccountFilePath { get; set; }
        /// <summary>Set of unlocked season reward IDs.</summary>
        public HashSet<string> SeasonUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Set of unlocked Twitch reward IDs.</summary>
        public HashSet<string> TwitchUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Set of unlocked platform reward IDs.</summary>
        public HashSet<string> PlatformUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>A human-readable status message on successful load.</summary>
        public string? StatusMessage { get; set; }
        /// <summary>An error message if account data could not be loaded.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// A lightweight entry from the rewards database for use in <see cref="BuildRewardRows"/>.
    /// </summary>
    internal sealed class RewardDbEntry
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>Expedition/season number (-1 if not applicable).</summary>
        public int SeasonId { get; init; } = -1;
        /// <summary>Progression stage within the expedition (-1 if not applicable).</summary>
        public int StageId { get; init; } = -1;
        /// <summary>Whether this reward requires explicit account-level unlocking.</summary>
        public bool MustBeUnlocked { get; init; }
    }

    /// <summary>
    /// Represents a single reward row for display in the UI.
    /// </summary>
    internal sealed class RewardRowData
    {
        /// <summary>The reward identifier.</summary>
        public string Id { get; }
        /// <summary>The human-readable reward name.</summary>
        public string Name { get; }
        /// <summary>Whether this reward is unlocked on the account.</summary>
        public bool Unlocked { get; }
        /// <summary>Whether this reward is redeemed in the current save slot.</summary>
        public bool Redeemed { get; }
        /// <summary>Expedition/season number this reward belongs to (-1 if not applicable).</summary>
        public int SeasonId { get; }
        /// <summary>Progression stage within the expedition (-1 if not applicable).</summary>
        public int StageId { get; }
        /// <summary>Whether this reward requires explicit account-level unlocking.</summary>
        public bool MustBeUnlocked { get; }

        /// <summary>
        /// Initializes a new reward row.
        /// </summary>
        public RewardRowData(string id, string name, bool unlocked, bool redeemed = false,
            int seasonId = -1, int stageId = -1, bool mustBeUnlocked = false)
        {
            Id = id;
            Name = name;
            Unlocked = unlocked;
            Redeemed = redeemed;
            SeasonId = seasonId;
            StageId = stageId;
            MustBeUnlocked = mustBeUnlocked;
        }
    }
}
