🛡️ LogSanitizer
A log pseudonymization tool designed to assist with GDPR & KVKK data privacy requirements.

LogSanitizer helps System Engineers, DevOps professionals, and Developers to sanitize sensitive information (PII) from log files while maintaining data traceability. Unlike simple redaction tools that replace data with ***, LogSanitizer replaces sensitive entities with consistent, unique tokens (e.g., [USR-A1B2C3]). This allows for secure log analysis and correlation without exposing raw user data, aligning with data minimization principles.

🚀 Features
Consistent Pseudonymization: Uses SHA256 (truncated) to hash data. The same User/IP always gets the same token within the session, preserving the context for debugging.

Real: gokhan.yildan@example.com -> Sanitized: [EML-7F3A91]

Dual Interface:

GUI: Modern WPF interface for quick, drag-and-drop operations.

CLI: Command-line tool for automation and CI/CD pipelines.

Comprehensive PII Detection:

✅ IPv4 & IPv6 Addresses

✅ Email Addresses

✅ Domain\User Accounts (e.g., CORP\jdoe, JSON compatible)

✅ Credit Card Numbers (Luhn check compliant)

✅ IBAN & SSN

✅ Phone Numbers

Performance: Optimized for large log files using stream processing.

📥 Installation
You don't need to build from source. Download the latest portable executable from the [şüpheli bağlantı kaldırıldı].

Download LogSanitizer.zip.

Extract the files.

Run LogSanitizer.GUI.exe.

💻 Usage (CLI)
Ideal for batch scripts or automated workflows.

PowerShell

# Basic usage (Sanitizes using default rules)
.\LogSanitizer.CLI.exe --input "C:\logs\server.log"

# Specify output file
.\LogSanitizer.CLI.exe --input "C:\logs\server.log" --output "C:\logs\server_clean.log"

# Custom target selection and overwrite mode
.\LogSanitizer.CLI.exe --input "C:\logs\app.log" --overwrite --targets IPv4Address Email DomainUser CreditCard
Available Targets: IPv4Address, IPv6Address, Email, CreditCard, SocialSecurityNumber, PhoneNumber, IBAN, DomainUser.

🖥️ Usage (GUI)
Launch LogSanitizer.GUI.exe.

Add Files: Click "Add Files" or drag & drop your log files into the source area.

Select Rules: Check the PII types you want to anonymize on the right panel.

Process: Click Start Batch. The tool will generate _sanitized files in the same directory (or your chosen output folder).

🛠️ Build from Source
Requirements: .NET 8.0 SDK

PowerShell

# Clone the repository
git clone https://github.com/gokhanyildan/LogSanitizer.git

# Navigate to the project
cd LogSanitizer

# Build the solution
dotnet build

# Create single-file executables (Publish)
dotnet publish src/LogSanitizer.GUI/LogSanitizer.GUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
⚖️ License
Distributed under the MIT License. See LICENSE for more information.