using HtmlAgilityPack;

namespace Xenolexia.Core.Services;

/// <summary>
/// Converts HTML chapter content to plain text for display in the reader.
/// Uses HtmlAgilityPack (MIT, GPL-compatible) — no custom parsing.
/// </summary>
public static class HtmlToPlainText
{
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.InnerText?.Trim() ?? string.Empty;
    }
}
