using Microsoft.Win32;
using NMSE.Extractor.Config;

namespace NMSE.Extractor.Util;

public static class SteamLocator
{
    public static string FindPcBanksPath()
    {
        string? steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath))
            throw new InvalidOperationException(
                "Could not find Steam installation path in the Windows registry.");

        string pcbanks = Path.Combine(steamPath, ExtractorConfig.NmsGamePath);
        if (!Directory.Exists(pcbanks))
            throw new DirectoryNotFoundException(
                $"PCBANKS directory not found: {pcbanks}");

        return pcbanks;
    }

    public static string? GetSteamInstallPath()
    {
        // Try 64-bit registry view first (Wow6432Node)
        string? path = ReadRegistryValue(
            @"SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath");
        if (!string.IsNullOrEmpty(path))
            return path;

        // Fallback to native key
        path = ReadRegistryValue(@"SOFTWARE\Valve\Steam", "InstallPath");
        return path;
    }

    private static string? ReadRegistryValue(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }
}
