using System.Text.RegularExpressions;

namespace LogSanitizer.Core.Constants;

public static class RegexDefinitions
{
    // IPv4: Matches standard IP addresses (e.g., 192.168.1.1) with 0-255 validation and boundary checks
    public static readonly Regex IPv4 = new Regex(
        @"(?<!\.)\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b(?!\.)",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Email: Standard email pattern
    public static readonly Regex Email = new Regex(
        @"\b[A-Za-z0-9._%+-]+@(?!\.)[A-Za-z0-9.-]+(?:\.[A-Za-z]{2,})?\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Credit Card: Basic 13-16 digit matching.
    // Refined to avoid simple 13-16 digit IDs by ensuring digit boundaries.
    public static readonly Regex CreditCard = new Regex(
        @"\b(?:\d[ -]*?){13,16}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        
    // Phone Number: Generic global phone match
    public static readonly Regex PhoneNumber = new Regex(
        @"(?<![A-Za-z0-9])(?:\+|00)?(?:\d{1,3}[\s\.\-])?(?:\(?\d{3}\)?[\s\.\-]\d{3}[\s\.\-]\d{4}|\(?\d{3}\)?[\s\.\-]\d{3}[\s\.\-]\d{2}[\s\.\-]\d{2})(?![A-Za-z0-9])",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // FQDN: Matches domain-like structures (e.g., server.internal.corp)
    public static readonly Regex FQDN = new Regex(
        @"\b(?!\d+\.\d+\.\d+\.\d+)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+(?!(?:zip|exe|dll|log|txt|png|core)\b)[a-zA-Z]{2,63}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    // Hostname: Matches words containing specific infrastructure keywords.
    // Logic: Must be alphanumeric (with hyphens) AND contain 'SW', 'SRV', 'SERVER'.
    public static readonly Regex Hostname = new Regex(
        @"\b[a-zA-Z0-9-]*(?:SW|SRV|SERVER)[a-zA-Z0-9-]*\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    // IPv6: Strict greedy match to consume the entire address, including optional scope ID (e.g. %14)
    public static readonly Regex IPv6 = new Regex(
        @"(?:[A-Fa-f0-9]{1,4}:){2,}(?:[A-Fa-f0-9]{1,4}|:)|fe80::[A-Fa-f0-9:%]+",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    // SSN: US Social Security Number (XXX-XX-XXXX)
    public static readonly Regex SSN = new Regex(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // IBAN: International Bank Account Number
    public static readonly Regex IBAN = new Regex(
        @"\b[A-Z]{2}\d{2}[A-Z0-9]{4}\d{7}([A-Z0-9]?){0,16}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // DomainUser: Matches DOMAIN\User format
    public static readonly Regex DomainUser = new Regex(
        @"\b[a-zA-Z0-9-]{2,15}\\{1,2}[a-zA-Z0-9._-]{2,30}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Username: Windows-style DOMAIN\User with stricter boundaries
    public static readonly Regex Username = new Regex(
        @"(?<=\s|^|\""|'|\\)[a-zA-Z0-9-]{2,15}\\[a-zA-Z0-9._-]{2,30}(?=\s|$|\""|'|\\|\])",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Certificate Thumbprint: SHA-1 Hex String (40 chars)
    public static readonly Regex CertificateThumbprint = new Regex(
        @"\b[0-9a-fA-F]{40}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Bearer Token: Matches "Bearer <token>"
    public static readonly Regex BearerToken = new Regex(
        @"\bBearer\s+[A-Za-z0-9\-\._~\+\/]+=*",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    // Connection String Password: Matches Password=..., Pwd=..., User ID=...
    public static readonly Regex ConnectionStringPassword = new Regex(
        @"(?<=(Password|Pwd|User ID|Uid)=)[^;]*",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

    public static readonly Regex IPv6TokenTrailing = new Regex(
        @"(?<=\[IP6\-[0-9A-F]{6}\])(?:%[0-9A-Za-z]+|:\d+)",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Path/WMI site code leaks: SMS_XXX or site_XXX
    public static readonly Regex PathWmiSite = new Regex(
        @"(SMS_|site_)([a-zA-Z0-9]{3})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Key-Value masking with optional brackets/tight spacing
    public static readonly Regex KeyValueKV = new Regex(
        @"(SiteCode|Site|DatabaseName|Database|Catalog|SQLServerName|Server|Source|SITE)(\s*\]?\s*[:=]\s*)([""'[]?)([^""'\s\],;]+)([""'\]]?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex LdapDomain = new Regex(
        @"DC=[a-zA-Z0-9-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex LdapCnOrDc = new Regex(
        @"\b(CN|OU|DC)=([^,]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Free-text site code example (heuristic)
    public static readonly Regex SiteCodeWord = new Regex(
        @"\bGYC\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex Url = new Regex(
        @"\b(https?|ftp)://([a-zA-Z0-9\.-]+)(:[0-9]+)?(/[^ \t\r\n]*)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex SqlConnectionKVGeneric = new Regex(
        @"(Data Source|Server|Initial Catalog|Database|User ID|Password)\s*=\s*([^;]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex CreditCardGeneric = new Regex(
        @"\b(?:\d{4}[- ]?){3}\d{4}\b",
        RegexOptions.Compiled);

    public static readonly Regex TurkishMobilePhone = new Regex(
        @"(?<![A-Za-z0-9])(?:\+90|0)\s*5\d{2}[\s\.\-]?\d{3}[\s\.\-]?\d{2}[\s\.\-]?\d{2}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex ApiKey = new Regex(
        @"\b((?:x-)?api-key)(\s*)([:=])(\s*)([A-Za-z0-9]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex TimestampHHMMSS = new Regex(
        @"^\d{2}:\d{2}:\d{2}$",
        RegexOptions.Compiled);
}
