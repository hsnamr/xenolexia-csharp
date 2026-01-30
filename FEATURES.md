# Xenolexia — Feature roadmap and FOSS stack

This document maps the product concept to implementation status and to **free and open source** libraries. Anything that cannot be implemented with FOSS is skipped.

**Principle**: Prefer existing FOSS libraries over custom code. Target **Linux, macOS, and Windows** for desktop (single Avalonia codebase).

---

## Concept summary

- Read in your language; a portion of words appear in the target language (density + level).
- Hover to reveal original word; save to vocabulary.
- 28+ language pairs, proficiency levels, word density, frequency-based selection.
- Offline-friendly: SQLite cache for vocabulary and translations.
- Library: import local files, discover free ebooks (Gutenberg, Standard Ebooks, etc.).
- Reader: multi-format, customizable (fonts, themes, margins), progress, hover-to-reveal.

---

## Core reading

| Feature | Status | FOSS approach |
|--------|--------|----------------|
| **Multi-format** | Done | **VersOne.Epub** for EPUB (metadata, TOC, chapters, cover). TXT/PDF minimal custom; FB2/MOBI import-only. |
| **Customizable reader** | Planned | Avalonia controls; fonts, themes (light/dark/sepia), margins, line spacing — no extra lib. |
| **Progress & bookmarking** | Partial | `Book` model has Progress, CurrentChapter, CurrentLocation; reader UI to persist on exit. |
| **Hover-to-reveal** | Planned | Avalonia tooltip/popup + `ITranslationService` (LibreTranslate/MyMemory/Lingva). |

---

## Language engine

| Feature | Status | FOSS approach |
|--------|--------|----------------|
| **28+ language pairs** | Done | **LibreTranslate** (free API). Add **MyMemory** / **Lingva** as fallbacks (free APIs). |
| **Proficiency levels** | Done | `ProficiencyLevel` (Beginner/Intermediate/Advanced); CEFR on model. |
| **Word density** | Done | `Book.WordDensity`; **TranslationEngine** selects words by density + level. |
| **Frequency-based selection** | Planned | Use open word lists (e.g. frequency lists per language); no proprietary data. |
| **Offline / cache** | Partial | SQLite for vocabulary; add translation cache table and use cache when offline. |

---

## Vocabulary

| Feature | Status | FOSS approach |
|--------|--------|----------------|
| **Save words with context** | Done | **StorageService** (SQLite); VocabularyItem with ContextSentence, BookId. |
| **SM-2 spaced repetition** | Planned | Implement SM-2 in Core (formula is public domain); store interval/ease in VocabularyItem. |
| **Vocabulary screen** | Done | VocabularyView: list, search, filter, edit, delete, export. |
| **Export** | Done | **ExportService**: CSV, Anki TSV, JSON. |
| **Review / flashcards** | Planned | New view + SM-2 scheduling; no extra lib. |

---

## Library

| Feature | Status | FOSS approach |
|--------|--------|----------------|
| **Import from local** | Done | **BookImportService** + **IFilePickerService** (Avalonia StorageProvider). |
| **Discover free ebooks** | Done | **BookDownloadService**: Project Gutenberg (Gutendex), Standard Ebooks (OPDS), Open Library. |
| **Library view** | Done | Grid of books (cover placeholder, title, author, progress); add/delete. |

---

## Platforms

| Platform | Project | Stack |
|----------|---------|--------|
| **Linux** | Xenolexia.Desktop | Avalonia, .NET 8 |
| **macOS** | Xenolexia.Desktop | Same Avalonia app |
| **Windows** | Xenolexia.Desktop | Same Avalonia app |

The desktop app is **Xenolexia.Desktop** (Avalonia) — single codebase for Linux, macOS, and Windows.

---

## Libraries in use (FOSS only)

| Library | Use | License |
|---------|-----|--------|
| **VersOne.Epub** | EPUB read, metadata, TOC, chapters, cover | Unlicense |
| **Avalonia** | Desktop UI (Linux, macOS, Windows) | MIT |
| **System.Data.SQLite.Core** | Books + vocabulary storage | Public domain |
| **Newtonsoft.Json** | JSON (APIs, export) | MIT |
| **CommunityToolkit.Mvvm** | MVVM (desktop) | MIT |
| **LibreTranslate** | Translation API (free tier) | AGPL (server) / public API |
| **Gutendex / Standard Ebooks / Open Library** | Free ebook discovery & download | Public / open |

Planned additions (all FOSS):

- **MyMemory** / **Lingva** (or similar) as translation fallbacks.
- **SM-2** (spaced repetition): implement from public algorithm.
- **Open frequency word lists** for frequency-based word selection.

---

## Skipped (non-FOSS or not feasible with FOSS)

- No proprietary translation APIs.
- No proprietary DRM or store clients.
- No features that require paid-only or closed-license SDKs.

---

## Build and run (desktop)

```bash
cd Xenolexia.Desktop
dotnet restore
dotnet build
dotnet run
```

Same commands on Linux, macOS, and Windows.
