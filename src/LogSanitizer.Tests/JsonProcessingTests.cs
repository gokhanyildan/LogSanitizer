using FluentAssertions;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.Tests;

public class JsonProcessingTests : IDisposable
{
    private readonly LogProcessor _processor;

    public JsonProcessingTests()
    {
        var config = SanitizationConfig.Default;
        config.EnableHashing = false; // Easier assertions
        config.DetectJson = true;
        _processor = new LogProcessor(config);
    }

    [Fact]
    public void SanitizeLine_ShouldSanitize_SimpleJsonValue()
    {
        string input = "{\"email\": \"test@example.com\"}";
        string result = _processor.SanitizeLine(input);
        
        result.Should().NotContain("test@example.com");
        result.Should().Contain("\"email\":");
        result.Should().Contain(SanitizationConfig.Default.MaskPlaceholder); // [REDACTED] or default
    }

    [Fact]
    public void SanitizeLine_ShouldSanitize_NestedJson()
    {
        string input = "{\"user\": { \"email\": \"test@example.com\", \"id\": 123 }, \"active\": true}";
        string result = _processor.SanitizeLine(input);

        result.Should().NotContain("test@example.com");
        result.Should().Contain("\"user\":");
        result.Should().Contain("\"email\":");
        result.Should().Contain("\"id\":123"); // Numbers shouldn't be touched by string regex
        // JsonNode might format with spaces depending on ToJsonString options, but structure remains.
    }

    [Fact]
    public void SanitizeLine_ShouldSanitize_JsonArray()
    {
        string input = "[{\"email\": \"a@b.com\"}, {\"email\": \"c@d.com\"}]";
        string result = _processor.SanitizeLine(input);

        result.Should().NotContain("a@b.com");
        result.Should().NotContain("c@d.com");
        result.Should().Contain("[{");
    }

    [Fact]
    public void SanitizeLine_ShouldNotCorrupt_JsonKeys()
    {
        // If a key looks like an email (weird but possible), we shouldn't sanitize it if we only look at values.
        // But our current implementation iterates values only.
        
        // However, if the KEY itself is PII (e.g. {"user@example.com": "value"}), our logic WON'T sanitize the key.
        // This is generally desired for JSON structure preservation.
        
        string input = "{\"safe_key\": \"test@example.com\"}";
        string result = _processor.SanitizeLine(input);
        
        result.Should().Contain("safe_key");
        result.Should().NotContain("test@example.com");
    }

    [Fact]
    public void SanitizeLine_ShouldFallback_ForInvalidJson()
    {
        string input = "{ invalid json: test@example.com }"; // Missing quotes
        string result = _processor.SanitizeLine(input);
        
        // Should fallback to string replacement
        result.Should().NotContain("test@example.com");
        // Structure might be preserved or just string replaced.
        // Since it's not valid JSON, it gets treated as string.
    }

    public void Dispose()
    {
        _processor.Dispose();
    }
}
