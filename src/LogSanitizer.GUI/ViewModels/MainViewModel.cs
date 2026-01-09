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
    private bool _overwriteOutput;
    private bool _isAllRulesSelected;

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

    public bool OverwriteOutput
    {
        get => _overwriteOutput;
        set { _overwriteOutput = value; OnPropertyChanged(); }
    }

    public bool IsAllRulesSelected
    {
        get => _isAllRulesSelected;
        set 
        { 
            if (_isAllRulesSelected != value)
            {
                _isAllRulesSelected = value; 
                OnPropertyChanged();
                
                // Update all items
                foreach (var option in PiiOptions)
                {
                    option.IsSelected = value;
                }
            }
        }
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
    public ICommand BrowseInputFolderCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand SanitizeCommand { get; }
    public ICommand ClearListCommand { get; }

    public MainViewModel()
    {
        // Initialize Commands
        BrowseInputCommand = new RelayCommand(_ => BrowseInput());
        BrowseInputFolderCommand = new RelayCommand(_ => BrowseInputFolder());
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

    public void AddSourceFiles(IEnumerable<string> paths)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        { 
            ".log", ".txt", ".csv", ".json", ".xml", ".out", ".trace" 
        };

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                         .Where(f => allowedExtensions.Contains(Path.GetExtension(f)));

                    foreach (var file in files)
                    {
                        if (!SourceFiles.Contains(file))
                        {
                            SourceFiles.Add(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error scanning folder '{path}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (File.Exists(path))
            {
                if (!SourceFiles.Contains(path))
                {
                    SourceFiles.Add(path);
                }
            }
        }
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
            AddSourceFiles(dialog.FileNames);
        }
    }

    private void BrowseInputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder to Sanitize",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            AddSourceFiles(dialog.FolderNames);
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
        
        int totalItems = SourceFiles.Count;
        int processedCount = 0;
        var failedFiles = new List<string>();

        try
        {
            // Prepare Config
            var selectedTypes = PiiOptions.Where(x => x.IsSelected).Select(x => x.Type).ToList();
            var config = new SanitizationConfig
            {
                OverwriteOutput = OverwriteOutput, 
                TargetPiiTypes = selectedTypes,
                MaskPlaceholder = "***",
                Salt = Guid.NewGuid().ToString() // Unique salt per batch run
            };

            using var processor = new LogProcessor(config);

            foreach (var inputPath in SourceFiles)
            {
                try 
                {
                    if (Directory.Exists(inputPath))
                    {
                        // Folder Processing
                        string outputDir = string.IsNullOrWhiteSpace(OutputDirectory) 
                            ? inputPath + "_sanitized"
                            : OutputDirectory;

                        await processor.ProcessDirectoryAsync(inputPath, outputDir, null);
                    }
                    else if (File.Exists(inputPath))
                    {
                        // File Processing
                        string outputDir = string.IsNullOrWhiteSpace(OutputDirectory) 
                            ? Path.GetDirectoryName(inputPath)! 
                            : OutputDirectory;

                        string fileName;
                        if (OverwriteOutput)
                        {
                            fileName = Path.GetFileName(inputPath);
                        }
                        else
                        {
                            fileName = Path.GetFileNameWithoutExtension(inputPath) + "_sanitized" + Path.GetExtension(inputPath);
                        }
                        
                        string outputPath = Path.Combine(outputDir, fileName);

                        await processor.ProcessFileAsync(inputPath, outputPath, null);
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{Path.GetFileName(inputPath)}: {ex.Message}");
                }
                finally
                {
                    processedCount++;
                    ProgressValue = (double)processedCount / totalItems * 100;
                    StatusMessage = $"Processed {processedCount}/{totalItems}";
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
                MessageBox.Show("All items processed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
