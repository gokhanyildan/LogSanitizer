using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;
using System;

namespace LogSanitizer.Tests;

public class LdapAndKeyValueTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _outputDir;

    public LdapAndKeyValueTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "LogSanitizerTests_" + Guid.NewGuid());
        _outputDir = Path.Combine(_testDir, "out");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskLdapDomain_DC()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "User from DC=GOKHANYILDAN,DC=COM";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"User from DC=\[DN-[0-9A-F]{6}\],DC=\[DN-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskSiteCode_TightSpacing()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "SITE=GYC";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"SITE=\[SITE-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskSiteCode_WithBrackets()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "[SiteCode]=[GYC]";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"\[SiteCode\]=\[SITE-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskDatabaseName()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "DatabaseName = CM_GYC";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"DatabaseName\s*=\s*\[DB-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskSqlServerName()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "SQLServerName: PROD-DB";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"SQLServerName:\s*\[SRV-[0-9A-F]{6}\]");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskCN_Values()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false;
        using var processor = new LogProcessor(config);

        var input = "CN=GOKHANYILDAN-DC01-CA,DC=corp,DC=local";
        var result = processor.SanitizeLine(input);
        result.Should().MatchRegex(@"CN=\[DN-[0-9A-F]{6}\],DC=\[DN-[0-9A-F]{6}\],DC=\[DN-[0-9A-F]{6}\]");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        }
        catch { }
    }
}
