using NMSE.Models;

namespace NMSE.IO;

/// <summary>
/// File paths for a save slot.
/// </summary>
public class SlotFiles
{
    /// <summary>Gets or sets the path to the save data file.</summary>
    public string? DataFile { get; set; }
    /// <summary>Gets or sets the path to the companion metadata file.</summary>
    public string? MetaFile { get; set; }
}

/// <summary>
/// Options for cross-platform save transfer.
/// Controls which ownership references to rewrite and the destination user identity.
/// </summary>
public class TransferOptions
{
    /// <summary>Source user UID to match (leave empty to transfer all).</summary>
    public string? SourceUID { get; set; }
    /// <summary>Destination user UID.</summary>
    public string? DestUID { get; set; }
    /// <summary>Destination user LID (lobby ID).</summary>
    public string? DestLID { get; set; }
    /// <summary>Destination user USN (username).</summary>
    public string? DestUSN { get; set; }
    /// <summary>Destination platform token (PC, XBX, PS4, NX).</summary>
    public string? DestPTK { get; set; }
    /// <summary>Transfer base ownership references.</summary>
    public bool TransferBases { get; set; } = true;
    /// <summary>Transfer discovery ownership references.</summary>
    public bool TransferDiscoveries { get; set; } = true;
    /// <summary>Transfer settlement ownership references.</summary>
    public bool TransferSettlements { get; set; } = true;
    /// <summary>Transfer ByteBeat song authorship.</summary>
    public bool TransferByteBeat { get; set; } = true;
}

/// <summary>
/// Save slot operations: copy, move, swap within a platform, and cross-platform transfer.
///
/// Slot copy/move/swap operates within the same save directory.
/// Cross-platform transfer converts ownership UIDs, platform tokens, as well as base,
/// settlement, discovery and ByteBeat author references so saves work correctly on the
/// destination platform.
/// </summary>
public static class SaveSlotManager
{
    // Helpers

    /// <summary>
    /// Get the token for a given platform.
    /// </summary>
    private static string GetPlatformToken(SaveFileManager.Platform platform) => platform switch
    {
        SaveFileManager.Platform.Steam => "PC",
        SaveFileManager.Platform.GOG => "PC",
        SaveFileManager.Platform.XboxGamePass => "XBX",
        SaveFileManager.Platform.PS4 => "PS4",
        SaveFileManager.Platform.Switch => "NX",
        _ => "PC",
    };

    /// <summary>
    /// Get file paths for a save slot on a given platform.
    /// </summary>
    public static SlotFiles GetSlotFiles(string saveDirectory, int slotIndex,
        SaveFileManager.Platform platform)
    {
        return platform switch
        {
            SaveFileManager.Platform.Steam or SaveFileManager.Platform.GOG =>
                GetSteamSlotFiles(saveDirectory, slotIndex),
            SaveFileManager.Platform.Switch =>
                GetSwitchSlotFiles(saveDirectory, slotIndex),
            SaveFileManager.Platform.PS4 =>
                GetPlaystationSlotFiles(saveDirectory, slotIndex),
            _ => new SlotFiles()
        };
    }

    private static SlotFiles GetSteamSlotFiles(string dir, int slotIndex)
    {
        // Steam/GOG: save.hg, save2.hg, save3.hg... with mf_ prefix for meta
        string dataName = slotIndex == 0 ? "save.hg" : $"save{slotIndex + 1}.hg";
        string dataPath = Path.Combine(dir, dataName);
        string metaPath = MetaFileWriter.GetSteamMetaPath(dataPath);
        return new SlotFiles { DataFile = dataPath, MetaFile = metaPath };
    }

    private static SlotFiles GetSwitchSlotFiles(string dir, int slotIndex)
    {
        // Switch: savedata{NN}.hg with manifest{NN}.hg for meta
        string dataPath = Path.Combine(dir, $"savedata{slotIndex:D2}.hg");
        string metaPath = Path.Combine(dir, $"manifest{slotIndex:D2}.hg");
        return new SlotFiles { DataFile = dataPath, MetaFile = metaPath };
    }

