namespace NMSE.Models;

/// <summary>
/// Represents an inventory grid (e.g., ship cargo, personal tech) in a No Man's Sky save.
/// </summary>
public class Inventory
{
    private readonly JsonObject _data;
    private readonly InventoryType _type;

    /// <summary>
    /// Initializes a new inventory wrapper around the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object containing the inventory's save data.</param>
    /// <param name="type">The category of inventory this represents.</param>
    public Inventory(JsonObject data, InventoryType type)
    {
        _data = data;
        _type = type;
    }

    /// <summary>The category of this inventory (e.g., Ship, PersonalCargo).</summary>
    public InventoryType Type => _type;

    /// <summary>The width of the inventory grid in slots.</summary>
    public int Width
    {
        get => _data.Contains("Width") ? _data.GetInt("Width") : 0;
        set => _data.Set("Width", value);
    }

    /// <summary>The height of the inventory grid in slots.</summary>
    public int Height
    {
        get => _data.Contains("Height") ? _data.GetInt("Height") : 0;
        set => _data.Set("Height", value);
    }

    /// <summary>The array of inventory slot entries, or <c>null</c> if not present.</summary>
    public JsonArray? Slots => _data.GetArray("Slots");

    /// <summary>The array of valid slot index values, or <c>null</c> if not present.</summary>
    public JsonArray? ValidSlotIndices => _data.GetArray("ValidSlotIndices");

    /// <summary>The raw JSON object backing this inventory instance.</summary>
    public JsonObject Data => _data;

    /// <inheritdoc />
    public override string ToString() => $"{_type} ({Width}x{Height})";
}
