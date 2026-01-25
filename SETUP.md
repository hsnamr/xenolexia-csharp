# Xenolexia C# - Setup and Development Guide

## Project Status

✅ **Core Library**: Complete with models and services
✅ **Build System**: Solution builds successfully
✅ **NuGet Packages**: All dependencies installed

## Project Structure

```
xenolexia-csharp/
├── Xenolexia.Core/              # Shared core library
│   ├── Models/                  # Domain models
│   │   ├── Language.cs          # Language types and enums
│   │   ├── Book.cs              # Book-related models
│   │   ├── Vocabulary.cs        # Vocabulary models
│   │   ├── Reader.cs            # Reader settings and processed content
│   │   └── Statistics.cs       # Statistics and user preferences
│   └── Services/                # Business logic services
│       ├── IStorageService.cs   # Storage interface
│       ├── StorageService.cs    # SQLite implementation
│       ├── IBookParserService.cs
│       ├── BookParserService.cs # EPUB/TXT parser
│       ├── ITranslationService.cs
│       ├── TranslationService.cs # LibreTranslate API client
│       └── TranslationEngine.cs  # Word replacement engine
├── Xenolexia.Android/           # Android application (console app structure)
├── Xenolexia.Linux/             # Linux application (console app structure)
└── Xenolexia.sln                # Solution file
```

## Installed NuGet Packages

### Core Library
- **System.Data.SQLite.Core** (1.0.118) - SQLite database
- **Newtonsoft.Json** (13.0.3) - JSON serialization
- **System.IO.Compression** (4.3.0) - ZIP/EPUB file handling
- **HtmlAgilityPack** (1.11.54) - HTML parsing
- **EPubSharp** (0.4.2) - EPUB parsing (legacy .NET Framework, but works)

## Features Implemented

### ✅ Core Models
- Language types and enums (28+ languages)
- Book models (Book, BookMetadata, Chapter, TableOfContents)
- Vocabulary models (WordEntry, VocabularyItem)
- Reader settings and processed content
- Statistics and user preferences

### ✅ Services
- **StorageService**: SQLite-based storage for books and vocabulary
- **BookParserService**: EPUB and TXT file parsing
- **TranslationService**: LibreTranslate API integration
- **TranslationEngine**: Word replacement based on proficiency level

## Next Steps for Full Implementation

### Android App
1. Convert console app to Android Activity-based app
2. Add Android UI framework (Xamarin.Forms or native Android)
3. Implement MVVM ViewModels
4. Add Android-specific file picker and storage

### Linux App
1. Add GUI framework (GTK# or Avalonia UI)
2. Implement desktop UI
3. Add file dialogs for book import
4. Implement native Linux file system access

### Additional Services Needed
- Book download service (Gutenberg, Standard Ebooks, Open Library)
- Image service for cover extraction
- Export service (CSV, Anki, JSON)
- SM-2 spaced repetition algorithm implementation

## Building

```bash
# Restore packages
dotnet restore

# Build all projects
dotnet build

# Run Linux app
cd Xenolexia.Linux
dotnet run

# Run Android app (requires Android SDK)
cd Xenolexia.Android
dotnet run
```

## Database Location

- **Linux**: `~/.xenolexia/xenolexia.db`
- **Android**: `/data/data/com.xenolexia.app/files/xenolexia.db`

## API Configuration

The TranslationService uses LibreTranslate API by default:
- Default URL: `https://libretranslate.com`
- Can be configured via constructor parameter

## Notes

- EPubSharp package shows compatibility warnings but should work
- For production, consider implementing a .NET 8-compatible EPUB parser
- Android app structure is currently a console app - needs conversion to Android Activity
- Linux app structure is currently a console app - needs GUI framework integration

## License

MIT License - see LICENSE file for details.
