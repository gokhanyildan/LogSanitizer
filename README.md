# LogSanitizer

## Description
A GDPR-compliant log anonymization tool with Consistent Hashing. This tool allows you to sanitize sensitive information from log files while maintaining traceability through consistent hashing of specific data types.

## Features
- **Supported PII Types**: 
  - IPv4 & IPv6 Addresses
  - Email Addresses
  - Credit Card Numbers
  - Social Security Numbers (SSN)
  - International Bank Account Numbers (IBAN)
  - Domain\User Accounts
  - Phone Numbers
  - Hostnames & FQDNs
- **Consistent Hashing**: Enables traceability for Users and IPs without revealing identity (SHA256 truncated).
- **Dual Interface**: Available as both a Command Line Interface (CLI) and a Graphical User Interface (GUI).

## Usage (CLI)

Run the tool from the command line using the following arguments:

```powershell
# Basic usage (defaults to IPv4, Email, CreditCard)
LogSanitizer.CLI.exe --input "C:\logs\app.log" --output "C:\logs\app_clean.log"

# Specify targets and overwrite existing file
LogSanitizer.CLI.exe --input "C:\logs\app.log" --output "C:\logs\app_clean.log" --overwrite --targets IPv4Address IPv6Address Email DomainUser
```

## Usage (GUI)

The WPF-based GUI provides a user-friendly way to sanitize logs:
1. **Input/Output**: Browse to select your source log file and destination path.
2. **Options**: Check the boxes for the PII types you wish to sanitize (e.g., Email, DomainUser).
3. **Start**: Click "Start Sanitization" to process the file. A progress bar will show the status.

## Build Instructions

To build the project from source, ensure you have the .NET 8 SDK installed and run:

```powershell
dotnet build
```

To create a release build (single-file executable), run the provided `publish.ps1` script.
