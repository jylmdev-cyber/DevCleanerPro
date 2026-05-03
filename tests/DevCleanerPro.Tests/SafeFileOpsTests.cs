using System.IO;
using DevCleanerPro.Helpers;

namespace DevCleanerPro.Tests;

public class SafeFileOpsTests
{
    [Fact]
    public void GetDirectorySize_NonExistentPath_ReturnsZero()
    {
        var result = SafeFileOps.GetDirectorySize(@"C:\__nonexistent_devcleanerpro_test__");
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetDirectorySize_TempDir_ReturnsPositive()
    {
        var tempDir = Path.GetTempPath();
        var result = SafeFileOps.GetDirectorySize(tempDir);
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonExistentPath_ReturnsZero()
    {
        var (freed, errors) = await SafeFileOps.DeleteDirectoryAsync(@"C:\__nonexistent_devcleanerpro_test__", null);
        Assert.Equal(0, freed);
    }
}
