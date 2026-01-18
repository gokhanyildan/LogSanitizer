# Log Sanitizer v3.3 🛡️

**The "Zero-Leak" Log Anonymization Tool for Enterprise Environments.**

Log Sanitizer is a sophisticated, context-aware cleaning tool designed to anonymize sensitive log data (SCCM, IIS, SQL, Custom Apps) while preserving structural integrity for troubleshooting. Unlike simple "find & replace" tools, it uses Deterministic Hashing to ensure that the same entity (e.g., an IP address or User ID) receives the same masked value across multiple files, allowing for correlation without data exposure.

---

## 🚀 Key Features

### 1. 🧠 Context-Aware Anonymization
- Intelligent Scrubbing: Distinguishes between version numbers (`5.00.9088`) and IP addresses (`192.168.1.10`).
- Protocol Smart: Recognizes URLs, SQL Connection Strings, and LDAP paths to mask only the sensitive parts (Hostnames, Credentials) without breaking the log format.

### 2. 🛡️ PII & Secrets Shield
- PII Protection: Auto-detects and hashes Emails (including intranet `user@internal`), Phone Numbers (Global/Local formats), and Credit Card numbers.
- Secrets Guard: Aggressively hunts down and masks Bearer Tokens (`Bearer [TOKEN-HASH]`) and API Keys (`x-api-key: [APIKEY-HASH]`).

### 3. 🔍 Traceability (Deterministic Hashing)
- Consistent Masking: `192.168.1.50` will ALWAYS become `[IP4-A1B2C3]` across all processed files.
- Debugging Ready: Allows support teams to correlate events (e.g., "This User [USR-X] caused error on Server [SRV-Y]") without knowing the real identity.

### 4. 🏢 Enterprise Hardened
- Global Domain Scrub: Instantly removes target domain names (e.g., `company.com`) from FQDNs.
- Site Code Protection: Specifically designed to handle SCCM Site Codes (e.g., `GYC`) in free-text, WMI, and Paths.

---

## ⚙️ How It Works (The Sanitization Logic)

The engine applies 8 strict layers of sanitization in a specific order to ensure zero leakage:

1. Global Domain Scrub: Removes the target domain (`gokhanyildan.com`) immediately.
2. Allowlist Check: Protects system terms (e.g., "Configuration Manager", "System32") from being masked.
3. Secrets Detection: Masks Bearer Tokens and API Keys.
4. Smart PII & URL: Handles Emails, Phones, Credit Cards, and extracts/masks Hostnames from URLs.
5. Network Masking: Masks IPv4 (ignoring versions) and IPv6 addresses.
6. Infrastructure Masking: Handles LDAP Distinguished Names (DN), UNC Paths, and SQL Strings.
7. General Entities: Masks remaining Users, Hosts, and Site Codes using heuristic patterns.

---

## 📦 Installation & Usage

Requirements: .NET 8.0 SDK

1. Clone the repository:

```bash
git clone https://github.com/gokhanyildan/LogSanitizer.git
cd LogSanitizer
```

2. Build the solution:

```bash
dotnet build
```

3. Run the tool:

- GUI:

```bash
dotnet run --project src/LogSanitizer.GUI/LogSanitizer.GUI.csproj
```

- CLI:

```bash
dotnet run --project src/LogSanitizer.CLI/LogSanitizer.CLI.csproj -- --input "C:\\logs\\server.log" --output "C:\\logs\\server_sanitized.log"
```

Select your log folder and let the tool handle the rest.

---

## 🔒 Security Guarantee

- Zero-Leak Verified: Tested against 120+ edge cases including mixed log formats, embedded JSON/XML, and complex connection strings.
- Safe Saving: Never overwrites original logs. Creates `_sanitized.log` files automatically.

---

Version: v3.3
