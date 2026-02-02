# Xenolexia C# — Implementation Plan (Missing Features)

This plan covers implementing what is missing in **xenolexia-csharp** to reach feature parity with the reference (xenolexia-typescript Electron). UI and platform code stay in C#; optional shared C (xenolexia-shared-c) can be used later for tokenizer/replacer if desired.

**Reference:** [docs/REQUIREMENTS_IMPLEMENTATION_STATUS.md](../docs/REQUIREMENTS_IMPLEMENTATION_STATUS.md)

---

## Current State

**Implemented:** Library (import, discover, grid), Vocabulary (list, search, filter, export, SRS backend), Reader shell (open book, chapters, TOC, raw content), About, Core schema + SM-2 + export spec. **TranslationEngine** and **ProcessedChapter** / **ForeignWordData** exist in Core but are not used in the reader.

**Missing:** Reader word replacement + hover-to-reveal + popup + save; reader customization; session/progress persistence; Review screen; Settings content; Onboarding; Statistics screen; preferences/session storage API.

---

## Phase 1: Reader — Word Replacement and Display

**Goal:** Show chapter content with a portion of words in the target language; display is still a single block (no interactive spans yet).

### 1.1 Wire TranslationEngine into ReaderViewModel

- **ReaderViewModel** currently takes `IBookParserService` only. Add **ITranslationService** and **TranslationEngine** (or a thin service that uses them). Resolve from `Program.ServiceProvider` when creating the reader VM (see `MainWindowViewModel.OnOpenBookRequested`).
- **Book** already has `LanguagePair` (SourceLanguage, TargetLanguage), `ProficiencyLevel`, `WordDensity`. Use these for processing.
- On **LoadAsync** and when changing chapter (**GoToChapterAsync**):
  - Parse book/chapter as today (BookParserService).
  - Call **TranslationEngine.ProcessChapterAsync**(chapter, book.LanguagePair, book.ProficiencyLevel, book.WordDensity) to get **ProcessedChapter** (ProcessedContent + ForeignWords).
  - Expose **ProcessedContent** (and **ForeignWords**) instead of raw/stripped content. Keep **CurrentChapterContent** bound to processed text for now so the reader shows replaced words.
- **Dependency:** TranslationEngine needs ITranslationService (already in ServiceProvider). TranslationService may need to be passed or created with same scope as in Program (LibreTranslate/MyMemory). Ensure Book has LanguagePair, ProficiencyLevel, WordDensity set (LibraryViewModel when opening a book).

**Deliverables:** Reader shows text with some words in target language; no UI for “reveal” or “save” yet.

### 1.2 Reader content as interactive spans (hover-to-reveal, popup, save)

- **Model:** Keep using **ProcessedChapter** with **ForeignWords** (list of OriginalWord, ForeignWord, StartIndex, EndIndex). Build a representation suitable for UI: e.g. list of segments (text + whether it’s a foreign word + ForeignWordData).
- **ReaderView:** Replace the single `TextBlock` bound to `CurrentChapterContent` with a control that can show inline “foreign” segments differently and handle pointer events:
  - **Option A (simpler):** Use a **ItemsControl** or **StackPanel** of inline runs: for each segment, a `TextBlock` or `Run` with a different style for foreign words; wrap foreign segments in a control that handles **PointerEnter** / **PointerLeave** (hover) and **Tapped** (optional). On hover/tap, show a **Popup** or **ToolTip** with original word, optional context, and “Save to vocabulary” button.
  - **Option B:** Use a single **TextBlock** with **Inlines** (runs); each foreign word is a **Run** with a distinct style and a way to attach a tooltip/popup (e.g. wrapping in a control that provides pointer events may require a different structure). Avalonia supports InlineUIContainer or similar for inline popups.
- **Popup content:** Original word (and, if available, context sentence), “Save to vocabulary” button. On “Save”, call **IStorageService.AddVocabularyItemAsync** with a **VocabularyItem** built from the current **ForeignWordData** (and current book id/title, context if available). Refresh or mark the word so the button doesn’t double-save.
- **Optional:** “I knew this” button that adds the word and immediately calls **RecordReviewAsync** with quality 5 (or skip if you don’t want to mix “save” and “review” here).

**Deliverables:** Reader shows processed content; foreign words are visually distinct; hover (or tap) shows popup with original + “Save to vocabulary”; save persists to StorageService.

---

## Phase 2: Reader — Progress, Session, and Customization

### 2.1 Persist progress on exit

