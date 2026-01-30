# Xenolexia C# - Setup and Development Guide

## Project Status

✅ **Core Library**: Complete with models and services
✅ **Desktop App**: Xenolexia.Desktop (Avalonia) — Linux, macOS, Windows
✅ **Build System**: Solution builds successfully

## Project Structure

```
xenolexia-csharp/
├── Xenolexia.Core/              # Shared core library
│   ├── Models/                  # Domain models
│   └── Services/                # Business logic services
├── Xenolexia.Desktop/           # Desktop app (Linux, macOS, Windows) — Avalonia
│   ├── Program.cs
│   ├── App.xaml
│   ├── Views/
│   └── ViewModels/
└── Xenolexia.sln
```

## Building

```bash
dotnet restore
dotnet build

# Run desktop app (Linux, macOS, or Windows)
cd Xenolexia.Desktop
dotnet run
```

## Data Location (Desktop)

- **Linux / macOS / Windows**: `~/.xenolexia/` (xenolexia.db, books/, exports/, covers/)

## API Configuration

See README and FEATURES.md for translation (LibreTranslate) and ebook discovery (Gutenberg, Standard Ebooks, Open Library) setup.
