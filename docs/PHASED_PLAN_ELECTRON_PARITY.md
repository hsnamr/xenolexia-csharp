# Xenolexia C# — Phased Plan for Feature Parity with Electron

This document outlines **what’s next** for **xenolexia-csharp** and a **phased plan** to bring it to **feature parity** with **xenolexia-typescript (Electron)**.

---

## 1. Current state vs Electron

| Area | Electron | C# | Gap |
|------|----------|-----|-----|
| **Library** | Import, discover, **grid/list toggle**, book detail modal | Import, discover, **grid only**, book cards + detail | List view toggle |
| **Reader** | Word replacement, hover-to-reveal, translation popup, save to vocab, progress/session, reader settings | Same (word replacement, hover-to-reveal, tooltip, save, progress/session, reader settings) | None |
| **Vocabulary** | List, search, filter, word detail modal, edit/delete, export (CSV/Anki/JSON) | Same | None |
| **Review** | Flashcard UI, SM-2 grading (Again/Hard/Good/Easy/Already Knew) | Same | None |
| **Settings / Onboarding** | Language pair, proficiency, density, daily goal; first-run onboarding | Same | None |
| **Statistics** | Stats + **“reading over time” bar chart** (last 7 days) | Stats only (no chart) | Chart |
| **Polish** | **Keyboard shortcuts**, **window state persistence**, **system tray** (Show/Hide, Quit), **E2E tests** (Playwright) | KeyDown only in dialogs; no window persistence; no tray; **unit tests only** | Shortcuts, window state, tray, E2E |
| **Docs** | README/PLAN reflect v1 complete | README/FEATURES still mark Customizable reader, Hover-to-reveal, SM-2, Review as “planned” | Update docs |

**Conclusion:** Core flows (library, reader, vocabulary, review, settings, onboarding, statistics) are already at parity. Gaps are: **library list view**, **statistics chart**, **keyboard shortcuts**, **window state persistence**, **system tray**, **E2E tests**, and **documentation updates**.

---

## 2. Phased plan

### Phase 1: Documentation (low effort)

**Goal:** README and FEATURES.md accurately reflect implemented features.

| Step | Task | Notes |
|------|------|--------|
| 1.1 | Update **FEATURES.md** | Mark **Customizable reader**, **Hover-to-reveal**, **Spaced repetition (SM-2)**, **Review** as ✅ Done; remove or update “planned”. |
| 1.2 | Update **README.md** | In Features / Core Reading and Vocabulary tables, set Customizable reader, Hover-to-reveal, SM-2, Review to ✅. Add short note that desktop app is feature-complete for core. |

**Exit criteria:** Anyone reading README/FEATURES sees current implementation status.

---

### Phase 2: Library — List view toggle (medium effort)

**Goal:** Match Electron’s grid/list toggle in the library.

| Step | Task | Notes |
|------|------|--------|
| 2.1 | Add view mode state | In **LibraryViewModel** (or equivalent), add a property for view mode: `Grid` \| `List` (and a command to toggle). |
| 2.2 | Implement list layout in **LibraryView.xaml** | When mode is List, show books in a list (e.g. `ItemsRepeater` / `ListBox` with a compact item template: small cover, title, author, progress in one row). Reuse the same item data; only the template and container change. |
| 2.3 | Add toggle control in UI | Button or segmented control in the library toolbar: “Grid” / “List”. Bind to the view-mode property. Optionally persist the choice (e.g. in preferences). |

**Exit criteria:** User can switch between grid and list in the library; behavior matches Electron conceptually.

---

### Phase 3: Statistics — “Reading over time” chart (medium effort)

**Goal:** Add a “reading over time” bar chart (last 7 days) to match Electron.

| Step | Task | Notes |
|------|------|--------|
| 3.1 | **Option A (quick parity):** Add chart UI only | In **StatisticsView.xaml**, add a “Reading over time” section with 7 bars (e.g. Mon–Sun or last 7 days). Use **WordsRevealedToday** for “today” and 0 for past days (same as Electron’s current behavior). No backend change. |
| 3.2 | **Option B (full parity):** Per-day data | Add **GetWordsRevealedByDayAsync(int lastDays)** to **IStorageService** and implement in **StorageService** / **LiteDbStorageService** (query `reading_sessions` grouped by date, sum `words_revealed` per day). **StatisticsViewModel** calls it and exposes a list of `(Date, Count)`. Chart binds to that. |
| 3.3 | Wire chart to **StatisticsViewModel** | Expose a collection for the last 7 days (either today + 6 zeroes, or real per-day from 3.2). Use Avalonia controls (e.g. `Rectangle` for bars, or a simple chart lib if you add one) to render bars with labels. |

