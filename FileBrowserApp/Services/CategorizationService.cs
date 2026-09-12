using System.IO;
using FileBrowserApp.Models;

namespace FileBrowserApp.Services;

/// <summary>Maps a file extension to a storage category. Pure logic, no I/O,
/// so it can be reasoned about (and tested) independently of the filesystem layer.</summary>
public static class CategorizationService
{
    private static readonly Dictionary<string, FileCategory> ExtensionMap = BuildMap();

    public static FileCategory Categorize(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return ExtensionMap.TryGetValue(ext, out var category) ? category : FileCategory.Miscellaneous;
    }

    private static Dictionary<string, FileCategory> BuildMap()
    {
        var map = new Dictionary<string, FileCategory>();

        void Add(FileCategory category, params string[] extensions)
        {
            foreach (var ext in extensions)
                map[ext] = category;
        }

        Add(FileCategory.Documents,
            "pdf", "doc", "docx", "txt", "rtf", "odt", "md", "xls", "xlsx", "csv",
            "ppt", "pptx", "pages", "epub", "tex", "log");

        Add(FileCategory.Music,
            "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a", "aiff", "alac");

        Add(FileCategory.Videos,
            "mp4", "mov", "avi", "mkv", "wmv", "flv", "webm", "m4v", "mpg", "mpeg");

        Add(FileCategory.Programs,
            "exe", "app", "dmg", "msi", "bat", "sh", "apk", "com", "bin", "appimage");

        Add(FileCategory.Archives,
            "zip", "rar", "7z", "tar", "gz", "bz2", "xz", "iso", "cab");

        return map;
    }
}
