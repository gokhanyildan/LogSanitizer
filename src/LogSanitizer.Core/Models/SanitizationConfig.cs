using LogSanitizer.Core.Enums;

namespace LogSanitizer.Core.Models;

public class SanitizationConfig
{
    // If true, the output file will overwrite the existing one (use with caution)
    public bool OverwriteOutput { get; set; } = false;

    // List of PII types to detect and sanitize
    public List<PiiType> TargetPiiTypes { get; set; } = new List<PiiType>();

    // String to use when masking (e.g., "***")
    public string MaskPlaceholder { get; set; } = "[REDACTED]";

    // If true, uses consistent hashing for traceability instead of simple masking
    public bool EnableHashing { get; set; } = true;

    // Factory method for default safe configuration
    public static SanitizationConfig Default => new SanitizationConfig
    {
        TargetPiiTypes = new List<PiiType> 
        { 
            PiiType.IPv4Address, 
            PiiType.Email 
        }
    };
}