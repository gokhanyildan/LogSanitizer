using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using LogSanitizer.Core.Constants;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;

namespace LogSanitizer.Core.Services;

public class LogProcessor : IDisposable
{
    private readonly SanitizationConfig _config;
    private readonly Dictionary<PiiType, Regex> _activeRegexes;
    // Cache must be thread-safe now if we share the processor instance
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _hashCache = new();
    private readonly ThreadLocal<SHA256> _sha256;

    public LogProcessor(SanitizationConfig config)
    {
        _config = config;
        _activeRegexes = InitializeRegexMap();
        _sha256 = new ThreadLocal<SHA256>(SHA256.Create);
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
        if (_config.TargetPiiTypes.Contains(PiiType.CertificateThumbprint)) map.Add(PiiType.CertificateThumbprint, RegexDefinitions.CertificateThumbprint);
        if (_config.TargetPiiTypes.Contains(PiiType.BearerToken)) map.Add(PiiType.BearerToken, RegexDefinitions.BearerToken);
        if (_config.TargetPiiTypes.Contains(PiiType.ConnectionStringPassword)) map.Add(PiiType.ConnectionStringPassword, RegexDefinitions.ConnectionStringPassword);
        
        return map;
    }

    public async Task ProcessFileAsync(string inputPath, string outputPath, IProgress<double>? progress = null)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input file not found: {inputPath}");

        string fullInputPath = Path.GetFullPath(inputPath);
        string fullOutputPath = Path.GetFullPath(outputPath);
        
