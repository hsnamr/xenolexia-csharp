using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.Converters;

/// <summary>
/// Converts Language to flag emoji for display on book thumbnails.
/// When value is LanguagePair, returns flag only if target != source.
/// </summary>
public class LanguageToFlagConverter : IValueConverter
{
    public static readonly LanguageToFlagConverter Instance = new();

    private static readonly System.Collections.Generic.Dictionary<Language, string> Flags = new()
    {
        { Language.En, "🇬🇧" },
        { Language.El, "🇬🇷" },
        { Language.Es, "🇪🇸" },
        { Language.Fr, "🇫🇷" },
        { Language.De, "🇩🇪" },
        { Language.It, "🇮🇹" },
        { Language.Pt, "🇵🇹" },
        { Language.Ru, "🇷🇺" },
        { Language.Ja, "🇯🇵" },
        { Language.Zh, "🇨🇳" },
        { Language.Ko, "🇰🇷" },
        { Language.Ar, "🇸🇦" },
        { Language.Nl, "🇳🇱" },
        { Language.Pl, "🇵🇱" },
        { Language.Tr, "🇹🇷" },
        { Language.Sv, "🇸🇪" },
        { Language.Da, "🇩🇰" },
        { Language.Fi, "🇫🇮" },
        { Language.No, "🇳🇴" },
        { Language.Cs, "🇨🇿" },
        { Language.Hu, "🇭🇺" },
        { Language.Ro, "🇷🇴" },
        { Language.Uk, "🇺🇦" },
        { Language.He, "🇮🇱" },
        { Language.Hi, "🇮🇳" },
        { Language.Th, "🇹🇭" },
        { Language.Vi, "🇻🇳" },
        { Language.Id, "🇮🇩" }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LanguagePair pair)
        {
            if (pair.SourceLanguage == pair.TargetLanguage)
                return string.Empty;
            return Flags.TryGetValue(pair.TargetLanguage, out var flag) ? flag : string.Empty;
        }
        if (value is Language lang)
            return Flags.TryGetValue(lang, out var f) ? f : string.Empty;
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
