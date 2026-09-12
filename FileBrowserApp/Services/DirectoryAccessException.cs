namespace FileBrowserApp.Services;

/// <summary>Thrown when a directory itself cannot be opened (permission denied, missing, etc.).
/// Carries a user-facing message so the UI can show it instead of crashing.</summary>
public sealed class DirectoryAccessException : Exception
{
    public string Path { get; }

    public DirectoryAccessException(string path, string message) : base(message)
    {
        Path = path;
    }
}