        // If paths are same and overwrite is disabled, error out.
        if (string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase) && !_config.OverwriteOutput)
            throw new IOException("Input and output paths are the same. Please enable overwrite mode or specify a different output path.");

        if (File.Exists(outputPath) && !_config.OverwriteOutput)
            throw new IOException($"Output file already exists: {outputPath}");

        // If overwrite enabled and paths match, use temp file
        string targetPath = fullOutputPath;
        string? tempPath = null;
        if (_config.OverwriteOutput && string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            tempPath = Path.Combine(Path.GetDirectoryName(fullOutputPath)!, $"temp_{Guid.NewGuid()}{Path.GetExtension(fullOutputPath)}");
            targetPath = tempPath;
        }

        long totalBytes = new FileInfo(inputPath).Length;
        long processedBytes = 0;

        try 
        {
            using (var reader = new StreamReader(inputPath))
            using (var writer = new StreamWriter(targetPath))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string sanitizedLine = SanitizeLine(line);
                    await writer.WriteLineAsync(sanitizedLine);

                    if (progress != null)
                    {
                        processedBytes += line.Length + Environment.NewLine.Length;
                        double percent = Math.Min(100.0, (double)processedBytes / totalBytes * 100);
                        progress.Report(percent);
                    }
                }
            }

            // If we used a temp file, replace original now
            if (tempPath != null)
            {
                File.Move(tempPath, fullOutputPath, overwrite: true);
            }
        }
        catch
        {
            // Cleanup temp file on failure
            if (tempPath != null && File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }

    public async Task ProcessDirectoryAsync(string inputDir, string outputDir, IProgress<double>? progress = null)
    {
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Input directory not found: {inputDir}");

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // Get all files
        var allFiles = Directory.GetFiles(inputDir, "*.*", SearchOption.TopDirectoryOnly);
        
        // Filter by extension
        var allowedExtensions = _config.AllowedExtensions
            .Select(e => e.Trim().ToLower())
            .ToHashSet();

        var filesToProcess = allFiles
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .Where(f => !Path.GetFileName(f).EndsWith("_sanitized.log", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        int totalFiles = filesToProcess.Length;
        int processedFiles = 0;

        // Parallel processing
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        await Parallel.ForEachAsync(filesToProcess, options, async (file, token) =>
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string outputPath = Path.Combine(outputDir, fileName);
                
                // We don't report byte-level progress for parallel batch to keep UI responsive
                // Reporting per-file completion is sufficient
                await ProcessFileAsync(file, outputPath, null);
                
                // Atomic increment for thread safety
                int currentCount = Interlocked.Increment(ref processedFiles);
                
                if (progress != null)
                {
                    progress.Report((double)currentCount / totalFiles * 100);
                }
            }
            catch (Exception)
            {
                // In batch mode, we might want to log failures but continue?
                // The current design lets exception bubble up if not handled in caller.
                // But generally parallel loop should aggregation exceptions.
                // For now, let's allow it to bubble up to preserve original behavior.
                throw;
            }
        });
    }

    public string SanitizeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return line;

        // JSON Detection & Sanitization
        if (_config.DetectJson && line.TrimStart().StartsWith("{") && line.TrimEnd().EndsWith("}"))
        {
            try
            {
                // Try parsing as JSON
                // Using System.Text.Json.Nodes to traverse efficiently without POCOs
                var options = new System.Text.Json.Nodes.JsonNodeOptions { PropertyNameCaseInsensitive = true };
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(line, options);

                if (jsonNode != null)
                {
                    SanitizeJsonStructure(jsonNode);
                    return jsonNode.ToJsonString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Fallback to simple string replacement if parsing fails
            }
        }

        return SanitizeContent(line);
    }

    // Better recursive approach: Traverse and replace at property/index level
    private void SanitizeJsonStructure(System.Text.Json.Nodes.JsonNode? node)
    {
        if (node is System.Text.Json.Nodes.JsonObject obj)
        {
            // Iterate properties and replace values if needed
            foreach (var kvp in obj.ToList())
            {
                if (kvp.Value is System.Text.Json.Nodes.JsonValue val && val.TryGetValue(out string? strVal))
                {
                    var sanitized = SanitizeContent(strVal);
                    if (sanitized != strVal)
                    {
                        // Explicit creation of JsonValue is required
                        obj[kvp.Key] = System.Text.Json.Nodes.JsonValue.Create(sanitized);
                    }
                }
                else
                {
                    SanitizeJsonStructure(kvp.Value);
                }
            }
        }
        else if (node is System.Text.Json.Nodes.JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is System.Text.Json.Nodes.JsonValue val && val.TryGetValue(out string? strVal))
                {
                    var sanitized = SanitizeContent(strVal);
                    if (sanitized != strVal)
                    {
                        // Explicit creation of JsonValue is required
                        arr[i] = System.Text.Json.Nodes.JsonValue.Create(sanitized);
                    }
                }
                else
                {
                    SanitizeJsonStructure(arr[i]);
                }
            }
        }
    }

    private string SanitizeContent(string input)
    {
        string current = input;

        current = RegexDefinitions.IPv6.Replace(current, m =>
        {
            var val = m.Value;
            if (RegexDefinitions.TimestampHHMMSS.IsMatch(val)) return val;
            if (val.Length <= 5 || !val.Contains(':')) return val;
            return GetConsistentToken(PiiType.IPv6Address, val);
        });

        current = RegexDefinitions.PathWmiSite.Replace(current, m =>
        {
            var prefix = m.Groups[1].Value;
            var code = m.Groups[2].Value;
            var token = GetKeyToken("SITE", code);
            return $"{prefix}{token}";
        });

        current = RegexDefinitions.LdapDomain.Replace(current, "DC=[DOMAIN]");

        current = RegexDefinitions.KeyValueKV.Replace(current, m =>
        {
            var key = m.Groups[1].Value;
            var value = m.Groups[4].Value;
            var prefix = key.Equals("SiteCode", StringComparison.OrdinalIgnoreCase) || key.Equals("SITE", StringComparison.OrdinalIgnoreCase)
                ? "SITE"
                : key.Equals("DatabaseName", StringComparison.OrdinalIgnoreCase)
                    ? "DB"
                    : "SRV";
            var token = GetKeyToken(prefix, value);
            var tokenOut = (m.Groups[3].Length > 0 || m.Groups[5].Length > 0) ? token.Trim('[', ']') : token;
            return $"{m.Groups[1].Value}{m.Groups[2].Value}{m.Groups[3].Value}{tokenOut}{m.Groups[5].Value}";
        });

        current = RegexDefinitions.LdapCnOrDc.Replace(current, m =>
        {
            var part = m.Groups[1].Value;
            var value = m.Groups[2].Value;
            var token = GetKeyToken("DN", value);
            return $"{part}={token}";
        });

        current = RegexDefinitions.SiteCodeWord.Replace(current, "[SITE-CODE]");

        foreach (var entry in _activeRegexes.Where(kv => kv.Key is not PiiType.Hostname and not PiiType.FQDN and not PiiType.DomainUser and not PiiType.Username and not PiiType.IPv6Address))
        {
            current = entry.Value.Replace(current, match =>
                _config.EnableHashing ? GetConsistentToken(entry.Key, match.Value) : _config.MaskPlaceholder);
        }

        foreach (var type in new[] { PiiType.DomainUser, PiiType.Username, PiiType.Hostname, PiiType.FQDN })
        {
            if (!_activeRegexes.TryGetValue(type, out var rx)) continue;
            current = rx.Replace(current, match =>
            {
                var value = match.Value;
                if (value.Contains("[")) return value;
                var tokens = value.Split(new[] { ' ', '\t', '\r', '\n', '\\', '/', '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Any(t =>
                {
                    var cleaned = Regex.Replace(t, @"[^\w]", "");
                    return _allowlist.Contains(cleaned);
                })) return value;
                return _config.EnableHashing ? GetConsistentToken(type, value) : _config.MaskPlaceholder;
            });
        }

        current = RegexDefinitions.IPv6TokenTrailing.Replace(current, "");
        return current;
    }

    private string GetConsistentToken(PiiType type, string input)
    {
        if (_hashCache.TryGetValue(input, out var cachedHash))
        {
            return cachedHash;
        }

        // Salted Hash
        var saltedInput = input + (_config.Salt ?? "");
        var bytes = Encoding.UTF8.GetBytes(saltedInput);
        
        // Use ThreadLocal SHA256 instance
        var sha = _sha256.Value!;
        var hashBytes = sha.ComputeHash(bytes);
        
        var sb = new StringBuilder();
        // Taking first 3 bytes (6 hex chars) for brevity
        for (int i = 0; i < 3; i++)
        {
            sb.Append(hashBytes[i].ToString("X2"));
        }
        var code = GetTypeCode(type);
        var token = $"[{code}-{sb}]";
        
        // ConcurrentDictionary usage
        _hashCache[input] = token;
        return token;
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
        PiiType.CertificateThumbprint => "THUMB",
        PiiType.BearerToken => "TKN",
        PiiType.ConnectionStringPassword => "SEC",
        _ => "ID"
    };

    private static readonly HashSet<string> _allowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bin","Setup","Bgb","Msi","Exec","Network","Authority","System","Local","Service","Program","Files","Microsoft","Windows","CCM","SMS","Site","Code"
    };

    private string GetKeyToken(string prefix, string value)
    {
        var cacheKey = $"{prefix}|{value}";
        if (_hashCache.TryGetValue(cacheKey, out var cached))
            return cached;
        var bytes = Encoding.UTF8.GetBytes(value + (_config.Salt ?? ""));
        var sha = _sha256.Value!;
        var hashBytes = sha.ComputeHash(bytes);
        var sb = new StringBuilder();
        for (int i = 0; i < 3; i++) sb.Append(hashBytes[i].ToString("X2"));
        var token = $"[{prefix}-{sb}]";
        _hashCache[cacheKey] = token;
        return token;
    }

    private static bool ShouldMaskHost(string token)
    {
        if (token.Contains('.')) return true;
        return Regex.IsMatch(token, @"(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9-]{4,}");
    }

    public void Dispose()
    {
        _sha256.Dispose();
        // Note: ThreadLocal.Dispose will dispose the ThreadLocal itself.
        // It does NOT automatically dispose the values on other threads. 
        // But for SHA256 which is a managed wrapper around unmanaged resources, letting GC handle it is often acceptable if explicit cleanup is hard.
        // Ideally we track all instances, but ThreadLocal doesn't expose them easily.
    }
}
