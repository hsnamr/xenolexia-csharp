# Implementation Summary

This document summarizes the implementation of the Xenolexia C# application. The product concept: read books in your language with a portion of words in the target language; hover to reveal and save to vocabulary. Implementation uses **free and open source libraries only**; features that cannot be done with FOSS are skipped.

**Platforms**: Desktop (Linux, macOS, Windows) via **Xenolexia.Linux** (Avalonia); Android via Xenolexia.Android (MAUI). See **FEATURES.md** for the full feature roadmap and FOSS stack.

## Completed Tasks

### 1. Android: Convert to Android Activity-based app with UI framework ✅

- **Framework**: .NET MAUI (Multi-platform App UI)
- **Project Structure**:
  - `MauiProgram.cs` - Application entry point with service registration
  - `App.xaml` / `App.xaml.cs` - Application class
  - `AppShell.xaml` / `AppShell.xaml.cs` - Shell navigation with tabs
  - `Views/` - XAML pages for Library, Reader, Vocabulary, and Settings
  - `ViewModels/` - MVVM ViewModels for each view

- **Features**:
  - Tab-based navigation (Library, Reader, Vocabulary, Settings)
  - Service dependency injection
  - MVVM pattern implementation
  - Basic UI for Library and Vocabulary screens

### 2. Desktop (Linux, macOS, Windows): Avalonia UI ✅

- **Framework**: Avalonia UI (cross-platform); single codebase for Linux, macOS, and Windows.
- **Project**: `Xenolexia.Linux` (name is historical; it is the cross-platform desktop app).
- **Project Structure**:
  - `Program.cs` - Application entry point with service initialization
  - `App.xaml` / `App.xaml.cs` - Application class
  - `Views/MainWindow.xaml` - Main window with tabbed interface
  - `Views/LibraryView.xaml` - Library (bookshelf grid), import, online discover
  - `Views/VocabularyView.xaml` - Vocabulary list, export
  - `Views/AboutView.xaml` - About (version, license, credits)
  - `ViewModels/` - MVVM using CommunityToolkit.Mvvm

- **Features**:
  - Tab-based interface (Library, Vocabulary, Settings, About)
  - Bookshelf grid, import from file, discover (Gutenberg, Standard Ebooks, Open Library)
  - Vocabulary screen with export (CSV, Anki, JSON)
  - Service initialization on startup; modern Avalonia UI

### 3. UI Layer: MVVM ViewModels and Views ✅

#### Android ViewModels:
- `BaseViewModel` - Base class with INotifyPropertyChanged
- `LibraryViewModel` - Manages book collection and operations
- `VocabularyViewModel` - Manages vocabulary items and export

#### Linux ViewModels:
- `ViewModelBase` - Base class using CommunityToolkit.Mvvm
- `MainWindowViewModel` - Main window coordination
- `LibraryViewModel` - Book management with async commands
- `VocabularyViewModel` - Vocabulary management with export functionality

#### Views:
- **Android**: XAML pages with CollectionView for lists
- **Linux**: XAML UserControls with DataGrid for tables

### 4. Additional Services ✅

#### BookDownloadService
- **Location**: `Xenolexia.Core/Services/BookDownloadService.cs`
- **Features**:
  - Search books from Project Gutenberg (Gutendex API)
  - Search books from Standard Ebooks (OPDS feed)
  - Search books from Open Library (Open Library API)
  - Download books with progress tracking
  - Support for EPUB, TXT formats

#### ImageProcessingService
- **Location**: `Xenolexia.Core/Services/ImageProcessingService.cs`
- **Features**:
  - Extract cover images from EPUB files
  - Download cover images from URLs
  - Resize images (placeholder implementation)
  - Convert image formats (placeholder implementation)

#### ExportService
- **Location**: `Xenolexia.Core/Services/ExportService.cs`
- **Features**:
  - Export vocabulary to CSV format
  - Export vocabulary to Anki TSV format (tab-separated)
  - Export vocabulary to JSON format
  - Filtering options (by status, language)
  - Configurable export options (include context, SRS data, book info)

## Project Structure

```
xenolexia-csharp/
├── Xenolexia.Core/
│   ├── Models/              # Domain models
│   └── Services/            # Business logic services
│       ├── IBookDownloadService.cs
│       ├── BookDownloadService.cs
│       ├── IImageProcessingService.cs
│       ├── ImageProcessingService.cs
│       ├── IExportService.cs
│       └── ExportService.cs
├── Xenolexia.Android/       # Android MAUI app
│   ├── MauiProgram.cs
│   ├── App.xaml
│   ├── AppShell.xaml
│   ├── Views/
│   │   ├── LibraryPage.xaml
│   │   ├── VocabularyPage.xaml
│   │   ├── ReaderPage.xaml
│   │   └── SettingsPage.xaml
│   └── ViewModels/
│       ├── BaseViewModel.cs
│       ├── LibraryViewModel.cs
│       └── VocabularyViewModel.cs
└── Xenolexia.Linux/         # Linux Avalonia app
    ├── Program.cs
    ├── App.xaml
    ├── Views/
    │   ├── MainWindow.xaml
    │   ├── LibraryView.xaml
    │   └── VocabularyView.xaml
    └── ViewModels/
        ├── ViewModelBase.cs
        ├── MainWindowViewModel.cs
        ├── LibraryViewModel.cs
        └── VocabularyViewModel.cs
```

