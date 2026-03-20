using NMSE.Extractor.Config;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NMSE.Extractor.Data;

/// <summary>
/// Extracts item icons from game files (extracted directory) and converts DDS to PNG.
/// Uses the portable ImageMagick magick.exe for DDS to PNG conversion.
/// </summary>
public static class ImageExtractor
{
    private static readonly string[] IconJsonFiles =
    {
        "Buildings.json", "Constructed Technology.json", "Food.json", "Corvette.json",
        "Curiosities.json", "Exocraft.json", "Fish.json",
        "Others.json", "Products.json", "Raw Materials.json", "Starships.json",
        "Technology.json", "Technology Module.json", "Trade.json", "Upgrades.json",
        "none.json"
    };

    public static string SanitizeFilename(string idStr)
    {
        if (string.IsNullOrEmpty(idStr)) return "unknown";
        return Regex.Replace(idStr, @"[\\/:*?""<>|]", "_").Trim();
    }

    public static List<(string Id, string IconPath)> CollectIdIconPairs(string jsonDir)
    {
        var seenIds = new HashSet<string>();
        var pairs = new List<(string, string)>();

        foreach (string filename in IconJsonFiles)
        {
            string path = Path.Combine(jsonDir, filename);
            if (!File.Exists(path)) continue;

            try
            {
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;

                    string idVal = item.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? "" :
                                   item.TryGetProperty("id", out idProp) ? idProp.GetString() ?? "" : "";

                    string iconVal = item.TryGetProperty("IconPath", out var iconProp) ? iconProp.GetString() ?? "" :
                                     item.TryGetProperty("iconPath", out iconProp) ? iconProp.GetString() ?? "" :
                                     item.TryGetProperty("Icon", out iconProp) ? iconProp.GetString() ?? "" :
                                     item.TryGetProperty("icon", out iconProp) ? iconProp.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(idVal) || string.IsNullOrEmpty(iconVal)) continue;
                    if (!seenIds.Add(idVal)) continue;
                    pairs.Add((idVal, iconVal));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Skip {filename}: {ex.Message}");
            }
        }
        return pairs;
    }

    /// <summary>
    /// Convert a DDS file to PNG using ImageMagick's magick.exe.
    /// </summary>
    public static bool DdsToPng(string magickPath, string source, string dest)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = magickPath,
                Arguments = $"\"{source}\" \"{dest}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process == null) return false;
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit(30_000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Find magick.exe in tools/imagemagick/ directory.
    /// </summary>
    public static string? FindMagickExe(string toolsDir)
    {
        string imDir = Path.Combine(toolsDir, ExtractorConfig.ImageMagickSubfolder);
        string magickPath = Path.Combine(imDir, "magick.exe");
        if (File.Exists(magickPath)) return magickPath;

        // Also check the tools directory directly
        magickPath = Path.Combine(toolsDir, "magick.exe");
        if (File.Exists(magickPath)) return magickPath;

        return null;
    }

    public static (int Success, int Skipped) ExtractIcons(
        string jsonDir, string extractedRoot, string outputDir, string toolsDir)
    {
        var pairs = CollectIdIconPairs(jsonDir);
        if (pairs.Count == 0)
        {
            Console.WriteLine("[WARN] No id+icon pairs found in JSON files.");
            return (0, 0);
        }

        Console.WriteLine($"[INFO] Found {pairs.Count} items with icons");
        Directory.CreateDirectory(outputDir);

        string? magickPath = FindMagickExe(toolsDir);
        if (magickPath == null)
        {
            Console.WriteLine("[ERROR] ImageMagick magick.exe not found. Cannot convert DDS to PNG.");
            return (0, pairs.Count);
        }

        int success = 0, skipped = 0;
        int progressInterval = Math.Max(1, Math.Min(100, pairs.Count / 20));

        for (int i = 0; i < pairs.Count; i++)
        {
            if ((i + 1) % progressInterval == 0 || i + 1 == pairs.Count)
                PakExtractor.WriteProgress($"  [{i + 1}/{pairs.Count}] Converting icons...");

            var (idVal, iconPath) = pairs[i];
            string source = Path.Combine(extractedRoot, iconPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source)) { skipped++; continue; }

            string safeId = SanitizeFilename(idVal);
            string dest = Path.Combine(outputDir, $"{safeId}.png");
            if (DdsToPng(magickPath, source, dest))
                success++;
            else
                skipped++;
        }

        return (success, skipped);
    }

    /// <summary>
    /// Normalize extracted folder to use lowercase paths (matching game texture references).
    /// </summary>
    public static void NormalizeExtracted(string extractedRoot)
    {
        string srcDir = Path.Combine(extractedRoot, "TEXTURES");
        if (!Directory.Exists(srcDir))
        {
            srcDir = Path.Combine(extractedRoot, "textures");
            if (!Directory.Exists(srcDir))
            {
                Console.WriteLine("[WARN] No TEXTURES folder found in extracted.");
                return;
            }
        }

        string destTextures = Path.Combine(extractedRoot, "textures");
        if (Path.GetFullPath(srcDir).Equals(Path.GetFullPath(destTextures), StringComparison.OrdinalIgnoreCase))
            return;

        Console.WriteLine("[INFO] Normalizing to extracted/textures/ (lowercase paths)...");
        var files = Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories).ToArray();
        int total = files.Length;

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string relative = Path.GetRelativePath(srcDir, file).ToLowerInvariant();
            string destFile = Path.Combine(destTextures, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Move(file, destFile, overwrite: true);

            if ((i + 1) % 500 == 0 || i + 1 == total)
                PakExtractor.WriteProgress($"  [{i + 1}/{total}] files normalized");
        }

        if (total > 0)
            PakExtractor.FinishProgress();

        if (Directory.Exists(srcDir) &&
            !Path.GetFullPath(srcDir).Equals(Path.GetFullPath(destTextures), StringComparison.OrdinalIgnoreCase))
        {
            Directory.Delete(srcDir, recursive: true);
        }
    }
}
