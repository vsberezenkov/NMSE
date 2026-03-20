namespace NMSE.Models;

/// <summary>
/// Represents a player-owned multitool weapon in a No Man's Sky save, wrapping the underlying JSON data.
/// </summary>
public class Multitool
{
    private readonly JsonObject _data;
    private readonly int _index;

    /// <summary>
    /// Initializes a new multitool wrapper around the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object containing the multitool's save data.</param>
    /// <param name="index">The zero-based position of this multitool in the player's collection.</param>
    public Multitool(JsonObject data, int index)
    {
        _data = data;
        _index = index;
    }

    /// <summary>The zero-based index of this multitool in the player's collection.</summary>
    public int Index => _index;

    /// <summary>The display name of the multitool.</summary>
    public string Name
    {
        get => _data.GetString("Name") ?? $"Multitool {_index + 1}";
        set => _data.Set("Name", value);
    }

    /// <summary>The procedural generation seed that determines the multitool's appearance.</summary>
    public string? Seed
    {
        get => _data.GetString("Seed");
        set => _data.Set("Seed", value);
    }

    /// <summary>The archetype of this multitool (e.g., Rifle, Pistol, Royal).</summary>
    public MultitoolType Type
    {
        get
        {
            var typeStr = _data.GetString("MultitoolType");
            return Enum.TryParse<MultitoolType>(typeStr, out var t) ? t : MultitoolType.Unknown;
        }
        set => _data.Set("MultitoolType", value.ToString());
    }

    /// <summary>The quality class of this multitool (C through S).</summary>
    public ShipClass Class
    {
        get
        {
            var classStr = _data.GetString("MultitoolClass");
            return Enum.TryParse<ShipClass>(classStr, out var c) ? c : ShipClass.C;
        }
        set => _data.Set("MultitoolClass", value.ToString());
    }

    /// <summary>The multitool's inventory data, or <c>null</c> if not present.</summary>
    public JsonObject? Inventory => _data.GetObject("Inventory");

    /// <summary>The raw JSON object backing this multitool instance.</summary>
    public JsonObject Data => _data;

    /// <inheritdoc />
    public override string ToString() => Name;
}
