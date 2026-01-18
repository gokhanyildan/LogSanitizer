using FluentAssertions;
using LogSanitizer.Core.Constants;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class LogProcessorTests
{
    [Fact]
    public void SanitizeLine_ShouldMaskEmail_WhenMaskingEnabled()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.Email }
        };
        var processor = new LogProcessor(config);
        var input = "Contact us at support@example.com for help.";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().MatchRegex(@"Contact us at \[EMAIL-[0-9A-F]{6}\] for help\.");
    }

    [Fact]
    public void SanitizeLine_ShouldHashIPv4_WhenHashingEnabled()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.IPv4Address }
        };
        var processor = new LogProcessor(config);
        var input = "Connection from 192.168.1.100 established.";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().MatchRegex(@"Connection from \[IP4-[0-9A-F]{6}\] established\.");
    }

    [Fact]
    public void SanitizeLine_ShouldMaintainConsistency_ForSameInput()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.Email }
        };
        var processor = new LogProcessor(config);
        var email = "user@example.com";
        var input1 = $"User {email} logged in.";
        var input2 = $"User {email} logged out.";

        // Act
        var result1 = processor.SanitizeLine(input1);
        var result2 = processor.SanitizeLine(input2);

        // Assert
        var token1 = result1.Split(' ')[1];
        var token2 = result2.Split(' ')[1];
        token1.Should().Be(token2);
    }
}
