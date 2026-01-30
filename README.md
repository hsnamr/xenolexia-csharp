# Xenolexia

> *Learn languages through the stories you love*

Read books in your native language while learning Spanish, French, German, Japanese, or any of **28+ supported languages**. A portion of words (based on your level and density settings) appear in the target language. You infer meaning from context; hovering shows the original word and lets you save it to your vocabulary.

**Example (English → Spanish, beginner):**

> "She walked into the **casa** and set down her keys."  
> _Hover "casa" → reveals "house"_

---

## Platforms

- **Desktop**: Linux, macOS, Windows (single codebase: Avalonia UI, .NET 8)
- **Android**: .NET MAUI (in progress)

All features use **free and open source libraries** only. Features that cannot be implemented with FOSS are skipped.

---

## Features

### Core Reading

| Feature | Status | Notes |
|--------|--------|------|
| **Multi-format** | ✅ | EPUB, TXT (full parsing); PDF, FB2, MOBI (import) — VersOne.Epub, minimal custom for TXT/PDF |
| **Customizable reader** | 🔲 | Fonts, themes (light/dark/sepia), margins, line spacing — planned |
| **Progress** | ✅ | Bookmarking and progress on `Book` model; reader UI to persist — partial |
| **Hover-to-reveal** | 🔲 | Translation popup on hover (desktop) — planned with TranslationService |

### Language Engine

| Feature | Status | Notes |
|--------|--------|------|
| **28+ language pairs** | ✅ | LibreTranslate (free API); MyMemory/Lingva — add as fallbacks (FOSS) |
| **Proficiency levels** | ✅ | Beginner, Intermediate, Advanced (CEFR) in models |
| **Word density** | ✅ | On `Book`; control % of words in target language — TranslationEngine |
| **Frequency-based selection** | 🔲 | Open word lists — planned |
| **Offline-friendly** | ✅ | SQLite cache for vocabulary; translation cache — partial |

### Vocabulary

| Feature | Status | Notes |
|--------|--------|------|
| **Save words** | ✅ | From reader with context — StorageService |
| **Spaced repetition (SM-2)** | 🔲 | For saved words — planned |
| **Vocabulary screen** | ✅ | Search, filter, edit, delete, export (CSV/Anki/JSON) |
| **Review** | 🔲 | Flashcard-style review — planned |

### Library

| Feature | Status | Notes |
|--------|--------|------|
| **Import** | ✅ | Local files (EPUB, PDF, TXT, FB2, MOBI) — file picker, BookImportService |
| **Discover** | ✅ | Project Gutenberg, Standard Ebooks, Open Library — BookDownloadService |
| **Library view** | ✅ | Grid of books, add/delete — LibraryView |

---

## Project structure

```
xenolexia-csharp/
├── Xenolexia.Core/          # Shared logic (models, services)
├── Xenolexia.Linux/         # Desktop app (Linux, macOS, Windows) — Avalonia
├── Xenolexia.Android/       # Android app — MAUI
└── README.md, IMPLEMENTATION.md, FEATURES.md
```

- **Xenolexia.Linux** is the cross-platform desktop app (Avalonia). Run it on Linux, macOS, or Windows with the same build.

---

## Prerequisites

- .NET 8 SDK  
- **Desktop (Linux/macOS/Windows)**: no extra deps; Avalonia is included.  
- **Android**: Android SDK/NDK for MAUI.

---

## Build and run

```bash
dotnet restore

# Desktop (Linux, macOS, Windows)
cd Xenolexia.Linux
dotnet build
dotnet run

# Core only
cd Xenolexia.Core
dotnet build
```

---

## Libraries (FOSS)

| Purpose | Library | License |
|--------|---------|--------|
| EPUB reading | [VersOne.Epub](https://github.com/vers-one/EpubReader) | Unlicense |
| UI (desktop) | [Avalonia](https://avaloniaui.net/) | MIT |
| Storage | System.Data.SQLite.Core | Public domain |
| Translation | LibreTranslate (API) | AGPL (self-hosted) / public API |
| HTTP/JSON | built-in + Newtonsoft.Json | MIT |

See **FEATURES.md** for a full feature-by-feature roadmap and library choices.

---

## License

MIT — see [LICENSE](LICENSE).
