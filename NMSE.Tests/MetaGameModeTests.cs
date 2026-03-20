using NMSE.IO;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

public class MetaGameModeTests
{
    private static string? GetResourcePath(params string[] parts)
    {
        var basePath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(new[] { basePath }.Concat(parts).ToArray());
        return File.Exists(path) || Directory.Exists(path) ? path : null;
    }

    [Fact]
    public void ExtractMetaInfo_FallsBackToDifficultyState()
    {
        var savePath = GetResourcePath("_ref", "save.hg");
        if (savePath == null) return; // Skip if reference save not available

        var mapperPath = GetResourcePath("Resources", "map", "mapping.json");
        if (mapperPath == null) return;

        var mapper = new JsonNameMapper();
        mapper.Load(mapperPath);
        JsonParser.SetDefaultMapper(mapper);

        var save = SaveFileManager.LoadSaveFile(savePath);
        var metaInfo = MetaFileWriter.ExtractMetaInfo(save);

        // Should detect game mode from DifficultyState when PresetGameMode is absent
        Assert.True(metaInfo.GameMode > 0,
            $"GameMode should be detected from DifficultyState, got {metaInfo.GameMode}");
    }
}
