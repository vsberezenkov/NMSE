namespace NMSE.Models;

/// <summary>
/// Metadata about a save file - version, compression type, play time, etc.
/// </summary>
public class SaveFileMetadata
{
    /// <summary>The save format version number.</summary>
    public int Version { get; set; }

    /// <summary>Total play time in seconds.</summary>
    public int PlayTime { get; set; }

    /// <summary>Whether the save file data is LZ4-compressed.</summary>
    public bool IsCompressed { get; set; }

    /// <summary>The player or save slot name, or <c>null</c> if unavailable.</summary>
    public string? Name { get; set; }

    /// <summary>An optional description string for the save, or <c>null</c> if unavailable.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creates a shallow copy of this metadata instance.
    /// </summary>
    /// <returns>A new <see cref="SaveFileMetadata"/> with the same property values.</returns>
    public SaveFileMetadata Clone() => new()
    {
        Version = Version,
        PlayTime = PlayTime,
        IsCompressed = IsCompressed,
        Name = Name,
        Description = Description
    };

    /// <summary>
    /// Gets the play time formatted as a human-readable duration string (e.g., "2:05:30" or "5:30").
    /// </summary>
    public string PlayTimeFormatted
    {
        get
        {
            int hours = PlayTime / 3600;
            int minutes = (PlayTime % 3600) / 60;
            int seconds = PlayTime % 60;
            return hours > 0 ? $"{hours}:{minutes:D2}:{seconds:D2}" : $"{minutes}:{seconds:D2}";
        }
    }
}