- **Book** already has **CurrentChapter**, **CurrentLocation**, **Progress**. When the user closes the reader or switches chapter:
  - Update **Book** with current chapter index, current scroll position (if you store it), and progress (e.g. percentage from current chapter / total chapters).
  - Call **IStorageService.UpdateBookAsync(book)**. Ensure ReaderViewModel has access to IStorageService and the same Book instance (or reload book from storage and update before save).
- **ReaderViewModel.Close** (or when **IsReaderVisible** becomes false): persist book progress before clearing the reader. If you use a “current position” (e.g. paragraph or offset), store it in **CurrentLocation** as a string (e.g. chapter index + offset).

**Deliverables:** Closing the reader saves current chapter and progress; reopening the book restores position (already partially supported if Book is reloaded from storage).

### 2.2 Reading session (start/end, words revealed/saved)

- **Storage:** Tables **reading_sessions** and **preferences** exist; add to **IStorageService** (and **StorageService**):
  - **StartReadingSessionAsync(bookId)** → create a row in **reading_sessions** (id, book_id, started_at = now ms, ended_at = null, pages_read/words_revealed/words_saved = 0).
  - **EndReadingSessionAsync(sessionId, wordsRevealed, wordsSaved)** → set ended_at, words_revealed, words_saved (and optionally duration).
  - **GetActiveSessionForBookAsync(bookId)** → optional, to resume a session.
- **ReaderViewModel:** When reader opens, call **StartReadingSessionAsync** (or get active session). Track **words revealed** (e.g. count hover/popup opens) and **words saved** (count AddVocabularyItemAsync from reader). On close, call **EndReadingSessionAsync** with those counts.
- **Deliverables:** Each reading open/close creates or updates a reading session with words revealed/saved.

### 2.3 Reader customization (font, theme, spacing)

- **Preferences:** Add to **IStorageService** / **StorageService**:
  - **GetPreferencesAsync()** → read from **preferences** table (e.g. key `reader_theme`, `reader_font_size`, etc.) and return **UserPreferences** (or a minimal DTO). Use existing **UserPreferences** in Core/Models/Statistics.cs (ReaderSettings, DefaultWordDensity, etc.).
  - **SavePreferencesAsync(UserPreferences)** → write keys into **preferences** table.
- **ReaderSettings** (Core/Models/Reader.cs) already has Theme, FontFamily, FontSize, LineHeight, margins. Store these in preferences and load when the app starts; pass into ReaderViewModel or a shared “reader settings” service.
- **ReaderView:** Apply **Theme** (Light/Dark/Sepia) via Avalonia styles or resource dictionaries (e.g. different background/foreground for reader content panel). Apply **FontSize**, **FontFamily**, **LineHeight** to the reader content control. Margins can be applied to the container.
- **Settings tab (Phase 3):** Add UI for reader theme, font size, font family, line spacing; save via **SavePreferencesAsync**. ReaderViewModel (or ReaderView) reads from the same preferences so changes can apply on next open or, if you support it, live.

**Deliverables:** Reader appearance driven by stored preferences; Settings tab can edit reader defaults (see Phase 3).

---

## Phase 3: Settings and Onboarding

### 3.1 Settings tab content

- Replace the placeholder “Settings - Coming Soon” in **MainWindow.xaml** with a **SettingsView** (UserControl) and **SettingsViewModel**.
- **SettingsViewModel:** Load **UserPreferences** via **GetPreferencesAsync** (once preferences API exists). Expose properties: DefaultSourceLanguage, DefaultTargetLanguage, DefaultProficiencyLevel, DefaultWordDensity, ReaderSettings (Theme, FontSize, FontFamily, LineHeight), DailyGoal, NotificationsEnabled. Commands: Save (call **SavePreferencesAsync**), optionally Reset to defaults.
- **SettingsView:** Form with:
  - Language pair (source/target combo boxes; use **Language** enum and existing list).
  - Proficiency (Beginner / Intermediate / Advanced).
  - Word density (slider or number, e.g. 0.1–0.5).
  - Reader: theme (Light/Dark/Sepia), font size, font family, line spacing.
  - Daily goal (minutes), notifications (checkbox).
- When the user opens a book, Library/Reader can use **UserPreferences** for default language/proficiency/density if the **Book** doesn’t override them (e.g. allow per-book overrides later).

**Deliverables:** Settings tab persists and loads preferences; reader and app use these defaults where applicable.

### 3.2 Onboarding flow

