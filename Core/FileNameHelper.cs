namespace NMSE.Core;

/// <summary>
/// Shared helper for sanitizing file names across all logic classes.
/// </summary>
internal static class FileNameHelper
{
    /// <summary>
    /// Sanitize a name for use in file names by replacing invalid chars and spaces with underscores.
    /// Returns "unnamed" for null, empty, or whitespace-only input.
    /// </summary>
    internal static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unnamed";
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Replace(' ', '_');
    }
}
