using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;
using System;

namespace LogSanitizer.Tests;

public class DirectorySkipSanitizedTests : IDisposable
{
    private readonly string _inputDir;
    private readonly string _outputDir;

    public DirectorySkipSanitizedTests()
    {
        _inputDir = Path.Combine(Path.GetTempPath(), "LogSanitizerSkip_" + Guid.NewGuid());
        _outputDir = Path.Combine(_inputDir, "out");
        Directory.CreateDirectory(_inputDir);
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task ProcessDirectoryAsync_ShouldSkipAlreadySanitizedFiles()
    {
        var unsanitized = Path.Combine(_inputDir, "app.log");
        var sanitized = Path.Combine(_inputDir, "app_sanitized.log");
        File.WriteAllText(unsanitized, "Address fe80::1");
        File.WriteAllText(sanitized, "Already sanitized");

        var config = SanitizationConfig.Default;
        config.AllowedExtensions = new List<string> { ".log" };
        using var processor = new LogProcessor(config);

        await processor.ProcessDirectoryAsync(_inputDir, _outputDir);

        var outputFiles = Directory.GetFiles(_outputDir);
        outputFiles.Should().Contain(f => Path.GetFileName(f) == "app.log");
        outputFiles.Should().NotContain(f => Path.GetFileName(f) == "app_sanitized.log");
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
