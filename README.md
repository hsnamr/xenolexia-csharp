# Xenolexia

> *Learn languages through the stories you love*

Read books in your native language while learning Spanish, French, German, Japanese, or any of **28+ supported languages**. A portion of words (based on your level and density settings) appear in the target language. You infer meaning from context; hovering shows the original word and lets you save it to your vocabulary.

**Example (English → Spanish, beginner):**

> "She walked into the **casa** and set down her keys."  
> _Hover "casa" → reveals "house"_

---

## Platforms

- **Desktop**: Linux, macOS, Windows (single codebase: Xenolexia.Desktop — Avalonia UI, .NET 8)

All features use **free and open source libraries compatible with GPL/AGPL/LGPL** only. **No custom implementation where a suitable library exists**; format parsing and HTML-to-text use FOSS libraries (VersOne.Epub, PdfPig, Fb2.Document, HtmlAgilityPack). Features that cannot be implemented with FOSS are skipped.

---

## Features

### Core Reading

| Feature | Status | Notes |
|--------|--------|------|
| **Multi-format** | ✅ | EPUB, PDF, TXT, FB2 via FOSS libs (VersOne.Epub, PdfPig, Fb2.Document); MOBI omitted (no FOSS full-text lib) |
| **Customizable reader** | ✅ | Fonts, themes (light/dark/sepia), margins, line spacing — ReaderSettings, persisted |
| **Progress** | ✅ | Bookmarking and progress on `Book` model; reader persists on chapter change and close |
| **Hover-to-reveal** | ✅ | Translation popup on hover (desktop); save to vocabulary from reader |

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
| **Spaced repetition (SM-2)** | ✅ | GetVocabularyDueForReviewAsync, RecordReviewAsync (SM-2 in C#) |
| **Vocabulary screen** | ✅ | Search, filter, edit, delete, export (CSV/Anki/JSON) |
| **Review** | ✅ | Flashcard-style review — ReviewView, SM-2 grading (Again/Hard/Good/Easy/Already Knew) |

### Library

| Feature | Status | Notes |
|--------|--------|------|
| **Import** | ✅ | Local files (EPUB, PDF, TXT, FB2, MOBI) — file picker, BookImportService |
| **Discover** | ✅ | Project Gutenberg, Standard Ebooks, Open Library — BookDownloadService |
| **Library view** | ✅ | Grid/list toggle, book cards, add/delete — LibraryView |

---

## Project structure

```
xenolexia-csharp/
├── Xenolexia.Core/          # Shared logic (models, services)
├── Xenolexia.Desktop/       # Desktop app (Linux, macOS, Windows) — Avalonia
└── README.md, IMPLEMENTATION.md, FEATURES.md
```

- **Xenolexia.Desktop** is the cross-platform desktop app (Avalonia). Run it on Linux, macOS, or Windows with the same build.

---

## Prerequisites

- .NET 8 SDK  
- **Desktop (Linux/macOS/Windows)**: no extra deps; Avalonia is included.  

---

## Build and run

```bash
dotnet restore

# Desktop (Linux, macOS, Windows)
cd Xenolexia.Desktop
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
GNU Affero General Public License v3.0
