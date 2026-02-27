using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Converts ReaderSettings to effective line height in pixels.
/// Avalonia TextBlock.LineHeight expects pixels, but ReaderSettings.LineHeight is a multiplier (e.g. 1.6).
/// Returns FontSize * LineHeight so lines don't overlap.
/// </summary>
public class ReaderSettingsLineHeightConverter : IValueConverter
{
    public static readonly ReaderSettingsLineHeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReaderSettings s)
            return s.FontSize * s.LineHeight;
        return 24.0; // fallback: 16 * 1.5
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
