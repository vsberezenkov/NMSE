namespace NMSE.IO;

/// <summary>
/// Metadata for a single slot within a PS4 memory.dat file.
/// </summary>
public class MemoryDatSlot
{
    /// <summary>Gets or sets the slot index within the memory.dat file.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets whether this slot contains valid save data.</summary>
    public bool Exists { get; set; }
    /// <summary>Gets or sets the metadata format version identifier.</summary>
    public uint MetaFormat { get; set; }
    /// <summary>Gets or sets the compressed data size in bytes.</summary>
    public uint CompressedSize { get; set; }
    /// <summary>Gets or sets the byte offset of the data chunk within memory.dat.</summary>
    public uint ChunkOffset { get; set; }
    /// <summary>Gets or sets the size of the data chunk region in bytes.</summary>
    public uint ChunkSize { get; set; }
    /// <summary>Gets or sets the metadata index for this slot.</summary>
    public uint MetaIndex { get; set; }
    /// <summary>Gets or sets the save timestamp, or null if not set.</summary>
    public DateTimeOffset? Timestamp { get; set; }
    /// <summary>Gets or sets the decompressed data size in bytes.</summary>
    public uint DecompressedSize { get; set; }
    /// <summary>Gets or sets whether this slot uses the SaveWizard export format.</summary>
    public bool IsSaveWizard { get; set; }
}

/// <summary>
/// Reads and writes PlayStation memory.dat monolithic save files.
///
/// The memory.dat format packs all save slots (account + up to 15 saves x 2 auto/manual)
/// into a single file. Each slot has a 32-byte metadata header at a fixed offset, followed
/// by a data region where the actual (LZ4-compressed) JSON is stored.
///
/// SaveWizard-exported files have an additional 20-byte preamble and per-slot SaveWizard
/// headers. Homebrew dumps (e.g., from a modded PS4) omit these.
/// </summary>
public static class MemoryDatManager
{
    // memory.dat layout constants
    private const int META_HEADER = 0x000007D0;
    private const int META_LENGTH_PER_SLOT = 32;  // 8 uint fields per slot

    // SaveWizard magic: "YOURTHESAVEWIZAR" (partial) followed by version bytes
    private static readonly byte[] SAVEWIZARD_HEADER = {
        0x59, 0x4F, 0x55, 0x52, 0x54, 0x48, 0x45, 0x53
    };

    // Fixed offsets within memory.dat
    private const int MEMORYDAT_OFFSET_META = 0x20;  // 32 - start of metadata region
    private const int MEMORYDAT_OFFSET_DATA = 0x4020; // metadata region is 0x4000 bytes

    // Per-slot data allocation sizes in non-streaming format
    private const int MEMORYDAT_LENGTH_ACCOUNTDATA = 0x40000;    // 256KB for account
    private const int MEMORYDAT_LENGTH_CONTAINER = 0x300000;     // 3MB per save slot

    // Total file sizes
    private const int MEMORYDAT_LENGTH_TOTAL = 0x2000020; // ~32MB total

