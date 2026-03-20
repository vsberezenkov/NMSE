namespace NMSE.Models;

/// <summary>
/// Represents a crafting/refining recipe from the game data.
/// </summary>
public class Recipe
{
    /// <summary>Unique recipe identifier.</summary>
    public string Id { get; set; } = "";
    /// <summary>Recipe category (e.g. "product", "cooking").</summary>
    public string Category { get; set; } = "";
    /// <summary>Recipe type (e.g. "standard", "refining", "cooking").</summary>
    public string RecipeType { get; set; } = "";
    /// <summary>Display name of the recipe.</summary>
    public string RecipeName { get; set; } = "";
    /// <summary>Localisation lookup key for RecipeName. Null when not available.</summary>
    public string? RecipeNameLocStr { get; set; }
    /// <summary>Localisation lookup key for RecipeType. Null when not available.</summary>
    public string? RecipeTypeLocStr { get; set; }
    /// <summary>The output item produced by this recipe.</summary>
    public RecipeElement? Result { get; set; }
    /// <summary>The input items required for this recipe.</summary>
    public RecipeElement[] Ingredients { get; set; } = [];
    /// <summary>Time in seconds to craft this recipe.</summary>
    public int TimeToMake { get; set; }
    /// <summary>Whether this is a cooking recipe.</summary>
    public bool Cooking { get; set; }

    /// <summary>Returns a display-friendly summary of the recipe.</summary>
    public override string ToString()
    {
        string ingredients = string.Join(" + ", Ingredients.Select(i => $"{i.Amount}x {i.Id}"));
        return $"{ingredients} → {Result?.Amount}x {Result?.Id} ({TimeToMake}s)";
    }
}

/// <summary>
/// Represents a single ingredient or result element in a recipe.
/// </summary>
public class RecipeElement
{
    /// <summary>Item ID (e.g. "FUEL1", "STELLAR2").</summary>
    public string Id { get; set; } = "";
    /// <summary>Inventory type classification (e.g. "Substance", "Product").</summary>
    public string Type { get; set; } = "";
    /// <summary>Quantity of this element used/produced.</summary>
    public int Amount { get; set; }

    /// <summary>Returns display string.</summary>
    public override string ToString() => $"{Amount}x {Id}";
}
