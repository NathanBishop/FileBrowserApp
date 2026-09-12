using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileBrowserApp.Converters;

/// <summary>Picks the maximize/restore glyph (Segoe Fluent Icons) to match the window's current state.</summary>
[ValueConversion(typeof(WindowState), typeof(string))]
public sealed class WindowStateToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WindowState.Maximized ? "" : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