- **First run:** On app startup (e.g. in **Program.Main** or **App.OnFrameworkInitializationCompleted**), call **GetPreferencesAsync** and check **HasCompletedOnboarding**. If false, show an **OnboardingView** (or a dedicated window) instead of the main tabs (or as a modal overlay).
- **OnboardingView:** Simple steps: (1) Welcome / short explanation, (2) Select source language, (3) Select target language, (4) Select proficiency, (5) Word density, (6) “Get started”. On finish, set **HasCompletedOnboarding = true** and **SavePreferencesAsync**; then show main window.
- **Skip:** “Skip” or “I’ll do this later” sets **HasCompletedOnboarding = true** with default preferences and closes onboarding.
- **MainWindowViewModel:** If you show onboarding as overlay, add a property **ShowOnboarding**; when false (after completion), hide onboarding and show tabs. Alternatively, show a separate OnboardingWindow first; on close, show MainWindow.

**Deliverables:** First launch shows onboarding; after completion (or skip), main app is used; subsequent launches skip onboarding.

---

## Phase 4: Review Screen (SRS Flashcards)

### 4.1 Review view and view model

- **ReviewViewModel:** 
  - Inject **IStorageService**. Expose: **CurrentItem** (VocabularyItem), **DueCount** (int), **IsFlipped** (bool), **ReviewedCount** (int).
  - **LoadDueAsync():** Call **GetVocabularyDueForReviewAsync(limit: 20)** (or 50); store in a list, set **CurrentItem** to first, **DueCount** = list count.
  - **FlipCommand:** Set **IsFlipped = true** (show back of card).
  - **GradeCommand(int quality):** Call **RecordReviewAsync(CurrentItem.Id, quality)**; advance to next item (or remove from list); if none left, call **LoadDueAsync** again or show “No more due” state. quality: 0 = Again, 1 = Hard, 3 = Good, 4 = Easy, 5 = Already knew (match TypeScript ReviewScreen).
- **ReviewView:** 
  - **Front:** Show **CurrentItem.TargetWord** (foreign word). 
  - **Back (when flipped):** Show **CurrentItem.SourceWord** and **ContextSentence** (if any). 
  - Buttons: Again (0), Hard (1), Good (3), Easy (4), Already knew (5). Bind to **GradeCommand** with the corresponding quality.
  - Show “Due today: N” (DueCount) and “Reviewed: M” (ReviewedCount). Optional: “No cards due” when list is empty after load.
- **Navigation:** Add a “Review” tab in **MainWindow** (or a button in the header that opens ReviewView). **MainWindowViewModel** adds **ReviewView** and a way to show it (e.g. new TabItem “Review”, or a flyout).

**Deliverables:** User can open Review, see due count, flip cards, grade with SM-2; backend (GetVocabularyDueForReviewAsync, RecordReviewAsync) is already implemented.

---

## Phase 5: Statistics Screen

### 5.1 Statistics data from storage

