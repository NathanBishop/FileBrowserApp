using System.Collections.ObjectModel;
using System.IO;
using FileBrowserApp.Models;
using FileBrowserApp.Services;

namespace FileBrowserApp.ViewModels;

/// <summary>
/// Coordinates navigation and the storage breakdown. Owns no filesystem or categorization
/// logic itself — it only calls into <see cref="FileSystemService"/> and
/// <see cref="StorageAnalyzer"/> and exposes the results for binding.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly FileSystemService _fileSystemService = new();

    private CancellationTokenSource? _navigationCts;
    private CancellationTokenSource? _scanCts;

    private string? _currentPath;
    private string? _statusMessage;
    private bool _isLoading;
    private bool _isScanning;
    private bool _includeSubfolders;
    private IReadOnlyList<CategoryTotal> _categoryTotals = Array.Empty<CategoryTotal>();
    private long _totalBytesInView;

    public ObservableCollection<FileSystemEntry> Entries { get; } = new();

    public string? CurrentPath
    {
        get => _currentPath;
        private set
        {
            if (SetProperty(ref _currentPath, value))
                OnPropertyChanged(nameof(DisplayPath));
        }
    }

    public string DisplayPath => CurrentPath ?? "This PC";

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set => SetProperty(ref _isScanning, value);
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set
        {
            if (SetProperty(ref _includeSubfolders, value))
                _ = RecalculateStorageAsync();
        }
    }

    public IReadOnlyList<CategoryTotal> CategoryTotals
    {
        get => _categoryTotals;
        private set => SetProperty(ref _categoryTotals, value);
    }

    public long TotalBytesInView
    {
        get => _totalBytesInView;
        private set => SetProperty(ref _totalBytesInView, value);
    }

    public RelayCommand OpenEntryCommand { get; }
    public RelayCommand NavigateUpCommand { get; }
    public RelayCommand GoHomeCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand CancelScanCommand { get; }

    public MainViewModel()
    {
        OpenEntryCommand = new RelayCommand(param =>
        {
            if (param is FileSystemEntry { IsDirectory: true } entry)
                _ = NavigateToAsync(entry.FullPath);
        });

        NavigateUpCommand = new RelayCommand(_ => _ = NavigateUpAsync(), _ => CurrentPath is not null);
        GoHomeCommand = new RelayCommand(_ => _ = ShowDrivesAsync());
        RefreshCommand = new RelayCommand(_ =>
        {
            if (CurrentPath is null)
                _ = ShowDrivesAsync();
            else
                _ = NavigateToAsync(CurrentPath);
        });
        CancelScanCommand = new RelayCommand(_ => CancelScan());
    }

    /// <summary>Stops an in-progress recursive scan and falls back to the current folder's
    /// already-loaded, non-recursive breakdown instead of leaving the user stuck waiting.</summary>
    private void CancelScan()
    {
        if (!IsScanning)
            return;

        StatusMessage = "Recursive scan cancelled — showing this folder's files only.";
        IncludeSubfolders = false;
    }

    public Task InitializeAsync() => ShowDrivesAsync();

    private async Task ShowDrivesAsync()
    {
        _navigationCts?.Cancel();
        IsLoading = true;
        StatusMessage = null;
        CurrentPath = null;

        try
        {
            var drives = await Task.Run(_fileSystemService.GetDrives);
            ReplaceEntries(drives);
        }
        finally
        {
            IsLoading = false;
        }

        await RecalculateStorageAsync();
    }

    public async Task NavigateToAsync(string path)
    {
        _navigationCts?.Cancel();
        var cts = new CancellationTokenSource();
        _navigationCts = cts;

        IsLoading = true;
        StatusMessage = null;

        try
        {
            var listing = await _fileSystemService.ListDirectoryAsync(path, cts.Token);
            if (cts.Token.IsCancellationRequested)
                return;

            CurrentPath = listing.Path;
            ReplaceEntries(listing.Entries);
            StatusMessage = listing.Warning;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (DirectoryAccessException ex)
        {
            StatusMessage = ex.Message;
            return;
        }
        finally
        {
            IsLoading = false;
        }

        await RecalculateStorageAsync();
    }

    private async Task NavigateUpAsync()
    {
        if (CurrentPath is null)
            return;

        DirectoryInfo? parent;
        try
        {
            parent = Directory.GetParent(CurrentPath);
        }
        catch (IOException)
        {
            parent = null;
        }

        if (parent is null)
            await ShowDrivesAsync();
        else
            await NavigateToAsync(parent.FullName);
    }

    private void ReplaceEntries(IReadOnlyList<FileSystemEntry> entries)
    {
        Entries.Clear();
        foreach (var entry in entries)
            Entries.Add(entry);
    }

    private async Task RecalculateStorageAsync()
    {
        _scanCts?.Cancel();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        if (CurrentPath is null)
        {
            CategoryTotals = Array.Empty<CategoryTotal>();
            TotalBytesInView = 0;
            return;
        }

        string path = CurrentPath;
        bool recursive = IncludeSubfolders;
        IsScanning = true;

        try
        {
            IReadOnlyList<CategoryTotal> totals;
            if (recursive)
            {
                totals = await Task.Run(
                    () => StorageAnalyzer.Aggregate(
                        _fileSystemService.EnumerateFiles(path, recursive: true, cts.Token)),
                    cts.Token);
            }
            else
            {
                // Immediate listing is already in memory; no need to touch the disk again.
                totals = StorageAnalyzer.Aggregate(Entries);
            }

            if (cts.Token.IsCancellationRequested)
                return;

            CategoryTotals = totals;
            TotalBytesInView = totals.Sum(t => t.TotalBytes);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer navigation or toggle; nothing to report.
        }
        finally
        {
            if (_scanCts == cts)
                IsScanning = false;
        }
    }
}
