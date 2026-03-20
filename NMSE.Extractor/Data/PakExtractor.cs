using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NMSE.Extractor;

public static partial class PakExtractor
{
    /// <summary>
    /// Regex to parse "Unpacked N files" from hgpaktool stderr output.
    /// </summary>
    [GeneratedRegex(@"Unpacked\s+(\d+)\s+files?", RegexOptions.IgnoreCase)]
    private static partial Regex UnpackedCountRegex();

    /// <summary>
    /// Determine if a .pak file could contain files matching our extraction filters.
    /// Based on known NMS pak naming conventions:
    /// - MetadataEtc -> METADATA/REALITY/TABLES and SIMULATION MBINs
    /// - Precache -> LANGUAGE MBINs
    /// - Tex* -> TEXTURES/*.DDS files
    /// Everything else (audio, mesh, fonts, shaders, animations, scenes, etc.) is skipped.
    /// </summary>
    public static bool IsPakRelevant(string pakFileName)
    {
        string name = Path.GetFileNameWithoutExtension(pakFileName) ?? "";
        string nameUpper = name.ToUpperInvariant();
        if (nameUpper.Contains("METADATAETC") || nameUpper.Contains("PRECACHE"))
            return true;
        // Match Tex* type segment (e.g., NMSARC.TexUI -> type "TEXUI" starts with "TEX")
        int firstDot = nameUpper.IndexOf('.');
        if (firstDot >= 0 && nameUpper[(firstDot + 1)..].StartsWith("TEX"))
            return true;
        return false;
    }

    /// <summary>
    /// Parse the "Unpacked N files" count from hgpaktool stderr output.
    /// Returns 0 if the pattern is not found.
    /// </summary>
    public static int ParseUnpackedCount(string? stderr)
    {
        if (string.IsNullOrEmpty(stderr)) return 0;
        var match = UnpackedCountRegex().Match(stderr);
        return match.Success && int.TryParse(match.Groups[1].Value, out int count) ? count : 0;
    }

    /// <summary>
    /// Write an in-place progress line, clearing any previous content on the line.
    /// Pads with spaces to the console width so shorter messages overwrite longer ones.
    /// </summary>
    public static void WriteProgress(string message)
    {
        int width;
        try { width = Console.WindowWidth - 1; }
        catch { width = 79; }
        if (width < 20) width = 79; // minimum usable width for progress messages
        Console.Write($"\r{message.PadRight(width)}");
    }

    /// <summary>
    /// Finish an in-place progress block by moving to a new line.
    /// </summary>
    public static void FinishProgress()
    {
        Console.WriteLine();
    }

    /// <summary>
    /// Extract filtered files from game .pak files, processing one pak at a time.
    /// For each pak: checks relevance, copies to banksDir, runs hgpaktool with filters, removes the copy.
    /// Extracted content accumulates in banksDir/extracted/.
    /// Irrelevant paks (audio, mesh, fonts, shaders, etc.) are skipped entirely.
    /// </summary>
    public static void ExtractPerPak(string hgpaktoolPath, string pcbanksPath, string banksDir, string[] filters)
    {
        if (!File.Exists(hgpaktoolPath))
            throw new FileNotFoundException($"hgpaktool not found: {hgpaktoolPath}");
        if (!Directory.Exists(pcbanksPath))
            throw new DirectoryNotFoundException($"PCBANKS directory not found: {pcbanksPath}");

        Directory.CreateDirectory(banksDir);

        string[] allPakFiles = Directory.GetFiles(pcbanksPath, "*.pak");
        string[] pakFiles = allPakFiles.Where(p => IsPakRelevant(Path.GetFileName(p))).ToArray();
        int skippedCount = allPakFiles.Length - pakFiles.Length;
        int total = pakFiles.Length;
        int paksWithContent = 0;
        int totalEntries = 0;

        Console.WriteLine($"[INFO] Found {allPakFiles.Length} .pak files, {skippedCount} irrelevant skipped, processing {total} with {filters.Length} filters...");

        // Build filter args once
        var filterArgParts = new List<string>();
        foreach (var filter in filters)
            filterArgParts.Add($"-f=\"{filter}\"");
        string filterArgStr = string.Join(" ", filterArgParts);

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < pakFiles.Length; i++)
        {
            string srcPak = pakFiles[i];
            string pakName = Path.GetFileName(srcPak);
            long sizeMB = new FileInfo(srcPak).Length / (1024 * 1024);

            WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) - extracting...");

            // Copy single pak to working directory
            string destPak = Path.Combine(banksDir, pakName);
            File.Copy(srcPak, destPak, overwrite: true);