- **IStorageService** / **StorageService:** Add methods to support **ReadingStats** and optional “reading over time”:
  - **GetReadingStatsAsync():** Aggregate from **reading_sessions** and **vocabulary**: total books read (or sessions), total reading time (sum of duration), total words learned (vocabulary count or status = learned), current streak (consecutive days with at least one session), longest streak, words revealed/saved today. **ReadingStats** model is in Core/Models/Statistics.cs. Implement by querying **reading_sessions** and **vocabulary** (e.g. COUNT, SUM, and streak logic in C#).
  - Optional: **GetSessionCountByDayAsync(lastDays)** → list of (date, count or minutes) for a simple “reading over time” chart (e.g. last 7 days).
- **StatisticsViewModel:** Load **GetReadingStatsAsync()** on appear; expose **ReadingStats** and optional list for chart. Refresh when navigating to the tab (or use a Refresh command).
- **StatisticsView:** 
  - Display **TotalBooksRead**, **TotalReadingTime**, **TotalWordsLearned**, **CurrentStreak**, **LongestStreak**, **WordsRevealedToday**, **WordsSavedToday** (labels + values).
  - Optional: bar or line chart for “last 7 days” reading time or session count (use a simple chart control or draw with Avalonia Canvas/Skia if available).
- **Navigation:** Add “Statistics” tab in **MainWindow** (or under Settings). **MainWindowViewModel** adds **StatisticsView** and binds it.

**Deliverables:** Statistics tab shows reading stats; data comes from existing **reading_sessions** and **vocabulary** tables.

---

## Phase 6: Preferences and Sessions API (Core)

This phase can be done earlier (e.g. alongside Phase 2) so that Settings and Reader have a real backend.

### 6.1 IStorageService additions

- **Preferences:** 
  - **GetPreferencesAsync()** → returns **UserPreferences**. Read from **preferences** table (e.g. keys `source_lang`, `target_lang`, `proficiency`, `word_density`, `reader_theme`, `reader_font_size`, `onboarding_done`, etc.). Map string values to enums/numbers.
  - **SavePreferencesAsync(UserPreferences)** → write key/value pairs into **preferences** (replace or insert).
- **Reading sessions:**
  - **StartReadingSessionAsync(string bookId)** → INSERT into **reading_sessions** (id, book_id, started_at = now ms, ended_at = null, pages_read = 0, words_revealed = 0, words_saved = 0). Return session id.
  - **EndReadingSessionAsync(string sessionId, int wordsRevealed, int wordsSaved)** → UPDATE set ended_at = now, words_revealed, words_saved, and duration = (ended_at - started_at) in seconds.
  - Optional: **GetActiveSessionForBookAsync(string bookId)** → SELECT where book_id = ? AND ended_at IS NULL ORDER BY started_at DESC LIMIT 1.

Implement in **StorageService**; keep schema (reading_sessions, preferences) as already created.

**Deliverables:** Callers can read/write preferences and create/end reading sessions; Settings and Reader use these.

---

## Implementation Order (Suggested)

| Order | Phase | Depends on |
|-------|--------|------------|
| 1 | **Phase 6** (preferences + sessions API) | None |
| 2 | **Phase 1.1** (TranslationEngine in ReaderViewModel) | None (TranslationEngine + ITranslationService exist) |
| 3 | **Phase 1.2** (interactive spans, popup, save) | Phase 1.1 |
| 4 | **Phase 2.1** (progress persist) | Phase 6 (UpdateBookAsync exists; ensure Book is saved on close) |
| 5 | **Phase 2.2** (reading session) | Phase 6 |
| 6 | **Phase 2.3** (reader customization) | Phase 6 |
| 7 | **Phase 3.1** (Settings tab) | Phase 6 |
| 8 | **Phase 3.2** (Onboarding) | Phase 6 |
| 9 | **Phase 4** (Review screen) | None (SRS API exists) |
| 10 | **Phase 5** (Statistics) | Phase 6 (sessions) |

You can implement **Phase 4 (Review)** early since it only needs existing **IStorageService** SRS methods; **Phase 5** needs session data, so at least **Phase 6** and **Phase 2.2** should be in place for meaningful stats.

---

## Optional / Later

- **xenolexia-shared-c tokenizer/replacer:** If you later add a C tokenizer + replacer (see docs/PLAN_C_SHARP_OBJC_SHARED_C.md), add P/Invoke in C# and optionally replace TranslationEngine’s tokenization/selection with a call to the shared lib; translation lookup stays in C# (ITranslationService).
- **Per-book language/density override:** Allow editing language pair and density per book (e.g. in a book detail dialog); ReaderViewModel would use Book’s values (already there) instead of only global preferences.
- **Reader: “I knew this”** from popup that calls RecordReviewAsync with quality 5 and optionally marks the word as learned.
- **Unit tests:** Add tests for TranslationEngine.ProcessChapterAsync, StorageService preferences/sessions, and ReviewViewModel (with mocked IStorageService).

---

## Summary Checklist

- [ ] **Phase 6:** IStorageService: GetPreferencesAsync, SavePreferencesAsync, StartReadingSessionAsync, EndReadingSessionAsync (and optional GetActiveSessionForBookAsync).
- [ ] **Phase 1.1:** ReaderViewModel uses TranslationEngine; shows ProcessedContent in reader.
- [ ] **Phase 1.2:** ReaderView: foreign words as interactive spans; hover/tap → popup with original + “Save to vocabulary”.
- [ ] **Phase 2.1:** Persist book progress on reader close.
- [ ] **Phase 2.2:** Start/end reading session; track words revealed/saved.
- [ ] **Phase 2.3:** Reader theme, font, spacing from preferences; apply in ReaderView.
- [ ] **Phase 3.1:** SettingsView + SettingsViewModel; persist language pair, proficiency, density, reader settings.
- [ ] **Phase 3.2:** OnboardingView on first run; set HasCompletedOnboarding.
- [ ] **Phase 4:** ReviewView + ReviewViewModel; flashcards + SM-2 grading (Again/Hard/Good/Easy/Already knew).
- [ ] **Phase 5:** GetReadingStatsAsync (and optional chart data); StatisticsView + StatisticsViewModel; Statistics tab.

This plan keeps all implementation in C# (Avalonia + Xenolexia.Core) and uses existing Core models and services; optional shared C can be integrated later without changing the overall structure.
