using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class AdvancedOptionsTests
{
    [Fact]
    public void SanitizeLine_ShouldScrubUrlHostname_WithDeterministicToken()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "Request: https://sccm01.contoso.com/App";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"Request: https://\[FQDN-[0-9A-F]{6}\]/App");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskSqlConnectionStringKeys()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "Data Source=sql01; Initial Catalog=CM_GYC; User ID=sa; Password=pass123;";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"Data Source=\[SRV-[0-9A-F]{6}\]; Initial Catalog=\[DB-[0-9A-F]{6}\]; User ID=\[SEC-[0-9A-F]{6}\]; Password=\[SEC-[0-9A-F]{6}\];");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskEmail_WithDeterministicToken()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "Contact admin@corp.local";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"Contact \[EMAIL-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskCreditCard_WithoutMatchingUuid()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = "Card 4532-1234-5678-9010 and UUID 123e4567-e89b-12d3-a456-426614174000";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"Card \[CC-[0-9A-F]{6}\]");
        result.Should().Contain("123e4567-e89b-12d3-a456-426614174000");
    }
}
