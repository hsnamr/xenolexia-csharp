# FOSS EPUB libraries for .NET (C#)

Possible libraries to read/parse EPUB and extract chapter content:

| Library | License | Notes |
|--------|---------|--------|
| **EpubCore** | MPL-2.0 | .NET 6 / .NET Standard 2.0. Read, write, manipulate EPUB 2.0/3.0/3.1. `EpubReader.Read(path)`, `book.TableOfContents`, `book.Resources.Html`, `book.ToPlainText()`. No external deps. |
| **VersOne.Epub** | Unlicense | .NET Standard 1.3+. Read EPUB 2/3. `ReadBookAsync`, `ReadingOrder` (content per file). NCX required for EPUB 2; strict parsing. |
| **Didstopia/EpubReader** | MIT | Fork of VersOne.Epub, .NET Standard 2.0. |
| **Manual (ZIP + XML)** | N/A | EPUB is a ZIP with `mimetype`, `META-INF/container.xml`, `*.opf`, XHTML. Use `System.IO.Compression` + XML/HTML parsing (e.g. HtmlAgilityPack). Full control, no third-party EPUB lib. |

This project uses **EpubCore** (MPL-2.0) for EPUB parsing and chapter content so the reader always receives non-empty chapter text.
