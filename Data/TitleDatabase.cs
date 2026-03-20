namespace NMSE.Data;

/// <summary>
/// Represents a player title that can be unlocked.
/// </summary>
public class TitleEntry
{
    /// <summary>Unique title identifier.</summary>
    public string Id { get; set; } = "";
    /// <summary>Display name of the title (may contain {0} for player name).</summary>
    public string Name { get; set; } = "";
    /// <summary>Localisation lookup key for Name. Null when not available.</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Description shown when unlocking the title.</summary>
    public string UnlockDescription { get; set; } = "";
    /// <summary>Localisation lookup key for UnlockDescription. Null when not available.</summary>
    public string? UnlockDescriptionLocStr { get; set; }
    /// <summary>Description shown when title is already unlocked.</summary>
    public string AlreadyUnlockedDescription { get; set; } = "";
    /// <summary>Localisation lookup key for AlreadyUnlockedDescription. Null when not available.</summary>
    public string? AlreadyUnlockedDescriptionLocStr { get; set; }
    /// <summary>Stat value threshold needed to unlock this title.</summary>
    public long UnlockedByStatValue { get; set; }
    /// <summary>Stat name required to unlock this title.</summary>
    public string UnlockedByStat { get; set; } = "";

    /// <summary>Returns display-friendly title name.</summary>
    public override string ToString() => Name;
}

/// <summary>
/// Loads and manages player title data from the Titles.json database file.
/// </summary>
public static class TitleDatabase
{
    private static readonly List<TitleEntry> _titles = new();
    private static readonly Dictionary<string, TitleEntry> _byId = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All loaded titles.</summary>
    public static IReadOnlyList<TitleEntry> Titles => _titles;

    /// <summary>Whether data has been loaded.</summary>
    public static bool IsLoaded => _titles.Count > 0;

    static TitleDatabase()
    {
        InitializeStaticTitles();
    }

    /// <summary>
    /// Populates title data from hardcoded static entries.
    /// </summary>
    /// <summary>
    /// No-op: title data is now loaded from Resources/json/Titles.json at startup
    /// via <see cref="LoadFromFile"/>. Kept for backwards compatibility.
    /// </summary>
    public static void InitializeStaticTitles()
    {
        // Hardcoded title data removed. Data comes from Titles.json.
        // The JSON file is produced by the extractor's ParseTitles() method
        // from PLAYERTITLEDATA.MXML and contains all 318 title entries with
        // IDs, names, loc keys, unlock descriptions, and stat requirements.
    }

    private static readonly Dictionary<string, (string Name, string UnlockDescription, string AlreadyUnlockedDescription)> _englishBackup = new();

    /// <summary>
    /// Loads titles from a Titles.json file.
    /// Falls back to hardcoded static data if the file is missing or invalid.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!System.IO.File.Exists(jsonPath))
        {
            if (!IsLoaded) InitializeStaticTitles();
            return IsLoaded;
        }

        try
        {
            var content = System.IO.File.ReadAllBytes(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                if (!IsLoaded) InitializeStaticTitles();
                return IsLoaded;
            }

            _titles.Clear();
            _byId.Clear();
            _englishBackup.Clear();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var title = new TitleEntry
                {
                    Id = elem.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "" : "",
                    Name = elem.TryGetProperty("Name", out var nameProp2) ? nameProp2.GetString() ?? ""
                        : elem.TryGetProperty("Title", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    NameLocStr = elem.TryGetProperty("Name_LocStr", out var nlProp) ? nlProp.GetString() : null,
                    UnlockDescription = elem.TryGetProperty("UnlockDescription", out var unlockProp) ? unlockProp.GetString() ?? ""
                        : elem.TryGetProperty("Description", out var descProp) ? descProp.GetString() ?? "" : "",
                    UnlockDescriptionLocStr = elem.TryGetProperty("UnlockDescription_LocStr", out var udlProp) ? udlProp.GetString() : null,
                    AlreadyUnlockedDescription = elem.TryGetProperty("AlreadyUnlockedDescription", out var alreadyProp) ? alreadyProp.GetString() ?? "" : "",
                    AlreadyUnlockedDescriptionLocStr = elem.TryGetProperty("AlreadyUnlockedDescription_LocStr", out var audlProp) ? audlProp.GetString() : null,
                    UnlockedByStat = elem.TryGetProperty("UnlockedByStat", out var statNameProp) ? statNameProp.GetString() ?? "" : "",
                    UnlockedByStatValue = elem.TryGetProperty("UnlockedByStatValue", out var statProp) && statProp.TryGetInt64(out long val) ? val : 0,
                };

                if (!string.IsNullOrEmpty(title.Id))
                {
                    _titles.Add(title);
                    _byId[title.Id] = title;
                }
            }

            if (_titles.Count == 0)
            {
                InitializeStaticTitles();
            }

            return _titles.Count > 0;
        }
        catch
        {
            if (!IsLoaded) InitializeStaticTitles();
            return IsLoaded;
        }
    }

    /// <summary>
    /// Applies localisation to title display values using the specified service.
    /// Titles use %NAME% in lang files which is mapped to {0} for C# string.Format.
    /// </summary>
    public static int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive) { RevertLocalisation(); return 0; }

        int count = 0;
        foreach (var title in _titles)
        {
            bool changed = false;

            if (!_englishBackup.ContainsKey(title.Id))
                _englishBackup[title.Id] = (title.Name, title.UnlockDescription, title.AlreadyUnlockedDescription);

            var backup = _englishBackup[title.Id];
            title.Name = backup.Name;
            title.UnlockDescription = backup.UnlockDescription;
            title.AlreadyUnlockedDescription = backup.AlreadyUnlockedDescription;

            if (!string.IsNullOrEmpty(title.NameLocStr))
            {
                var loc = service.Lookup(title.NameLocStr);
                if (loc != null) { title.Name = loc.Replace("%NAME%", "{0}"); changed = true; }
            }

            if (!string.IsNullOrEmpty(title.UnlockDescriptionLocStr))
            {
                var loc = service.Lookup(title.UnlockDescriptionLocStr);
                if (loc != null) { title.UnlockDescription = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(title.AlreadyUnlockedDescriptionLocStr))
            {
                var loc = service.Lookup(title.AlreadyUnlockedDescriptionLocStr);
                if (loc != null) { title.AlreadyUnlockedDescription = loc; changed = true; }
            }

            if (changed) count++;
        }
        return count;
    }

    /// <summary>
    /// Reverts all title display values to their original English defaults.
    /// </summary>
    public static void RevertLocalisation()
    {
        foreach (var kvp in _englishBackup)
        {
            if (_byId.TryGetValue(kvp.Key, out var title))
            {
                title.Name = kvp.Value.Name;
                title.UnlockDescription = kvp.Value.UnlockDescription;
                title.AlreadyUnlockedDescription = kvp.Value.AlreadyUnlockedDescription;
            }
        }
        _englishBackup.Clear();
    }

    /// <summary>Gets a title by its ID.</summary>
    public static TitleEntry? GetTitle(string id) =>
        _byId.TryGetValue(id, out var title) ? title : null;
}
