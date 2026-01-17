using FluentAssertions;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;
using LogSanitizer.Core.Constants;

namespace LogSanitizer.Tests;

public class IPv6RegressionTests
{
    [Fact]
    public void SanitizeLine_ShouldNotMaskTimestamp_WhenItLooksLikeIPv6()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.IPv6Address }
        };
        using var processor = new LogProcessor(config);
        var input = "Event occurred at 22:18:12.";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().Be("Event occurred at 22:18:12.");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskRealIPv6()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.IPv6Address }
        };
        using var processor = new LogProcessor(config);
        var input = "Connection from fe80::1 established.";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().MatchRegex(@"Connection from \[IP6-[0-9A-F]{6}\] established\.");
    }
    
    [Fact]
    public void SanitizeLine_ShouldIgnoreShortSequences()
    {
        // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.IPv6Address }
        };
        using var processor = new LogProcessor(config);
        var input = "Code: 12:34 error.";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().Be("Code: 12:34 error.");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskIPv6_WithScope()
    {
         // Arrange
        var config = new SanitizationConfig
        {
            EnableHashing = true,
            TargetPiiTypes = new List<PiiType> { PiiType.IPv6Address }
        };
        using var processor = new LogProcessor(config);
        var input = "Address: fe80::a00:27ff:fe8f:698%1";

        // Act
        var result = processor.SanitizeLine(input);

        // Assert
        result.Should().MatchRegex(@"Address: \[IP6-[0-9A-F]{6}\]");
    }
}
