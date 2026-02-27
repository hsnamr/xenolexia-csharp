using System.Text;
using HtmlAgilityPack;

namespace Xenolexia.Core.Services;

/// <summary>
/// Converts HTML chapter content to plain text for display in the reader.
/// Uses HtmlAgilityPack (MIT) with robust extraction per VersOne.Epub recommendations.
/// Preserves paragraph breaks for block elements (p, div, h1-h6, li, br).
/// </summary>
public static class HtmlToPlainText
{
    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
        { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "li", "tr", "blockquote", "hr" };

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var sb = new StringBuilder();
        var textNodes = doc.DocumentNode.SelectNodes("//text()");
        if (textNodes == null)
            return doc.DocumentNode.InnerText?.Trim() ?? string.Empty;

        for (var i = 0; i < textNodes.Count; i++)
        {
            var node = textNodes[i];
            var text = node.InnerText?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            var parent = node.ParentNode;
            var isBlock = parent != null && BlockTags.Contains(parent.Name);

            if (sb.Length > 0)
            {
                var prevNode = i > 0 ? textNodes[i - 1] : null;
                var prevParent = prevNode?.ParentNode;
                var prevWasBlock = prevParent != null && BlockTags.Contains(prevParent.Name);
                if (isBlock || prevWasBlock)
                    sb.AppendLine();
                else
                    sb.Append(' ');
            }
            sb.Append(text);
        }

        return sb.ToString().Trim();
    }
}
