using System.Runtime.InteropServices;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// P/Invoke for xenolexia-shared-c FB2 (libxml2 parser). Use when libxenolexia_fb2 is available.
/// </summary>
internal static class Fb2Native
{
    private const string LibName = "xenolexia_fb2";

    private enum XenolexiaFb2Error
    {
        Ok = 0,
        ErrOpen = 1001,
        ErrParse = 1008,
        ErrOther = 1099
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr xenolexia_fb2_open([MarshalAs(UnmanagedType.LPStr)] string path, out XenolexiaFb2Error outError);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_fb2_close(IntPtr fb2);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_fb2_copy_title(IntPtr fb2);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_fb2_copy_author(IntPtr fb2);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_fb2_section_count(IntPtr fb2);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_fb2_copy_section_title(IntPtr fb2, int index);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_fb2_copy_section_text(IntPtr fb2, int index);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_fb2_free(IntPtr ptr);

    private static string? PtrToStringUtf8AndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringUTF8(ptr); }
        finally { xenolexia_fb2_free(ptr); }
    }

    public static ParsedBook? TryParseFb2(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var err = XenolexiaFb2Error.Ok;
            var fb2 = xenolexia_fb2_open(filePath, out err);
            if (fb2 == IntPtr.Zero) return null;
            try
            {
                var metadata = new BookMetadata
                {
                    Title = PtrToStringUtf8AndFree(xenolexia_fb2_copy_title(fb2)) ?? Path.GetFileNameWithoutExtension(filePath),
                    Author = PtrToStringUtf8AndFree(xenolexia_fb2_copy_author(fb2)),
                    Subjects = new List<string>()
                };
                var chapters = new List<Chapter>();
                int sectionCount = xenolexia_fb2_section_count(fb2);
                int totalWords = 0;
                for (int i = 0; i < sectionCount; i++)
                {
                    var title = PtrToStringUtf8AndFree(xenolexia_fb2_copy_section_title(fb2, i)) ?? $"Section {i + 1}";
                    var text = PtrToStringUtf8AndFree(xenolexia_fb2_copy_section_text(fb2, i)) ?? "";
                    var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    totalWords += wordCount;
                    chapters.Add(new Chapter
                    {
                        Id = $"chapter-{i}",
                        Title = title,
                        Index = i,
                        Content = text,
                        WordCount = wordCount
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
            finally { xenolexia_fb2_close(fb2); }
        }
        catch (DllNotFoundException) { return null; }
    }
}