    private static SlotFiles GetPlaystationSlotFiles(string dir, int slotIndex)
    {
        // PS4 streaming: savedata{NN}.hg with manifest{NN}.hg for meta
        string dataPath = Path.Combine(dir, $"savedata{slotIndex:D2}.hg");
        string metaPath = Path.Combine(dir, $"manifest{slotIndex:D2}.hg");
        return new SlotFiles { DataFile = dataPath, MetaFile = metaPath };
    }

    private static void WriteMetaForPlatform(SlotFiles files, JsonObject saveData,
        SaveFileManager.Platform platform, int slotIndex)
    {
        if (files.DataFile == null) return;

        var metaInfo = MetaFileWriter.ExtractMetaInfo(saveData);

        switch (platform)
        {
            case SaveFileManager.Platform.Steam:
            case SaveFileManager.Platform.GOG:
                if (File.Exists(files.DataFile))
                {
                    byte[] compressedData = File.ReadAllBytes(files.DataFile);
                    // Calculate decompressed size from the save data
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    int storageSlot = StorageSlotFromFileName(files.DataFile);
                    MetaFileWriter.WriteSteamMeta(files.DataFile, compressedData, decompressedSize, metaInfo, storageSlot);
                }
                break;

            case SaveFileManager.Platform.Switch:
                {
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    MetaFileWriter.WriteSwitchMeta(files.DataFile, decompressedSize, metaInfo, slotIndex);
                    break;
                }

            case SaveFileManager.Platform.PS4:
                {
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    MetaFileWriter.WritePlaystationStreamingMeta(files.DataFile, decompressedSize, metaInfo, slotIndex);
                    break;
                }
        }
    }

    private static JsonArray? GetJsonArray(JsonObject root, string path)
    {
        object? value = root.GetValue(path);
        return value as JsonArray;
    }

    private static void SetJsonValueByPath(JsonObject root, string key, object value)
    {
        root.Set(key, value);
    }

    /// <summary>
    /// Copy a save from one slot to another within the same save directory.
    /// Creates a backup of the destination if it exists.
    /// </summary>
    /// <param name="saveDirectory">Path to the save directory.</param>
    /// <param name="sourceSlotIndex">Source slot index (0-based, as stored on disk).</param>
    /// <param name="destSlotIndex">Destination slot index.</param>
    /// <param name="platform">Platform type for the saves.</param>
    public static void CopySlot(string saveDirectory, int sourceSlotIndex, int destSlotIndex,
        SaveFileManager.Platform platform)
    {
        if (sourceSlotIndex == destSlotIndex) return;

        var sourceFiles = GetSlotFiles(saveDirectory, sourceSlotIndex, platform);
        var destFiles = GetSlotFiles(saveDirectory, destSlotIndex, platform);

        if (sourceFiles.DataFile == null || !File.Exists(sourceFiles.DataFile))
            throw new FileNotFoundException($"Source save slot {sourceSlotIndex} not found.");

        // Backup destination if it exists
        if (destFiles.DataFile != null && File.Exists(destFiles.DataFile))
        {
            string backup = destFiles.DataFile + ".backup";
            File.Copy(destFiles.DataFile, backup, true);
        }

        // Copy data file
        File.Copy(sourceFiles.DataFile, destFiles.DataFile!, true);

        // Copy meta file if it exists
        if (sourceFiles.MetaFile != null && File.Exists(sourceFiles.MetaFile) && destFiles.MetaFile != null)
        {
            File.Copy(sourceFiles.MetaFile, destFiles.MetaFile, true);
        }
    }

    /// <summary>
    /// Move a save from one slot to another (copy then delete source).
    /// </summary>
    public static void MoveSlot(string saveDirectory, int sourceSlotIndex, int destSlotIndex,
        SaveFileManager.Platform platform)
    {
        CopySlot(saveDirectory, sourceSlotIndex, destSlotIndex, platform);
        DeleteSlot(saveDirectory, sourceSlotIndex, platform);
    }

