# Xenolexia - C# Implementation

> *Learn languages through the stories you love*

This is a .NET Core/C# reimplementation of the Xenolexia React Native application, targeting Android and Linux platforms.

## Project Structure

```
xenolexia-csharp/
├── Xenolexia.Core/          # Shared core library
│   ├── Models/              # Domain models (Book, Vocabulary, Language, etc.)
│   └── Services/            # Business logic services
│       ├── IStorageService.cs
│       ├── StorageService.cs
│       ├── IBookParserService.cs
│       ├── BookParserService.cs
│       ├── ITranslationService.cs
│       └── TranslationService.cs
├── Xenolexia.Android/       # Android application (to be created)
└── Xenolexia.Linux/         # Linux desktop application (to be created)
```

## Features

- 📖 **EPUB Book Parsing**: Extract chapters, metadata, and table of contents
- 🌍 **Multi-language Translation**: Support for 28+ languages via LibreTranslate API
- 💾 **SQLite Storage**: Local database for books and vocabulary
- 📚 **Library Management**: Add, update, and delete books
- 📝 **Vocabulary Building**: Save and review words with spaced repetition

## Prerequisites

- .NET 8.0 SDK
- For Android: Android SDK and Android NDK
- For Linux: GTK# or Avalonia UI runtime

## Building

```bash
# Restore packages
dotnet restore

# Build core library
cd Xenolexia.Core
dotnet build

# Run tests (when available)
dotnet test
```

## Architecture

The application follows a layered architecture:

- **Core Layer**: Shared business logic, models, and services
- **Platform Layer**: Platform-specific UI implementations (Android, Linux)
- **MVVM Pattern**: ViewModels for UI logic separation

## Services

### StorageService
SQLite-based storage for books and vocabulary items.

### BookParserService
Parses EPUB and TXT files, extracting metadata, chapters, and table of contents.

### TranslationService
Translates text between languages using the LibreTranslate API.

## License

MIT License - see LICENSE file for details.
