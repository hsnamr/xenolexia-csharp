using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// P/Invoke for xenolexia-shared-c MOBI (libmobi). Use when libxenolexia_mobi is available.
/// </summary>
internal static class MobiNative
{
    private const string LibName = "xenolexia_mobi";

    private enum XenolexiaMobiError
    {
        Ok = 0,
        ErrOpen = 1001,
        ErrParse = 1008,
        ErrOther = 1099
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr xenolexia_mobi_open([MarshalAs(UnmanagedType.LPStr)] string path, out XenolexiaMobiError outError);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_mobi_close(IntPtr mobi);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_mobi_copy_title(IntPtr mobi);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_mobi_copy_author(IntPtr mobi);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_mobi_part_count(IntPtr mobi);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_mobi_copy_full_text(IntPtr mobi);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_mobi_copy_part(IntPtr mobi, int index);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_mobi_free(IntPtr ptr);

    private static string? PtrToStringUtf8AndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringUTF8(ptr); }
        finally { xenolexia_mobi_free(ptr); }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var s = Regex.Replace(html, @"<[^>]+>", " ");
        s = Regex.Replace(s, @"\s+", " ");
        return s.Trim();
    }

    public static ParsedBook? TryParseMobi(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var err = XenolexiaMobiError.Ok;
            var mobi = xenolexia_mobi_open(filePath, out err);
            if (mobi == IntPtr.Zero) return null;
            try
            {
                var metadata = new BookMetadata
                {
                    Title = PtrToStringUtf8AndFree(xenolexia_mobi_copy_title(mobi)) ?? Path.GetFileNameWithoutExtension(filePath),
                    Author = PtrToStringUtf8AndFree(xenolexia_mobi_copy_author(mobi)),
                    Subjects = new List<string>()
                };
                var chapters = new List<Chapter>();
                int partCount = xenolexia_mobi_part_count(mobi);
                int totalWords = 0;
                if (partCount > 0)
                {
                    for (int i = 0; i < partCount; i++)
                    {
                        var partHtml = PtrToStringUtf8AndFree(xenolexia_mobi_copy_part(mobi, i));
                        var text = StripHtml(partHtml ?? "");
                        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        totalWords += wordCount;
                        chapters.Add(new Chapter
                        {
                            Id = $"part-{i}",
                            Title = $"Part {i + 1}",
                            Index = i,
                            Content = partHtml ?? "",
                            WordCount = wordCount
                        });
                    }
                }
                else
                {
                    var fullText = PtrToStringUtf8AndFree(xenolexia_mobi_copy_full_text(mobi));
                    var text = StripHtml(fullText ?? "");
                    int wc = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    totalWords = wc;
                    chapters.Add(new Chapter
                    {
                        Id = "chapter-0",
                        Title = "Content",
                        Index = 0,
                        Content = fullText ?? "",
                        WordCount = wc
                    });
                }
                if (chapters.Count == 0)
                {
                    chapters.Add(new Chapter { Id = "chapter-0", Title = "Content", Index = 0, Content = "", WordCount = 0 });
                }
                return new ParsedBook
                {
                    Metadata = metadata,
                    Chapters = chapters,
                    TableOfContents = new List<TableOfContentsItem>(),
                    TotalWordCount = totalWords
                };
            }
            finally { xenolexia_mobi_close(mobi); }
        }
        catch (DllNotFoundException) { return null; }
    }
}
