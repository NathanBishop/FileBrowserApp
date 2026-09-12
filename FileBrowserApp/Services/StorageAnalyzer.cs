using FileBrowserApp.Models;

namespace FileBrowserApp.Services;

/// <summary>Aggregates file entries into per-category byte totals for the storage infographic.</summary>
public static class StorageAnalyzer
{
    public static IReadOnlyList<CategoryTotal> Aggregate(IEnumerable<FileSystemEntry> files)
    {
        var totals = Enum.GetValues<FileCategory>()
            .ToDictionary(c => c, c => new CategoryTotal { Category = c });

        foreach (var file in files)
        {
            if (file.IsDirectory || file.SizeBytes is null)
                continue;

            var category = file.Category ?? FileCategory.Miscellaneous;
            totals[category].TotalBytes += file.SizeBytes.Value;
        }

        return totals.Values
            .OrderByDescending(t => t.TotalBytes)
            .ToList();
    }
}
