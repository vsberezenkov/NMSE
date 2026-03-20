namespace NMSE.Data;

/// <summary>Represents a single wiki guide topic.</summary>
public class WikiGuideTopic
{
    /// <summary>Unique topic identifier.</summary>
    public string Id { get; set; } = "";
    /// <summary>Human-readable topic name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Localisation lookup key for Name. Null when not available.</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Category this topic belongs to (e.g. "Survival", "Combat").</summary>
    public string Category { get; set; } = "";
    /// <summary>Localisation lookup key for Category. Null when not available.</summary>
    public string? CategoryLocStr { get; set; }
    /// <summary>Icon key for this topic (e.g. "COMBAT1").</summary>
    public string IconKey { get; set; } = "";

    /// <summary>Returns display name.</summary>
    public override string ToString() => Name;
}

/// <summary>NMS Wiki/Guide topic database with ID, display name, category, and icon key.</summary>
public static class WikiGuideDatabase
{
    /// <summary>
    /// All guide topics. Populated at startup from WikiGuide.json
    /// via <see cref="LoadFromFile"/>. Empty until loaded.
    /// </summary>
    public static readonly List<WikiGuideTopic> Topics = new();

    // ── Hardcoded fallback data removed ──
    // Topic data is now loaded from Resources/json/WikiGuide.json at startup.
    // The JSON file is produced by the extractor's ParseWikiGuide() method
    // from WIKI.MXML and contains topic IDs, names, categories, loc keys,
    // and icon keys for all 57 guide topics.

    private static Dictionary<string, WikiGuideTopic> _byId = BuildLookup();

    private static Dictionary<string, WikiGuideTopic> BuildLookup()
    {
        var dict = new Dictionary<string, WikiGuideTopic>(StringComparer.Ordinal);
        foreach (var t in Topics)
            dict[t.Id] = t;
        return dict;
    }

    private static readonly Dictionary<string, (string Name, string Category)> _englishBackup = new();

    /// <summary>Look up the display name for a topic ID, returning the ID itself if not found.</summary>
    public static string GetTopicName(string topicId) =>
        _byId.TryGetValue(topicId, out var info) ? info.Name : topicId;

    /// <summary>Look up the category for a topic ID.</summary>
    public static string GetTopicCategory(string topicId) =>
        _byId.TryGetValue(topicId, out var info) ? info.Category : "";

    /// <summary>
    /// Returns the original English category for a topic, using the backup dictionary
    /// when localisation is active. This ensures grid placement is always based on
    /// the hardcoded English GuideCategories array regardless of active language.
    /// </summary>
    public static string GetEnglishCategory(string topicId)
    {
        if (_englishBackup.TryGetValue(topicId, out var backup))
            return backup.Category;
        return _byId.TryGetValue(topicId, out var info) ? info.Category : "";
    }

    /// <summary>Look up the icon key for a topic ID (e.g. "COMBAT1").</summary>
    public static string GetTopicIconKey(string topicId) =>
        _byId.TryGetValue(topicId, out var info) ? info.IconKey : "";

    /// <summary>
    /// Loads wiki guide topics from a WikiGuide.json file.
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

            var loaded = new List<WikiGuideTopic>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                loaded.Add(new WikiGuideTopic
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    Name = elem.TryGetProperty("Name", out var nP) ? nP.GetString() ?? "" : "",
                    NameLocStr = elem.TryGetProperty("Name_LocStr", out var nlP) ? nlP.GetString() : null,
                    Category = elem.TryGetProperty("Category", out var cP) ? cP.GetString() ?? "" : "",
                    CategoryLocStr = elem.TryGetProperty("Category_LocStr", out var clP) ? clP.GetString() : null,
                    IconKey = elem.TryGetProperty("IconKey", out var ikP) ? ikP.GetString() ?? "" : "",
                });
            }

            if (loaded.Count > 0)
            {
                Topics.Clear();
                Topics.AddRange(loaded);
                _byId = BuildLookup();
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Applies localisation to topic names and categories using the specified service.
    /// </summary>
    public static int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive) { RevertLocalisation(); return 0; }

        int count = 0;
        foreach (var topic in Topics)
        {
            bool changed = false;

            if (!_englishBackup.ContainsKey(topic.Id))
                _englishBackup[topic.Id] = (topic.Name, topic.Category);

            var backup = _englishBackup[topic.Id];
            topic.Name = backup.Name;
            topic.Category = backup.Category;

            if (!string.IsNullOrEmpty(topic.NameLocStr))
            {
                var loc = service.Lookup(topic.NameLocStr);
                if (loc != null) { topic.Name = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(topic.CategoryLocStr))
            {
                var loc = service.Lookup(topic.CategoryLocStr);
                if (loc != null) { topic.Category = loc; changed = true; }
            }

            if (changed) count++;
        }
        return count;
    }

    /// <summary>
    /// Reverts all topic names and categories to their original English values.
    /// </summary>
    public static void RevertLocalisation()
    {
        foreach (var kvp in _englishBackup)
        {
            if (_byId.TryGetValue(kvp.Key, out var topic))
            {
                topic.Name = kvp.Value.Name;
                topic.Category = kvp.Value.Category;
            }
        }
        _englishBackup.Clear();
    }
}
