namespace LogSanitizer.Core.Enums;

public enum PiiType
{
    IPv4Address,
    IPv6Address,
    Email,
    CreditCard,
    SocialSecurityNumber,
    PhoneNumber,
    IBAN,
    Hostname, // NetBIOS Name (e.g., SERVER01)
    FQDN,      // Fully Qualified Domain Name (e.g., server01.corp.local)
    DomainUser, // Domain\User format
    Username,
    CertificateThumbprint, // SHA-1 Thumbprint (40 hex chars)
    BearerToken,
    ConnectionStringPassword
}
