using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class EdgeCaseTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _outputDir;

    public EdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LogSanitizer_Edge_" + Guid.NewGuid());
        _outputDir = Path.Combine(Path.GetTempPath(), "LogSanitizer_EdgeOut_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleEmptyFile_Gracefully()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "empty.log");
        var outputFile = Path.Combine(_outputDir, "empty.log");
        File.WriteAllText(inputFile, "");
        
        using var processor = new LogProcessor(SanitizationConfig.Default);

        // Act
        await processor.ProcessFileAsync(inputFile, outputFile);

        // Assert
        File.Exists(outputFile).Should().BeTrue();
        new FileInfo(outputFile).Length.Should().Be(0); // Should remain empty
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldThrow_WhenInputFileNotFound()
    {
        // Arrange
        var missingFile = Path.Combine(_testDir, "nonexistent.log");
        var outputFile = Path.Combine(_outputDir, "output.log");
        
        using var processor = new LogProcessor(SanitizationConfig.Default);

        // Act
        var act = async () => await processor.ProcessFileAsync(missingFile, outputFile);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ProcessDirectoryAsync_ShouldHandleEmptyDirectory()
    {
        // Arrange
        var emptyInputDir = Path.Combine(_testDir, "EmptyDir");
        Directory.CreateDirectory(emptyInputDir);
        
        using var processor = new LogProcessor(SanitizationConfig.Default);

        // Act
        await processor.ProcessDirectoryAsync(emptyInputDir, _outputDir);

        // Assert
        // No exceptions thrown, output dir remains empty of new files
        Directory.GetFiles(_outputDir).Should().BeEmpty();
    }

    [Fact]
    public async Task SanitizeLine_ShouldHandleMassiveLine_WithoutCrashing()
    {
        // Arrange
        // Create a 1MB line of text
        var massiveLine = new string('A', 1024 * 1024); 
        var input = $"User {massiveLine} logged in.";
        
        using var processor = new LogProcessor(SanitizationConfig.Default);

        // Act
        // This tests that Regex doesn't hit a timeout or StackOverflow for a simple long string
        // Note: Complex backtracking regexes might still fail, but our current ones are simple.
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().Be(input); // Nothing to sanitize here actually, just ensuring it returns
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
