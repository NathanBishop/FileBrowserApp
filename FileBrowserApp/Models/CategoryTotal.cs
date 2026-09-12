namespace FileBrowserApp.Models;

/// <summary>Aggregated byte total for one category, used to render the storage infographic.</summary>
public sealed class CategoryTotal
{
    public required FileCategory Category { get; init; }
    public long TotalBytes { get; set; }
}
