using System;
using Xenolexia.Core.Models;

namespace Xenolexia.Desktop.ViewModels;

/// <summary>
/// Static navigation for opening a book in the reader from the library.
/// </summary>
public static class BookNavigation
{
    public static event Action<Book>? OpenBookRequested;
    public static event Action? RequestCloseReader;

    public static void RequestOpenBook(Book book) => OpenBookRequested?.Invoke(book);
    public static void CloseReader() => RequestCloseReader?.Invoke();
}
