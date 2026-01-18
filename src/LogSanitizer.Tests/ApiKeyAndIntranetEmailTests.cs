using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class ApiKeyAndIntranetEmailTests
{
    [Fact]
    public void SanitizeLine_ShouldMaskApiKey_WithDeterministicToken()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "x-api-key: abc12345";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"x-api-key:\s*\[APIKEY-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskApiKey_EqualsForm()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "api-key=secret987";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"api-key=\s*\[APIKEY-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskIntranetEmail_WithoutTld()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "Contact admin@internal";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"Contact \[EMAIL-[0-9A-F]{6}\]");
    }
}
