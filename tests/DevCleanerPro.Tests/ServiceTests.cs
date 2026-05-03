using System.IO;
using DevCleanerPro.Services;
using DevCleanerPro.Models;

namespace DevCleanerPro.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Load_ReturnsDefaultSettings_WhenNoFile()
    {
        var svc = new SettingsService();
        var settings = svc.Load();
        Assert.NotNull(settings);
        Assert.Equal("Dark", settings.Theme);
        Assert.True(settings.ConfirmBeforeDelete);
        Assert.Equal(30, settings.LogRetentionDays);
    }
}

public class AuditLogServiceTests
{
    [Fact]
    public void Log_CreatesEntry()
    {
        var svc = new AuditLogService();
        svc.Log("TestModule", "TestAction", 1024, 5);
        var entries = svc.LoadDay(DateTime.Now);
        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.Module == "TestModule");
    }

    [Fact]
    public void ExportToCsv_WritesFile()
    {
        var svc = new AuditLogService();
        svc.Log("ExportTest", "TestAction", 0);
        var tmpFile = Path.Combine(Path.GetTempPath(), $"devcleanerpro_test_{Guid.NewGuid()}.csv");
        try
        {
            svc.ExportToCsv(tmpFile, DateTime.Now.AddDays(-1), DateTime.Now);
            Assert.True(File.Exists(tmpFile));
            Assert.True(new FileInfo(tmpFile).Length > 0);
        }
        finally { if (File.Exists(tmpFile)) File.Delete(tmpFile); }
    }
}

public class AiAgentCleanerServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_DoesNotThrow()
    {
        var svc = new AiAgentCleanerService();
        var result = await svc.AnalyzeAsync();
        Assert.True(result >= 0);
    }

    [Theory]
    [InlineData("Cursor")]
    [InlineData("Claude")]
    [InlineData("Copilot")]
    [InlineData("Ollama")]
    [InlineData("Antigravity")]
    public void IsToolInstalled_ReturnsBool(string tool)
    {
        var svc = new AiAgentCleanerService();
        var result = svc.IsToolInstalled(tool);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task ScanAllAsync_ReturnsListOfTools()
    {
        var svc = new AiAgentCleanerService();
        var tools = await svc.ScanAllAsync();
        Assert.NotNull(tools);
        foreach (var tool in tools)
            Assert.False(string.IsNullOrEmpty(tool.Name));
    }

    [Fact]
    public async Task GetAllModelsAsync_ReturnsListOfModels()
    {
        var svc = new AiAgentCleanerService();
        var models = await svc.GetAllModelsAsync();
        Assert.NotNull(models);
    }
}
