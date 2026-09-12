namespace FileBrowserApp.Models;

/// <summary>Result of listing one directory: the entries that could be read, plus a
/// human-readable note about anything that was skipped (permission denied, I/O errors, etc.).</summary>
public sealed class DirectoryListing
{
    public required string Path { get; init; }
    public required IReadOnlyList<FileSystemEntry> Entries { get; init; }
    public string? Warning { get; init; }
}
