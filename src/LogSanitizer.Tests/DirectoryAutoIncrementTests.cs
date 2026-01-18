using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;
using System;

namespace LogSanitizer.Tests;

public class DirectoryAutoIncrementTests : IDisposable
{
    private readonly string _inputDir;
    private readonly string _outputDir;

    public DirectoryAutoIncrementTests()
    {
        _inputDir = Path.Combine(Path.GetTempPath(), "LogSanitizerAutoInc_" + Guid.NewGuid());
        _outputDir = Path.Combine(_inputDir, "out");
        Directory.CreateDirectory(_inputDir);
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task ProcessDirectoryAsync_ShouldAutoIncrementOutputNames_OnCollision()
    {
        var sourceFile = Path.Combine(_inputDir, "app.log");
        File.WriteAllText(sourceFile, "Connection from 192.168.1.1");
        var collide1 = Path.Combine(_outputDir, "app.log");
        var collide2 = Path.Combine(_outputDir, "app_sanitized.log");
        File.WriteAllText(collide1, "exists");
        File.WriteAllText(collide2, "exists");

        var config = SanitizationConfig.Default;
        config.AllowedExtensions = new List<string> { ".log" };
        using var processor = new LogProcessor(config);

        await processor.ProcessDirectoryAsync(_inputDir, _outputDir);

        var outputFiles = Directory.GetFiles(_outputDir)
            .Select(Path.GetFileName)
            .ToArray();

        outputFiles.Should().Contain("app_sanitized_1.log");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_inputDir)) Directory.Delete(_inputDir, true);
        }
        catch { }
    }
}
