using DevCleanerPro.Helpers;

namespace DevCleanerPro.Tests;

public class FileSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741824, "1.00 GB")]
    [InlineData(1099511627776, "1.00 TB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(2621440, "2.50 MB")]
    public void Format_ReturnsCorrectString(long bytes, string expected)
    {
        var result = FileSizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_NegativeBytes_ReturnsString()
    {
        var result = FileSizeFormatter.Format(-1);
        Assert.NotNull(result);
    }
}
