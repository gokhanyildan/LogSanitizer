using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class PathWmiMaskingTests
{
    [Fact]
    public void SanitizeLine_ShouldMaskSmsFolderSiteCode()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = @"\\SERVER\SMS_GYC\inbox";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"\\\\SERVER\\SMS_\[SITE-[0-9A-F]{6}\]\\inbox");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskWmiNamespaceSiteCode()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = true;
        using var processor = new LogProcessor(config);

        var input = @"root\sms\site_GYC";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"root\\sms\\site_\[SITE-[0-9A-F]{6}\]");
    }
}
