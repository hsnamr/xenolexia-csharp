# Xenolexia C# (Avalonia) – Smoke Test Checklist

Use this checklist when testing the Xenolexia desktop app on **Windows**, **macOS**, or **Linux** after a build or before release.

---

## Prerequisites

- Built app: run `dotnet build` and `dotnet run` from `Xenolexia.Desktop`, or run from IDE.
- Optionally: one EPUB or TXT file for import.

---

## 1. Launch and onboarding

- [ ] App launches without crash.
- [ ] On first run, onboarding is shown (Welcome, language pair, proficiency, word density, Get started).
- [ ] Completing onboarding (or Skip) closes onboarding and shows the main tabs (Library, Vocabulary, Review, Settings, Statistics, About).
- [ ] On next launch, onboarding is not shown (main tabs appear directly).

---

## 2. Library

- [ ] **Library** tab shows (empty state or list of books).
- [ ] **Grid** / **List** toggle switches between grid of cards and list view; choice persists after restart.
- [ ] **Import from file** opens a file dialog; selecting an EPUB, PDF, TXT, or FB2 adds the book to the library.
- [ ] Book appears in the grid/list with title (and cover placeholder).
- [ ] **Read** opens the reader for that book; **Remove** removes it from the library.
- [ ] **Get from online** opens the discovery panel; search and download from Gutenberg, Standard Ebooks, or Open Library work without crash.

---

## 3. Reader

- [ ] Reader opens with chapter content.
- [ ] Some words appear in the target language (word replacement).
- [ ] **Hover** on a foreign word shows a tooltip (original word, optional context).
- [ ] Tooltip has **Save to vocabulary**; using it adds the word (no crash).
- [ ] **Chapter navigation** (prev/next or chapter list) changes content.
- [ ] **Reader settings** (theme, font, line spacing) are available in Settings and change appearance.
- [ ] Closing the reader (Back or equivalent) returns to Library; progress is saved (reopening the book restores position/chapter).

---

## 4. Vocabulary and review

- [ ] **Vocabulary** tab shows the list of saved words (including the one just saved).
- [ ] Search/filter works.
- [ ] **Review** tab shows “Due today” count and flashcard(s) when there are due items.
- [ ] Flipping a card shows the back (source word, context); grading buttons (Again/Hard/Good/Easy/Already Knew) work and advance to the next card.
- [ ] After reviewing, when no more due, “No cards due” or similar is shown.

---

## 5. Export

- [ ] From **Vocabulary**, **Export** (CSV, Anki, or JSON) is available.
- [ ] Choosing a format and location saves a file; opening it shows the expected format (headers and data).

---

## 6. Settings and statistics

- [ ] **Settings** tab: language pair, proficiency, word density, reader defaults (theme, font, line spacing), daily goal can be changed and persist after restart.
- [ ] **Statistics** tab: shows reading stats (books read, reading time, words learned, streak, words revealed/saved today) and **“Reading over time”** chart (last 7 days).

---

## 7. Keyboard shortcuts

- [ ] **Ctrl+1** through **Ctrl+6** switch to Library, Vocabulary, Review, Settings, Statistics, About respectively.

---

## 8. Window state and system tray

- [ ] Window can be resized and maximized; **size, position, and maximized state** are restored on next launch.
- [ ] **Tray icon** appears in the system tray (Windows/Linux taskbar or macOS menu bar).
- [ ] Tray menu **Show/Hide** shows or hides the main window.
- [ ] Tray menu **Quit** closes the app.

---

## 9. About and build

- [ ] **About** tab shows app name and version.
- [ ] No unhandled exceptions during normal use; app exits cleanly.

---

## Notes

- If any step fails, note the OS, .NET version, and error message for debugging.
- Run this checklist on **Windows**, **macOS**, and **Linux** at least once for full coverage.
