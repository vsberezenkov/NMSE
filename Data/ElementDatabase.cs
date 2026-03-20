namespace NMSE.Data;

/// <summary>
/// Contains elemental symbol data for NMS resources.
/// Each entry maps a resource name to its elemental symbol abbreviation.
/// </summary>
public static class ElementDatabase
{
    private static readonly Dictionary<string, string> _elements = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Carbon", "C" },
        { "Condensed Carbon", "C+" },
        { "Oxygen", "O2" },
        { "Sodium", "Na" },
        { "Sodium Nitrate", "Na+" },
        { "Di-hydrogen", "H" },
        { "Tritium", "H3" },
        { "Deuterium", "D" },
        { "Ferrite Dust", "Fe" },
        { "Pure Ferrite", "Fe+" },
        { "Magnetised Ferrite", "Fe++" },
        { "Copper", "Cu" },
        { "Cadmium", "Cd" },
        { "Emeril", "Em" },
        { "Indium", "In" },
        { "Activated Copper", "Cu+" },
        { "Activated Cadmium", "Cd+" },
        { "Activated Emeril", "Em+" },
        { "Activated Indium", "In+" },
        { "Cobalt", "Co" },
        { "Ionised Cobalt", "Co+" },
        { "Salt", "NaCl" },
        { "Chlorine", "Cl" },
        { "Pyrite", "Py" },
        { "Ammonia", "NH3" },
        { "Uranium", "U" },
        { "Dioxite", "CO2" },
        { "Phosphorus", "P" },
        { "Paraffinium", "Pf" },
        { "Star Bulb", "Sb" },
        { "Cactus Flesh", "Cc" },
        { "Frost Crystal", "Fc" },
        { "Gamma Root", "Gr" },
        { "Solanium", "So" },
        { "Fungal Mould", "Ml" },
        { "Gold", "Au" },
        { "Silver", "Ag" },
        { "Platinum", "Pt" },
        { "Chromatic Metal", "Ch" },
        { "Nitrogen", "N" },
        { "Sulphurine", "Su" },
        { "Radon", "Rn" },
        { "Mordite", "Mo" },
        { "Faecium", "Fa" },
        { "Pugneum", "Pg" },
        { "Nanite Cluster", "n" },
        { "QUICKSILVER", "Q" },
        { "Tainted Metal", "Tm" },
        { "Rusted Metal", "Rm" },
        { "Residual Goop", "Rg" },
        { "Viscous Fluids", "Vf" },
        { "Living Slime", "Ls" },
        { "Runaway Mould", "Rw" },
    };

    /// <summary>
    /// Gets the elemental symbol for a resource name, or null if not found.
    /// </summary>
    public static string? GetSymbol(string name)
    {
        return _elements.TryGetValue(name, out var symbol) ? symbol : null;
    }

}
