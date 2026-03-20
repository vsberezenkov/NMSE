namespace NMSE.Models;

/// <summary>
/// Represents a companion creature in a No Man's Sky save, wrapping the underlying JSON data.
/// </summary>
public class Companion
{
    private readonly JsonObject _data;
    private readonly int _index;

    /// <summary>
    /// Initializes a new companion wrapper around the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object containing the companion's save data.</param>
    /// <param name="index">The zero-based position of this companion in the player's collection.</param>
    public Companion(JsonObject data, int index)
    {
        _data = data;
        _index = index;
    }

    /// <summary>The zero-based index of this companion in the player's collection.</summary>
    public int Index => _index;

    /// <summary>The creature type of this companion, or <c>null</c> if not set.</summary>
    public string? Type
    {
        get => _data.GetString("Type");
        set => _data.Set("Type", value);
    }

    /// <summary>The procedural generation seed that determines the companion's appearance.</summary>
    public string? Seed
    {
        get => _data.GetString("Seed");
        set => _data.Set("Seed", value);
    }

    /// <summary>The raw JSON object backing this companion instance.</summary>
    public JsonObject Data => _data;

    /// <inheritdoc />
    public override string ToString() => $"Companion {_index + 1} ({Type ?? "Unknown"})";
}