            // Run hgpaktool with filters on just this pak
            string arguments = $"-U {filterArgStr} \"{banksDir}\"";
            var psi = new ProcessStartInfo
            {
                FileName = hgpaktoolPath,
                Arguments = arguments,
                WorkingDirectory = banksDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            int pakEntries = 0;
            using (var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start hgpaktool."))
            {
                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                stdoutTask.GetAwaiter().GetResult();
                string stderr = stderrTask.GetAwaiter().GetResult();
                process.WaitForExit();

                // Parse actual count from hgpaktool stderr ("Unpacked N files")
                pakEntries = ParseUnpackedCount(stderr);

                if (process.ExitCode != 0)
                {
                    FinishProgress();
                    Console.WriteLine($"  [WARN] hgpaktool error (code {process.ExitCode}) for {pakName}");
                }
            }

            // Remove the pak file copy immediately (saves disk space)
            try { File.Delete(destPak); } catch { /* best-effort */ }

            totalEntries += pakEntries;
            if (pakEntries > 0)
            {
                paksWithContent++;
                WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) -> {pakEntries} files");
            }
            else
            {
                WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) -> no matching files");
            }
            FinishProgress();
        }

        Console.WriteLine($"[OK] Extracted {totalEntries} entries from {paksWithContent}/{total} .pak files ({sw.Elapsed.TotalSeconds:F0}s)");
    }

    /// <summary>
    /// Delete all .pak files from the local banks working directory.
    /// </summary>
    public static void CleanupPakFiles(string banksDir)
    {
        if (!Directory.Exists(banksDir)) return;

        string[] pakFiles = Directory.GetFiles(banksDir, "*.pak");
        if (pakFiles.Length == 0) return;

        int total = pakFiles.Length;
        int removed = 0;
        Console.WriteLine($"[INFO] Cleaning up {total} .pak files from banks directory...");

        for (int i = 0; i < pakFiles.Length; i++)
        {
            try
            {
                File.Delete(pakFiles[i]);
                removed++;
            }
            catch { /* best-effort cleanup */ }

            if ((i + 1) % 10 == 0 || i + 1 == total)
                WriteProgress($"  [{i + 1}/{total}] deleted");
        }

        FinishProgress();
        Console.WriteLine($"[OK] Cleaned up {removed}/{total} .pak files from banks directory");
    }

    /// <summary>
    /// Delete the entire banks working directory including all contents (pak files, extracted/, etc.).
    /// </summary>
    public static void CleanupBanksDir(string banksDir)
    {
        if (!Directory.Exists(banksDir)) return;

        try
        {
            Directory.Delete(banksDir, recursive: true);
            Console.WriteLine("[OK] Cleaned up banks directory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Could not fully clean banks directory: {ex.Message}");
            // Fallback: try to at least remove .pak files
            CleanupPakFiles(banksDir);
        }
    }

    /// <summary>
    /// Calculate the total size of .pak files in the PCBANKS directory.
    /// </summary>
    public static long GetPakFilesSize(string pcbanksPath)
    {
        return GetPakFilesSize(pcbanksPath, filter: null);
    }

    /// <summary>
    /// Calculate the total size of .pak files in the PCBANKS directory,
    /// optionally filtering to only include matching files.
    /// </summary>
    public static long GetPakFilesSize(string pcbanksPath, Func<string, bool>? filter)
    {
        if (!Directory.Exists(pcbanksPath)) return 0;

        long total = 0;
        foreach (string pakFile in Directory.GetFiles(pcbanksPath, "*.pak"))
        {
            if (filter != null && !filter(Path.GetFileName(pakFile))) continue;
            total += new FileInfo(pakFile).Length;
        }
        return total;
    }

    /// <summary>
    /// Estimate the maximum disk space consumed during extraction.
    /// With per-pak extraction, peak = largest single pak (temp copy) + filtered extracted content.
    /// Uses pak total size as a conservative upper bound for extracted content.
    /// </summary>
    public static long EstimateMaxStorageBytes(long pakFilesSize, long largestPakSize)
    {
        // Per-pak approach: only 1 pak on disk at a time + extracted content
        // Extracted content with filters is much smaller than total, but use 2x pak size as safe upper bound
        return largestPakSize + (pakFilesSize * 2);
    }

    /// <summary>
    /// Get the size of the largest .pak file in the directory.
    /// </summary>
    public static long GetLargestPakFileSize(string pcbanksPath)
    {
        return GetLargestPakFileSize(pcbanksPath, filter: null);
    }

    /// <summary>
    /// Get the size of the largest .pak file in the directory,
    /// optionally filtering to only include matching files.
    /// </summary>
    public static long GetLargestPakFileSize(string pcbanksPath, Func<string, bool>? filter)
    {
        if (!Directory.Exists(pcbanksPath)) return 0;

        long largest = 0;
        foreach (string pakFile in Directory.GetFiles(pcbanksPath, "*.pak"))
        {
            if (filter != null && !filter(Path.GetFileName(pakFile))) continue;
            long size = new FileInfo(pakFile).Length;
            if (size > largest) largest = size;
        }
        return largest;
    }

}
