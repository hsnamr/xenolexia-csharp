using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Converts Language enum to full display name.
/// </summary>
public class LanguageToDisplayNameConverter : IValueConverter
{
    public static readonly LanguageToDisplayNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Language lang)
            return GetDisplayName(lang);
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    public static string GetDisplayName(Language lang) => lang switch
    {
        Language.En => "English",
        Language.El => "Greek",
        Language.Es => "Spanish",
        Language.Fr => "French",
        Language.De => "German",
        Language.It => "Italian",
        Language.Pt => "Portuguese",
        Language.Ru => "Russian",
        Language.Ja => "Japanese",
        Language.Zh => "Chinese",
        Language.Ko => "Korean",
        Language.Ar => "Arabic",
        Language.Nl => "Dutch",
        Language.Pl => "Polish",
        Language.Tr => "Turkish",
        Language.Sv => "Swedish",
        Language.Da => "Danish",
        Language.Fi => "Finnish",
        Language.No => "Norwegian",
        Language.Cs => "Czech",
        Language.Hu => "Hungarian",
        Language.Ro => "Romanian",
        Language.Uk => "Ukrainian",
        Language.Hi => "Hindi",
        Language.Th => "Thai",
        Language.Vi => "Vietnamese",
        Language.Id => "Indonesian",
        _ => lang.ToString()
    };
}