## NuGet Packages

### Core Library:
- **VersOne.Epub** (3.3.4) – EPUB reading: metadata, TOC, chapters, cover extraction (replaces custom EPUB/OPF parsing and EPubSharp/HtmlAgilityPack)
- **System.Data.SQLite.Core** – SQLite storage
- **Newtonsoft.Json** – JSON (e.g. Gutendex, Open Library APIs)

### Android (MAUI):
- `Microsoft.Maui.Controls` (8.0.49)
- `Microsoft.Maui.Controls.Compatibility` (8.0.49)
- `Microsoft.Maui.Essentials` (8.0.49)

### Desktop (Xenolexia.Linux — Avalonia, Linux/macOS/Windows):
- `Avalonia` (11.0.7), `Avalonia.Desktop` (11.0.7), `Avalonia.ReactiveUI` (11.0.7)
- `Avalonia.Fonts.Inter` (11.0.7), `Avalonia.Themes.Fluent` (11.0.7)
- `CommunityToolkit.Mvvm` (8.2.2)

## Building and Running

### Prerequisites:
- .NET 8.0 SDK
- **Desktop (Linux, macOS, Windows)**: no extra deps; Avalonia is included.
- **Android**: Android SDK and NDK

### Build Commands:

```bash
dotnet restore

# Desktop (Linux, macOS, Windows) — same project
cd Xenolexia.Linux
dotnet build
dotnet run

# Core only
cd Xenolexia.Core
dotnet build

# Android (requires Android SDK)
cd Xenolexia.Android
dotnet build
```

## Notes

1. **Image Processing**: The image resizing and format conversion methods are currently placeholders. For production, implement using ImageSharp or System.Drawing.

2. **Service Initialization**: Linux app initializes services in `Program.cs` before starting the UI. Android app uses MAUI's dependency injection in `MauiProgram.cs`.

3. **Database / data (desktop)**: Linux, macOS, Windows use `~/.xenolexia/` (xenolexia.db, books/, exports/, covers/). Android uses app data directory.

### 5. Desktop ebook reader: bookshelf, import, online libraries ✅

- **Bookshelf home screen**: Library tab shows a grid of book cards (cover placeholder, title, author, progress). Right-click to remove from library.
- **Import from local storage**: "Import from file" opens a file picker; supported formats: EPUB, PDF, TXT, FB2, MOBI. Files are copied to `~/.xenolexia/books/`, metadata and cover extracted when possible, and the book is added to the library.
- **Import from free online libraries**: "Get from online" opens a panel to search Project Gutenberg, Standard Ebooks, or Open Library. Search results show title, author, and a Download button; downloaded books are added to the library with cover when available.
- **Ebook format support**: Core supports **EPUB**, **PDF**, **TXT**, **FB2**, and **MOBI** (import and display in library). Full parsing (chapters, TOC) is implemented for EPUB (via **VersOne.Epub**) and TXT; PDF uses metadata-only (title from filename); FB2/MOBI can be imported but full parsing is not yet implemented.
- **Libraries over custom code**: EPUB parsing and cover extraction use **VersOne.Epub** (open source, Unlicense); custom ZIP/OPF/HTML parsing and EPubSharp/HtmlAgilityPack have been removed.
- **Core services**: `IBookImportService` / `BookImportService` (local import and add-downloaded-book), `IFilePickerService` / `FilePickerService` (Avalonia file picker). Linux app registers these and uses them in `LibraryViewModel`.

### 6. About application screen ✅

- **About tab**: New tab in the main window shows app name, tagline, version (from assembly), short description, supported formats, license (MIT), and credits (e.g. .NET 8, Avalonia UI, VersOne.Epub).

## Next Steps (see FEATURES.md for full roadmap)

1. **Reader screen**: EPUB/TXT rendering with customizable fonts, themes (light/dark/sepia), margins, line spacing; persist progress on exit.
2. **Hover-to-reveal**: Translation popup on hover in reader; integrate ITranslationService (LibreTranslate; add MyMemory/Lingva as fallbacks).
3. **SM-2 spaced repetition**: Implement in Core; store interval/ease in VocabularyItem.
4. **Frequency-based word selection**: Use open word lists (FOSS) in TranslationEngine.
5. **Translation cache**: SQLite table for translations; use when offline.
6. **Settings screen**: Language pair, proficiency, word density.
7. **Vocabulary review**: Flashcard-style review view.
8. **Error handling and UX**: Toasts/dialogs; unit tests for services.

## Testing

Desktop (Linux, macOS, or Windows):
```bash
cd Xenolexia.Linux
dotnet build
dotnet run
```
The app starts with a tabbed interface: Library (bookshelf, import, discover), Vocabulary, Settings, About.
