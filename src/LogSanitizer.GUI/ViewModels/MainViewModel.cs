using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LogSanitizer.Core.Enums;
using LogSanitizer.Core.Models;
using LogSanitizer.Core.Services;
using Microsoft.Win32;

namespace LogSanitizer.GUI.ViewModels;

// Wrapper class to handle Checkbox selection for each PiiType
public class PiiTypeSelection : INotifyPropertyChanged
{
    public PiiType Type { get; set; }
    public string Name => Type.ToString();
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class MainViewModel : INotifyPropertyChanged
{
    // Properties tied to the UI
    private ObservableCollection<string> _sourceFiles = new();
    private string _outputDirectory = string.Empty;
    private double _progressValue;
    private string _statusMessage = "Ready";
    private bool _isBusy;

    public ObservableCollection<string> SourceFiles
    {
        get => _sourceFiles;
        set { _sourceFiles = value; OnPropertyChanged(); }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set { _outputDirectory = value; OnPropertyChanged(); }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    // List of Checkboxes for UI
    public ObservableCollection<PiiTypeSelection> PiiOptions { get; set; }

    // Commands
    public ICommand BrowseInputCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand SanitizeCommand { get; }
    public ICommand ClearListCommand { get; }

    public MainViewModel()
    {
        // Initialize Commands
        BrowseInputCommand = new RelayCommand(_ => BrowseInput());
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        ClearListCommand = new RelayCommand(_ => SourceFiles.Clear());
        SanitizeCommand = new RelayCommand(async _ => await ExecuteSanitizationAsync(), _ => CanSanitize());

        // Initialize PII Options (Defaulting generic ones to true)
        PiiOptions = new ObservableCollection<PiiTypeSelection>(
            Enum.GetValues<PiiType>().Select(t => new PiiTypeSelection 
            { 
                Type = t, 
                IsSelected = (t == PiiType.IPv4Address || t == PiiType.Email || t == PiiType.CreditCard || t == PiiType.Username) 
            })
        );
    }

    private void BrowseInput()
    {
        var dialog = new OpenFileDialog 
        { 
            Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!SourceFiles.Contains(file))
                {
                    SourceFiles.Add(file);
                }
            }
        }
    }

    private void BrowseOutput()
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    private bool CanSanitize()
    {
        // Button is enabled only if not busy, we have files, and at least one type is selected
        return !IsBusy && 
               SourceFiles.Any() && 
               PiiOptions.Any(x => x.IsSelected);
    }

    private async Task ExecuteSanitizationAsync()
    {
        IsBusy = true;
        StatusMessage = "Processing Batch...";
        ProgressValue = 0;
        
        int totalFiles = SourceFiles.Count;
        int processedCount = 0;
        var failedFiles = new List<string>();

        try
        {
            // Prepare Config
            var selectedTypes = PiiOptions.Where(x => x.IsSelected).Select(x => x.Type).ToList();
            var config = new SanitizationConfig
            {
                OverwriteOutput = true, 
                TargetPiiTypes = selectedTypes,
                MaskPlaceholder = "***"
            };

            var processor = new LogProcessor(config);

            foreach (var inputFile in SourceFiles)
            {
                try 
                {
                    // Determine output path
                    string outputDir = string.IsNullOrWhiteSpace(OutputDirectory) 
                        ? Path.GetDirectoryName(inputFile)! 
                        : OutputDirectory;

                    string fileName = Path.GetFileNameWithoutExtension(inputFile) + "_sanitized" + Path.GetExtension(inputFile);
                    string outputPath = Path.Combine(outputDir, fileName);

                    // Process single file
                    // We can't easily map byte progress to total batch progress without pre-scanning all file sizes.
                    // So we'll just update progress per file completion for now, or maybe a "fake" progress per file.
                    // Let's use a sub-progress approach if we really wanted smooth bars, but per-file is safer for batch.
                    
                    await processor.ProcessFileAsync(inputFile, outputPath, null);
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{Path.GetFileName(inputFile)}: {ex.Message}");
                }
                finally
                {
                    processedCount++;
                    ProgressValue = (double)processedCount / totalFiles * 100;
                    StatusMessage = $"Processed {processedCount}/{totalFiles}";
                }
            }

            if (failedFiles.Any())
            {
                StatusMessage = "Completed with Errors.";
                string errors = string.Join("\n", failedFiles.Take(10)); // Limit error msg size
                if (failedFiles.Count > 10) errors += "\n...";
                
                MessageBox.Show($"Batch completed with {failedFiles.Count} errors:\n\n{errors}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                StatusMessage = "Batch Completed Successfully!";
                MessageBox.Show("All files processed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Critical Error.";
            MessageBox.Show($"Critical Error: {ex.Message}", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
