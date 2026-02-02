using System.Runtime.InteropServices;
using System.Text;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// P/Invoke for xenolexia-shared-c PDF (MuPDF). Use when libxenolexia_pdf is available.
/// </summary>
internal static class PdfNative
{
    private const string LibName = "xenolexia_pdf";

    private enum XenolexiaPdfError
    {
        Ok = 0,
        ErrOpen = 1001,
        ErrOther = 1099
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr xenolexia_pdf_open([MarshalAs(UnmanagedType.LPStr)] string path, out XenolexiaPdfError outError);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_pdf_close(IntPtr pdf);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_pdf_copy_title(IntPtr pdf);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_pdf_copy_author(IntPtr pdf);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_pdf_page_count(IntPtr pdf);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_pdf_copy_page_text(IntPtr pdf, int pageIndex);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_pdf_free(IntPtr ptr);

    private static string? PtrToStringUtf8AndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringUTF8(ptr); }
        finally { xenolexia_pdf_free(ptr); }
    }

    public static ParsedBook? TryParsePdf(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var err = XenolexiaPdfError.Ok;
            var pdf = xenolexia_pdf_open(filePath, out err);
            if (pdf == IntPtr.Zero) return null;
            try
            {
                var metadata = new BookMetadata
                {
                    Title = PtrToStringUtf8AndFree(xenolexia_pdf_copy_title(pdf)) ?? Path.GetFileNameWithoutExtension(filePath),
                    Author = PtrToStringUtf8AndFree(xenolexia_pdf_copy_author(pdf)),
                    Subjects = new List<string>()
                };
                var chapters = new List<Chapter>();
                int pageCount = xenolexia_pdf_page_count(pdf);
                int totalWords = 0;
                for (int i = 0; i < pageCount; i++)
                {
                    var textPtr = xenolexia_pdf_copy_page_text(pdf, i);
                    var text = PtrToStringUtf8AndFree(textPtr) ?? "";
                    var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    totalWords += wordCount;
                    chapters.Add(new Chapter
                    {
                        Id = $"page-{i}",
                        Title = $"Page {i + 1}",
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
            finally { xenolexia_pdf_close(pdf); }
        }
        catch (DllNotFoundException) { return null; }
    }
}
