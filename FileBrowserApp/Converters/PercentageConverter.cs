using System.Globalization;
using System.Windows.Data;

namespace FileBrowserApp.Converters;

/// <summary>Formats values[0] (a category's bytes) as a percentage of values[1] (the total).</summary>
public sealed class PercentageConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is [long part, long total] && total > 0)
            return $"{(double)part / total:P0}";

        return "0%";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
