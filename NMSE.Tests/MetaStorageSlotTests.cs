using NMSE.IO;

namespace NMSE.Tests;

public class MetaStorageSlotTests
{
    [Theory]
    [InlineData("accountdata.hg", 0)]
    [InlineData("save.hg", 2)]
    [InlineData("save2.hg", 3)]
    [InlineData("save3.hg", 4)]
    [InlineData("save4.hg", 5)]
    [InlineData("save5.hg", 6)]
    [InlineData("save28.hg", 29)]
    public void StorageSlotFromFileName_ReturnsCorrectSlot(string fileName, int expected)
    {
        Assert.Equal(expected, SaveSlotManager.StorageSlotFromFileName(fileName));
    }

    [Fact]
    public void StorageSlotFromFileName_WithFullPath_ReturnsCorrectSlot()
    {
        Assert.Equal(2, SaveSlotManager.StorageSlotFromFileName(Path.Combine("some", "path", "save.hg")));
        Assert.Equal(3, SaveSlotManager.StorageSlotFromFileName(Path.Combine("Users", "test", "save2.hg")));
        Assert.Equal(0, SaveSlotManager.StorageSlotFromFileName(Path.Combine("saves", "accountdata.hg")));
    }

    [Fact]
    public void StorageSlotFromFileName_UnknownFile_DefaultsToSlot2()
    {
        Assert.Equal(2, SaveSlotManager.StorageSlotFromFileName("unknown.hg"));
    }
}
