namespace FileBrowserApp.Services;

/// <summary>Formats byte counts as human-readable strings (e.g. "4.2 MB").</summary>
public static class SizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long? bytes)
    {
        if (bytes is null)
            return string.Empty;

        double value = bytes.Value;
        int unitIndex = 0;
        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{value:0} {Units[unitIndex]}"
            : $"{value:0.#} {Units[unitIndex]}";
    }
}
