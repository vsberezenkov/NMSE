using NMSE.Models;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace NMSE.IO;

/// <summary>
/// Manages loading and saving NMS save files.
/// Handles Steam, GOG, Xbox Game Pass, and PS4 save locations.
/// </summary>
public class SaveFileManager
{
    /// <summary>
    /// ISO-8859-1 (Latin-1) encoding which maps bytes 0x00-0xFF to Unicode code points 1:1.
    /// Used instead of UTF-8 when reading save files so that binary data embedded in JSON
    /// string values (e.g. TechBox item IDs) is preserved as individual characters rather
    /// than being corrupted by invalid-UTF-8 replacement.  The JSON parser then detects
    /// characters ≥ 0x80 inside string tokens and produces BinaryData objects.
    /// </summary>
    private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);
    /// <summary>
    /// Supported save file platform types.
    /// </summary>
    public enum Platform { Steam, XboxGamePass, PS4, GOG, Switch, Unknown }

    /// <summary>
    /// Represents a single save slot with its file paths and metadata.
    /// </summary>
    public class SaveSlot
    {
        /// <summary>Gets or sets the zero-based slot index.</summary>
        public int Index { get; set; }
        /// <summary>Gets or sets the path to the save data file.</summary>
        public string? FilePath { get; set; }
        /// <summary>Gets or sets the path to the companion metadata file.</summary>
        public string? MetadataPath { get; set; }
        /// <summary>Gets or sets whether this slot has no save data.</summary>
        public bool IsEmpty { get; set; } = true;
        /// <summary>Gets or sets the last modification time of the save file.</summary>
        public DateTime LastModified { get; set; }
        /// <summary>Gets or sets the platform this save slot belongs to.</summary>
        public Platform Platform { get; set; }
    }

    private static readonly byte[] Lz4Magic = { 0xE5, 0xA1, 0xED, 0xFE };

    /// <summary>
    /// Detects the platform type of saves in the specified directory.
    /// </summary>
    /// <param name="directory">The save directory to inspect.</param>
    /// <returns>The detected platform, or <see cref="Platform.Unknown"/> if unrecognized.</returns>
    public static Platform DetectPlatform(string directory)
    {
        if (File.Exists(Path.Combine(directory, "containers.index")))
            return Platform.XboxGamePass;
        if (Directory.GetFiles(directory, "manifest*.dat").Length > 0)
            return Platform.Switch;
        if (File.Exists(Path.Combine(directory, "memory.dat")) ||
            Directory.GetFiles(directory, "savedata*.hg").Length > 0)
            return Platform.PS4;
        if (Directory.GetFiles(directory, "save*.hg").Length > 0 ||
            File.Exists(Path.Combine(directory, "accountdata.hg")))
        {
            // GOG uses DefaultUser directory name; Steam uses st_<SteamID>
            string dirName = new DirectoryInfo(directory).Name;
            if (string.Equals(dirName, "DefaultUser", StringComparison.OrdinalIgnoreCase))
                return Platform.GOG;
            return Platform.Steam;
        }
        return Platform.Unknown;
    }

    /// <summary>
    /// Attempts to find the default NMS save directory for the current OS.
    /// <list type="bullet">
    /// <item><description>Windows (Steam): <c>%APPDATA%\HelloGames\NMS\{profile}</c></description></item>
    /// <item><description>Windows (Xbox GP): <c>%LOCALAPPDATA%\Packages\HelloGames*</c></description></item>
    /// <item><description>macOS: <c>~/Library/Application Support/HelloGames/NMS/{profile}</c></description></item>
    /// <item><description>Linux (Steam/Proton): <c>~/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/{profile}</c></description></item>
    /// </list>
    /// </summary>
    /// <returns>The path to the first discovered save profile directory, or null if not found.</returns>
    public static string? FindDefaultSaveDirectory()
    {
        // Windows: Steam default location (%APPDATA%\HelloGames\NMS)
        string steamPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HelloGames", "NMS");

        if (Directory.Exists(steamPath))
        {
            var dirs = Directory.GetDirectories(steamPath);
            if (dirs.Length > 0)
                return dirs[0]; // Return first profile directory
        }

        // Windows: Xbox Game Pass location
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string xboxPath = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(xboxPath))
        {
            var nmsDirs = Directory.GetDirectories(xboxPath, "HelloGames*");
            if (nmsDirs.Length > 0)
                return nmsDirs[0];
        }

        // macOS: ~/Library/Application Support/HelloGames/NMS
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsMacOS())
        {
            string macPath = Path.Combine(home, "Library", "Application Support", "HelloGames", "NMS");
            if (Directory.Exists(macPath))
            {
                var dirs = Directory.GetDirectories(macPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }
        }

        // Linux: Steam/Proton compatibility data
        if (OperatingSystem.IsLinux())
        {
            string protonPath = Path.Combine(home, ".local", "share", "Steam", "steamapps",
                "compatdata", "275850", "pfx", "drive_c", "users", "steamuser",
                "AppData", "Roaming", "HelloGames", "NMS");
            if (Directory.Exists(protonPath))
            {
                var dirs = Directory.GetDirectories(protonPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }

            // Flatpak Steam location
            string flatpakPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                "data", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c",
                "users", "steamuser", "AppData", "Roaming", "HelloGames", "NMS");
            if (Directory.Exists(flatpakPath))
            {
                var dirs = Directory.GetDirectories(flatpakPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a timestamped zip backup of the save directory, retaining up to 10 backups.
    /// </summary>
    /// <param name="saveDirectory">The save directory to back up.</param>
    public static void BackupSaveDirectory(string saveDirectory)
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string backupRoot = Path.Combine(exeDir, "Save Backups");
        Directory.CreateDirectory(backupRoot);

        string dirName = new DirectoryInfo(saveDirectory).Name;
        string backupPattern = $"{dirName}_*.zip";
        var existingBackups = Directory.GetFiles(backupRoot, backupPattern)
            .OrderBy(f => File.GetCreationTimeUtc(f))
            .ToList();

        // If there are 10 or more backups, delete the oldest one
        if (existingBackups.Count >= 10)
        {
            File.Delete(existingBackups[0]);
            existingBackups.RemoveAt(0);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupName = $"{dirName}_{timestamp}.zip";
        string backupPath = Path.Combine(backupRoot, backupName);

        // Avoid zipping if already exists for this second
        if (!File.Exists(backupPath))
        {
            ZipFile.CreateFromDirectory(saveDirectory, backupPath, CompressionLevel.Fastest, false);
        }
    }

    /// <summary>
    /// Load a save file and return the JSON data.
    /// Handles both compressed (LZ4) and uncompressed save files.
    /// Uses streaming I/O and string.Create to minimize intermediate memory allocations.
    /// </summary>
    public static JsonObject LoadSaveFile(string filePath)
    {
        string json;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 65536, FileOptions.SequentialScan))
        {
            byte[] header = new byte[4];
            int headerRead = fs.Read(header, 0, Math.Min(4, (int)fs.Length));
            fs.Position = 0;

            if (headerRead >= 4 && IsLz4Compressed(header))
            {
                json = DecompressLz4SaveStreamed(fs);
            }
            else
            {
                json = ReadPlainSave(fs);
            }
        }

        var result = JsonObject.Parse(json);

        // Register the PlayerStateData and SpawnStateData context transforms.
        RegisterContextTransforms(result);

        return result;
    }

    /// <summary>
    /// Save JSON data back to a file with LZ4 compression.
    /// Optionally writes a platform-appropriate meta file alongside the save.
    /// </summary>
    /// <param name="filePath">Path to the save file.</param>
    /// <param name="data">The JSON save data to write.</param>
    /// <param name="compress">Whether to LZ4-compress the output.</param>
    /// <param name="writeMeta">Whether to also write a platform meta file (mf_*.hg, manifest*.hg).</param>
    /// <param name="platform">Platform to determine meta format. Defaults to auto-detect from directory.</param>
    /// <param name="slotIndex">Slot index for meta file naming and encryption key.</param>
    public static void SaveToFile(string filePath, JsonObject data, bool compress = true,
        bool writeMeta = false, Platform? platform = null, int slotIndex = 0)
    {
        // NMS save files use compact JSON (no whitespace) with a null terminator byte.
        string json = data.ToString();
        byte[] jsonBytes = Latin1.GetBytes(json);

        // Append null terminator (NMS expects \0 after JSON data)
        byte[] dataBytes = new byte[jsonBytes.Length + 1];
        Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);
        // dataBytes[jsonBytes.Length] is already 0 (null terminator)

        // Create backup
        if (File.Exists(filePath))
        {
            string backupPath = filePath + ".backup";
            File.Copy(filePath, backupPath, true);
        }

        byte[]? compressedBytes = null;
        if (compress)
        {
            // Compress to memory first, then write to file (avoids double compression when writeMeta is true)
            using var ms = new MemoryStream();
            using (var compressor = new Lz4CompressorStream(ms))
            {
                compressor.Write(dataBytes, 0, dataBytes.Length);
            }
            compressedBytes = ms.ToArray();
            File.WriteAllBytes(filePath, compressedBytes);
        }
        else
        {
            File.WriteAllBytes(filePath, dataBytes);
            compressedBytes = dataBytes;
        }

        // Write platform meta file if requested
        if (writeMeta && compressedBytes != null)
        {
            var detectedPlatform = platform ?? DetectPlatform(Path.GetDirectoryName(filePath)!);
            var metaInfo = MetaFileWriter.ExtractMetaInfo(data);
            uint decompressedSize = (uint)dataBytes.Length;
            // Derive storage slot from the file name for correct encryption key.
            // Using the wrong slot produces garbled meta data (e.g. save name
            // shows as random characters like ","  in the game's slot browser).
            int storageSlot = SaveSlotManager.StorageSlotFromFileName(filePath);

            switch (detectedPlatform)
            {
                case Platform.Steam:
                case Platform.GOG:
                    MetaFileWriter.WriteSteamMeta(filePath, compressedBytes, decompressedSize, metaInfo, storageSlot);
                    break;
                case Platform.Switch:
                    MetaFileWriter.WriteSwitchMeta(filePath, decompressedSize, metaInfo, slotIndex);
                    break;
                case Platform.PS4:
                    MetaFileWriter.WritePlaystationStreamingMeta(filePath, decompressedSize, metaInfo, slotIndex);
                    break;
            }
        }
    }

    /// <summary>
    /// Load a save from an Xbox Game Pass containers.index directory.
    /// </summary>
    /// <param name="containersIndexPath">Path to the containers.index file.</param>
    /// <param name="saveIdentifier">Slot identifier (e.g., "Slot1Auto").</param>
    /// <returns>Parsed JSON object, or null if the slot doesn't exist.</returns>
    public static JsonObject? LoadXboxSave(string containersIndexPath, string saveIdentifier)
    {
        var slots = ContainersIndexManager.ParseContainersIndex(containersIndexPath);
        if (!slots.TryGetValue(saveIdentifier, out var slotInfo)) return null;

        string? json = ContainersIndexManager.LoadXboxSave(slotInfo);
        if (json == null) return null;

        var result = JsonObject.Parse(json);
        RegisterContextTransforms(result);
        return result;
    }

    /// <summary>
    /// Load a save from a PS4 memory.dat file.
    /// </summary>
    /// <param name="memoryDatPath">Path to memory.dat.</param>
    /// <param name="slotIndex">Slot index within memory.dat.</param>
    /// <returns>Parsed JSON object, or null if the slot doesn't exist.</returns>
    public static JsonObject? LoadPS4MemoryDatSave(string memoryDatPath, int slotIndex)
    {
        string? json = MemoryDatManager.ExtractSlotData(memoryDatPath, slotIndex);
        if (json == null) return null;

        var result = JsonObject.Parse(json);
        RegisterContextTransforms(result);
        return result;
    }

    /// <summary>
    /// Register the ActiveContext-based path transforms on a loaded save.
    /// </summary>
    internal static void RegisterContextTransforms(JsonObject result)
    {
        if (result.Get("PlayerStateData") == null)
        {
            result.RegisterTransform("PlayerStateData", obj =>
            {
                if (obj is not JsonObject root) return "PlayerStateData";
                var activeContext = root.Get("ActiveContext") as string;
                if (activeContext == "Main" && root.GetValue("BaseContext.PlayerStateData") != null)
                    return "BaseContext.PlayerStateData";
                if (activeContext == "Season" && root.GetValue("ExpeditionContext.PlayerStateData") != null)
                    return "ExpeditionContext.PlayerStateData";
                return "PlayerStateData";
            });
        }

        if (result.Get("SpawnStateData") == null)
        {
            result.RegisterTransform("SpawnStateData", obj =>
            {
                if (obj is not JsonObject root) return "SpawnStateData";
                var activeContext = root.Get("ActiveContext") as string;
                if (activeContext == "Main" && root.GetValue("BaseContext.SpawnStateData") != null)
                    return "BaseContext.SpawnStateData";
                if (activeContext == "Season" && root.GetValue("ExpeditionContext.SpawnStateData") != null)
                    return "ExpeditionContext.SpawnStateData";
                return "SpawnStateData";
            });
        }
    }

    private static bool IsLz4Compressed(byte[] data)
    {
        if (data.Length < 4) return false;
        return data[0] == Lz4Magic[0] && data[1] == Lz4Magic[1] &&
               data[2] == Lz4Magic[2] && data[3] == Lz4Magic[3];
    }

    /// <summary>
    /// Read an uncompressed save file from a stream using Latin1 encoding.
    /// Uses ArrayPool to avoid a long-lived byte[] allocation and string.Create
    /// to avoid the intermediate char[] that Encoding.GetString would allocate.
    /// </summary>
    private static string ReadPlainSave(FileStream fs)
    {
        int length = (int)fs.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            int read = 0;
            while (read < length)
            {
                int n = fs.Read(buffer, read, length - read);
                if (n <= 0) break;
                read += n;
            }
            return BytesToLatin1String(buffer, read);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Decompress an LZ4 save file from a stream.
    /// Streams compressed blocks directly from the FileStream instead of loading
    /// the entire file into memory first, and uses pooled buffers for compressed blocks.
    /// </summary>
    private static string DecompressLz4SaveStreamed(FileStream fs)
    {
        byte[] header = new byte[16];

        // First pass: calculate total decompressed size by scanning block headers
        int totalSize = 0;
        long scanPos = 0;
        while (scanPos + 16 <= fs.Length)
        {
            fs.Position = scanPos;
            if (fs.Read(header, 0, 16) < 16) break;
            if (header[0] != Lz4Magic[0] || header[1] != Lz4Magic[1] ||
                header[2] != Lz4Magic[2] || header[3] != Lz4Magic[3])
                break;

            int compressedLen = header[4] | (header[5] << 8) |
                               (header[6] << 16) | (header[7] << 24);
            int uncompressedLen = header[8] | (header[9] << 8) |
                                 (header[10] << 16) | (header[11] << 24);

            if (compressedLen < 0 || uncompressedLen < 0)
                throw new IOException("Corrupt save file: negative length values");
            if (compressedLen > 256 * 1024 * 1024 || uncompressedLen > 256 * 1024 * 1024)
                throw new IOException("Corrupt save file: block size exceeds 256MB limit");

            totalSize += uncompressedLen;
            scanPos += 16 + compressedLen;
        }

        // Single allocation for all decompressed data
        byte[] result = new byte[totalSize];
        int writePos = 0;
        fs.Position = 0;

        while (fs.Position + 16 <= fs.Length)
        {
            if (fs.Read(header, 0, 16) < 16) break;
            if (header[0] != Lz4Magic[0] || header[1] != Lz4Magic[1] ||
                header[2] != Lz4Magic[2] || header[3] != Lz4Magic[3])
                break;

            int compressedLen = header[4] | (header[5] << 8) |
                               (header[6] << 16) | (header[7] << 24);
            int uncompressedLen = header[8] | (header[9] << 8) |
                                 (header[10] << 16) | (header[11] << 24);

            if (fs.Position + compressedLen > fs.Length)
                throw new IOException("Corrupt save file: compressed data exceeds file length");

            // Read compressed block using pooled buffer
            byte[] compressedBlock = ArrayPool<byte>.Shared.Rent(compressedLen);
            try
            {
                int totalRead = 0;
                while (totalRead < compressedLen)
                {
                    int n = fs.Read(compressedBlock, totalRead, compressedLen - totalRead);
                    if (n <= 0) break;
                    totalRead += n;
                }

                // Decompress directly from byte array into result (no stream overhead)
                int decompressed = Lz4Compressor.Decompress(
                    compressedBlock, 0, totalRead,
                    result, writePos, uncompressedLen);
                writePos += decompressed;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(compressedBlock);
            }
        }

        return BytesToLatin1String(result, writePos);
    }

    /// <summary>
    /// Convert bytes to a Latin1 string using the framework's optimized Latin1 encoder.
    /// Latin1 maps bytes 0x00-0xFF to Unicode code points U+0000-U+00FF one-to-one.
    /// The .NET runtime uses SIMD-optimized widening for this encoding, which is
    /// significantly faster than a manual byte-to-char loop.
    /// </summary>
    private static string BytesToLatin1String(byte[] bytes, int length)
    {
        return Latin1.GetString(bytes, 0, length);
    }

    /// <summary>
    /// Format play time as MM:SS or H:MM:SS string.
    /// </summary>
    public static string FormatPlayTime(long seconds)
    {
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;
        return hours > 0 ? $"{hours}:{minutes:D2}:{secs:D2}" : $"{minutes}:{secs:D2}";
    }

    /// <summary>
    /// Quickly detect the game mode from a save file without fully parsing it.
    /// Only reads and decompresses the first LZ4 block to scan for PresetGameMode.
    /// </summary>
    public static int DetectGameModeFast(string filePath)
    {
        try
        {
            string text;
            byte[] header = new byte[16];

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Read(header, 0, 16) < 16) return 0;

            if (header[0] == Lz4Magic[0] && header[1] == Lz4Magic[1] &&
                header[2] == Lz4Magic[2] && header[3] == Lz4Magic[3])
            {
                // LZ4 compressed - read only the first block
                int compressedLen = header[4] | (header[5] << 8) | (header[6] << 16) | (header[7] << 24);
                int uncompressedLen = header[8] | (header[9] << 8) | (header[10] << 16) | (header[11] << 24);
                if (compressedLen <= 0 || uncompressedLen <= 0) return 0;
                if (compressedLen > 256 * 1024 * 1024 || uncompressedLen > 256 * 1024 * 1024) return 0;

                byte[] compressedBlock = new byte[compressedLen];
                int totalRead = 0;
                while (totalRead < compressedLen)
                {
                    int n = fs.Read(compressedBlock, totalRead, compressedLen - totalRead);
                    if (n <= 0) break;
                    totalRead += n;
                }

                using var blockStream = new MemoryStream(compressedBlock, 0, totalRead);
                using var lz4Stream = new Lz4DecompressorStream(blockStream, uncompressedLen);
                byte[] decompressed = new byte[uncompressedLen];
                int read = 0;
                while (read < uncompressedLen)
                {
                    int n = lz4Stream.Read(decompressed, read, uncompressedLen - read);
                    if (n <= 0) break;
                    read += n;
                }
                text = Latin1.GetString(decompressed, 0, read);
            }
            else
            {
                // Uncompressed - read a limited prefix
                int limit = (int)Math.Min(fs.Length, 64 * 1024);
                byte[] prefix = new byte[limit];
                fs.Position = 0;
                int read = 0;
                while (read < limit)
                {
                    int n = fs.Read(prefix, read, limit - read);
                    if (n <= 0) break;
                    read += n;
                }
                text = Latin1.GetString(prefix, 0, read);
            }

            // Try PresetGameMode first (human-readable or obfuscated key "pwt")
            int result = ScanKeyForGameMode(text, "\"PresetGameMode\"");
            if (result <= 0) result = ScanKeyForGameMode(text, "\"pwt\"");
            if (result > 0) return result;

            // PresetGameMode may be "Unspecified" - try DifficultyState.Preset.DifficultyPresetType
            // Obfuscated: "LyC" = DifficultyState, "7ND" = DifficultyPresetType
            int dsIdx = text.IndexOf("\"DifficultyState\"", StringComparison.Ordinal);
            if (dsIdx < 0) dsIdx = text.IndexOf("\"LyC\"", StringComparison.Ordinal);
            if (dsIdx >= 0)
            {
                int dpIdx = text.IndexOf("\"DifficultyPresetType\"", dsIdx, StringComparison.Ordinal);
                if (dpIdx < 0) dpIdx = text.IndexOf("\"7ND\"", dsIdx, StringComparison.Ordinal);
                if (dpIdx >= 0)
                {
                    // Skip past the key to find the colon and then the value
                    int colonIdx = text.IndexOf(':', dpIdx + 1);
                    if (colonIdx >= 0)
                    {
                        result = ScanValueForGameMode(text, colonIdx + 1);
                        if (result > 0) return result;
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Map a game mode string to its corresponding integer.
    /// </summary>
    private static int GameModeStringToInt(string mode) => mode switch
    {
        "Normal" => 1,
        "Survival" => 2,
        "Permadeath" => 3,
        "Creative" => 4,
        "Custom" => 5,
        "Seasonal" => 6,
        "Relaxed" => 7,
        "Hardcore" => 8,
        _ => 0
    };

    /// <summary>
    /// Scan for a JSON key in text and return its game mode integer value.
    /// Handles both numeric values (1-9) and string values ("Normal", etc).
    /// </summary>
    private static int ScanKeyForGameMode(string text, string key, int startFrom = 0)
    {
        int idx = startFrom > 0 ? startFrom : text.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return 0;
        return ScanValueForGameMode(text, idx + key.Length);
    }

    /// <summary>
    /// Scan from a position past a JSON key colon for a game mode value (numeric or string).
    /// </summary>
    private static int ScanValueForGameMode(string text, int searchStart)
    {
        int valStart = -1;
        for (int i = searchStart; i < text.Length && i < searchStart + 20; i++)
        {
            char c = text[i];
            if (c >= '1' && c <= '9')
                return c - '0';
            if (c == '"')
            {
                valStart = i + 1;
                break;
            }
        }
        if (valStart >= 0)
        {
            int valEnd = text.IndexOf('"', valStart);
            if (valEnd > valStart)
            {
                string mode = text.Substring(valStart, valEnd - valStart);
                return GameModeStringToInt(mode);
            }
        }
        return 0;
    }
}
