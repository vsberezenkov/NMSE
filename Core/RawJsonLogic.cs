using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Provides utilities for parsing, formatting, and displaying raw JSON save data.
/// </summary>
internal static class RawJsonLogic
{
    /// <summary>
    /// Parses and reformats a JSON string with consistent indentation.
    /// </summary>
    /// <param name="jsonText">The raw JSON text to format.</param>
    /// <returns>The formatted JSON string.</returns>
    internal static string FormatJson(string jsonText)
    {
        var obj = JsonObject.Parse(jsonText);
        return obj.ToFormattedString();
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="jsonText">The raw JSON text to parse.</param>
    /// <returns>The parsed JSON object.</returns>
    internal static JsonObject ParseJson(string jsonText)
    {
        return JsonObject.Parse(jsonText);
    }

    /// <summary>
    /// Converts save data to a human-readable display string.
    /// </summary>
    /// <param name="saveData">The save data JSON object.</param>
    /// <returns>A display-friendly string representation of the save data.</returns>
    internal static string ToDisplayString(JsonObject saveData)
    {
        return saveData.ToDisplayString();
    }
}
