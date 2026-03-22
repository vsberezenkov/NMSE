using NMSE.Core;
using NMSE.Models;

namespace NMSE.Tests;

public class UpdateServiceTests
{
    // ParseVersion

    [Theory]
    [InlineData("v1.2.3",       1, 2, 3)]
    [InlineData("1.2.3",        1, 2, 3)]
    [InlineData("NMSE v1.1.139", 1, 1, 139)]
    [InlineData("v0.0.1",       0, 0, 1)]
    [InlineData("v10.20.300",   10, 20, 300)]
    [InlineData("release-2.5.0", 2, 5, 0)]
    public void ParseVersion_ValidInput_ReturnsParsedVersion(
        string input, int major, int minor, int patch)
    {
        var v = UpdateService.ParseVersion(input);
        Assert.NotNull(v);
        Assert.Equal(major, v.Major);
        Assert.Equal(minor, v.Minor);
        Assert.Equal(patch, v.Build); // Version uses Build for third component
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-version-here")]
    [InlineData("v1")]
    [InlineData("v1.2")]
    public void ParseVersion_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(UpdateService.ParseVersion(input));
    }

    // IsNewer

    [Fact]
    public void IsNewer_RemoteIsNewer_ReturnsTrue()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 140);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_RemoteIsSame_ReturnsFalse()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 139);
        Assert.False(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_RemoteIsOlder_ReturnsFalse()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 138);
        Assert.False(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_MajorVersionBump_ReturnsTrue()
    {
        var current = new Version(1, 9, 999);
        var remote  = new Version(2, 0, 0);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_MinorVersionBump_ReturnsTrue()
    {
        var current = new Version(1, 1, 999);
        var remote  = new Version(1, 2, 0);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    // FindAssetDownloadUrl

    [Fact]
    public void FindAssetDownloadUrl_ValidRelease_ReturnsZipUrl()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "body": "Automated release",
            "assets": [
                {
                    "name": "NMSE-1.1.140-Release.zip",
                    "browser_download_url": "https://github.com/vectorcmdr/NMSE/releases/download/v1.1.140/NMSE-1.1.140-Release.zip"
                }
            ]
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        string? url = UpdateService.FindAssetDownloadUrl(release);
        Assert.Equal(
            "https://github.com/vectorcmdr/NMSE/releases/download/v1.1.140/NMSE-1.1.140-Release.zip",
            url);
    }

    [Fact]
    public void FindAssetDownloadUrl_NoZipAsset_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "assets": [
                {
                    "name": "source.tar.gz",
                    "browser_download_url": "https://example.com/source.tar.gz"
                }
            ]
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Null(UpdateService.FindAssetDownloadUrl(release));
    }

    [Fact]
    public void FindAssetDownloadUrl_EmptyAssets_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "assets": []
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Null(UpdateService.FindAssetDownloadUrl(release));
    }

    [Fact]
    public void FindAssetDownloadUrl_NoAssetsKey_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140"
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Null(UpdateService.FindAssetDownloadUrl(release));
    }

    [Fact]
    public void FindAssetDownloadUrl_MultipleAssets_ReturnsFirstZip()
    {
        string json = """
        {
            "tag_name": "v2.0.0",
            "assets": [
                {
                    "name": "checksums.txt",
                    "browser_download_url": "https://example.com/checksums.txt"
                },
                {
                    "name": "NMSE-2.0.0-Release.zip",
                    "browser_download_url": "https://example.com/NMSE-2.0.0-Release.zip"
                },
                {
                    "name": "NMSE-2.0.0-Debug.zip",
                    "browser_download_url": "https://example.com/NMSE-2.0.0-Debug.zip"
                }
            ]
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Equal("https://example.com/NMSE-2.0.0-Release.zip",
            UpdateService.FindAssetDownloadUrl(release));
    }

    // FindReleaseVersion

    [Fact]
    public void FindReleaseVersion_PrefersName()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140"
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Equal("NMSE v1.1.140", UpdateService.FindReleaseVersion(release));
    }

    [Fact]
    public void FindReleaseVersion_FallsBackToTagName()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": ""
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Equal("v1.1.140", UpdateService.FindReleaseVersion(release));
    }

    // FindReleaseNotes

    [Fact]
    public void FindReleaseNotes_ReturnsBody()
    {
        string json = """
        {
            "body": "Bug fixes and improvements."
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Equal("Bug fixes and improvements.", UpdateService.FindReleaseNotes(release));
    }

    [Fact]
    public void FindReleaseNotes_NoBody_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140"
        }
        """;
        var release = JsonObject.Parse(json);
        Assert.NotNull(release);

        Assert.Null(UpdateService.FindReleaseNotes(release));
    }

    // GenerateUpdaterScript

    [Fact]
    public void GenerateUpdaterScript_ContainsProcessId()
    {
        string script = UpdateService.GenerateUpdaterScript(
            12345, @"C:\temp\extract", @"C:\app", "NMSE.exe");
        // Verify the PID is used in the correct tasklist filter context, not just
        // that the number appears somewhere in the script.
        Assert.Contains("tasklist /fi \"PID eq 12345\"", script);
    }

    [Fact]
    public void GenerateUpdaterScript_RemovesResourcesFolder()
    {
        string script = UpdateService.GenerateUpdaterScript(
            1, @"C:\temp\extract", @"C:\app", "NMSE.exe");
        Assert.Contains(@"rmdir /s /q ""C:\app\Resources""", script);
    }

    [Fact]
    public void GenerateUpdaterScript_CopiesNewFiles()
    {
        string script = UpdateService.GenerateUpdaterScript(
            1, @"C:\temp\extract", @"C:\app", "NMSE.exe");
        Assert.Contains(@"xcopy /e /y /q ""C:\temp\extract\*"" ""C:\app\""", script);
    }

    [Fact]
    public void GenerateUpdaterScript_RelaunchesExe()
    {
        string script = UpdateService.GenerateUpdaterScript(
            1, @"C:\temp\extract", @"C:\app", "MyApp.exe");
        Assert.Contains(@"start """" ""C:\app\MyApp.exe""", script);
    }

    // ReleasesApiUrl

    [Fact]
    public void ReleasesApiUrl_ContainsOwnerAndRepo()
    {
        string url = UpdateService.ReleasesApiUrl;
        Assert.Contains(UpdateService.GitHubOwner, url);
        Assert.Contains(UpdateService.GitHubRepo, url);
        Assert.Contains("/releases/latest", url);
        Assert.StartsWith("https://api.github.com/repos/", url);
    }

    // Constants

    [Fact]
    public void Constants_AreConfigurable()
    {
        // Verify the constants exist and have non-empty values.
        // These are the values that would be changed for the final release repo.
        Assert.False(string.IsNullOrEmpty(UpdateService.GitHubOwner));
        Assert.False(string.IsNullOrEmpty(UpdateService.GitHubRepo));
    }
}
