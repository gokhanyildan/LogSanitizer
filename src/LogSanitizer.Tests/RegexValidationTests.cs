using FluentAssertions;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class RegexValidationTests
{
    private LogProcessor CreateProcessor(PiiType piiType)
    {
        var config = new SanitizationConfig
        {
            EnableHashing = false,
            MaskPlaceholder = "***",
            TargetPiiTypes = new List<PiiType> { piiType }
        };
        return new LogProcessor(config);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("8.8.8.8")]
    public void SanitizeLine_ShouldMaskIPv4_WhenValid(string ip)
    {
        var processor = CreateProcessor(PiiType.IPv4Address);
        var input = $"Connection from {ip} established.";
        var result = processor.SanitizeLine(input);
        result.Should().Be("Connection from *** established.");
    }

    [Theory]
    [InlineData("999.999.999.999")] // Invalid octets (technically regex might catch 999 if it's simple \d{1,3}, let's check strictness)
    [InlineData("192.168.1")] // Incomplete
    [InlineData("1.1.1.1.1")] // Too long
    public void SanitizeLine_ShouldIgnoreIPv4_WhenInvalid(string ip)
    {
        var processor = CreateProcessor(PiiType.IPv4Address);
        var input = $"Value {ip} is not an IP.";
        
        // Note: If the regex is loose (e.g. \d{1,3}), 999 might match. 
        // The current regex in RegexDefinitions is \b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b
        // It allows 999. Use known invalid structure for "not touched" check.
        
        var result = processor.SanitizeLine(input);
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("firstname.lastname@company.co.uk")]
    public void SanitizeLine_ShouldMaskEmail_WhenValid(string email)
    {
        var processor = CreateProcessor(PiiType.Email);
        var input = $"Contact {email} for details.";
        var result = processor.SanitizeLine(input);
        result.Should().Be("Contact *** for details.");
    }

    [Theory]
    [InlineData("user@")] // Missing domain
    [InlineData("@example.com")] // Missing user
    [InlineData("user@.com")] // Missing domain name
    public void SanitizeLine_ShouldIgnoreEmail_WhenInvalid(string email)
    {
        var processor = CreateProcessor(PiiType.Email);
        var input = $"Invalid email {email} here.";
        var result = processor.SanitizeLine(input);
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskCreditCard_WhenValid()
    {
        var processor = CreateProcessor(PiiType.CreditCard);
        var input = "Payment via 1234-5678-9012-3456 processed.";
        var result = processor.SanitizeLine(input);
        result.Should().Be("Payment via *** processed.");
    }

    [Fact]
    public void SanitizeLine_ShouldIgnoreCreditCard_WhenInvalid()
    {
        var processor = CreateProcessor(PiiType.CreditCard);
        // Too short
        var input = "My number is 1234-5678."; 
        var result = processor.SanitizeLine(input);
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskCertificateThumbprint_WhenValid()
    {
        var processor = CreateProcessor(PiiType.CertificateThumbprint);
        // 40 hex chars
        var thumbprint = "a94a8fe5ccb19ba61c4c0873d391e987982fbbd3"; 
        var input = $"Thumbprint: {thumbprint}";
        var result = processor.SanitizeLine(input);
        result.Should().Be("Thumbprint: ***");
    }

    [Theory]
    [InlineData("a94a8fe5ccb19ba61c4c0873d391e987982fbbd")] // 39 chars
    [InlineData("a94a8fe5ccb19ba61c4c0873d391e987982fbbd3a")] // 41 chars
    [InlineData("z94a8fe5ccb19ba61c4c0873d391e987982fbbd3")] // Invalid char 'z'
    public void SanitizeLine_ShouldIgnoreCertificateThumbprint_WhenInvalid(string thumbprint)
    {
        var processor = CreateProcessor(PiiType.CertificateThumbprint);
        var input = $"Check {thumbprint} value.";
        var result = processor.SanitizeLine(input);
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskMixedContent_Correctly()
    {
        var config = new SanitizationConfig
        {
            EnableHashing = false,
            MaskPlaceholder = "[PII]",
            TargetPiiTypes = new List<PiiType> 
            { 
                PiiType.IPv4Address, 
                PiiType.Email, 
                PiiType.CreditCard,
                PiiType.CertificateThumbprint
            }
        };
        var processor = new LogProcessor(config);

        var ip = "192.168.1.10";
        var email = "admin@corp.local";
        var cc = "4532-1234-5678-9010";
        var thumb = "1234567890abcdef1234567890abcdef12345678";

        var input = $"User {email} logged in from {ip} using card {cc}. Cert: {thumb}.";
        var expected = "User [PII] logged in from [PII] using card [PII]. Cert: [PII].";

        var result = processor.SanitizeLine(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskBearerToken()
    {
        var processor = CreateProcessor(PiiType.BearerToken);
        var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var input = $"Authorization: {token}";
        var expected = "Authorization: ***";

        var result = processor.SanitizeLine(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskBearerToken_CaseInsensitive()
    {
        var processor = CreateProcessor(PiiType.BearerToken);
        var input = "bearer abc-123.xyz";
        var expected = "***";

        var result = processor.SanitizeLine(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeLine_ShouldMaskConnectionStringPassword()
    {
        var processor = CreateProcessor(PiiType.ConnectionStringPassword);
        var input = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
        // Note: Regex is (?<=(Password|Pwd|User ID|Uid)=)[^;]*
        // So "User Id=myUsername" -> "User Id=***"
        // "Password=myPassword" -> "Password=***"
        
        var result = processor.SanitizeLine(input);
        
        result.Should().Contain("User Id=***");
        result.Should().Contain("Password=***");
        result.Should().NotContain("myUsername");
        result.Should().NotContain("myPassword");
    }

    [Fact]
    public void SanitizeLine_ShouldMaskConnectionStringPwd_ShortForm()
    {
        var processor = CreateProcessor(PiiType.ConnectionStringPassword);
        var input = "Data Source=server;Pwd=secret123;Uid=admin;";
        
        var result = processor.SanitizeLine(input);
        
        result.Should().Contain("Pwd=***");
        result.Should().Contain("Uid=***");
        result.Should().NotContain("secret123");
        result.Should().NotContain("admin");
    }
}
