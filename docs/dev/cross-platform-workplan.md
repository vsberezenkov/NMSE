# Cross-Platform Work Plan: NMSE WinForms -> Avalonia UI

## Single Source of Truth for Cross-Platform Migration

**Last updated:** 2026-03-22

> **Note:** The previous Eto.Forms-based workplan is archived in [`cross-platform-workplan-old.md`](cross-platform-workplan-old.md).

---

## Table of Contents

1. [Decision Summary](#1-decision-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Phase 0 - Wine/Compatibility Layer Quick-Ship](#3-phase-0--winecompatibility-layer-quick-ship)
4. [Phase 1 - Avalonia UI Implementation](#4-phase-1--avalonia-ui-implementation)
5. [Phase 2 - Polish, Packaging & Release](#5-phase-2--polish-packaging--release)
6. [Working Methodology](#6-working-methodology)
7. [Risk Register](#7-risk-register)
8. [Appendix A - File Inventory](#appendix-a--file-inventory)
9. [Appendix B - WinForms -> Avalonia Control Map](#appendix-b--winforms--avalonia-control-map)
10. [Appendix C - Avalonia Dependencies & Platform Support](#appendix-c--avalonia-dependencies--platform-support)
11. [Appendix D - UI/UX Design Reference: Sidebar Navigation & Dark Theme](#appendix-d--uiux-design-reference-sidebar-navigation--dark-theme)

---

## 1. Decision Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **UI Framework** | **Avalonia UI** | Best-in-class cross-platform .NET UI framework. Skia-based rendering gives pixel-perfect consistency across Windows, Linux, and macOS. Built-in theming (dark/light), rich styling, and a large, active community (26k+ GitHub stars). |
| **Migration strategy** | **In-place conversion** | Convert the single NMSE project directly from WinForms to Avalonia on the `avalonia` branch. No intermediate library extraction step. |
| **Interim cross-platform** | **Wine bundle + setup guides** | Ship a Wine-bundled Linux version and CrossOver/Whisky guides for macOS immediately, zero code changes. |
| **Branch strategy** | `main` -> branch `avalonia` -> UI implementation -> merge into `experimental` -> merge into `main` when ready |
| **Navigation model** | **Collapsible sidebar** | Replace top-level tabs with a left-hand sidebar panel. Icons + names when expanded, icons-only when collapsed. Toggle button at the top of the sidebar. |
| **Default theme** | **Dark** | Dark theme by default with a user-accessible toggle to switch to light. Leverages Avalonia's built-in `FluentTheme` with `Dark` / `Light` variants. |
| **Work cadence** | Work produced in lots -> maintainer tests each lot -> iterate -> merge |

### Why Avalonia over Eto.Forms

The project originally considered Eto.Forms for its API proximity to WinForms. Collaboration from other developers makes Avalonia more practical while also being the better long-term choice:

1. **Skia rendering engine**  - Every pixel is drawn by Skia, guaranteeing identical appearance across Windows, Linux, and macOS. No surprises from native control differences between platforms.
2. **Built-in theming**  - Avalonia ships `FluentTheme` with `Dark` and `Light` variants out of the box. The planned dark-by-default UI is trivial to implement. Eto.Forms wraps native controls, making cross-platform dark theming significantly harder.
3. **Modern layout system**  - XAML-based layout (Grid, StackPanel, DockPanel) is powerful and composable. The sidebar navigation pattern is straightforward to build. Eto.Forms has no direct equivalent for the collapsible sidebar design.
4. **Large, active community**  - 26k+ GitHub stars, frequent releases, extensive documentation, and active Discord. Eto.Forms has ~3.5k stars and slower development cadence.
5. **Custom rendering**  - `DrawingPresenter` / custom `Control.Render()` with Skia gives far more capable rendering than Eto's `Drawable`, which is important for the InventoryGridPanel's slot cells, adjacency borders, and marquee labels.
6. **MVVM optional**  - Avalonia supports MVVM but does **not require** it. Code-behind event-driven patterns work fine, allowing a migration strategy similar to the WinForms approach where imperative logic translates directly.
7. **No native toolkit dependency**  - Unlike Eto.Forms which requires GTK3 on Linux and MonoMac on macOS, Avalonia is entirely self-contained. Zero external dependencies for end users.
8. **Data grid support**  - `Avalonia.Controls.DataGrid` provides a full-featured data grid (sorting, editing, custom cell templates, virtualisation) that matches or exceeds WinForms `DataGridView`. Eto.Forms' `GridView` is more limited.
9. **Production-proven**  - Used by JetBrains (Rider), Warp terminal, Lunacy, and many other production applications.

### Trade-offs Accepted

- **XAML learning curve**  - Developers need familiarity with AXAML (Avalonia's XAML dialect). However, simple layouts can also be built entirely in C# code-behind.
- **Not native controls**  - Avalonia draws its own controls via Skia rather than wrapping native OS controls. For NMSE this is a benefit (consistent appearance), but some users may notice non-native feel.
- **Larger binary size**  - Skia runtime adds ~20-30 MB to the self-contained publish. Acceptable for a desktop application.
- **No XAML hot reload in all IDEs**  - Hot reload works in Rider and VS with extensions. Less polished than WinForms designer, but Avalonia previewer in Rider/VS is functional.
- **Port is a redesign, not a translation**  - Unlike Eto.Forms (near-mechanical WinForms translation), Avalonia requires rethinking layouts in XAML/code-behind. This is offset by the superior result.

---

## 2. Architecture Overview

### Current Architecture (Monolithic WinForms)

```
NMSE.csproj (net10.0-windows, WinForms)
├── Core/     (21 files, 5,712 lines)    - business logic
├── Data/     (24 files, 15,142 lines)   - databases, lookups, localisation
├── IO/       (12 files, 3,781 lines)    - save file I/O, compression
├── Models/   (20 files, 2,708 lines)    - data structures, JSON engine
├── Config/   (1 file, 191 lines)        - app settings
├── UI/       (46 files, 24,839 lines)   - WinForms panels, controls, utilities
├── Resources/ (~4,950 files)            - icons, JSON, localisation, map
└── Program.cs                           - WinForms entry point
```

### Target Architecture (Avalonia, single project)

```
NMSE.slnx
+-- NMSE/                  (net10.0 - Avalonia cross-platform app)
|   +-- Core/              (21 files - unchanged)
|   +-- Data/              (24 files - IconManager updated for Avalonia)
|   +-- IO/                (12 files - unchanged)
|   +-- Models/            (20 files - unchanged)
|   +-- Config/            (1 file - unchanged)
|   +-- Resources/         (~4,950 files - JSON, icons, localisation, map)
|   +-- UI/
|   |   +-- Views/         (panel views - AXAML + code-behind)
|   |   +-- Controls/      (custom controls - SlotCell, MarqueeLabel, etc.)
|   |   +-- Util/          (utilities - IconProvider, FontManager, etc.)
|   |   +-- Sidebar/       (sidebar navigation shell)
|   |   +-- Themes/        (dark/light theme definitions)
|   +-- App.axaml          (Avalonia application definition + theme)
|   +-- App.axaml.cs       (Application code-behind)
|   +-- MainWindow.axaml   (Main window with sidebar + content area)
|   +-- MainWindow.axaml.cs
|   +-- Program.cs         (Avalonia entry point)
|
+-- NMSE.Tests/            (net10.0 - references NMSE directly)
+-- NMSE.Extractor/        (net10.0-windows - unchanged)
+-- NMSE.Extractor.Tests/  (net10.0 - unchanged)
```

### Key Architectural Principles

1. **Sidebar navigation replaces tabs.** The 15 top-level tabs become sidebar items with icons. The sidebar can be expanded (icon + label) or collapsed (icon only) via a toggle button at its top. Content area loads the selected panel.

2. **Dark theme by default.** The Avalonia `FluentTheme` ships with `Dark` and `Light` variants. NMSE defaults to `Dark`, with a user toggle in the menu bar to switch. Custom accent colours for inventory grid cells, adjacency borders, etc. are defined in theme resource dictionaries.

3. **1:1 functionality parity.** Every panel, sub-panel, tab, button, field, grid, combo box, tooltip, context menu, marquee label, inventory slot cell, adjacency colour system, and item count/amount display from the WinForms version must have a functional equivalent in the Avalonia version. No features are dropped or deferred.

---

## 3. Phase 0 - Wine/Compatibility Layer Quick-Ship

**Goal:** Give Linux/macOS users a way to run NMSE immediately with zero code changes.

**Branch:** `main` (documentation + scripts only, no source changes)

**Status:** ✅ **COMPLETE**

### Lot 0.1 - Wine Testing & Linux Bundle ✅

| Item | Detail |
|------|--------|
| **Work** | Create a Linux launch script (`nmse.sh`), test the Windows build under Wine, document known quirks, create a tar.gz distribution layout |
| **Deliverables** | `scripts/linux/nmse.sh`, `scripts/linux/README.md`, `docs/wine-linux-guide.md` |
| **Additional Deliverables** | `scripts/linux/build-appimage.sh` (AppImage builder with bundled Wine), `scripts/linux/AppRun` (AppImage entry point), `scripts/linux/nmse.desktop` (FreeDesktop entry), `scripts/linux/bottles.yml` (Bottles configuration reference) |
| **Effort** | ~1-2 hours |
| **User Testing** | Test on a Linux system with Wine installed  - verify app launches, saves load, basic editing works |
| **Acceptance** | NMSE launches via `./nmse.sh`, can load/save a save file, all panels render |

### Lot 0.2 - macOS Setup Guides ✅

| Item | Detail |
|------|--------|
| **Work** | Write setup guides for CrossOver (paid, Apple Silicon), Whisky (free), and Bottles (Linux GUI). Test where possible. |
| **Deliverables** | `docs/crossover-macos-guide.md`, `docs/whisky-macos-guide.md`, `docs/bottles-linux-guide.md` |
| **Additional Deliverables** | `scripts/macos/nmse-whisky.rb` (Homebrew Cask formula for Whisky), `scripts/macos/README.md` (macOS packaging documentation) |
| **Effort** | ~1-2 hours |
| **User Testing** | Review guides for accuracy, test on macOS if available |
| **Acceptance** | Guides are clear, step-by-step, and include screenshot placeholders |

### Phase 0 Total: ~2-4 hours ✅ COMPLETE

---

## 4. Phase 1 - Avalonia UI Implementation

**Goal:** Replace the WinForms UI layer with an Avalonia UI application, producing a single cross-platform binary with a collapsible sidebar navigation and dark-by-default theme. Every panel, button, field, grid, tooltip, context menu, and custom control from the WinForms version must have a functional equivalent.

**Branch:** `avalonia` (branched from `main`)

**Approach:** The Avalonia project replaces the WinForms project entirely. There is no period where both coexist  - the branch is the Avalonia version, and `main` remains WinForms until the branch is merged through `experimental` first.

### Lot 1.1 - Avalonia Project Setup & Theme Foundation

| Item | Detail |
|------|--------|
| **Work** | Create new `NMSE/NMSE.csproj` targeting `net10.0` with Avalonia NuGet packages. Create `App.axaml` with `FluentTheme` configured for `Dark` mode by default. Create `Program.cs` with Avalonia `AppBuilder` entry point. Set up the theme switching infrastructure (Dark <-> Light) stored in `AppConfig.Theme`. |
| **NuGet Packages** | `Avalonia` (core), `Avalonia.Desktop` (desktop hosting), `Avalonia.Themes.Fluent` (built-in Fluent theme), `Avalonia.Controls.DataGrid` (data grid), `Avalonia.Fonts.Inter` (default font fallback) |
| **Files Created** | `NMSE/NMSE.csproj`, `NMSE/Program.cs`, `NMSE/App.axaml`, `NMSE/App.axaml.cs` |
| **Theme Setup** | `FluentTheme` with `Mode=Dark` default. Custom resource dictionary for NMSE-specific colours (inventory cell backgrounds, adjacency border colours, status bar, etc.). Theme toggle reads/writes `AppConfig.Theme`. |
| **Effort** | ~2-3 hours |
| **User Testing** | `dotnet build` succeeds. `dotnet run` opens an empty Avalonia window with dark theme on Windows, Linux, or macOS. Theme can be toggled programmatically. |
| **Acceptance** | Empty Avalonia app launches with dark Fluent theme. Light theme toggle works. Non-UI code (Core/, Data/, IO/, Models/) is unchanged. |

#### Theme Resource Dictionary Structure

```
NMSE/UI/Themes/
├── NmseTheme.axaml           (shared colour/brush keys for both themes)
├── NmseDarkOverrides.axaml   (dark-specific overrides: grid cell, slot, badges)
└── NmseLightOverrides.axaml  (light-specific overrides)
```

Key resource keys:
- `SlotCellBackground`, `SlotCellBorder`, `SlotCellAmountForeground`
- `AdjacencyBorderGreen`, `AdjacencyBorderBlue`, `AdjacencyBorderYellow`, `AdjacencyBorderPurple`
- `SidebarBackground`, `SidebarSelectedItem`, `SidebarHoverItem`
- `StatusBarBackground`, `StatusBarForeground`
- `MarqueeLabelForeground`

### Lot 1.2 - MainWindow Shell with Sidebar Navigation

| Item | Detail |
|------|--------|
| **Work** | Create `MainWindow.axaml` with the core layout: a left sidebar panel and a right content area. The sidebar contains a toggle button at the top (expand/collapse) and a vertical list of navigation items, one per panel. Each item has an icon and a label. When collapsed, only icons show. When expanded, icons + labels show. Clicking a sidebar item loads the corresponding panel into the content area. Content area uses deferred/lazy loading  - panels are created on first selection. |
| **Sidebar Items** (15, matching current tabs) | Player, Exosuit, Multi-tools, Starships, Fleet, Exocraft, Companions, Bases & Storage, Discoveries, Milestones, Settlements, ByteBeats, Account Rewards, Export Settings, Raw JSON Editor |
| **Sidebar Behaviour** | Toggle button at top switches between expanded (icon + label, ~220px wide) and collapsed (icon only, ~48px wide). State persisted in `AppConfig`. Smooth width transition via Avalonia animation. Selected item highlighted. Hover effect. |
| **Files Created** | `NMSE/MainWindow.axaml`, `NMSE/MainWindow.axaml.cs`, `NMSE/UI/Sidebar/SidebarItem.cs` (data class for icon + label + panel factory) |
| **Effort** | ~3-4 hours |
| **User Testing** | App launches with sidebar showing 15 items. Toggle collapses/expands. Clicking an item shows placeholder text in content area. Sidebar state persists across restart. |
| **Acceptance** | Sidebar navigation fully functional with expand/collapse. Dark theme applied. Placeholder content loads for each item. |

#### Sidebar Layout Sketch

```
+--------------------------------------------------------------+
| [Dir] [Slot] [File] [Load] [Save]     [Menu] [Theme] [?]    |
+------+-------------------------------------------------------+
| [=]  |                                                       |
|      |                                                       |
| P    |                                                       |
| E    |              Content Area                              |
| M    |              (Selected Panel)                          |
| S    |                                                       |
| F    |                                                       |
| X    |                                                       |
| C    |                                                       |
| B    |                                                       |
| D    |                                                       |
| Mi   |                                                       |
| Se   |                                                       |
| By   |                                                       |
| R    |                                                       |
| Ex   |                                                       |
| {}   |                                                       |
|      |                                                       |
+------+-------------------------------------------------------+
| Status: Ready                              | DB Items: 1234 |
+--------------------------------------------------------------+
```

Expanded sidebar:
```
+--------------------+
| [=] NMSE           |
|                    |
| P  Player          |
| E  Exosuit         |
| M  Multi-tools     |
| S  Starships       |
| F  Fleet           |
| X  Exocraft        |
| C  Companions      |
| B  Bases           |
| D  Discoveries     |
| Mi Milestones      |
| Se Settlements     |
| By ByteBeats       |
| R  Rewards         |
| Ex Export           |
| {} Raw JSON        |
+--------------------+
```

> **Note:** Final icons will be SVG or PNG assets from `Resources/ui/icons/`, designed specifically for sidebar navigation. The icon set should be consistent with the game's aesthetic.

### Lot 1.3 - Menu Bar, Toolbar & Status Bar

| Item | Detail |
|------|--------|
| **Work** | Build the top toolbar area: directory browser combo, slot selector, file selector, Load/Save buttons. Build the menu bar (File, Edit, Language, Help). Build the status bar at the bottom. Port all keyboard shortcuts. Port the deferred panel loading system. Wire up `UiStrings` localisation. |
| **WinForms -> Avalonia** | `MenuStrip` -> Avalonia `Menu`, `ToolStrip` -> custom `StackPanel`/`DockPanel` with Avalonia `Button`/`ComboBox`/`AutoCompleteBox`, `StatusStrip` -> `DockPanel` at bottom with `TextBlock` elements |
| **Files Modified** | `MainWindow.axaml`, `MainWindow.axaml.cs` |
| **Effort** | ~3-4 hours |
| **User Testing** | Menu bar works. Toolbar combos populate with directories/slots/files. Load/Save cycle works. Status bar updates. Language switching works. |
| **Acceptance** | Full menu structure, toolbar with combos, status bar, language switching. Save load/save cycle functional. |

### Lot 1.4 - AvaloniaIconProvider Implementation

| Item | Detail |
|------|--------|
| **Work** | Implement `icon loading interface` using `Avalonia.Media.Imaging.Bitmap` for icon loading, caching, and downscaling. Port the parallel preload logic from the WinForms `IconManager`. Avalonia's `Bitmap` can load from streams, making this straightforward. |
| **Files Created** | `NMSE/UI/Util/AvaloniaIconProvider.cs` |
| **Effort** | ~1-2 hours |
| **Verification** | Unit test that loads an icon via AvaloniaIconProvider and verifies non-null return |
| **Acceptance** | Icons load correctly from Resources/images/ using Avalonia types. Parallel preload completes without error. |

### Lot 1.5 - FontManager Port

| Item | Detail |
|------|--------|
| **Work** | Port `UI/Util/FontManager.cs` from `PrivateFontCollection` (GDI+) to Avalonia font loading. Avalonia supports `FontFamily` construction from embedded resources or file paths (e.g., `avares://NMSE/Resources/app/NMSGeoSans_Kerned.ttf#NMSGeoSans`). |
| **Files Created** | `NMSE/UI/Util/FontManager.cs` (Avalonia version) |
| **Effort** | ~1 hour |
| **Acceptance** | NMSGeoSans font loads and can be applied to Avalonia `TextBlock` / `Label` elements. Heading styles use the custom font. |

### Lot 1.6 - Utility Classes Port

| Item | Detail |
|------|--------|
| **Work** | Port `ItemPickerDialog.cs` (modal item selector) to an Avalonia `Window` (modal). Port filter, search, multi-select, shift-select fix logic. `ColorEmojiLabel.cs` (17 lines) becomes unnecessary  - Avalonia renders colour emoji natively via Skia/HarfBuzz. `RedrawHelper.cs` becomes unnecessary  - Avalonia's retained-mode rendering doesn't need manual paint suspension. |
| **Files Created** | `NMSE/UI/Util/ItemPickerDialog.axaml`, `NMSE/UI/Util/ItemPickerDialog.axaml.cs` |
| **Files Not Needed** | `ColorEmojiLabel` (native emoji), `RedrawHelper` (not applicable), `DoubleBufferedTabControl` (Avalonia has no flicker issue) |
| **Effort** | ~2-3 hours |
| **Acceptance** | Item picker launches as modal, search/filter works, multi-select with shift-click works, selected items returned correctly |

### Lots 1.7-1.26 - Panel-by-Panel Implementation

Each WinForms panel is implemented as an Avalonia `UserControl`. Panels are ordered from simplest to most complex to build confidence and establish patterns early.

#### Implementation Pattern

For each panel:
1. Create `NMSE/UI/Views/{PanelName}View.axaml` with Avalonia layout (using `Grid`, `StackPanel`, `DockPanel`, `TabControl`, etc.)
2. Create `NMSE/UI/Views/{PanelName}View.axaml.cs` with code-behind event handlers  - most calls to `*Logic` classes are unchanged
3. Wire up `ApplyUiLocalisation()` using `UiStrings.Get()`  - the localisation system is and is framework-agnostic
4. Connect icon loading where icons are displayed (using `Image` control with Avalonia `Bitmap`)
5. Ensure all buttons, fields, combos, grids, checkboxes, tooltips, and context menus from the WinForms version are present and functional

#### Critical Parity Requirements

The following features **must** be present in every panel where they exist in the WinForms version:

- All **buttons** (Add, Remove, Export, Import, Resize, Delete, Generate, Learn, Unlearn, Travel, Apply, etc.)
- All **text fields** and **numeric fields** with the same validation/formatting
- All **combo boxes** (drop-downs) with the same items and selection behaviour
- All **data grids** with the same columns, sorting, editing, and cell formatting
- All **checkboxes** and their change handlers
- All **tooltips** on controls
- All **context menus** on grids and cells
- All **sub-tabs** within panels (BasePanel has 3, FreighterPanel has 2-3, DiscoveryPanel has 6, etc.)
- All **localised strings** via `UiStrings.Get()`
- All **Export/Import** functionality for lists and configurations

#### Panel Order and Effort Estimates

| Lot | Panel | WinForms Lines | Complexity | Effort |
|-----|-------|----------------|------------|--------|
| 1.7 | **FleetPanel** | 127 | Trivial (container for 3 sub-panels) | 30 min |
| 1.8 | **ExosuitPanel** | 191 | Simple (inventory grid host) | 45 min |
| 1.9 | **RecipePanel** | 285 | Simple (read-only DataGrid + filter + search) | 1 hr |
| 1.10 | **MilestonePanel** | 388 | Simple (grids with checkboxes) | 1 hr |
| 1.11 | **AccountPanel** | 896 | Simple-Medium (reward grids, unlock buttons) | 1.5 hr |
| 1.12 | **ExportConfigPanel** | 577 | Medium (checkboxes, file paths, options) | 1.5 hr |
| 1.13 | **ExocraftPanel** | 693 | Medium (vehicle selector + inventory grids) | 2 hr |
| 1.14 | **ByteBeatPanel** | 662 | Medium (byte beat library grid + controls) | 2 hr |
| 1.15 | **SquadronPanel** | 623 | Medium (pilot details + inventory grid) | 2 hr |
| 1.16 | **MultitoolPanel** | 714 | Medium (selector + details + inventory grid) | 2 hr |
| 1.17 | **FreighterPanel** | 1,053 | Medium-Hard (details + 2 inventory grids + rooms) | 3 hr |
| 1.18 | **StarshipPanel** | 1,213 | Medium-Hard (selector + details + inventory grids) | 3 hr |
| 1.19 | **FrigatePanel** | 1,438 | Medium-Hard (fleet list + details + seed gen) | 3 hr |
| 1.20 | **SettlementPanel** | 1,065 | Medium (3-column layout, 18 perk slots, decision) | 2.5 hr |
| 1.21 | **MainStatsPanel** | 1,852 | Hard (many combos, grids, coordinates, portal glyphs, guides, titles) | 4-5 hr |
| 1.22 | **RawJsonPanel** | 1,135 | Medium-Hard (tree view + text editor + search + validation) | 3 hr |
| 1.23 | **CompanionPanel** | 1,552 | Hard (complex data, images, descriptors, mood/trust/scale sliders) | 3-4 hr |
| 1.24 | **BasePanel** | 1,602 | Hard (3 sub-panels: BasesSubPanel, ChestsSubPanel, StorageSubPanel; NPC management; lazy-loaded grids) | 4-5 hr |
| 1.25 | **DiscoveryPanel** | 2,268 | Hard (6 internal tabs: Technologies, Products, Words, Glyphs, Locations, Fish; race icons; tree views) | 4-5 hr |
| 1.26 | **InventoryGridPanel** | 3,926 | **Very Hard** (custom SlotCell rendering, adjacency borders, MarqueeLabel, context menus, drag-to-move, supercharged slots, 14+ instances across app) | 8-12 hr |

**Panel Implementation Total: ~52-62 hours**

#### InventoryGridPanel - Special Considerations (Lot 1.26)

This is by far the hardest panel to implement. The WinForms version (~3,926 lines) uses:

- **Custom `SlotCell` inner class** (~459 lines, extends `Panel`) with custom `Paint` handlers rendering 5 visual elements per cell:
  1. **Item icon** - the item's image from the database, downscaled to fit the cell
  2. **Marquee label** - a scrolling text label for long item names that animates via timer
  3. **Amount display** - formatted counts with filtering for how the counts are displayed base on type (e.g. "" for base tech, "250/250", 60% for chargeable items, etc.) 
  4. **Class mini icon** - for technology/upgrade items, shows C/B/A/S class badge
  5. **Element badge** - element symbol overlay
- **`MarqueeLabel`** (~89 lines) - custom scrolling text control for long item names
- **Adjacency border overlay** - colour-coded borders drawn over cells based on `TechAdjacencyDatabase` calculations (green, blue, yellow, purple gradients)
- **Context menus** per cell - Add Item, Remove, Move, Copy, technology operations
- **Drag-to-move** - drag a slot to move it to another position; Ctrl+drag to duplicate the item into the target slot
- **Supercharged slot support** - slots can be toggled as supercharged (gold highlight with lightning bolt indicator), with per-inventory constraints (max slots, max row)
- **Cell colouring by item type:**
  - Technology = blue (40, 60, 120)
  - Product = orange (120, 80, 30)
  - Substance = teal (30, 100, 100)
  - Supercharged = gold
  - Selected = blue highlight (80, 120, 200)
  - Non-activated = red tint overlay
- **14+ instances** across the app - used by ExosuitPanel, MultitoolPanel, StarshipPanel, FreighterPanel (x2), ExocraftPanel, CompanionPanel, SquadronPanel, BasePanel's ChestsSubPanel (x10), StorageSubPanel (x2+)

The Avalonia approach:

| WinForms Feature | Avalonia Equivalent |
|------------------|---------------------|
| `SlotCell : Panel` with custom `Paint` | Custom `UserControl` with `DrawingPresenter` or custom `Render()` override using Skia |
| GDI+ `Graphics.DrawImage/DrawString` | `DrawingContext.DrawImage()`, `DrawingContext.DrawText()` using `FormattedText` |
| `MarqueeLabel` (timer-based scroll) | Custom control with `DispatcherTimer` + `RenderTransform` translation animation, or Avalonia `Animation` on `TranslateTransform` |
| Adjacency border colours | `Border` control with `BorderBrush` bound to computed colour, or painted in `Render()` |
| Context menus | Avalonia `ContextMenu` with `MenuItem` items |
| Drag-to-move / Ctrl+drag-to-duplicate | `PointerPressed`, `PointerMoved`, `PointerReleased` with keyboard modifier detection |
| Supercharged slot toggle | Gold `Background` + lightning bolt overlay, constraint logic unchanged |
| `ToolTip` per cell | `ToolTip.Tip` attached property on each SlotCell |

**Recommendation:** Split Lot 1.26 into sub-lots if needed:
- 1.26a - SlotCell custom control (icon, name, amount, class mini icon, element badge rendering) (~3-4 hr)
- 1.26b - MarqueeLabel control (scrolling animation) (~1-2 hr)
- 1.26c - Adjacency border system (~2-3 hr)
- 1.26d - Grid container (layout, selection, context menus, drag-to-move, supercharged slots, resize) (~2-3 hr)

### Lot 1.27 - CoordinateHelper Glyph Rendering (Avalonia)

| Item | Detail |
|------|--------|
| **Work** | Create Avalonia equivalent of the `#if WINFORMS` block in `CoordinateHelper.cs`. The shared lib keeps the coordinate math; the UI project provides glyph panel creation using Avalonia controls. Portal glyph images render as `Image` controls in a horizontal `StackPanel`. |
| **Files Created** | `NMSE/UI/Controls/GlyphRenderer.axaml`, `NMSE/UI/Controls/GlyphRenderer.axaml.cs` |
| **Effort** | ~1-2 hours |
| **Acceptance** | Portal glyph images render correctly in the MainStatsPanel coordinate display |

### Lot 1.28 - Full Localisation Pass

| Item | Detail |
|------|--------|
| **Work** | Verify all 16 languages work correctly across all panels. Ensure `ApplyUiLocalisation()` is called on every view when language changes. Verify that the sidebar item labels, menu items, toolbar labels, and status bar all update. Verify right-to-left text doesn't break layout (if applicable in future). |
| **Effort** | ~2-3 hours |
| **Acceptance** | All 16 languages display correctly. Language switching updates every visible string. No truncated or missing labels. |

### Lot 1.29 - Integration Testing & Parity Verification

| Item | Detail |
|------|--------|
| **Work** | Comprehensive testing pass: |
| | 1. Load a save file -> navigate all 15 sidebar items -> verify each panel displays data correctly |
| | 2. Edit values in every panel -> save -> reload -> verify changes persisted |
| | 3. Test all inventory grid operations: click, right-click, context menu, add item, remove item, move item, resize grid |
| | 4. Verify marquee labels scroll for long item names |
| | 5. Verify adjacency border colours appear correctly for tech items |
| | 6. Test Export/Import on every panel that supports it |
| | 7. Test theme switching (dark <-> light)  - all panels render correctly in both themes |
| | 8. Test sidebar collapse/expand  - content area resizes correctly |
| | 9. Test on Windows. If Linux/macOS available, test there too. |
| | 10. Create a **parity checklist** documenting every feature from the WinForms version and its status in Avalonia. |
| **Effort** | ~3-4 hours |
| **User Testing** | Full manual testing on all available platforms |
| **Acceptance** | Feature parity with WinForms version confirmed. All 15 panels load and function. All inventory grid features work. Dark/light themes work. Sidebar works. Localisation works. Save/load cycle works. |

### Phase 1 Summary

| Category | Lots | Effort |
|----------|------|--------|
| Project setup, theme & sidebar | 2.1-2.3 | ~8-11 hr |
| Infrastructure (icons, fonts, utils) | 2.4-2.6 | ~4-6 hr |
| Simple panels (4) | 2.7-2.10 | ~3.25 hr |
| Simple-Medium panels (3) | 2.11-2.13 | ~5 hr |
| Medium panels (4) | 2.14-2.17 | ~10 hr |
| Medium-Hard panels (3) | 2.18-2.20 | ~8.5 hr |
| Hard panels (4) | 2.21-2.25 | ~15.5-19.5 hr |
| InventoryGridPanel (Very Hard) | 1.26 | ~8-12 hr |
| Glyph rendering | 1.27 | ~1-2 hr |
| Localisation pass | 1.28 | ~2-3 hr |
| Integration testing | 1.29 | ~3-4 hr |
| **Phase 2 Total** | **29 lots** | **~68-82 hours** |

---

## 5. Phase 2 - Polish, Packaging & Release

**Goal:** Prepare the Avalonia application for release. Cross-platform packaging, documentation, and final polish.

**Branch:** `avalonia` (continues from Phase 1, then merged into `experimental`, then into `main`)

### Lot 2.1 - Cross-Platform Publishing Profiles

| Item | Detail |
|------|--------|
| **Work** | Set up `dotnet publish` profiles for: Windows x64, Linux x64, macOS x64, macOS ARM64 (Apple Silicon). Create publish scripts. Avalonia apps publish as standard .NET applications  - no platform-specific backends needed (unlike Eto.Forms). |
| **Deliverables** | Publish profiles in `NMSE/Properties/PublishProfiles/`, build scripts |
| **Publish Commands** | See [Appendix C](#appendix-c--avalonia-dependencies--platform-support) |
| **Effort** | ~2 hours |
| **Acceptance** | `dotnet publish` for all four targets produces working executables |

### Lot 2.2 - Linux Packaging

| Item | Detail |
|------|--------|
| **Work** | Create a `.desktop` file, icon, and AppImage build script. No GTK dependency (Avalonia is self-contained). Create a Flatpak manifest if desired. |
| **Deliverables** | `scripts/linux/nmse.desktop`, `scripts/linux/build-appimage.sh`, `docs/linux-install.md` |
| **Effort** | ~2 hours |
| **Acceptance** | AppImage runs on Ubuntu 22.04+ / Fedora 38+. No external dependencies needed (Avalonia bundles Skia). |

### Lot 2.3 - macOS Packaging

| Item | Detail |
|------|--------|
| **Work** | Create a `.app` bundle structure, `Info.plist`, icon set (`.icns`). Avalonia apps on macOS are self-contained  - no Mono or Cocoa dependencies. Create a DMG build script if desired. |
| **Deliverables** | `scripts/macos/`, `docs/macos-install.md` |
| **Effort** | ~2 hours |
| **Acceptance** | `.app` bundle launches on macOS Intel and Apple Silicon. No external dependencies. |

### Lot 2.4 - Windows Packaging Update

| Item | Detail |
|------|--------|
| **Work** | Update the existing Windows publish/zip workflow for the Avalonia project structure. Ensure ReadyToRun, build versioning, and zip packaging still work. |
| **Effort** | ~1 hour |
| **Acceptance** | Windows build produces a zip with the same distribution structure as current releases |

### Lot 2.5 - Documentation Update

| Item | Detail |
|------|--------|
| **Work** | Update README.md with cross-platform install instructions for Windows, Linux, macOS. Update architecture docs. Deprecate Wine guides (native app now available). Update CONTRIBUTING.md with Avalonia development setup (recommend Rider or VS with Avalonia extension). Add sidebar navigation and theme switching to user guide. |
| **Effort** | ~2-3 hours |
| **Acceptance** | Documentation reflects new architecture, new UI, and new platform support |

### Lot 2.6 - Merge Strategy

| Item | Detail |
|------|--------|
| **Work** | Merge `avalonia` branch into a new `experimental` branch. Allow broader testing by multiple developers. Collect feedback, fix issues. When all developers are satisfied, merge `experimental` into `main`. Tag release. |
| **Merge Path** | `avalonia` -> `experimental` (broader testing) -> `main` (release) |
| **Effort** | ~2-3 hours (including conflict resolution and final verification) |
| **Acceptance** | `main` now contains the Avalonia application. WinForms code is fully removed. All platforms work. |

### Phase 2 Summary

| Lot | Description | Effort |
|-----|-------------|--------|
| 2.1 | Publish profiles | 2 hr |
| 2.2 | Linux packaging | 2 hr |
| 2.3 | macOS packaging | 2 hr |
| 2.4 | Windows packaging | 1 hr |
| 2.5 | Documentation | 2-3 hr |
| 2.6 | Merge strategy | 2-3 hr |
| **Phase 3 Total** | | **~11-13 hours** |

---

## 6. Working Methodology

### Cadence

```
Developer:   Produces Lot N -> commits to branch -> notifies maintainer
Maintainer:  Tests Lot N -> provides feedback -> approves or requests changes
Developer:   Addresses feedback (if any) -> moves to Lot N+1
```

### What "Effort" Means

The effort estimates represent the time a developer spends actively working on the lot, including:
- Reading/understanding existing code
- Writing new code (AXAML layout + C# code-behind)
- Running builds and tests
- Fixing issues found during build/test
- Committing and pushing

Estimates assume:
- Single-session focus per lot
- No external blockers
- Clean build environment
- Ability to reference the WinForms source and port functionality systematically

### Branching

```
main
 |
 +-- Phase 0: Wine guides (docs only) done
 |
 +-- avalonia (branch from main)
       +-- Phase 1: UI implementation (Lots 1.1-1.29)
       +-- Phase 2: Polish & packaging (Lots 2.1-2.6)
       |
       +-- -> merge to experimental (broader developer testing)
             +-- -> merge to main (release, when all satisfied)
```

### Testing Gates

Every lot must pass before the next begins:

| Gate | Criteria |
|------|----------|
| **Build gate** | `dotnet build` succeeds with 0 errors |
| **Test gate** | All existing tests pass (NMSE.Tests + NMSE.Extractor.Tests) |
| **Smoke test** | App launches and basic functionality works |
| **Parity check** | Panel has same buttons, fields, grids, and behaviour as WinForms version |
| **Theme check** | Panel renders correctly in both Dark and Light themes |
| **Developer approval** | Maintainer confirms the lot's deliverables |

### Rollback

- **Phase 1-2** (on `avalonia`): The branch is independent. If the entire approach fails, `main` is unaffected.
- **Experimental -> Main**: The `experimental` branch provides a buffer for broader testing before `main` is affected.

---

## 7. Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **InventoryGridPanel complexity**  - 3,926 lines of custom rendering with SlotCell, MarqueeLabel, adjacency borders | High | High | Dedicate the most time (8-12 hr). Split into sub-lots. Build SlotCell and MarqueeLabel as standalone testable controls first. |
| **Avalonia DataGrid feature gaps**  - `Avalonia.Controls.DataGrid` may lack some `DataGridView` features (e.g., checkbox columns, image columns, custom cell painting) | Medium | Medium | Audit all DataGrid usage early (Lot 2.9). For missing features, use `DataGridTemplateColumn` with custom cell templates. Avalonia DataGrid supports this well. |
| **Custom Skia rendering performance**  - SlotCell rendering (icon + text + badge + border) across 14+ grids with 100+ cells each | Medium | Medium | Profile early. Use `RenderTargetBitmap` caching if needed (render to bitmap, display bitmap). Avalonia's Skia backend is generally fast. |
| **Sidebar navigation UX**  - users accustomed to tabs may find sidebar unfamiliar | Low | Low | Keep sidebar item order identical to current tab order. Provide keyboard shortcuts for panel switching. Consider tooltip on collapsed icons. |
| **Theme switching edge cases**  - some custom colours (adjacency borders, cell backgrounds) may not update correctly when switching themes | Medium | Low | Define all colours as `DynamicResource` keys. Test both themes for every panel. |
| **Avalonia font rendering**  - custom NMSGeoSans font may render differently from GDI+ (kerning, size, weight) | Low-Medium | Low | Test font rendering early (Lot 2.5). Avalonia uses HarfBuzz for text shaping, which is high-quality. Fine-tune font sizes if needed. |
| **Cross-platform file dialogs**  - folder/file dialog behaviour may differ across platforms | Low | Low | Avalonia has cross-platform file dialog support. Test on each platform. |
| **Large merge conflict**  - `avalonia` branch diverges significantly from `main` | Medium | Medium | Keep `main` stable during Phase 2 (minimal changes). Rebase `avalonia` regularly against `main`. |
| **Clipboard operations**  - clipboard API may differ across platforms | Low | Low | Avalonia has cross-platform `Clipboard` support. Test copy/paste on all platforms. |
| **NuGet package stability**  - Avalonia may have breaking changes between versions | Low | Medium | Pin to a specific Avalonia version (e.g., `11.x.x`). Only upgrade deliberately after testing. |
| **macOS notarisation**  - Apple may require notarisation for .app bundles | Medium | Medium | Research and document notarisation process. May need Apple Developer account. Can defer to Phase 3 if needed. |
| **Tree view differences**  - Avalonia `TreeView` data model differs from WinForms | Medium | Low | Use `TreeView` with `HierarchicalDataTemplate`. RawJsonPanel and DiscoveryPanel tree views will need model adaptation. |

---

## Appendix A - File Inventory

### Source File Inventory

#### Models/ (20 files, 2,708 lines)
```
JsonObject.cs (455)    JsonArray.cs (250)     JsonParser.cs (720)
JsonReader.cs (175)    JsonException.cs (45)  RawDouble.cs (32)
BinaryData.cs (78)     IPropertyChangeListener.cs (15)
SaveFileMetadata.cs (49)  DifficultyLevel.cs (15)  Recipe.cs (51)
Companion.cs (44)      Frigate.cs (54)        Inventory.cs (50)
InventoryType.cs (20)  Multitool.cs (69)      MultitoolType.cs (17)
Ship.cs (75)           ShipClass.cs (12)      ShipType.cs (21)
```

#### Config/ (1 file, 191 lines)
```
AppConfig.cs (191)
```

#### IO/ (12 files, 3,781 lines)
```
SaveFileManager.cs (639)       SaveSlotManager.cs (458)
BinaryIO.cs (139)              ContainersIndexManager.cs (473)
Lz4Compressor.cs (286)         Lz4CompressorStream.cs (119)
Lz4DecompressorStream.cs (199) Lz4BufferedCompressorStream.cs (99)
Lz4ChunkedCompressorStream.cs (104)
MetaCrypto.cs (424)            MetaFileWriter.cs (490)
MemoryDatManager.cs (251)
```

#### Core/ (21 files, 5,712 lines)
```
AccountLogic.cs (238)     BaseLogic.cs (49)        CompanionLogic.cs (273)
DiscoveryLogic.cs (279)   ExocraftLogic.cs (86)    ExosuitLogic.cs (24)
ExportConfig.cs (262)     FileNameHelper.cs (18)   FreighterLogic.cs (548)
FrigateLogic.cs (577)     InventoryImportHelper.cs (172)
MainStatsLogic.cs (106)   MilestoneLogic.cs (85)   MultitoolLogic.cs (464)
MxmlRewardEditor.cs (220) RawJsonLogic.cs (40)     SeedHelper.cs (57)
SettlementLogic.cs (339)  SquadronLogic.cs (266)   StarshipLogic.cs (753)
StatHelper.cs (55)
```

#### Data/ (24 files, 15,142 lines - 22 move as-is, 1 abstracted, 1 new interface)
```
22 files:
  BaseStatLimits.cs (200)      CompanionDatabase.cs (8,787)
  CoordinateHelper.cs (283)    ElementDatabase.cs (74)
  FrigateTraitDatabase.cs (159) GalaxyDatabase.cs (302)
  GameItem.cs (204)            GameItemDatabase.cs (590)
  InventoryStackDatabase.cs (284) JsonNameMapper.cs (116)
  LeveledStatDatabase.cs (140)  LocalisationService.cs (166)
  ProceduralStubs.cs (59)      RecipeDatabase.cs (175)
  RewardDatabase.cs (179)      SettlementPerkDatabase.cs (209)
  TechAdjacencyDatabase.cs (880) TechPackDatabase.cs (796)
  TechPackDatabase.Generated.cs (397) TitleDatabase.cs (196)
  UiStrings.cs (220)           WikiGuideDatabase.cs (168)
  WordDatabase.cs (205)

WinForms-specific (to be ported):
  IconManager.cs (178)  - WinForms implementation of icon loading interface

New in NMSE (1 file):
  icon loading interface.cs (~20 lines)  - platform-agnostic interface
```

#### Resources/ (~4,950 files)
```
  Resources/json/     (24 files  - game databases)
  Resources/json/lang/ (16 files  - game localisation)
  Resources/icons/    (some icon files)
  Resources/images/   (4,892 files  - item icons)
  Resources/map/      (1 file  - galaxy map data)
  Resources/ui/lang/  (16 files  - UI localisation)

Staying in UI project:
  Resources/app/NMSE.ico              (app icon  - embedded)
  Resources/app/NMSGeoSans_Kerned.ttf (custom font  - embedded)
```

### Files Being Implemented in Phase 2 (WinForms -> Avalonia)

#### MainWindow (replaces MainForm)
```
WinForms:
  UI/MainForm.cs (2,167 lines) + UI/MainForm.Designer.cs (63 lines) = 2,230 lines

Avalonia:
  MainWindow.axaml + MainWindow.axaml.cs
  UI/Sidebar/SidebarItem.cs
```

#### Panel Views (20 panels -> 20 Avalonia UserControls)
```
Each panel: {Panel}.cs + {Panel}.Designer.cs -> {PanelName}View.axaml + {PanelName}View.axaml.cs

Total WinForms: 42 files, ~22,258 lines
Expected Avalonia: ~40 files (20 .axaml + 20 .axaml.cs), ~16,000-20,000 lines
```

#### Controls
```
WinForms custom controls -> Avalonia custom controls:

  SlotCell (inner class, 459 lines)    -> NMSE/UI/Controls/SlotCell.axaml[.cs]
  MarqueeLabel (inner class, 89 lines) -> NMSE/UI/Controls/MarqueeLabel.axaml[.cs]
  ColorEmojiLabel (17 lines)           -> NOT NEEDED (Avalonia renders emoji natively)
  DoubleBufferedTabControl             -> NOT NEEDED (Avalonia has no flicker issue)
```

#### Utilities
```
WinForms:                             -> Avalonia:
  UI/Util/FontManager.cs (103 lines)   -> NMSE/UI/Util/FontManager.cs
  UI/Util/ItemPickerDialog.cs (197 l.)  -> NMSE/UI/Util/ItemPickerDialog.axaml[.cs]
  UI/Util/RedrawHelper.cs (34 lines)    -> NOT NEEDED (retained-mode rendering)
```

#### New Files (Avalonia-specific)
```
NMSE/App.axaml + App.axaml.cs                - Avalonia Application definition + theme config
NMSE/MainWindow.axaml + MainWindow.axaml.cs   - Main window with sidebar + content
NMSE/UI/Sidebar/SidebarItem.cs                - Sidebar navigation item model
NMSE/UI/Util/AvaloniaIconProvider.cs           - icon loading interface using Avalonia.Media.Imaging
NMSE/UI/Controls/GlyphRenderer.axaml[.cs]     - Portal glyph rendering
NMSE/UI/Themes/NmseTheme.axaml                - Shared theme resource dictionary
NMSE/UI/Themes/NmseDarkOverrides.axaml         - Dark theme overrides
NMSE/UI/Themes/NmseLightOverrides.axaml        - Light theme overrides
NMSE/Program.cs                                - Avalonia entry point
```

---

## Appendix B - WinForms -> Avalonia Control Map

This is the detailed translation reference for Phase 2.

### Layout

| WinForms | Avalonia | Notes |
|----------|----------|-------|
| `Form` | `Window` | Avalonia uses `Window` for top-level windows |
| `UserControl` | `UserControl` | Nearly identical concept |
| `Panel` | `Panel` / `Border` / `Canvas` | `Panel` for simple container, `Border` for bordered content, `Canvas` for absolute positioning |
| `FlowLayoutPanel` | `WrapPanel` / `StackPanel` | `WrapPanel` for wrapping flow, `StackPanel` for linear flow |
| `TableLayoutPanel` | `Grid` (with RowDefinitions/ColumnDefinitions) | Avalonia `Grid` is more powerful and flexible |
| `SplitContainer` | `SplitView` / `GridSplitter` in a `Grid` | `GridSplitter` for resizable split |
| `GroupBox` | `HeaderedContentControl` or `Border` + `TextBlock` | No direct `GroupBox`; use styled `Border` with header |
| `TabControl` + `TabPage` | `TabControl` + `TabItem` | Nearly identical concept |
| `ScrollableControl` | `ScrollViewer` | Wraps content with scrollbars |
| `DockPanel` (if used) | `DockPanel` | Identical concept |

### Common Controls

| WinForms | Avalonia | Notes |
|----------|----------|-------|
| `Label` | `TextBlock` | `TextBlock` for display-only text. `Label` exists but `TextBlock` is more common. |
| `TextBox` | `TextBox` | Nearly identical |
| `Button` | `Button` | Nearly identical |
| `CheckBox` | `CheckBox` | Nearly identical |
| `ComboBox` (DropDownList) | `ComboBox` | Nearly identical (non-editable by default) |
| `ComboBox` (editable) | `AutoCompleteBox` or `ComboBox` with `IsEditable=true` | |
| `NumericUpDown` | `NumericUpDown` | Nearly identical |
| `RadioButton` | `RadioButton` | Nearly identical |
| `ProgressBar` | `ProgressBar` | Nearly identical |
| `PictureBox` | `Image` (control) | `Image` control with `Source` property |
| `RichTextBox` | `TextBox` with `AcceptsReturn=true` | Avalonia `TextBox` supports multi-line |
| `TreeView` | `TreeView` | Uses `HierarchicalDataTemplate` for items |
| `DataGridView` | `DataGrid` (from `Avalonia.Controls.DataGrid`) | Column-based with `DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridTemplateColumn` |
| `ListBox` | `ListBox` | Nearly identical |
| `ToolTip` | `ToolTip.Tip` attached property | Set via `ToolTip.Tip="text"` on any control |

### Menus and Toolbars

| WinForms | Avalonia | Notes |
|----------|----------|-------|
| `MenuStrip` | `Menu` (in `NativeMenu` or AXAML `Menu`) | |
| `ToolStripMenuItem` | `MenuItem` | |
| `ToolStripSeparator` | `Separator` | |
| `ToolStrip` | Custom `StackPanel` with `Button`/`ComboBox` | No direct ToolStrip equivalent  - build with layout panels |
| `ToolStripComboBox` | `ComboBox` in a layout panel | |
| `ToolStripButton` | `Button` in a layout panel | |
| `StatusStrip` | `DockPanel` docked to bottom with `TextBlock` elements | |
| `ContextMenuStrip` | `ContextMenu` | Attached via `ContextMenu` property on controls |

### Dialogs

| WinForms | Avalonia | Notes |
|----------|----------|-------|
| `OpenFileDialog` | `OpenFileDialog` (from `Avalonia.Controls`) | API differs slightly  - uses `StorageProvider` on newer versions |
| `SaveFileDialog` | `SaveFileDialog` | Similar |
| `FolderBrowserDialog` | `OpenFolderDialog` | |
| `MessageBox.Show()` | Custom dialog or community package | Avalonia has no built-in `MessageBox`. Use `Avalonia.MessageBox` community package or create a simple modal `Window`. |
| Custom dialog (`Form`) | `Window` shown with `ShowDialog()` | |

### Drawing / Custom Rendering

| WinForms (GDI+) | Avalonia (DrawingContext) | Notes |
|------------------|--------------------------|-------|
| `Graphics g` (from PaintEventArgs) | `DrawingContext` (from `Render()` override) | Obtained by overriding `public override void Render(DrawingContext context)` |
| `g.DrawImage(img, rect)` | `context.DrawImage(bitmap, sourceRect, destRect)` | |
| `g.DrawString(text, font, brush, x, y)` | `context.DrawText(formattedText, point)` | Use `FormattedText` object |
| `g.FillRectangle(brush, rect)` | `context.FillRectangle(brush, rect)` | Uses `IBrush` (e.g., `SolidColorBrush`) |
| `g.DrawRectangle(pen, rect)` | `context.DrawRectangle(brush, pen, rect)` | Uses `IPen` |
| `GraphicsPath` | `StreamGeometry` / `PathGeometry` | Build paths with `StreamGeometryContext` |
| `g.FillPath(brush, path)` | `context.DrawGeometry(brush, pen, geometry)` | |
| `SolidBrush(Color)` | `new SolidColorBrush(Color)` | Or use `Brushes.Red`, `Brush.Parse("#FF0000")` |
| `Pen(Color, width)` | `new Pen(brush, thickness)` | |
| `Bitmap(width, height)` | `new RenderTargetBitmap(pixelSize)` | For offscreen rendering |
| `Image.FromStream(stream)` | `new Bitmap(stream)` | `Avalonia.Media.Imaging.Bitmap` |
| `g.MeasureString()` | `formattedText.Bounds` | Create `FormattedText`, read its `Bounds` |
| `StringFormat` / text alignment | `FormattedText` + `TextAlignment` | |
| `g.InterpolationMode` | `RenderOptions.BitmapInterpolationMode` attached property | Set on the control or in render code |

### Events

| WinForms | Avalonia | Notes |
|----------|----------|-------|
| `Click += handler` | `Click += handler` (on Button) | For other controls use `Tapped` or `PointerPressed` |
| `Paint += handler` | Override `Render(DrawingContext)` | Or use `DrawingPresenter` control |
| `MouseDown += handler` | `PointerPressed += handler` | Uses `PointerPressedEventArgs` |
| `MouseMove += handler` | `PointerMoved += handler` | |
| `MouseUp += handler` | `PointerReleased += handler` | |
| `SelectedIndexChanged` | `SelectionChanged` | |
| `TextChanged` | `TextChanged` (on TextBox) | Or bind to `Text` property changes |
| `KeyDown` | `KeyDown` | |
| `DragDrop` | `DragDrop.Drop` | Avalonia uses attached events for drag/drop |
| `Shown` | `Opened` | On `Window` |
| `FormClosing` | `Closing` | On `Window` |

---

## Appendix C - Avalonia Dependencies & Platform Support

### NuGet Packages

```xml
<!-- Core -->
<PackageReference Include="Avalonia" Version="11.*" />
<PackageReference Include="Avalonia.Desktop" Version="11.*" />

<!-- Theme -->
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />

<!-- Data Grid -->
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.*" />

<!-- Default fonts (optional, for consistent cross-platform text) -->
<PackageReference Include="Avalonia.Fonts.Inter" Version="11.*" />

<!-- Diagnostics (development only) -->
<PackageReference Include="Avalonia.Diagnostics" Version="11.*" Condition="'$(Configuration)' == 'Debug'" />
```

### Program.cs Entry Point

```csharp
using Avalonia;

namespace NMSE;

static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()   // fallback font
            .LogToTrace();
}
```

### App.axaml Theme Setup

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="NMSE.App">
    <Application.Styles>
        <!-- Default: Dark theme. Toggled via code-behind. -->
        <FluentTheme Mode="Dark" />
    </Application.Styles>
</Application>
```

Theme switching in `App.axaml.cs`:
```csharp
public void SetTheme(string mode)
{
    var fluentTheme = Application.Current!.Styles.OfType<FluentTheme>().First();
    fluentTheme.Mode = mode == "Light" ? FluentThemeMode.Light : FluentThemeMode.Dark;
}
```

### Runtime Dependencies

| Platform | Runtime Dependency | How Users Get It |
|----------|-------------------|-----------------|
| **Windows** | .NET 10 Runtime | Bundled (self-contained publish) or separate install |
| **Linux** | .NET 10 Runtime | Bundled. Avalonia uses Skia  - **no GTK or other native toolkit required**. |
| **macOS** | .NET 10 Runtime | Bundled. Avalonia uses Skia  - **no Mono, Cocoa, or other native dependency**. |

> **Key advantage over Eto.Forms:** Avalonia is fully self-contained on all platforms. No GTK3 on Linux, no MonoMac on macOS. The only dependency is the .NET runtime, which can be bundled.

### Self-Contained Publish Commands

```bash
# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true

# Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

---

## Appendix D - UI/UX Design Reference: Sidebar Navigation & Dark Theme

### Sidebar Behaviour Specification

| Property | Value |
|----------|-------|
| **Default state** | Expanded (icon + label) |
| **Expanded width** | ~220px |
| **Collapsed width** | ~48px |
| **Toggle button** | Hamburger icon (☰) at the top of the sidebar |
| **Transition** | Smooth animated width transition (~200ms ease) |
| **Persistence** | Collapse/expand state saved in `AppConfig` and restored on startup |
| **Selection indicator** | Highlighted background on the selected item (accent colour) |
| **Hover effect** | Subtle background highlight on mouse-over |
| **Keyboard** | Arrow keys navigate items, Enter selects, Ctrl+1..Ctrl+9 for quick panel access |
| **Tooltips** | When collapsed, hovering over an icon shows the panel name as a tooltip |

### Sidebar Item Structure

Each sidebar item consists of:
```
┌─────────────────────┐
│ [Icon]  Panel Name   │  <- Expanded (icon 24x24 + label)
└─────────────────────┘

┌──────┐
│[Icon]│  <- Collapsed (icon only, centred)
└──────┘
```

### Sidebar Items (15 items, matching current tab order)

| # | Icon Source | Label | Panel |
|---|------------|-------|-------|
| 1 | New icon in `Resources/ui/icons/` | Player | MainStatsPanel |
| 2 | New icon in `Resources/ui/icons/` | Exosuit | ExosuitPanel |
| 3 | New icon in `Resources/ui/icons/` | Multi-tools | MultitoolPanel |
| 4 | New icon in `Resources/ui/icons/` | Starships | StarshipPanel |
| 5 | New icon in `Resources/ui/icons/` | Fleet | FleetPanel |
| 6 | New icon in `Resources/ui/icons/` | Exocraft | ExocraftPanel |
| 7 | New icon in `Resources/ui/icons/` | Companions | CompanionPanel |
| 8 | New icon in `Resources/ui/icons/` | Bases & Storage | BasePanel |
| 9 | New icon in `Resources/ui/icons/` | Discoveries | DiscoveryPanel |
| 10 | New icon in `Resources/ui/icons/` | Milestones | MilestonePanel |
| 11 | New icon in `Resources/ui/icons/` | Settlements | SettlementPanel |
| 12 | New icon in `Resources/ui/icons/` | ByteBeats | ByteBeatPanel |
| 13 | New icon in `Resources/ui/icons/` | Account Rewards | AccountPanel |
| 14 | New icon in `Resources/ui/icons/` | Export Settings | ExportConfigPanel |
| 15 | New icon in `Resources/ui/icons/` | Raw JSON Editor | RawJsonPanel |

> **Icon assets:** Reuse existing icons from `Resources/icons/` or `Resources/images/` where possible. Create new SVG icons for sidebar items if existing game icons don't suit the navigation context. All sidebar icons should be consistent in size (24×24 or 20×20) and style.

### Theme Specification

No theme specific colours specified.

### Theme Toggle

- **Location:** Menu bar (Settings -> Theme -> Dark / Light) and/or a toggle icon in the sidebar footer
- **Behaviour:** Immediate switch, no restart required. Avalonia `FluentTheme.Mode` property change triggers re-render.
- **Persistence:** Saved in `AppConfig.Theme` (`"Dark"` or `"Light"`)

---

## Overall Timeline Summary

```
Phase 0: Wine/Compatibility Quick-Ship ✅ COMPLETE
  Lot 0.1  Wine testing + Linux bundle         ~1-2 hr    ─┐
  Lot 0.2  macOS setup guides                  ~1-2 hr    ─┘ Total: ~2-4 hr

Phase 1: Avalonia UI Implementation (on avalonia branch)
  Lot 1.1  Avalonia project + theme setup      ~2-3 hr    ─┐
  Lot 1.2  MainWindow shell + sidebar          ~3-4 hr     │
  Lot 1.3  Menu, toolbar, status bar           ~3-4 hr     │
  Lot 1.4  AvaloniaIconProvider                ~1-2 hr     │
  Lot 1.5  FontManager port                    ~1 hr       │
  Lot 1.6  Utility classes port                ~2-3 hr     │
  Lots 1.7-1.10  Simple panels (4)             ~3.25 hr    │
  Lots 1.11-1.13 Simple-Medium panels (3)      ~5 hr       │
  Lots 1.14-1.17 Medium panels (4)             ~10 hr      │
  Lots 1.18-1.20 Medium-Hard panels (3)        ~8.5 hr     │
  Lots 1.21-1.25 Hard panels (4)               ~15.5-19.5h │
  Lot 1.26 InventoryGridPanel                  ~8-12 hr    │
  Lot 1.27 Glyph rendering                    ~1-2 hr     │
  Lot 1.28 Localisation pass                   ~2-3 hr     │
  Lot 1.29 Integration testing                 ~3-4 hr    ─┘ Total: ~68-82 hr

Phase 2: Polish, Packaging & Release (on avalonia branch)
  Lot 2.1  Publish profiles                    ~2 hr      ─┐
  Lot 2.2  Linux packaging                     ~2 hr       │
  Lot 2.3  macOS packaging                     ~2 hr       │
  Lot 2.4  Windows packaging update            ~1 hr       │
  Lot 2.5  Documentation                       ~2-3 hr     │
  Lot 2.6  Merge to experimental -> main        ~2-3 hr    ─┘ Total: ~11-13 hr

══════════════════════════════════════════════════════════════
GRAND TOTAL (Effort):  ~81-99 hours across ~35 lots
══════════════════════════════════════════════════════════════
```

### In Terms of Sessions

Assuming each session is ~2-4 hours of focused work:

| Phase | Sessions | Calendar Time (1 session/day) |
|-------|----------|-------------------------------|
| Phase 0 | ✅ Complete |  - |
| Phase 2 | 20-28 sessions | 4-6 weeks |
| Phase 3 | 4-5 sessions | 4-5 days |
| **Total** | **~24-33 sessions** | **~5-7 weeks** |

---

*This document is the single source of truth for the NMSE cross-platform migration. All architectural decisions, effort estimates, file inventories, and acceptance criteria are maintained here. Update this document as work progresses*
