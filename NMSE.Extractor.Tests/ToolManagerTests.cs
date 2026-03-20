using NMSE.Extractor.Util;

namespace NMSE.Extractor.Tests;

public class ToolManagerTests
{
    [Fact]
    public async Task GetLatestReleaseTag_ReturnsTagOrNull()
    {
        // Test with a known URL - this makes a real HTTP request
        // but handles failure gracefully (returns null on network issues)
        var tag = await ToolManager.GetLatestReleaseTagAsync(
            "https://github.com/monkeyman192/HGPAKtool/releases/latest/");
        // tag is either a valid string or null (if no network)
        if (tag != null)
            Assert.False(string.IsNullOrWhiteSpace(tag));
    }
}