    /// <summary>
    /// Swap two save slots.
    /// </summary>
    public static void SwapSlots(string saveDirectory, int slotA, int slotB,
        SaveFileManager.Platform platform)
    {
        if (slotA == slotB) return;

        string tempDir = Path.Combine(Path.GetTempPath(), $"nmse_swap_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filesA = GetSlotFiles(saveDirectory, slotA, platform);
            var filesB = GetSlotFiles(saveDirectory, slotB, platform);

            // Move A to temp
            if (filesA.DataFile != null && File.Exists(filesA.DataFile))
                File.Move(filesA.DataFile, Path.Combine(tempDir, "data_a"));
            if (filesA.MetaFile != null && File.Exists(filesA.MetaFile))
                File.Move(filesA.MetaFile, Path.Combine(tempDir, "meta_a"));

            // Move B to A
            if (filesB.DataFile != null && File.Exists(filesB.DataFile))
                File.Move(filesB.DataFile, filesA.DataFile!);
            if (filesB.MetaFile != null && File.Exists(filesB.MetaFile) && filesA.MetaFile != null)
                File.Move(filesB.MetaFile, filesA.MetaFile);

            // Move temp (A) to B
            string tempData = Path.Combine(tempDir, "data_a");
            string tempMeta = Path.Combine(tempDir, "meta_a");
            if (File.Exists(tempData) && filesB.DataFile != null)
                File.Move(tempData, filesB.DataFile);
            if (File.Exists(tempMeta) && filesB.MetaFile != null)
                File.Move(tempMeta, filesB.MetaFile);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Delete a save slot.
    /// </summary>
    public static void DeleteSlot(string saveDirectory, int slotIndex,
        SaveFileManager.Platform platform)
    {
        var files = GetSlotFiles(saveDirectory, slotIndex, platform);
        if (files.DataFile != null && File.Exists(files.DataFile))
            File.Delete(files.DataFile);
        if (files.MetaFile != null && File.Exists(files.MetaFile))
            File.Delete(files.MetaFile);
    }

    /// <summary>
    /// Transfer a save file from one platform to another.
    /// Loads the source JSON, rewrites ownership UIDs for the destination platform,
    /// and saves in the destination format.
    /// </summary>
    /// <param name="sourceFilePath">Source save file path.</param>
    /// <param name="destDirectory">Destination save directory.</param>
    /// <param name="destSlotIndex">Destination slot index.</param>
    /// <param name="destPlatform">Destination platform type.</param>
    /// <param name="transferOptions">Options controlling what data to transfer.</param>
    public static void TransferCrossPlatform(string sourceFilePath, string destDirectory,
        int destSlotIndex, SaveFileManager.Platform destPlatform, TransferOptions? transferOptions = null)
    {
        var options = transferOptions ?? new TransferOptions();

        // Load source save
        var saveData = SaveFileManager.LoadSaveFile(sourceFilePath);

        // Update Platform token in the save
        string platformToken = GetPlatformToken(destPlatform);
        SetJsonValueByPath(saveData, "Platform", platformToken);

        // Transfer ownership references
        if (options.TransferBases)
            TransferBaseOwnership(saveData, options);

        if (options.TransferDiscoveries)
            TransferDiscoveryOwnership(saveData, options);

        if (options.TransferSettlements)
            TransferSettlementOwnership(saveData, options);

        if (options.TransferByteBeat)
            TransferByteBeatOwnership(saveData, options);

        // Save to destination
        var destFiles = GetSlotFiles(destDirectory, destSlotIndex, destPlatform);
        if (destFiles.DataFile == null)
            throw new InvalidOperationException("Cannot determine destination file path.");

        SaveFileManager.SaveToFile(destFiles.DataFile, saveData, compress: true);

        // Write platform-appropriate meta file
        WriteMetaForPlatform(destFiles, saveData, destPlatform, destSlotIndex);
    }

    /// <summary>
    /// Rewrite ownership UIDs in base objects.
    /// Bases have Owner.UID, Owner.LID, Owner.USN, Owner.PTK fields that need
    /// to match the destination platform user.
    /// </summary>
    private static void TransferBaseOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PersistentPlayerBases array
        var bases = GetJsonArray(saveData, "PlayerStateData.PersistentPlayerBases");
        if (bases == null) return;

        for (int i = 0; i < bases.Length; i++)
        {
            if (bases.Get(i) is not JsonObject baseObj) continue;

            var owner = baseObj.Get("Owner") as JsonObject;
            if (owner == null) continue;

            // Only transfer bases owned by the source user (match UID)
            string? ownerUid = owner.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            // Rewrite ownership
            RewriteOwnership(owner, options);
        }
    }

    private static void TransferDiscoveryOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk DiscoveryManagerData.DiscoveryData-v1.Store.Record array
        var record = GetJsonArray(saveData, "DiscoveryManagerData.DiscoveryData-v1.Store.Record");
        if (record == null) return;

        for (int i = 0; i < record.Length; i++)
        {
            if (record.Get(i) is not JsonObject discoveryObj) continue;

            var ows = discoveryObj.Get("OWS") as JsonObject;
            if (ows == null) continue;

            string? ownerUid = ows.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            RewriteOwnership(ows, options);
        }
    }

