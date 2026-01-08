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
        
        // Input path (file or directory)
        var inputOption = new Option<string>(
            name: "--input",
            description: "The path to the source log file or directory.")
            { IsRequired = true };

        // Output path (file or directory)
        var outputOption = new Option<string>(
            name: "--output",
            description: "The path where the sanitized content will be saved.")
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

    private static async Task RunSanitizationAsync(string input, string output, bool overwrite, List<PiiType> targets)
    {
        Console.WriteLine($"Starting sanitization...");
        Console.WriteLine($"Source: {input}");
        Console.WriteLine($"Target: {output}");
        
        // Configuration setup
        var config = new SanitizationConfig
        {
            OverwriteOutput = overwrite,
            TargetPiiTypes = targets,
            MaskPlaceholder = "***", // Default mask
            Salt = Guid.NewGuid().ToString() // Generate a random salt for this run
        };

        using var processor = new LogProcessor(config);

        try
        {
            // Simple text-based progress indicator
            var progress = new Progress<double>(percent =>
            {
                // \r allows overwriting the same line
                Console.Write($"\rProgress: {percent:F1}%");
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Determine if input is file or directory
            if (File.Exists(input))
            {
                 // Processing single file
                 await processor.ProcessFileAsync(input, output, progress);
            }
            else if (Directory.Exists(input))
            {
                Console.WriteLine("Mode: Batch Directory Processing");
                // Processing directory
                await processor.ProcessDirectoryAsync(input, output, progress);
            }
            else
            {
                throw new FileNotFoundException($"Input path not found: {input}");
            }
            
            stopwatch.Stop();
            Console.WriteLine($"\n\nDone! Processed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            Console.WriteLine($"Check output at: {output}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\nERROR: {ex.Message}");
            Console.ResetColor();
        }
    }
}