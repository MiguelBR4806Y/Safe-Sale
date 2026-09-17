using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SS.Converters;

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "";
    public string FalseValue { get; set; } = "";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? TrueValue : FalseValue;
        return FalseValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#4CAF50";
    public string FalseColor { get; set; } = "#4CAF50";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = (value is bool b && b) ? TrueColor : FalseColor;
        return new SolidColorBrush(Color.Parse(hex));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
