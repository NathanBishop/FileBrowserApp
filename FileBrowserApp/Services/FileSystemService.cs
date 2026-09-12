using System.IO;
using FileBrowserApp.Models;

namespace FileBrowserApp.Services;

/// <summary>
/// All real filesystem access lives here. Every entry point is defensive: a folder
/// that can't be opened, a file whose metadata can't be read, or a subfolder that
/// disappears mid-scan produces a warning/skip instead of an unhandled exception.
/// </summary>
public sealed class FileSystemService
{
    /// <summary>Top-level "This PC" view: every ready drive on the machine.</summary>
    public IReadOnlyList<FileSystemEntry> GetDrives()
    {
        var entries = new List<FileSystemEntry>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            try
            {
                entries.Add(new FileSystemEntry
                {
                    Name = drive.VolumeLabel is { Length: > 0 } label
                        ? $"{label} ({drive.Name.TrimEnd('\\')})"
                        : drive.Name.TrimEnd('\\'),
                    FullPath = drive.RootDirectory.FullName,
                    IsDirectory = true,
                    SizeBytes = null,
                    ModifiedUtc = null
                });
            }
            catch (IOException)
            {
                // Drive dropped out (e.g. removable media) between enumeration and inspection.
            }
        }

        return entries;
    }

    /// <summary>Lists the immediate contents of <paramref name="path"/>.</summary>
    /// <exception cref="DirectoryAccessException">The directory itself could not be opened.</exception>
    public Task<DirectoryListing> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
        => Task.Run(() => ListDirectory(path, cancellationToken), cancellationToken);

    private DirectoryListing ListDirectory(string path, CancellationToken cancellationToken)
    {
        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateFileSystemEntries(path);
        }
        catch (UnauthorizedAccessException)
        {
            throw new DirectoryAccessException(path, "You don't have permission to open this folder.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new DirectoryAccessException(path, "This folder no longer exists.");
        }
        catch (PathTooLongException)
        {
            throw new DirectoryAccessException(path, "This folder's path is too long to open.");
        }
        catch (IOException ex)
        {
            throw new DirectoryAccessException(path, $"This folder could not be read: {ex.Message}");
        }

        var entries = new List<FileSystemEntry>();
        int skipped = 0;

        using var enumerator = children.GetEnumerator();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string current;
            try
            {
                if (!enumerator.MoveNext())
                    break;
                current = enumerator.Current;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // The underlying directory walk hit an entry it can't see anymore; stop
                // rather than risk resuming a faulted enumerator, and surface what happened.
                skipped++;
                break;
            }

            entries.Add(BuildEntry(current, ref skipped));
        }

        entries.Sort(CompareEntries);

        string? warning = skipped > 0
            ? $"{skipped} item(s) could not be read and were skipped."
            : null;

        return new DirectoryListing { Path = path, Entries = entries, Warning = warning };
    }

    private static FileSystemEntry BuildEntry(string fullPath, ref int skipped)
    {
        string name = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(name))
            name = fullPath;

        bool isDirectory;
        try
        {
            isDirectory = File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            skipped++;
            return new FileSystemEntry
            {
                Name = name,
                FullPath = fullPath,
                IsDirectory = false,
                SizeBytes = null,
                ModifiedUtc = null,
                Category = null,
                IsAccessible = false
            };
        }

        if (isDirectory)
        {
            var info = new DirectoryInfo(fullPath);
            return new FileSystemEntry
            {
                Name = name,
                FullPath = fullPath,
                IsDirectory = true,
                SizeBytes = null,
                ModifiedUtc = TryGet(() => info.LastWriteTimeUtc),
                Category = null,
                IsAccessible = true
            };
        }

        var file = new FileInfo(fullPath);
        long? size = TryGet<long?>(() => file.Length);
        if (size is null)
            skipped++;

        return new FileSystemEntry
        {
            Name = name,
            FullPath = fullPath,
            IsDirectory = false,
            SizeBytes = size,
            ModifiedUtc = TryGet(() => file.LastWriteTimeUtc),
            Category = CategorizationService.Categorize(name),
            IsAccessible = size is not null
        };
    }

    private static T? TryGet<T>(Func<T> read)
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return default;
        }
    }

    private static int CompareEntries(FileSystemEntry a, FileSystemEntry b)
    {
        if (a.IsDirectory != b.IsDirectory)
            return a.IsDirectory ? -1 : 1;
        return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Safely walks every file under <paramref name="rootPath"/>, optionally recursing into
    /// subfolders. Inaccessible subfolders are skipped rather than aborting the whole scan,
    /// so one locked-down folder can't stop the storage breakdown.
    /// </summary>
    public IEnumerable<FileSystemEntry> EnumerateFiles(
        string rootPath,
        bool recursive,
        CancellationToken cancellationToken)
    {
        int skipped = 0;
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string dir = pending.Pop();

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateFileSystemEntries(dir);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                continue;
            }

            using var enumerator = children.GetEnumerator();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string current;
                try
                {
                    if (!enumerator.MoveNext())
                        break;
                    current = enumerator.Current;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    break;
                }

                bool isDirectory;
                try
                {
                    isDirectory = File.GetAttributes(current).HasFlag(FileAttributes.Directory);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    continue;
                }

                if (isDirectory)
                {
                    if (recursive)
                        pending.Push(current);
                    continue;
                }

                var entry = BuildEntry(current, ref skipped);
                if (!entry.IsAccessible)
                    continue;

                yield return entry;
            }
        }
    }
}
