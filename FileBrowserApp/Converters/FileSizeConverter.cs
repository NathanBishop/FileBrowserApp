using System.Globalization;
using System.Windows.Data;
using FileBrowserApp.Services;

namespace FileBrowserApp.Converters;

[ValueConversion(typeof(long?), typeof(string))]
public sealed class FileSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => SizeFormatter.Format(value as long?);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
