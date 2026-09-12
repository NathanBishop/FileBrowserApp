using System.Globalization;
using System.Windows.Data;
using FileBrowserApp.Controls;
using FileBrowserApp.Models;

namespace FileBrowserApp.Converters;

[ValueConversion(typeof(FileCategory), typeof(System.Windows.Media.Brush))]
public sealed class CategoryToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FileCategory category ? CategoryPalette.GetBrush(category) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
