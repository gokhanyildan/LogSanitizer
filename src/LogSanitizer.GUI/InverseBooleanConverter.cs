using System;
using System.Globalization;
using System.Windows.Data;

namespace LogSanitizer.GUI;

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBooleanConverter : IValueConverter
{
    public static InverseBooleanConverter Instance { get; } = new InverseBooleanConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool) && targetType != typeof(bool?))
            throw new InvalidOperationException("The target must be a boolean");

        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool) && targetType != typeof(bool?))
            throw new InvalidOperationException("The target must be a boolean");

        return !(bool)value;
    }
}