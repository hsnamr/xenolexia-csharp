using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Returns true when the bound enum value equals the ConverterParameter string (e.g. "Light").
/// Used for theme class bindings (reader_theme_light, etc.).
/// </summary>
public class EnumEqualsConverter : IValueConverter
{
    public static readonly EnumEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        var paramStr = parameter.ToString();
        if (string.IsNullOrEmpty(paramStr)) return false;
        return value.ToString()?.Equals(paramStr, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
