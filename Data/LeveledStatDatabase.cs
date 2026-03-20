using System.Text.Json;

namespace NMSE.Data;

/// <summary>
/// Represents a leveled stat with progression values.
/// </summary>
public class LeveledStat
{
    /// <summary>Display name of the stat.</summary>
    public string Name { get; set; } = "";
    /// <summary>Unique stat identifier.</summary>
    public string Id { get; set; } = "";
    /// <summary>Icon filename for this stat.</summary>
    public string Icon { get; set; } = "";
    /// <summary>Whether this stat uses floating-point values.</summary>
    public bool IsFloat { get; set; }
    /// <summary>Progression levels for this stat.</summary>
    public LeveledStatLevel[] Levels { get; set; } = [];

    /// <summary>Returns the display name.</summary>
    public override string ToString() => Name;
}

/// <summary>
/// Represents a single level within a leveled stat's progression.
/// </summary>
public class LeveledStatLevel
{
    /// <summary>Display name for this level (e.g. "Class C", "Class S").</summary>
    public string Name { get; set; } = "";
    /// <summary>Stat values at this level keyed by stat type name.</summary>
    public Dictionary<string, int> Value { get; set; } = new();
}

/// <summary>
/// Loads and manages leveled stat data from the LeveledStats.json database file.
/// </summary>
public static class LeveledStatDatabase
{
    private static readonly List<LeveledStat> _stats = new();
    private static readonly Dictionary<string, LeveledStat> _byId = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All loaded leveled stats.</summary>
    public static IReadOnlyList<LeveledStat> Stats => _stats;

    /// <summary>Whether data has been loaded.</summary>
    public static bool IsLoaded => _stats.Count > 0;

    /// <summary>
    /// Loads leveled stats from a LeveledStats.json file.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            _stats.Clear();
            _byId.Clear();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var stat = new LeveledStat
                {
                    Name = elem.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    Id = elem.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "" : "",
                    Icon = elem.TryGetProperty("Icon", out var iconProp) ? iconProp.GetString() ?? "" : "",
                    IsFloat = elem.TryGetProperty("IsFloat", out var floatProp) && floatProp.ValueKind == JsonValueKind.True,
                };

                if (elem.TryGetProperty("Levels", out var levelsProp) && levelsProp.ValueKind == JsonValueKind.Array)
                {
                    var levels = new List<LeveledStatLevel>();
                    foreach (var levelElem in levelsProp.EnumerateArray())
                    {
                        var level = new LeveledStatLevel
                        {
                            Name = levelElem.TryGetProperty("Name", out var lnProp) ? lnProp.GetString() ?? "" : "",
                        };
                        if (levelElem.TryGetProperty("Value", out var valProp) && valProp.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var kvp in valProp.EnumerateObject())
                            {
                                if (kvp.Value.TryGetInt32(out int v))
                                    level.Value[kvp.Name] = v;
                            }
                        }
                        levels.Add(level);
                    }
                    stat.Levels = levels.ToArray();
                }

                if (!string.IsNullOrEmpty(stat.Id))
                {
                    _stats.Add(stat);
                    _byId[stat.Id] = stat;
                }
            }

            return _stats.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets a leveled stat by its ID.</summary>
    public static LeveledStat? GetStat(string id) =>
        _byId.TryGetValue(id, out var stat) ? stat : null;

    /// <summary>
    /// Gets the value for a specific stat at a given class level.
    /// </summary>
    /// <param name="statId">The leveled stat ID.</param>
    /// <param name="className">The class name (e.g. "C", "B", "A", "S").</param>
    /// <param name="valueKey">The specific value key within the level.</param>
    /// <returns>The stat value, or null if not found.</returns>
    public static int? GetValueAtClass(string statId, string className, string valueKey)
    {
        var stat = GetStat(statId);
        if (stat == null) return null;

        foreach (var level in stat.Levels)
        {
            if (level.Name.Equals(className, StringComparison.OrdinalIgnoreCase) &&
                level.Value.TryGetValue(valueKey, out int val))
            {
                return val;
            }
        }
        return null;
    }
}
