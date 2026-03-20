namespace NMSE.Models;

/// <summary>
/// Represents a frigate in the player's fleet in a No Man's Sky save, wrapping the underlying JSON data.
/// </summary>
public class Frigate
{
    private readonly JsonObject _data;
    private readonly int _index;

    /// <summary>
    /// Initializes a new frigate wrapper around the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object containing the frigate's save data.</param>
    /// <param name="index">The zero-based position of this frigate in the player's fleet.</param>
    public Frigate(JsonObject data, int index)
    {
        _data = data;
        _index = index;
    }

    /// <summary>The zero-based index of this frigate in the player's fleet.</summary>
    public int Index => _index;

    /// <summary>Whether this frigate is currently enabled and active.</summary>
    public bool IsEnabled
    {
        get => _data.Contains("Enabled") && _data.GetBool("Enabled");
    }

    /// <summary>The NPC type classification of this frigate, or <c>null</c> if not set.</summary>
    public string? NpcType
    {
        get => _data.GetString("Type");
    }

    /// <summary>The ship type of this frigate, or <c>null</c> if not set.</summary>
    public string? ShipType
    {
        get => _data.GetString("ShipType");
    }

    /// <summary>The rank of this frigate, or <c>null</c> if not set.</summary>
    public string? Rank
    {
        get => _data.GetString("Rank");
    }

    /// <summary>The raw JSON object backing this frigate instance.</summary>
    public JsonObject Data => _data;

    /// <inheritdoc />
    public override string ToString() => $"Frigate {_index + 1} ({NpcType ?? "Unknown"})";
}
