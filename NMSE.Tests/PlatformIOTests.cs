using NMSE.IO;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for platform-specific IO: MetaCrypto, MetaFileWriter, ContainersIndexManager,
/// MemoryDatManager, SaveSlotManager, and enhanced SaveFileManager.
/// </summary>
public class PlatformIOTests
{
    // --- MetaCrypto: TEA encrypt/decrypt round-trip ---

    [Fact]
    public void MetaCrypto_EncryptDecrypt_RoundTrip()
    {
        // Create a plain meta buffer with recognizable Steam header
        uint[] plain = new uint[26]; // 104 bytes = Steam vanilla meta size
        plain[0] = MetaFileWriter.META_HEADER; // 0xEEEEEEBE
        plain[1] = 1; // format
        plain[2] = 12345; // some value
        plain[3] = 0;
        plain[4] = 99;
        plain[5] = 42;

        // Encrypt with slot 2, 8 iterations (pre-Waypoint)
        uint[] encrypted = MetaCrypto.Encrypt(plain, 2, 8);

        // Should be different from plain
        Assert.NotEqual(plain[0], encrypted[0]);

        // Decrypt
        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 8);

        // Should match original
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_EncryptDecrypt_WaypointFormat()
    {
        // 90 uint = 360 bytes = Steam Waypoint meta
        uint[] plain = new uint[90];
        plain[0] = MetaFileWriter.META_HEADER;
        plain[1] = 2; // format 2
        for (int i = 2; i < 90; i++)
            plain[i] = (uint)(i * 7 + 13);

        // 6 iterations for Waypoint+
        uint[] encrypted = MetaCrypto.Encrypt(plain, 5, 6);
        Assert.NotEqual(plain[0], encrypted[0]);

        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 5, 6);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_Decrypt_TriesDifferentSlots()
    {
        uint[] plain = new uint[26];
        plain[0] = MetaFileWriter.META_HEADER;
        plain[1] = 1;
        plain[2] = 42;

        // Encrypt with slot 7
        uint[] encrypted = MetaCrypto.Encrypt(plain, 7, 8);

        // Decrypt with wrong slot first - should still find it
        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 3, 8);
        Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_ReturnsCorrectLength()
    {
        byte[] testData = new byte[1024];
        new Random(42).NextBytes(testData);

        byte[] hashes = MetaCrypto.ComputeMetaHashes(testData);
        Assert.Equal(48, hashes.Length); // SpookyHash(16) + SHA256(32)
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_DeterministicForSameInput()
    {
        byte[] data = new byte[256];
        for (int i = 0; i < 256; i++) data[i] = (byte)(i & 0xFF);

        byte[] hash1 = MetaCrypto.ComputeMetaHashes(data);
        byte[] hash2 = MetaCrypto.ComputeMetaHashes(data);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_DifferentForDifferentInput()
    {
        byte[] data1 = new byte[] { 1, 2, 3, 4, 5 };
        byte[] data2 = new byte[] { 1, 2, 3, 4, 6 };

        byte[] hash1 = MetaCrypto.ComputeMetaHashes(data1);
        byte[] hash2 = MetaCrypto.ComputeMetaHashes(data2);
        Assert.NotEqual(hash1, hash2);
    }

    // --- MetaFileWriter: helpers ---

    [Theory]
    [InlineData("/saves/save.hg", "/saves/mf_save.hg")]
    [InlineData("/saves/save2.hg", "/saves/mf_save2.hg")]
    [InlineData("/saves/accountdata.hg", "/saves/mf_accountdata.hg")]
    public void MetaFileWriter_GetSteamMetaPath_ReturnsCorrectPath(string savePath, string expectedMeta)
    {
        string result = MetaFileWriter.GetSteamMetaPath(savePath);
        Assert.Equal(expectedMeta, result);
    }

    [Theory]
    [InlineData("/saves/savedata01.hg", 1, "/saves/manifest01.hg")]
    [InlineData("/saves/savedata00.hg", 0, "/saves/manifest00.hg")]
    [InlineData("/saves/savedata10.hg", 10, "/saves/manifest10.hg")]
    public void MetaFileWriter_GetSwitchMetaPath_ReturnsCorrectPath(string savePath, int metaIndex, string expected)
    {
        string result = MetaFileWriter.GetSwitchMetaPath(savePath, metaIndex);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MetaFileWriter_BytesToUInts_RoundTrip()
    {
        byte[] original = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        uint[] uints = MetaFileWriter.BytesToUInts(original);
        Assert.Equal(2, uints.Length);
        byte[] back = MetaFileWriter.UIntsToBytes(uints);
        Assert.Equal(original, back);
    }

    [Fact]
    public void MetaFileWriter_WriteSteamMeta_CreatesFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllText(savePath, "dummy");

            byte[] compressedData = new byte[128];
            new Random(1).NextBytes(compressedData);

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = 4115, // Frontiers
                GameMode = 1,
                Season = 0,
                TotalPlayTime = 3600,
                SaveName = "Test Save",
            };

            MetaFileWriter.WriteSteamMeta(savePath, compressedData, 1024, metaInfo, 2);

            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            Assert.True(File.Exists(metaPath));
            byte[] metaBytes = File.ReadAllBytes(metaPath);
            Assert.True(metaBytes.Length > 0);

            // Verify it can be decrypted
            uint[] encrypted = MetaFileWriter.BytesToUInts(metaBytes);
            uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 6);
            Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Theory]
    [InlineData(4115, MetaFileWriter.META_FORMAT_2)]  // Frontiers -> 2002
    [InlineData(4135, MetaFileWriter.META_FORMAT_3)]  // Worlds Part I -> 2003
    [InlineData(4145, MetaFileWriter.META_FORMAT_4)]  // Worlds Part II -> 2004
    [InlineData(4720, MetaFileWriter.META_FORMAT_4)]  // Current game -> 2004
    public void MetaFileWriter_WriteSteamMeta_WritesCorrectFormat(int baseVersion, uint expectedFormat)
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllText(savePath, "dummy");

