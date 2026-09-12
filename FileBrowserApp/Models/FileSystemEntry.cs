namespace FileBrowserApp.Models;

/// <summary>
/// A single row in the file browser: either a directory or a file.
/// Directory sizes are left null since computing them requires a recursive
/// scan that the browser does not perform just to render a listing.
/// </summary>
public sealed class FileSystemEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required bool IsDirectory { get; init; }
    public long? SizeBytes { get; init; }
    public DateTime? ModifiedUtc { get; init; }
    public FileCategory? Category { get; init; }
    public bool IsAccessible { get; init; } = true;
}
