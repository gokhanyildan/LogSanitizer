using System.CommandLine;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;

namespace LogSanitizer.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Define CLI Options
        
        // Input file path
        var inputOption = new Option<FileInfo>(
            name: "--input",
            description: "The path to the source log file.")
            { IsRequired = true };

        // Output file path
        var outputOption = new Option<FileInfo>(
            name: "--output",
            description: "The path where the sanitized file will be saved.")
            { IsRequired = true };

        // Overwrite permission
        var overwriteOption = new Option<bool>(
            name: "--overwrite",
            description: "Overwrite the output file if it exists.",
            getDefaultValue: () => false);

        // Target PII types (allows selecting what to clean)
        // Usage example: --targets Email IPv4Address
        var targetsOption = new Option<List<PiiType>>(
            name: "--targets",
            description: "List of PII types to sanitize.",
            getDefaultValue: () => new List<PiiType> { PiiType.IPv4Address, PiiType.Email, PiiType.CreditCard });

        // 2. Create Root Command
        var rootCommand = new RootCommand("LogSanitizer: GDPR Compliant Log Anonymization Tool");
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(overwriteOption);
        rootCommand.AddOption(targetsOption);

        // 3. Define the Handler (The glue between CLI args and Core Logic)
        rootCommand.SetHandler(async (input, output, overwrite, targets) =>
        {
            await RunSanitizationAsync(input, output, overwrite, targets);
        }, inputOption, outputOption, overwriteOption, targetsOption);

        // 4. Execute
        return await rootCommand.InvokeAsync(args);
    }

    private static async Task RunSanitizationAsync(FileInfo input, FileInfo output, bool overwrite, List<PiiType> targets)
    {
        Console.WriteLine($"Starting sanitization...");
        Console.WriteLine($"Source: {input.FullName}");
        Console.WriteLine($"Target: {output.FullName}");
        
        // Configuration setup
        var config = new SanitizationConfig
        {
            OverwriteOutput = overwrite,
            TargetPiiTypes = targets,
            MaskPlaceholder = "***" // Default mask
        };

        var processor = new LogProcessor(config);

        try
        {
            // Simple text-based progress indicator
            var progress = new Progress<double>(percent =>
            {
                // \r allows overwriting the same line
                Console.Write($"\rProgress: {percent:F1}%");
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Start the actual processing
            await processor.ProcessFileAsync(input.FullName, output.FullName, progress);
            
            stopwatch.Stop();
            Console.WriteLine($"\n\nDone! Processed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            Console.WriteLine($"Check output at: {output.FullName}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\nERROR: {ex.Message}");
            Console.ResetColor();
        }
    }
}