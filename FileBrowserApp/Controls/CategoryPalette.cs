using System.Windows;
using System.Windows.Media;
using FileBrowserApp.Models;

namespace FileBrowserApp.Controls;

/// <summary>
/// Category colors for the "Aurora" dark theme: saturated, mutually distinguishable
/// hues that read clearly against a near-black surface, each with a matching
/// gradient variant for the donut chart's glow.
/// </summary>
public static class CategoryPalette
{
    private static readonly Dictionary<FileCategory, Color> Colors = new()
    {
        [FileCategory.Documents] = Color.FromRgb(0x2F, 0xE2, 0xC4), // teal
        [FileCategory.Music] = Color.FromRgb(0x8B, 0x6C, 0xF0),     // violet
        [FileCategory.Videos] = Color.FromRgb(0xF2, 0x6E, 0xB0),    // magenta
        [FileCategory.Programs] = Color.FromRgb(0xF2, 0xB8, 0x60),  // amber
        [FileCategory.Archives] = Color.FromRgb(0x5B, 0x8C, 0xFF),  // blue
        [FileCategory.Miscellaneous] = Color.FromRgb(0x5C, 0x64, 0x84), // slate
    };

    private static readonly Dictionary<FileCategory, SolidColorBrush> Brushes =
        Colors.ToDictionary(kv => kv.Key, kv => Freeze(new SolidColorBrush(kv.Value)));

    private static readonly Dictionary<FileCategory, LinearGradientBrush> GradientBrushes =
        Colors.ToDictionary(kv => kv.Key, kv => Freeze(new LinearGradientBrush(
            Lighten(kv.Value, 0.18), kv.Value, new Point(0, 0), new Point(1, 1))));

    public static SolidColorBrush GetBrush(FileCategory category) => Brushes[category];

    public static LinearGradientBrush GetGradientBrush(FileCategory category) => GradientBrushes[category];

    private static Color Lighten(Color color, double amount)
    {
        byte Blend(byte channel) => (byte)(channel + (255 - channel) * amount);
        return Color.FromRgb(Blend(color.R), Blend(color.G), Blend(color.B));
    }

    private static T Freeze<T>(T brush) where T : Brush
    {
        brush.Freeze();
        return brush;
    }
}
