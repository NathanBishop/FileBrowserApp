using System.Globalization;
using System.Windows.Data;

namespace FileBrowserApp.Converters;

[ValueConversion(typeof(DateTime?), typeof(string))]
public sealed class DateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime utc ? utc.ToLocalTime().ToString("g", culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