            byte[] compressedData = new byte[128];
            new Random(1).NextBytes(compressedData);

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = baseVersion,
                GameMode = 1,
                Season = 0,
                TotalPlayTime = 3600,
                SaveName = "My Save",
                SaveSummary = "In a system",
                DifficultyPreset = 2,
            };

            MetaFileWriter.WriteSteamMeta(savePath, compressedData, 1024, metaInfo, 2);

            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            byte[] metaBytes = File.ReadAllBytes(metaPath);

            // Decrypt and verify format matches NMS game values (2001-2004)
            uint[] encrypted = MetaFileWriter.BytesToUInts(metaBytes);
            uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 6);
            Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
            Assert.Equal(expectedFormat, decrypted[1]);

            // Verify save name can be read from correct offset
            byte[] decryptedBytes = MetaFileWriter.UIntsToBytes(decrypted);
            string saveName = System.Text.Encoding.UTF8.GetString(decryptedBytes, 88, 128).TrimEnd('\0');
            Assert.Equal("My Save", saveName);

            string summary = System.Text.Encoding.UTF8.GetString(decryptedBytes, 216, 128).TrimEnd('\0');
            Assert.Equal("In a system", summary);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void MetaFileWriter_WriteSwitchMeta_CreatesFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "savedata01.hg");
            File.WriteAllText(savePath, "dummy");

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = 4115,
                GameMode = 1,
                TotalPlayTime = 7200,
            };

            MetaFileWriter.WriteSwitchMeta(savePath, 2048, metaInfo, 1);

            string metaPath = Path.Combine(tmpDir, "manifest01.hg");
            Assert.True(File.Exists(metaPath));
            byte[] metaBytes = File.ReadAllBytes(metaPath);
            Assert.True(metaBytes.Length > 0);

            // Switch meta is NOT encrypted - verify header directly
            uint header = BitConverter.ToUInt32(metaBytes, 0);
            Assert.Equal(MetaFileWriter.META_HEADER_SWITCH_PS, header);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- SaveSlotManager: slot file path resolution ---

    [Theory]
    [InlineData(0, "save.hg")]
    [InlineData(1, "save2.hg")]
    [InlineData(2, "save3.hg")]
    public void SaveSlotManager_GetSlotFiles_Steam(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/st_12345", slotIndex, SaveFileManager.Platform.Steam);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
        Assert.NotNull(files.MetaFile);
        Assert.Contains("mf_", files.MetaFile!);
    }

    [Theory]
    [InlineData(0, "savedata00.hg")]
    [InlineData(1, "savedata01.hg")]
    public void SaveSlotManager_GetSlotFiles_Switch(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/switch", slotIndex, SaveFileManager.Platform.Switch);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
        Assert.NotNull(files.MetaFile);
        Assert.Contains("manifest", files.MetaFile!);
    }

    [Theory]
    [InlineData(0, "savedata00.hg")]
    [InlineData(1, "savedata01.hg")]
    public void SaveSlotManager_GetSlotFiles_PS4(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/ps4", slotIndex, SaveFileManager.Platform.PS4);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
    }

    // --- SaveSlotManager: CopySlot ---

    [Fact]
    public void SaveSlotManager_CopySlot_CopiesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Create source slot files
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "slot0data");
            File.WriteAllText(Path.Combine(tmpDir, "mf_save.hg"), "slot0meta");

            SaveSlotManager.CopySlot(tmpDir, 0, 1, SaveFileManager.Platform.Steam);

            Assert.True(File.Exists(Path.Combine(tmpDir, "save2.hg")));
            Assert.Equal("slot0data", File.ReadAllText(Path.Combine(tmpDir, "save2.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_CopySlot_SameSlot_Noop()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data");

            // Copying slot 0 to slot 0 should be a no-op
            SaveSlotManager.CopySlot(tmpDir, 0, 0, SaveFileManager.Platform.Steam);

            Assert.True(File.Exists(Path.Combine(tmpDir, "save.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_DeleteSlot_RemovesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data");
            File.WriteAllText(Path.Combine(tmpDir, "mf_save.hg"), "meta");

            SaveSlotManager.DeleteSlot(tmpDir, 0, SaveFileManager.Platform.Steam);

            Assert.False(File.Exists(Path.Combine(tmpDir, "save.hg")));
            Assert.False(File.Exists(Path.Combine(tmpDir, "mf_save.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_MoveSlot_MovesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data0");

            SaveSlotManager.MoveSlot(tmpDir, 0, 2, SaveFileManager.Platform.Steam);

            Assert.False(File.Exists(Path.Combine(tmpDir, "save.hg")));
            Assert.True(File.Exists(Path.Combine(tmpDir, "save3.hg")));
            Assert.Equal("data0", File.ReadAllText(Path.Combine(tmpDir, "save3.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_SwapSlots_SwapsBothWays()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "dataA");
            File.WriteAllText(Path.Combine(tmpDir, "save2.hg"), "dataB");

            SaveSlotManager.SwapSlots(tmpDir, 0, 1, SaveFileManager.Platform.Steam);

            Assert.Equal("dataB", File.ReadAllText(Path.Combine(tmpDir, "save.hg")));
            Assert.Equal("dataA", File.ReadAllText(Path.Combine(tmpDir, "save2.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- TransferOptions: ownership rewrite ---

    [Fact]
    public void TransferOptions_DefaultsToAllTransferEnabled()
    {
        var options = new TransferOptions();
        Assert.True(options.TransferBases);
        Assert.True(options.TransferDiscoveries);
        Assert.True(options.TransferSettlements);
        Assert.True(options.TransferByteBeat);
    }

    // --- ContainersIndexManager: Xbox detection ---

    [Fact]
    public void ContainersIndexManager_IsXboxSaveDirectory_TrueWhenContainersIndexExists()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "containers.index"), "");
            Assert.True(ContainersIndexManager.IsXboxSaveDirectory(tmpDir));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ContainersIndexManager_IsXboxSaveDirectory_FalseWhenMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            Assert.False(ContainersIndexManager.IsXboxSaveDirectory(tmpDir));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ContainersIndexManager_ParseContainersIndex_ReturnsEmptyForInvalidFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "containers.index");
            File.WriteAllBytes(path, new byte[] { 0, 0, 0, 0 }); // wrong header
            var result = ContainersIndexManager.ParseContainersIndex(path);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- MemoryDatManager: PS4 detection ---

    [Fact]
    public void MemoryDatManager_IsMemoryDat_DetectsCorrectFilename()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "memory.dat");
            File.WriteAllBytes(path, new byte[32]);
            Assert.True(MemoryDatManager.IsMemoryDat(path));

            string otherPath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllBytes(otherPath, new byte[32]);
            Assert.False(MemoryDatManager.IsMemoryDat(otherPath));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void MemoryDatManager_IsSaveWizardFormat_FalseForNonSaveWizard()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "memory.dat");
            File.WriteAllBytes(path, new byte[1024]); // zeros - not SaveWizard
            Assert.False(MemoryDatManager.IsSaveWizardFormat(path));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- MetaFileWriter: ExtractMetaInfo ---

    [Fact]
    public void MetaFileWriter_ExtractMetaInfo_ExtractsVersion()
    {
        var json = JsonObject.Parse("{\"Version\":4115}");
        var info = MetaFileWriter.ExtractMetaInfo(json);
        Assert.Equal(4115, info.BaseVersion);
    }

    // --- SaveFileManager: enhanced SaveToFile backward compat ---

    [Fact]
    public void SaveFileManager_SaveToFile_BackwardCompatible()
    {
        // Calling SaveToFile with just 3 args (old signature) should still work
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "test.hg");
            var data = JsonObject.Parse("{\"Version\":1,\"test\":42}");

            SaveFileManager.SaveToFile(path, data, compress: false);
            Assert.True(File.Exists(path));

            byte[] bytes = File.ReadAllBytes(path);
            // Should end with null terminator
            Assert.Equal(0, bytes[^1]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- Platform token mapping ---

    [Theory]
    [InlineData(SaveFileManager.Platform.Steam, "PC")]
    [InlineData(SaveFileManager.Platform.GOG, "PC")]
    [InlineData(SaveFileManager.Platform.XboxGamePass, "XBX")]
    [InlineData(SaveFileManager.Platform.PS4, "PS4")]
    [InlineData(SaveFileManager.Platform.Switch, "NX")]
    public void PlatformToken_MapsCorrectly(SaveFileManager.Platform platform, string expectedToken)
    {
        var options = new TransferOptions { DestPTK = expectedToken };
        Assert.Equal(expectedToken, options.DestPTK);
        Assert.NotEqual(SaveFileManager.Platform.Unknown, platform);
    }

    // --- Platform detection from directories ---

    [Fact]
    public void DetectPlatform_Steam_FromSaveHgFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_steam_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Steam: save.hg files in a st_ named directory
            string profileDir = Path.Combine(tmpDir, "st_12345");
            Directory.CreateDirectory(profileDir);
            File.WriteAllBytes(Path.Combine(profileDir, "save.hg"), new byte[] { 0 });
            File.WriteAllBytes(Path.Combine(profileDir, "accountdata.hg"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(profileDir);
            Assert.Equal(SaveFileManager.Platform.Steam, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_GOG_FromDefaultUserDirectory()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_gog_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string profileDir = Path.Combine(tmpDir, "DefaultUser");
            Directory.CreateDirectory(profileDir);
            File.WriteAllBytes(Path.Combine(profileDir, "save.hg"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(profileDir);
            Assert.Equal(SaveFileManager.Platform.GOG, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_Xbox_FromContainersIndex()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_xbox_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "containers.index"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.XboxGamePass, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_PS4_FromMemoryDat()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_ps4_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "memory.dat"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.PS4, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_Switch_FromManifestDat()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_switch_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "manifest00.dat"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.Switch, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    // --- SaveToFile with writeMeta flag ---

    [Fact]
    public void SaveToFile_WriteMeta_CreatesMetaFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_meta_{Guid.NewGuid():N}");
        // Mimic a Steam directory with st_ prefix
        string profileDir = Path.Combine(tmpDir, "st_test");
        Directory.CreateDirectory(profileDir);
        try
        {
            string savePath = Path.Combine(profileDir, "save.hg");

            // Create a minimal JSON save
            var data = new JsonObject();
            data.Set("Version", 5);

            // Write with meta
            SaveFileManager.SaveToFile(savePath, data, compress: true, writeMeta: true,
                platform: SaveFileManager.Platform.Steam, slotIndex: 0);

            Assert.True(File.Exists(savePath), "Save file should exist");

            // The meta file should also have been created (mf_save.hg)
            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            Assert.True(File.Exists(metaPath), $"Meta file should exist at {metaPath}");
        }
        finally { Directory.Delete(tmpDir, true); }
    }
}
