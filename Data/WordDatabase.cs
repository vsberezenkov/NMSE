using System.Text.Json;

namespace NMSE.Data;

/// <summary>
/// A single word with its display text and per-race group mappings.
/// Each word can have groups for different races (e.g., ^TRA_ATLAS for Gek, ^WAR_ATLAS for Vy'keen).
/// </summary>
public class WordEntry
{
    /// <summary>Unique word identifier (e.g. "^ATLAS").</summary>
    public string Id { get; }
    /// <summary>Display text for this word.</summary>
    public string Text { get; set; }
    /// <summary>Localisation lookup key for Text. Null when not available.</summary>
    public string? TextLocStr { get; }

    /// <summary>
    /// Maps group name (e.g., "^TRA_ATLAS") to race ordinal (0=Gek, 1=Vy'keen, 2=Korvax, 4=Atlas, 8=Autophage).
    /// </summary>
    public Dictionary<string, int> Groups { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Reverse mapping from race ordinal to group name for O(1) lookups.
    /// </summary>
    private readonly Dictionary<int, string> _raceToGroup = new();

    /// <summary>Creates a new word entry with the given ID, display text, and optional localisation key.</summary>
    /// <param name="id">Unique word identifier.</param>
    /// <param name="text">Display text for this word.</param>
    /// <param name="textLocStr">Optional localisation lookup key for Text.</param>
    public WordEntry(string id, string text, string? textLocStr = null)
    {
        Id = id;
        Text = text;
        TextLocStr = textLocStr;
    }

    /// <summary>
    /// Call after populating Groups to build the reverse lookup.
    /// </summary>
    public void BuildReverseLookup()
    {
        _raceToGroup.Clear();
        foreach (var kvp in Groups)
            _raceToGroup.TryAdd(kvp.Value, kvp.Key);
    }

    /// <summary>
    /// Returns true if this word has a group for the given race ordinal.
    /// </summary>
    public bool HasRace(int raceOrdinal) => _raceToGroup.ContainsKey(raceOrdinal);

    /// <summary>
    /// Returns the group name for the given race ordinal, or null if not available.
    /// </summary>
    public string? GetGroupForRace(int raceOrdinal)
        => _raceToGroup.TryGetValue(raceOrdinal, out var group) ? group : null;
}

/// <summary>
/// Manages the word database. Loads word data from Words.json
/// (produced by NMSE.Extractor from the game's alien speech table MXML).
/// </summary>
public class WordDatabase
{
    private readonly List<WordEntry> _words = new();

    /// <summary>All loaded word entries, sorted alphabetically after loading.</summary>
    public IReadOnlyList<WordEntry> Words => _words;

    /// <summary>
    /// Total number of words in the database.
    /// </summary>
    public int Count => _words.Count;

    /// <summary>
    /// Loads word data from a Words.json file produced by the extractor.
    /// Falls back gracefully if the file is missing or invalid.
    /// </summary>
    /// <param name="jsonPath">Path to Words.json.</param>
    /// <returns>True if words were loaded successfully.</returns>
    public bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            _words.Clear();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                string id = elem.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "" : "";
                string text = elem.TryGetProperty("Text", out var textProp) ? textProp.GetString() ?? "" : "";

                if (string.IsNullOrEmpty(id)) continue;

                string? textLocStr = elem.TryGetProperty("Text_LocStr", out var tlsProp) ? tlsProp.GetString() : null;
                var entry = new WordEntry(id, text, textLocStr);

                if (elem.TryGetProperty("Groups", out var groupsProp) &&
                    groupsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var group in groupsProp.EnumerateObject())
                    {
                        if (group.Value.TryGetInt32(out int raceOrdinal))
                            entry.Groups[group.Name] = raceOrdinal;
                    }
                }

                entry.BuildReverseLookup();
                _words.Add(entry);
            }

            // Sort alphabetically by display text
            _words.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));

            return _words.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private readonly Dictionary<string, string> _englishTextBackup = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Applies localised display text from the specified localisation service to all words.
    /// Backs up the original English text so it can be restored via <see cref="RevertLocalisation"/>.
    /// </summary>
    /// <param name="service">The localisation service with a loaded language.</param>
    /// <returns>The number of words that had their text localised.</returns>
    public int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive)
        {
            RevertLocalisation();
            return 0;
        }

        int count = 0;
        foreach (var word in _words)
        {
            if (!_englishTextBackup.ContainsKey(word.Id))
                _englishTextBackup[word.Id] = word.Text;
            string englishText = _englishTextBackup[word.Id];
            word.Text = englishText; // Restore baseline first

            string? loc = null;

            // Try primary TextLocStr key first
            if (!string.IsNullOrEmpty(word.TextLocStr))
                loc = service.Lookup(word.TextLocStr);

            // If the primary key returned the same English text (untranslated) or
            // was not found, try all group keys as fallback. Some languages only
            // have translations under specific race-prefixed keys (e.g. BUI_ACCESS
            // has a Japanese translation but TRA_ACCESS does not).
            if (loc == null || loc.Equals(englishText, StringComparison.OrdinalIgnoreCase))
            {
                foreach (string groupKey in word.Groups.Keys)
                {
                    // Group keys are stored with ^ prefix (e.g. "^BUI_ACCESS")
                    string lookupKey = groupKey.StartsWith('^') ? groupKey[1..] : groupKey;
                    string? groupLoc = service.Lookup(lookupKey);
                    if (groupLoc != null && !groupLoc.Equals(englishText, StringComparison.OrdinalIgnoreCase))
                    {
                        loc = groupLoc;
                        break;
                    }
                }
            }

            if (loc != null) { word.Text = loc; count++; }
        }

        // Re-sort alphabetically after localisation
        _words.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));
        return count;
    }

    /// <summary>
    /// Restores all word text to their original English defaults.
    /// </summary>
    public void RevertLocalisation()
    {
        foreach (var word in _words)
        {
            if (_englishTextBackup.TryGetValue(word.Id, out var englishText))
                word.Text = englishText;
        }

        _englishTextBackup.Clear();

        // Re-sort alphabetically after reverting
        _words.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));
    }
}