using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using LogSanitizer.Core.Constants;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;

namespace LogSanitizer.Core.Services;

public class LogProcessor
{
    private readonly SanitizationConfig _config;
    private readonly Dictionary<PiiType, Regex> _activeRegexes;
    private readonly Dictionary<string, string> _hashCache = new();

    public LogProcessor(SanitizationConfig config)
    {
        _config = config;
        _activeRegexes = InitializeRegexMap();
    }

    private Dictionary<PiiType, Regex> InitializeRegexMap()
    {
        var map = new Dictionary<PiiType, Regex>();

        if (_config.TargetPiiTypes.Contains(PiiType.IPv4Address)) map.Add(PiiType.IPv4Address, RegexDefinitions.IPv4);
        if (_config.TargetPiiTypes.Contains(PiiType.IPv6Address)) map.Add(PiiType.IPv6Address, RegexDefinitions.IPv6);
        if (_config.TargetPiiTypes.Contains(PiiType.Email)) map.Add(PiiType.Email, RegexDefinitions.Email);
        if (_config.TargetPiiTypes.Contains(PiiType.CreditCard)) map.Add(PiiType.CreditCard, RegexDefinitions.CreditCard);
        if (_config.TargetPiiTypes.Contains(PiiType.SocialSecurityNumber)) map.Add(PiiType.SocialSecurityNumber, RegexDefinitions.SSN);
        if (_config.TargetPiiTypes.Contains(PiiType.PhoneNumber)) map.Add(PiiType.PhoneNumber, RegexDefinitions.PhoneNumber);
        if (_config.TargetPiiTypes.Contains(PiiType.IBAN)) map.Add(PiiType.IBAN, RegexDefinitions.IBAN);
        if (_config.TargetPiiTypes.Contains(PiiType.FQDN)) map.Add(PiiType.FQDN, RegexDefinitions.FQDN);
        if (_config.TargetPiiTypes.Contains(PiiType.Hostname)) map.Add(PiiType.Hostname, RegexDefinitions.Hostname);
        if (_config.TargetPiiTypes.Contains(PiiType.DomainUser)) map.Add(PiiType.DomainUser, RegexDefinitions.DomainUser);
        if (_config.TargetPiiTypes.Contains(PiiType.Username)) map.Add(PiiType.Username, RegexDefinitions.Username);
        
        return map;
    }

    public async Task ProcessFileAsync(string inputPath, string outputPath, IProgress<double>? progress = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input file not found: {inputPath}");

        string fullInputPath = Path.GetFullPath(inputPath);
        string fullOutputPath = Path.GetFullPath(outputPath);

        if (string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Input and output paths cannot be the same file. Please specify a different output path.");

        if (File.Exists(outputPath) && !_config.OverwriteOutput)
            throw new IOException($"Output file already exists: {outputPath}");

        long totalBytes = new FileInfo(inputPath).Length;
        long processedBytes = 0;

        using (var reader = new StreamReader(inputPath))
        using (var writer = new StreamWriter(outputPath))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                string sanitizedLine = SanitizeLine(line);
                await writer.WriteLineAsync(sanitizedLine);

                if (progress != null)
                {
                    processedBytes += line.Length + Environment.NewLine.Length;
                    double percent = (double)processedBytes / totalBytes * 100;
                    progress.Report(percent);
                }
            }
        }
    }

    private string SanitizeLine(string line)
    {
        string currentLine = line;

        foreach (var entry in _activeRegexes)
        {
            if (_config.EnableHashing)
            {
                currentLine = entry.Value.Replace(currentLine, match => GetConsistentToken(entry.Key, match.Value));
            }
            else
            {
                currentLine = entry.Value.Replace(currentLine, _config.MaskPlaceholder);
            }
        }

        return currentLine;
    }

    private string GetConsistentToken(PiiType type, string input)
    {
        if (_hashCache.TryGetValue(input, out var cachedHash))
        {
            return cachedHash;
        }

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            var code = GetTypeCode(type);
            var token = $"[{code}-{sb}]";
            _hashCache[input] = token;
            return token;
        }
    }

    private static string GetTypeCode(PiiType type) => type switch
    {
        PiiType.IPv4Address => "IP4",
        PiiType.IPv6Address => "IP6",
        PiiType.Email => "EML",
        PiiType.CreditCard => "CC",
        PiiType.SocialSecurityNumber => "SSN",
        PiiType.PhoneNumber => "PHN",
        PiiType.IBAN => "IBAN",
        PiiType.Hostname => "HOST",
        PiiType.FQDN => "FQDN",
        PiiType.DomainUser => "USR",
        PiiType.Username => "USR",
        _ => "ID"
    };
}
