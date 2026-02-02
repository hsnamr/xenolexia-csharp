using System.Runtime.InteropServices;
using System.Text;

namespace Xenolexia.Core.Services;

/// <summary>
/// P/Invoke for xenolexia-shared-c EPUB (EPUB3Processor). Use when libxenolexia_epub is available
/// so C# and Obj-C share the same FOSS EPUB implementation.
/// </summary>
internal static class EpubNative
{
    private const string LibName = "xenolexia_epub";

    private enum XenolexiaEpubError
    {
        Ok = 0,
        ErrOpen = 1001,
        ErrInvalid = 1002,
        ErrNotFound = 1004,
        ErrOther = 1099
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr xenolexia_epub_open([MarshalAs(UnmanagedType.LPStr)] string path, out XenolexiaEpubError outError);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_epub_close(IntPtr epub);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_epub_copy_title(IntPtr epub);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr xenolexia_epub_copy_meta(IntPtr epub, [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_epub_copy_language(IntPtr epub);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_epub_spine_count(IntPtr epub);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr xenolexia_epub_copy_spine_path(IntPtr epub, int index);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_epub_toc_count(IntPtr epub);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int xenolexia_epub_toc_at(IntPtr epub, int index, out IntPtr title, out IntPtr href, out int level);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
    private static extern int xenolexia_epub_read_file(IntPtr epub, [MarshalAs(UnmanagedType.LPStr)] string path, out IntPtr bytes, out uint size);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_epub_free(IntPtr ptr);

    private static string? PtrToStringUtf8AndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            xenolexia_epub_free(ptr);
        }
    }

    /// <summary>
    /// Try to parse an EPUB using the shared C library. Returns null if the native library
    /// is not available or parsing fails.
    /// </summary>
    public static Xenolexia.Core.Models.ParsedBook? TryParseEpub(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var err = XenolexiaEpubError.Ok;
            var epub = xenolexia_epub_open(filePath, out err);
            if (epub == IntPtr.Zero) return null;
            try
            {
                var metadata = new Xenolexia.Core.Models.BookMetadata
                {
                    Title = PtrToStringUtf8AndFree(xenolexia_epub_copy_title(epub)) ?? "Unknown",
                    Author = PtrToStringUtf8AndFree(xenolexia_epub_copy_meta(epub, "creator")),
                    Language = PtrToStringUtf8AndFree(xenolexia_epub_copy_language(epub)),
                    Subjects = new List<string>()
                };

                var chapters = new List<Xenolexia.Core.Models.Chapter>();
                int spineCount = xenolexia_epub_spine_count(epub);
                for (int i = 0; i < spineCount; i++)
                {
                    var pathPtr = xenolexia_epub_copy_spine_path(epub, i);
                    if (pathPtr == IntPtr.Zero) continue;
                    string? spinePath = PtrToStringUtf8AndFree(pathPtr);
                    if (string.IsNullOrEmpty(spinePath)) continue;
                    IntPtr bytes = IntPtr.Zero;
                    uint size = 0;
                    if (xenolexia_epub_read_file(epub, spinePath, out bytes, out size) != 0 || bytes == IntPtr.Zero || size == 0)
                        continue;
                    try
                    {
                        var raw = new byte[size];
                        Marshal.Copy(bytes, raw, 0, (int)size);
                        var content = Encoding.UTF8.GetString(raw);
                        var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t', '<', '>', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        chapters.Add(new Xenolexia.Core.Models.Chapter
                        {
                            Id = $"chapter-{i}",
                            Title = $"Chapter {i + 1}",
                            Index = i,
                            Content = content,
                            WordCount = wordCount,
                            Href = spinePath
                        });
                    }
                    finally
                    {
                        xenolexia_epub_free(bytes);
                    }
                }

                var toc = new List<Xenolexia.Core.Models.TableOfContentsItem>();
                int tocCount = xenolexia_epub_toc_count(epub);
                for (int t = 0; t < tocCount; t++)
                {
                    IntPtr tTitle, tHref;
                    int tLevel;
                    if (xenolexia_epub_toc_at(epub, t, out tTitle, out tHref, out tLevel) != 0) continue;
                    toc.Add(new Xenolexia.Core.Models.TableOfContentsItem
                    {
                        Id = "",
                        Title = PtrToStringUtf8AndFree(tTitle) ?? "",
                        Href = PtrToStringUtf8AndFree(tHref) ?? "",
                        Level = tLevel
                    });
                }

                return new Xenolexia.Core.Models.ParsedBook
                {
                    Metadata = metadata,
                    Chapters = chapters,
                    TableOfContents = toc,
                    TotalWordCount = chapters.Sum(c => c.WordCount)
                };
            }
            finally
            {
                xenolexia_epub_close(epub);
            }
        }
        catch (DllNotFoundException)
        {
            return null;
        }
    }
}
