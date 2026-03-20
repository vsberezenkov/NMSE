using System;
using System.IO;
using System.Text;
using NMSE.IO;
using NMSE.Models;
using Xunit;
using Xunit.Abstractions;

namespace NMSE.Tests;

public class MetaDifficultyAnalysis
{
    private readonly ITestOutputHelper _output;
    public MetaDifficultyAnalysis(ITestOutputHelper output) { _output = output; }

    [Fact]
    public void WriteSteamMeta_WorldsPartII_WritesDifficultyTag()
    {
        // Arrange: create a meta with Custom difficulty for Worlds Part II format
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            string savePath = Path.Combine(dir, "save.hg");
            byte[] fakeCompressed = new byte[100];
            new Random(42).NextBytes(fakeCompressed);

            var info = new SaveMetaInfo
            {
                BaseVersion = 4720,   // Worlds Part II
                GameMode = 1,         // Normal
                Season = 0,
                TotalPlayTime = 12345,
                SaveName = "TestSave",
                SaveSummary = "In the test system",
                DifficultyPreset = 1, // Custom
                DifficultyPresetTag = "Custom"
            };

            // Act
            MetaFileWriter.WriteSteamMeta(savePath, fakeCompressed, 999, info, 2);

            // Assert: decrypt and verify
            uint[]? decrypted = MetaFileWriter.ReadSteamMeta(savePath, 2);
            Assert.NotNull(decrypted);

            byte[] bytes = MetaFileWriter.UIntsToBytes(decrypted);
            Assert.Equal(432, bytes.Length); // Worlds Part II size

            // Verify header + format
            Assert.Equal(MetaFileWriter.META_HEADER, BitConverter.ToUInt32(bytes, 0));
            Assert.Equal((uint)2004, BitConverter.ToUInt32(bytes, 4));

            // Verify save name at offset 88
            string saveName = Encoding.UTF8.GetString(bytes, 88, 128).TrimEnd('\0');
            Assert.Equal("TestSave", saveName);

            // Verify summary at offset 216
            string summary = Encoding.UTF8.GetString(bytes, 216, 128).TrimEnd('\0');
            Assert.Equal("In the test system", summary);

            // Verify difficulty integer at offset 344
            Assert.Equal((uint)1, BitConverter.ToUInt32(bytes, 344)); // Custom = 1

            // Verify meta format copy at offset 360
            Assert.Equal((uint)2004, BitConverter.ToUInt32(bytes, 360));

            // Verify difficulty tag string at offset 364
            string diffTag = Encoding.UTF8.GetString(bytes, 364, 64).TrimEnd('\0');
            Assert.Equal("Custom", diffTag);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Theory]
    [InlineData("Normal", 2)]
    [InlineData("Custom", 1)]
    [InlineData("Creative", 3)]
    [InlineData("Relaxed", 4)]
    [InlineData("Survival", 5)]
    [InlineData("Permadeath", 6)]
    public void WriteSteamMeta_WorldsPartII_AllDifficultyTags(string presetTag, int presetInt)
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            string savePath = Path.Combine(dir, "save.hg");
            byte[] fakeCompressed = new byte[100];

            var info = new SaveMetaInfo
            {
                BaseVersion = 4720,
                GameMode = 1,
                DifficultyPreset = presetInt,
                DifficultyPresetTag = presetTag
            };

            MetaFileWriter.WriteSteamMeta(savePath, fakeCompressed, 999, info, 2);

            uint[]? decrypted = MetaFileWriter.ReadSteamMeta(savePath, 2);
            Assert.NotNull(decrypted);
            byte[] bytes = MetaFileWriter.UIntsToBytes(decrypted);

            // Verify difficulty integer
            Assert.Equal((uint)presetInt, BitConverter.ToUInt32(bytes, 344));

            // Verify difficulty tag string
            string diffTag = Encoding.UTF8.GetString(bytes, 364, 64).TrimEnd('\0');
            Assert.Equal(presetTag, diffTag);

            // Verify meta format copy
            Assert.Equal((uint)2004, BitConverter.ToUInt32(bytes, 360));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteSteamMeta_WorldsPartI_WritesFormatCopyButNoDifficultyTag()
    {
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            string savePath = Path.Combine(dir, "save.hg");
            byte[] fakeCompressed = new byte[100];

            var info = new SaveMetaInfo
            {
                BaseVersion = 4135,   // Worlds Part I
                GameMode = 1,
                DifficultyPreset = 2, // Normal
                DifficultyPresetTag = "Normal"
            };

            MetaFileWriter.WriteSteamMeta(savePath, fakeCompressed, 999, info, 2);

            uint[]? decrypted = MetaFileWriter.ReadSteamMeta(savePath, 2);
            Assert.NotNull(decrypted);
            byte[] bytes = MetaFileWriter.UIntsToBytes(decrypted);

            Assert.Equal(384, bytes.Length); // Worlds Part I size

            // Format copy at 360
            Assert.Equal((uint)2003, BitConverter.ToUInt32(bytes, 360));

            // No difficulty tag at 364 (buffer only goes to 384)
            // Verify difficulty integer at 344
            Assert.Equal((uint)2, BitConverter.ToUInt32(bytes, 344));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ExtractMetaInfo_PopulatesDifficultyPresetTag()
    {
        var saveData = new JsonObject();
        var ps = new JsonObject();
        var diffState = new JsonObject();
        var preset = new JsonObject();
        preset.Add("DifficultyPresetType", "Custom");
        diffState.Add("Preset", preset);
        ps.Add("DifficultyState", diffState);
        saveData.Add("PlayerStateData", ps);

        var info = MetaFileWriter.ExtractMetaInfo(saveData);
        Assert.Equal(1, info.DifficultyPreset); // Custom = 1
        Assert.Equal("Custom", info.DifficultyPresetTag);
    }
}
