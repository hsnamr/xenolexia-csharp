# Xenolexia.Desktop

Cross-platform desktop app for **Linux, macOS, and Windows**. Same codebase (Avalonia UI, .NET 8).

- **Library**: Import from local files (EPUB, PDF, TXT, FB2); discover free ebooks (Project Gutenberg, Standard Ebooks, Open Library).
- **Vocabulary**: View, search, filter, export (CSV, Anki, JSON).
- **About**: App version, license, credits.

## Run

```bash
dotnet restore
dotnet build
dotnet run
```

Data directory: `~/.xenolexia/` (books, covers, exports, database).
