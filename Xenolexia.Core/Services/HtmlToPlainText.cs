using System.Text;
using System.Text.RegularExpressions;

namespace Xenolexia.Core.Services;

/// <summary>
/// Converts HTML chapter content to plain text for display in the reader.
/// </summary>
public static class HtmlToPlainText
{
    private static readonly Regex StripScriptStyle = new(
        @"<(?:script|style)[^>]*>[\s\S]*?</(?:script|style)>",
        RegexOptions.IgnoreCase);

    private static readonly Regex BlockTags = new(
        @"</?(?:p|div|br|h[1-6]|li|tr|blockquote|hr)[^>]*>",
        RegexOptions.IgnoreCase);

    private static readonly Regex AnyTag = new(@"<[^>]+>");

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var s = StripScriptStyle.Replace(html, " ");
        s = BlockTags.Replace(s, "\n");
        s = AnyTag.Replace(s, " ");
        s = DecodeEntities(s);
        s = Regex.Replace(s, @"[ \t]+", " ");
        s = Regex.Replace(s, @"\n\s*\n", "\n\n");
        return s.Trim();
    }

    private static string DecodeEntities(string s)
    {
        var sb = new StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '&' && i + 1 < s.Length)
            {
                int end = s.IndexOf(';', i + 1);
                if (end > i && end - i < 20)
                {
                    var entity = s.Substring(i, end - i + 1);
                    var ch = DecodeEntity(entity);
                    if (ch.HasValue)
                    {
                        sb.Append(ch.Value);
                        i = end + 1;
                        continue;
                    }
                }
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }

    private static char? DecodeEntity(string entity)
    {
        return entity switch
        {
            "&nbsp;" => '\u00A0',
            "&amp;" => '&',
            "&lt;" => '<',
            "&gt;" => '>',
            "&quot;" => '"',
            "&apos;" => '\'',
            "&#39;" => '\'',
            "&mdash;" => '\u2014',
            "&ndash;" => '\u2013',
            "&hellip;" => '\u2026',
            "&copy;" => '\u00A9',
            "&reg;" => '\u00AE',
            "&trade;" => '\u2122',
            _ when entity.StartsWith("&#") && entity.EndsWith(";") => TryParseNumericEntity(entity),
            _ => null
        };
    }

    private static char? TryParseNumericEntity(string entity)
    {
        var num = entity.Substring(2, entity.Length - 3);
        if (num.StartsWith("x", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(num.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out var hex) && hex >= 0 && hex <= 0x10FFFF)
                return (char)hex;
        }
        else if (int.TryParse(num, out var code) && code >= 0 && code <= 0x10FFFF)
        {
            return (char)code;
        }
        return null;
    }
}