**Exit criteria:** Statistics screen shows a “Reading over time” chart for the last 7 days; at least today’s words revealed is correct (Option B improves past days).

---

### Phase 4: Polish — Keyboard shortcuts, window state, system tray (medium–high effort)

**Goal:** Match Electron’s polish: global shortcuts, window state persistence, system tray.

| Step | Task | Notes |
|------|------|--------|
| 4.1 | **Keyboard shortcuts** | In **MainWindow** or **App**, register **KeyBindings** or **InputBindings** (Avalonia): e.g. Ctrl+1–5 to switch tabs (Library, Reader, Vocabulary, Review, Settings), Ctrl+O to focus library / open import, Escape to close dialogs. Document in README or in-app help. |
| 4.2 | **Window state persistence** | On **MainWindow** closing: save position, size, and **WindowState** (Normal/Maximized) to user settings (e.g. **UserPreferences** or a separate settings file). On startup: restore position, size, and state. Use Avalonia’s `Position`, `Width`, `Height`, `WindowState`. |
| 4.3 | **System tray** | Use Avalonia’s **TrayIcon** (or equivalent): show icon in system tray; context menu: “Show/Hide” (toggle main window visibility), “Quit” (exit app). On “Show”, restore and focus main window. Optional: minimize to tray when closing window (user preference). |

**Exit criteria:** Shortcuts work; window size/position/state restore on restart; tray icon with Show/Hide and Quit.

---

### Phase 5: E2E tests (optional, higher effort)

**Goal:** Automated UI tests comparable to Electron’s Playwright E2E.

| Step | Task | Notes |
|------|------|--------|
| 5.1 | Choose E2E approach | Options: **Avalonia UI Test** (if available for your Avalonia version), **dotnet Playwright** driving the app (if feasible), or **manual smoke test checklist** only. |
| 5.2 | Add smoke test checklist | Create **xenolexia-csharp/docs/SMOKE_TEST_CHECKLIST.md** (mirror Electron’s: launch, onboarding, library import, reader, vocabulary, review, export, settings, statistics, tray, window state). |
| 5.3 | Implement E2E (if chosen) | Add a test project that launches the app, navigates tabs, imports a book, opens reader, saves a word, runs a review, and checks statistics. Run in CI if possible. |

**Exit criteria:** Either automated E2E tests run in CI, or a clear smoke test checklist is used for releases.

---

## 3. Suggested order and effort

| Phase | Description | Effort | Suggested order |
|-------|-------------|--------|------------------|
| **1** | Documentation | Low | First |
| **2** | Library list view | Medium | Second |
| **3** | Statistics chart | Medium | Third |
| **4** | Shortcuts, window state, tray | Medium–High | Fourth |
| **5** | E2E / smoke checklist | Optional / High | Last or skip |

**Total (Phases 1–4):** about 2–4 weeks for one developer, depending on familiarity with Avalonia and system tray on each OS.

---

## 4. Out of scope for this plan

- **Format parity:** Electron and C# both support EPUB, TXT; C# has PDF/FB2/MOBI. No change required for parity.
- **Discovery:** Both have Gutenberg, Standard Ebooks, Open Library.
- **Frequency-based word lists:** Optional enhancement; not required for Electron parity.
- **MOBI full-text via FOSS:** Optional; documented as skipped where no FOSS lib exists.

---

## 5. References

- **Electron feature set:** `xenolexia-typescript/electron-app/PLAN.md`, `electron-app/docs/SMOKE_TEST_CHECKLIST.md`
- **Requirements comparison:** repo root `docs/REQUIREMENTS_IMPLEMENTATION_STATUS.md`
- **Monorepo roadmap:** `docs/ROADMAP.md`

---

*This plan is for bringing xenolexia-csharp to feature parity with xenolexia-typescript (Electron). After Phase 1–4, C# desktop will match Electron on library (grid+list), statistics (with chart), and polish (shortcuts, window state, tray).*
