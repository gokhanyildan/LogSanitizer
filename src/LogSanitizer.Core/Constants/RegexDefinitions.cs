using System.Text.RegularExpressions;

namespace LogSanitizer.Core.Constants;

public static class RegexDefinitions
{
    // IPv4: Matches standard IP addresses (e.g., 192.168.1.1)
    public static readonly Regex IPv4 = new Regex(
        @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Email: Standard email pattern
    public static readonly Regex Email = new Regex(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    // Credit Card: Basic 13-16 digit matching.
    // Refined to avoid simple 13-16 digit IDs by ensuring digit boundaries.
    public static readonly Regex CreditCard = new Regex(
        @"\b(?:\d[ -]*?){13,16}\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        
    // Phone Number: Generic global phone match
    public static readonly Regex PhoneNumber = new Regex(
        @"(?<!\d)(?:\+?\d{1,3}[- .]?)?\(?\d{3}\)?[- .]?\d{3}[- .]?\d{4}(?!\d)",
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

    // IPv6: Matches standard IPv6 addresses
    public static readonly Regex IPv6 = new Regex(
        @"\b(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\b|(?=(?:[0-9a-fA-F]{0,4}:){0,7}[0-9a-fA-F]{0,4}\b)(([0-9a-fA-F]{1,4}:){1,7}:|:(:[0-9a-fA-F]{1,4}){1,7})\b",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
}