    /// <summary>
    /// Check if a file is in memory.dat format (PS4 monolithic save).
    /// </summary>
    public static bool IsMemoryDat(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        var fi = new FileInfo(filePath);
        // memory.dat files are typically exactly 32MB (or close to it for SaveWizard)
        return fi.Name.Equals("memory.dat", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detect whether a memory.dat file was created by SaveWizard.
    /// </summary>
    public static bool IsSaveWizardFormat(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            using var fs = File.OpenRead(filePath);
            byte[] header = new byte[SAVEWIZARD_HEADER.Length];
            if (fs.Read(header, 0, header.Length) < header.Length) return false;
            return header.AsSpan().SequenceEqual(SAVEWIZARD_HEADER);
        }
        catch { return false; }
    }

    /// <summary>
    /// Parse all save slot metadata from a memory.dat file.
    /// </summary>
    /// <param name="filePath">Path to memory.dat</param>
    /// <returns>Array of slot metadata (index 0 = account, 1-30 = save slots).</returns>
    public static MemoryDatSlot[] ReadSlots(string filePath)
    {
        byte[] data = File.ReadAllBytes(filePath);
        bool isSaveWizard = data.Length >= SAVEWIZARD_HEADER.Length &&
            data.AsSpan(0, SAVEWIZARD_HEADER.Length).SequenceEqual(SAVEWIZARD_HEADER);

        int metaOffset = isSaveWizard ? MEMORYDAT_OFFSET_META : 0;
        int totalSlots = 1 + 30; // account + 15 slots x 2 (auto + manual)
        var slots = new MemoryDatSlot[totalSlots];

        for (int i = 0; i < totalSlots; i++)
        {
            int slotMetaOffset = metaOffset + (i * META_LENGTH_PER_SLOT);
            if (slotMetaOffset + META_LENGTH_PER_SLOT > data.Length)
            {
                slots[i] = new MemoryDatSlot { Index = i, Exists = false };
                continue;
            }

            uint header = ReadUInt32LE(data, slotMetaOffset);
            uint format = ReadUInt32LE(data, slotMetaOffset + 4);
            uint compressedSize = ReadUInt32LE(data, slotMetaOffset + 8);
            uint chunkOffset = ReadUInt32LE(data, slotMetaOffset + 12);
            uint chunkSize = ReadUInt32LE(data, slotMetaOffset + 16);
            uint metaIndex = ReadUInt32LE(data, slotMetaOffset + 20);
            uint timestamp = ReadUInt32LE(data, slotMetaOffset + 24);
            uint decompressedSize = ReadUInt32LE(data, slotMetaOffset + 28);

            bool exists = header == META_HEADER && chunkOffset != 0;

            slots[i] = new MemoryDatSlot
            {
                Index = i,
                Exists = exists,
                MetaFormat = format,
                CompressedSize = compressedSize,
                ChunkOffset = chunkOffset,
                ChunkSize = chunkSize,
                MetaIndex = metaIndex,
                Timestamp = timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds(timestamp) : null,
                DecompressedSize = decompressedSize,
                IsSaveWizard = isSaveWizard,
            };
        }

        return slots;
    }

    /// <summary>
    /// Extract the JSON data for a specific slot from a memory.dat file.
    /// Returns the decompressed JSON string.
    /// </summary>
    public static string? ExtractSlotData(string filePath, int slotIndex)
    {
        var slots = ReadSlots(filePath);
        if (slotIndex < 0 || slotIndex >= slots.Length) return null;
        var slot = slots[slotIndex];
        if (!slot.Exists) return null;

        byte[] data = File.ReadAllBytes(filePath);

        int dataOffset;
        int dataLength;

        if (slot.IsSaveWizard)
        {
            // SaveWizard format: offset is stored in the extended meta
            int extendedMetaOffset = MEMORYDAT_OFFSET_META + (slotIndex * META_LENGTH_PER_SLOT) + 32;
            if (extendedMetaOffset + 4 <= data.Length)
            {
                dataOffset = (int)ReadUInt32LE(data, extendedMetaOffset);
                dataLength = (int)slot.DecompressedSize;
            }
            else
            {
                return null;
            }
        }
        else
        {
            // Homebrew: data at fixed offset within the memory.dat data region
            dataOffset = (int)slot.ChunkOffset;
            dataLength = (int)slot.CompressedSize;
        }

        if (dataOffset <= 0 || dataOffset + dataLength > data.Length)
            return null;

        byte[] compressedData = new byte[dataLength];
        Buffer.BlockCopy(data, dataOffset, compressedData, 0, dataLength);

        // Decompress (single-block LZ4 for memory.dat format)
        byte[] decompressed = new byte[slot.DecompressedSize];
        int written = Lz4Compressor.Decompress(compressedData, 0, dataLength, decompressed, 0, (int)slot.DecompressedSize);

        // Convert to string using Latin1 encoding
        return System.Text.Encoding.GetEncoding(28591).GetString(decompressed, 0, written);
    }

    /// <summary>
    /// Write a complete memory.dat file from slot data.
    /// </summary>
    /// <param name="outputPath">Output file path.</param>
    /// <param name="slotData">Dictionary of slotIndex -> compressed data bytes.</param>
    /// <param name="slotMeta">Dictionary of slotIndex -> slot metadata.</param>
    public static void WriteMemoryDat(string outputPath, Dictionary<int, byte[]> slotData, Dictionary<int, MemoryDatSlot> slotMeta)
    {
        byte[] buffer = new byte[MEMORYDAT_LENGTH_TOTAL];

        using var ms = new MemoryStream(buffer);
        using var writer = new BinaryWriter(ms);

        int totalSlots = Math.Max(31, slotMeta.Count);

        // Write metadata for each slot
        for (int i = 0; i < 31; i++)
        {
            if (slotMeta.TryGetValue(i, out var meta) && meta.Exists)
            {
                writer.Write(META_HEADER);                                      // 4
                writer.Write((uint)1);                                          // 4 - format
                writer.Write(meta.CompressedSize);                              // 4
                writer.Write(meta.ChunkOffset);                                 // 4
                writer.Write(meta.ChunkSize);                                   // 4
                writer.Write((uint)i);                                          // 4 - meta index
                writer.Write((uint)(meta.Timestamp?.ToUnixTimeSeconds() ?? 0)); // 4
                writer.Write(meta.DecompressedSize);                            // 4
            }
            else
            {
                // Empty slot
                writer.Write(META_HEADER);
                writer.Write((uint)1);
                writer.Seek(12, SeekOrigin.Current);
                writer.Write(uint.MaxValue);
                writer.Seek(8, SeekOrigin.Current);
            }
        }

        // Write data regions
        ms.Position = MEMORYDAT_OFFSET_DATA;

        foreach (var kvp in slotData.OrderBy(k => k.Key))
        {
            if (slotMeta.TryGetValue(kvp.Key, out var meta) && meta.Exists)
            {
                ms.Position = meta.ChunkOffset;
                writer.Write(kvp.Value);
            }
        }

        File.WriteAllBytes(outputPath, buffer);
    }

    private static uint ReadUInt32LE(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}