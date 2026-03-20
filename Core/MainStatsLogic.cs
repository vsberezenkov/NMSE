using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles reading and writing core player stats such as health, shield, energy, units, nanites, and quicksilver.
/// </summary>
internal static class MainStatsLogic
{
    /// <summary>
    /// Definitions for each player stat field, including display label, JSON key, and maximum allowed value.
    /// </summary>
    internal static readonly (string Label, string Key, decimal Maximum)[] StatFields =
    {
        ("Health", "Health", 999999),
        ("Shield", "Shield", 999999),
        ("Energy", "Energy", 999999),
        ("Units", "Units", uint.MaxValue),
        ("Nanites", "Nanites", uint.MaxValue),
        ("Quicksilver", "Specials", uint.MaxValue),
    };

    /// <summary>
    /// Reads a numeric stat value from the player state, clamping it within the specified range.
    /// Handles various underlying types including int, long, double, and decimal.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="key">The JSON key for the stat.</param>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <returns>The clamped stat value, or 0 on failure.</returns>
    internal static decimal ReadStatValue(JsonObject playerState, string key, decimal minimum, decimal maximum)
    {
        try
        {
            var value = playerState.Get(key) ?? playerState.GetValue(key);
            if (value == null) return 0;

            if (value is JsonObject jobj)
            {
                value = jobj.Get("Value") ?? jobj.Get("value");
                if (value == null) return 0;
            }

            decimal numericValue;
            if (value is int i)
                numericValue = (decimal)(uint)i;
            else if (value is long l)
                numericValue = (decimal)(l & 0xFFFFFFFFL);
            else if (value is Models.RawDouble rd)
            {
                double rdv = rd.Value;
                if (rdv < 0 && rdv >= int.MinValue)
                    numericValue = (decimal)(uint)(int)rdv;
                else
                    numericValue = (decimal)rdv;
            }
            else if (value is double dbl)
            {
                if (dbl < 0 && dbl >= int.MinValue)
                    numericValue = (decimal)(uint)(int)dbl;
                else
                    numericValue = (decimal)dbl;
            }
            else if (value is decimal d)
            {
                if (d < 0 && d >= int.MinValue)
                    numericValue = (decimal)(uint)(int)d;
                else
                    numericValue = d;
            }
            else
                numericValue = Convert.ToDecimal(value);

            if (numericValue < minimum) numericValue = minimum;
            if (numericValue > maximum) numericValue = maximum;
            return numericValue;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Writes all core player stat values to the player state JSON object.
    /// Currency values (units, nanites, quicksilver) are stored as unchecked unsigned integers.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="health">The health value to write.</param>
    /// <param name="shield">The shield value to write.</param>
    /// <param name="energy">The energy value to write.</param>
    /// <param name="units">The units (currency) value to write.</param>
    /// <param name="nanites">The nanites value to write.</param>
    /// <param name="quicksilver">The quicksilver value to write.</param>
    internal static void WriteStatValues(JsonObject playerState, decimal health, decimal shield, decimal energy,
        decimal units, decimal nanites, decimal quicksilver)
    {
        playerState.Set("Health", (int)health);
        playerState.Set("Shield", (int)shield);
        playerState.Set("Energy", (int)energy);
        playerState.Set("Units", unchecked((int)(uint)units));
        playerState.Set("Nanites", unchecked((int)(uint)nanites));
        playerState.Set("Specials", unchecked((int)(uint)quicksilver));
    }
}
