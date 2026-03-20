namespace NMSE.Models;

/// <summary>
/// Represents a player-owned ship in a No Man's Sky save, wrapping the underlying JSON data.
/// </summary>
public class Ship
{
    private readonly JsonObject _data;
    private readonly int _index;

    /// <summary>
    /// Initializes a new ship wrapper around the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object containing the ship's save data.</param>
    /// <param name="index">The zero-based position of this ship in the player's fleet.</param>
    public Ship(JsonObject data, int index)
    {
        _data = data;
        _index = index;
    }

    /// <summary>The zero-based index of this ship in the player's fleet.</summary>
    public int Index => _index;

    /// <summary>The display name of the ship.</summary>
    public string Name
    {
        get => _data.GetString("Name") ?? $"Ship {_index + 1}";
        set => _data.Set("Name", value);
    }

    /// <summary>The procedural generation seed that determines the ship's appearance.</summary>
    public string? Seed
    {
        get => _data.GetString("Seed");
        set => _data.Set("Seed", value);
    }

    /// <summary>The archetype of this ship (e.g., Fighter, Hauler, Exotic).</summary>
    public ShipType Type
    {
        get
        {
            var typeStr = _data.GetString("ShipType");
            return Enum.TryParse<ShipType>(typeStr, out var t) ? t : ShipType.Unknown;
        }
        set => _data.Set("ShipType", value.ToString());
    }

    /// <summary>The quality class of this ship (C through S).</summary>
    public ShipClass Class
    {
        get
        {
            var classStr = _data.GetString("ShipClass");
            return Enum.TryParse<ShipClass>(classStr, out var c) ? c : ShipClass.C;
        }
        set => _data.Set("ShipClass", value.ToString());
    }

    /// <summary>The ship's general inventory data, or <c>null</c> if not present.</summary>
    public JsonObject? Inventory => _data.GetObject("Inventory");

    /// <summary>The ship's technology-only inventory data, or <c>null</c> if not present.</summary>
    public JsonObject? TechInventory => _data.GetObject("Inventory_TechOnly");

    /// <summary>The ship's cargo inventory data, or <c>null</c> if not present.</summary>
    public JsonObject? CargoInventory => _data.GetObject("Inventory_Cargo");

    /// <summary>The raw JSON object backing this ship instance.</summary>
    public JsonObject Data => _data;

    /// <inheritdoc />
    public override string ToString() => Name;
}
