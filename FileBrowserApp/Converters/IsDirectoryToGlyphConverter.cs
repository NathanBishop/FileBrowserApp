using System.Globalization;
using System.Windows.Data;

namespace FileBrowserApp.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public sealed class IsDirectoryToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "" : ""; // Segoe Fluent Icons: folder / document

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
