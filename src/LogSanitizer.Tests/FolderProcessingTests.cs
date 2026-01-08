using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class FolderProcessingTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _outputDir;

    public FolderProcessingTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LogSanitizer_Input_" + Guid.NewGuid());
        _outputDir = Path.Combine(Path.GetTempPath(), "LogSanitizer_Output_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task ProcessDirectoryAsync_ShouldOnlyProcessAllowedExtensions()
    {
        // Arrange
        // Create files with different extensions
        File.WriteAllText(Path.Combine(_testDir, "test1.log"), "User test@example.com logged in.");
        File.WriteAllText(Path.Combine(_testDir, "test2.txt"), "IP 192.168.1.1 detected.");
        File.WriteAllText(Path.Combine(_testDir, "ignore.exe"), "Binary content");
        File.WriteAllText(Path.Combine(_testDir, "readme.md"), "# Readme");

        var config = SanitizationConfig.Default;
        config.AllowedExtensions = new List<string> { ".log", ".txt" };
        
        using var processor = new LogProcessor(config);

        // Act
        await processor.ProcessDirectoryAsync(_testDir, _outputDir);

        // Assert
        var outputFiles = Directory.GetFiles(_outputDir);
        outputFiles.Should().HaveCount(2);
        outputFiles.Should().Contain(f => f.EndsWith("test1.log"));
        outputFiles.Should().Contain(f => f.EndsWith("test2.txt"));
        outputFiles.Should().NotContain(f => f.EndsWith("ignore.exe"));
        outputFiles.Should().NotContain(f => f.EndsWith("readme.md"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
            if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true);
        }
        catch { /* Best effort cleanup */ }
    }
}
