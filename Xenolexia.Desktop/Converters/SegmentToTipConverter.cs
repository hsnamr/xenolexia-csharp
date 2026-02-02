using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Returns the segment when it is a foreign word (has WordData), otherwise null.
/// Used so the reader tooltip only shows for foreign-word segments.
/// </summary>
public class SegmentToTipConverter : IValueConverter
{
    public static readonly SegmentToTipConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ReaderContentSegment segment && segment.IsForeign && segment.WordData != null)
            return segment;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
