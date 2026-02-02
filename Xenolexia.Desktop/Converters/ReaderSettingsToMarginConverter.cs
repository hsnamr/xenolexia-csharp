using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Converts ReaderSettings to Thickness (MarginHorizontal, MarginVertical, ...).
/// </summary>
public class ReaderSettingsToMarginConverter : IValueConverter
{
    public static readonly ReaderSettingsToMarginConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReaderSettings s)
            return new Thickness(s.MarginHorizontal, s.MarginVertical, s.MarginHorizontal, s.MarginVertical);
        return new Thickness(24, 16, 24, 16);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