    private static void TransferSettlementOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PlayerStateData.SettlementStatesV2 array
        var settlements = GetJsonArray(saveData, "PlayerStateData.SettlementStatesV2");
        if (settlements == null) return;

        for (int i = 0; i < settlements.Length; i++)
        {
            if (settlements.Get(i) is not JsonObject settlementObj) continue;

            var owner = settlementObj.Get("Owner") as JsonObject;
            if (owner == null) continue;

            string? ownerUid = owner.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            RewriteOwnership(owner, options);
        }
    }

    private static void TransferByteBeatOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PlayerStateData.ByteBeatLibrary.MySongs array
        var songs = GetJsonArray(saveData, "PlayerStateData.ByteBeatLibrary.MySongs");
        if (songs == null) return;

        for (int i = 0; i < songs.Length; i++)
        {
            if (songs.Get(i) is not JsonObject songObj) continue;

            string? authorId = songObj.Get("AuthorOnlineID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && authorId != options.SourceUID)
                continue;

            if (!string.IsNullOrEmpty(options.DestUID))
                songObj.Set("AuthorOnlineID", options.DestUID);
            if (!string.IsNullOrEmpty(options.DestUSN))
                songObj.Set("AuthorUsername", options.DestUSN);
            if (!string.IsNullOrEmpty(options.DestPTK))
                songObj.Set("AuthorPlatform", options.DestPTK);
        }
    }

    private static void RewriteOwnership(JsonObject ownerObj, TransferOptions options)
    {
        if (!string.IsNullOrEmpty(options.DestUID))
            ownerObj.Set("UID", options.DestUID);

        string? lid = ownerObj.Get("LID") as string;
        if (!string.IsNullOrEmpty(lid) && !string.IsNullOrEmpty(options.DestLID))
            ownerObj.Set("LID", options.DestLID);

        string? usn = ownerObj.Get("USN") as string;
        if (!string.IsNullOrEmpty(usn) && !string.IsNullOrEmpty(options.DestUSN))
            ownerObj.Set("USN", options.DestUSN);

        if (!string.IsNullOrEmpty(options.DestPTK))
            ownerObj.Set("PTK", options.DestPTK);
    }

    /// <summary>
    /// Convert a save slot index to the persistent storage slot used in meta encryption.
    /// Slot 0 = AccountData (storage slot 0), slots 1+ = save data (storage slot 2+slotIndex).
    /// The gap at slot 1 is reserved for Settings data.
    /// </summary>
    internal static int SlotIndexToStorageSlot(int slotIndex)
    {
        return slotIndex == 0 ? 0 : 2 + slotIndex;
    }

    /// <summary>
    /// Derive the persistent storage slot from a save file name.
    /// NMS uses the following convention:
    /// <list type="bullet">
    ///   <item><c>accountdata.hg</c> -> storage slot 0</item>
    ///   <item><c>save.hg</c> -> storage slot 2 (first manual save)</item>
    ///   <item><c>saveN.hg</c> (N ≥ 2) -> storage slot N + 1</item>
    /// </list>
    /// The meta encryption key depends on the storage slot, so using the
    /// wrong slot produces a garbled meta file that the game cannot read.
    /// </summary>
    internal static int StorageSlotFromFileName(string filePath)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);

        if (name.Equals("accountdata", StringComparison.OrdinalIgnoreCase))
            return 0;

        // save.hg -> storage slot 2
        if (name.Equals("save", StringComparison.OrdinalIgnoreCase))
            return 2;

        // saveN.hg -> storage slot N + 1  (save2.hg -> 3, save3.hg -> 4, etc.)
        if (name.StartsWith("save", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(name.AsSpan(4), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int n) && n >= 2)
            return n + 1;

        // Unknown file name - fall back to slot 2 as a safe default
        return 2;
    }
}