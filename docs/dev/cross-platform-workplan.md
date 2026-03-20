# Cross-Platform Work Plan: NMSE WinForms -> Eto.Forms

## Single Source of Truth for Cross-Platform Migration

**Last updated:** 2026-03-17

---

## Table of Contents

1. [Decision Summary](#1-decision-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Phase 0 - Wine/Compatibility Layer Quick-Ship](#3-phase-0--winecompatibility-layer-quick-ship)
4. [Phase 1 - NMSE.Lib Extraction (Shared Library)](#4-phase-1--nmselib-extraction)
5. [Phase 2 - Eto.Forms UI Conversion](#5-phase-2--etoforms-ui-conversion)
6. [Phase 3 - Polish, Packaging & Release](#6-phase-3--polish-packaging--release)
7. [Working Methodology](#7-working-methodology)
8. [Risk Register](#8-risk-register)
9. [Appendix A - File Inventory](#appendix-a--file-inventory)
10. [Appendix B - WinForms -> Eto.Forms Control Map](#appendix-b--winforms--etoforms-control-map)
11. [Appendix C - Eto.Forms Dependencies by Platform](#appendix-c--etoforms-dependencies-by-platform)

---

## 1. Decision Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **UI Framework** | **Eto.Forms** | Closest API to WinForms - near-mechanical translation. Event-driven (not MVVM). Same `Form`, `Panel`, `Drawable`, `Graphics` concepts. |
| **Migration strategy** | **Sequential, not coexistent** | Extract NMSE.Lib on `main`, then branch and fully convert UI to Eto.Forms. No period where both WinForms and Eto.Forms coexist in the same binary. |
| **Interim cross-platform** | **Wine bundle + setup guides** | Ship a Wine-bundled Linux version and CrossOver/Whisky guides for macOS immediately, zero code changes. |
| **Branch strategy** | `main` -> NMSE.Lib extraction -> merge -> branch `eto-forms` -> UI conversion -> merge when ready |
| **Work cadence** | Work produced in lots -> maintainer tests each lot -> iterate -> merge |

### Why Eto.Forms over Avalonia

While Avalonia has a larger community and richer rendering (Skia), Eto.Forms was chosen because:

1. **API proximity** - Eto's `Form`, `Panel`, `Drawable`, `Graphics`, `Button`, `ComboBox`, `MessageBox` are near-identical to WinForms equivalents. This means the existing ~21,800 lines of UI code can be translated almost line-by-line rather than redesigned into XAML/MVVM.
2. **Event-driven model** - No need to learn/adopt MVVM, data binding, or XAML. The existing imperative style is preserved.
3. **Faster port** - Estimated 6–8 weeks vs 8–10 for Avalonia.
4. **Native controls** - Eto wraps real WinForms on Windows, GTK on Linux, Cocoa on macOS. Controls look and behave natively per-platform.
5. **Lower risk** - The mechanical translation reduces the risk of subtle behaviour changes.

### Trade-offs Accepted

- Smaller community (~3.5k vs 26k+ GitHub stars)
- GTK dependency on Linux (GTK3+ must be installed)
- MonoMac dependency on macOS (can lag behind macOS versions)
- Custom dark theming is harder (native controls don't support custom themes as easily as Skia)
- Less powerful rendering than Skia for complex graphics (but Eto's `Drawable` + `Graphics` covers the inventory grid use case)

---

## 2. Architecture Overview

### Current Architecture (Monolithic WinForms)

```
NMSE.csproj (net10.0-windows, WinForms)
├── Core/     (21 files, 5,712 lines)   ─ business logic
├── Data/     (24 files, 15,142 lines)  ─ databases, lookups, localisation
├── IO/       (12 files, 3,781 lines)   ─ save file I/O, compression
├── Models/   (20 files, 2,708 lines)   ─ data structures, JSON engine
├── Config/   (1 file, 191 lines)       ─ app settings
├── UI/       (48 files, 21,833 lines)  ─ WinForms panels, controls, utilities
├── Resources/ (~4,950 files)           ─ icons, JSON, localisation, map
└── Program.cs                          ─ WinForms entry point
```

### Target Architecture (Shared Lib + Eto.Forms)

```
NMSE.slnx
├── NMSE.Lib/              (net10.0 - cross-platform shared library)
│   ├── Core/              (21 files - move as-is)
│   ├── Data/              (24 files - IconManager abstracted)
│   ├── IO/                (12 files - move as-is)
│   ├── Models/            (20 files - move as-is)
│   ├── Config/            (1 file - move as-is)
│   └── Resources/         (~4,950 files - JSON, icons, localisation, map)
│
├── NMSE/                  (net10.0 - Eto.Forms cross-platform UI)
│   ├── UI/                (translated panels, controls, utilities)
│   ├── Program.cs         (Eto.Forms entry point with platform detection)
│   └── References -> NMSE.Lib
│
├── NMSE.Tests/            (net10.0 - references NMSE.Lib directly)
├── NMSE.Extractor/        (net10.0-windows - unchanged)
└── NMSE.Extractor.Tests/  (net10.0 - unchanged)
```

### Key Architectural Principle

**No UI code in NMSE.Lib.** The shared library contains zero references to any UI framework - not WinForms, not Eto.Forms. Icon handling is abstracted behind an `IIconProvider` interface that each UI project implements.

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
| **Effort** | ~1 session (1–2 hours) |
| **User Testing** | Test on a Linux system with Wine installed - verify app launches, saves load, basic editing works |
| **Acceptance** | NMSE launches via `./nmse.sh`, can load/save a save file, all 20 tabs render |

### Lot 0.2 - macOS Setup Guides ✅

| Item | Detail |
|------|--------|
| **Work** | Write setup guides for CrossOver (paid, Apple Silicon), Whisky (free), and Bottles (Linux GUI). Test where possible. |
| **Deliverables** | `docs/crossover-macos-guide.md`, `docs/whisky-macos-guide.md`, `docs/bottles-linux-guide.md` |
| **Additional Deliverables** | `scripts/macos/nmse-whisky.rb` (Homebrew Cask formula for Whisky), `scripts/macos/README.md` (macOS packaging documentation) |
| **Effort** | ~1 session (1–2 hours) |
| **User Testing** | Review guides for accuracy, test on macOS if available |
| **Acceptance** | Guides are clear, step-by-step, and include screenshots placeholders |

### Phase 0 Total: ~2 sessions (2–4 hours time)

---

## 4. Phase 1 - NMSE.Lib Extraction

**Goal:** Extract all platform-independent code into a standalone `NMSE.Lib` class library targeting `net10.0` (no `-windows`). The existing WinForms app continues to work by referencing NMSE.Lib.

**Branch:** `main` (direct commits - this is foundational restructuring)

**Why on main:** The lib extraction doesn't change any behaviour - it's a refactor. The existing WinForms app keeps working throughout. Tests keep passing. This is safe to do on main because every lot is independently verifiable.

### Lot 1.1 - Create NMSE.Lib Project Skeleton

| Item | Detail |
|------|--------|
| **Work** | Create `NMSE.Lib/NMSE.Lib.csproj` targeting `net10.0`. Set up namespace (`NMSE`), resource handling, and the project in `NMSE.slnx`. |
| **Files Created** | `NMSE.Lib/NMSE.Lib.csproj` |
| **Files Modified** | `NMSE.slnx` |
| **Effort** | ~30 minutes |
| **User Testing** | `dotnet build NMSE.Lib/` compiles with 0 errors |
| **Acceptance** | Empty lib project builds, is in solution |

### Lot 1.2 - Move Models Layer

| Item | Detail |
|------|--------|
| **Work** | Move all 20 files from `Models/` to `NMSE.Lib/Models/`. Update `NMSE.csproj` to exclude `NMSE.Lib/**` and add `<ProjectReference>` to NMSE.Lib. Verify all namespaces remain `NMSE.Models`. |
| **Files Moved** | 20 files: `JsonObject.cs`, `JsonArray.cs`, `JsonParser.cs`, `JsonReader.cs`, `JsonException.cs`, `RawDouble.cs`, `BinaryData.cs`, `IPropertyChangeListener.cs`, `SaveFileMetadata.cs`, `DifficultyLevel.cs`, `Recipe.cs`, `Companion.cs`, `Frigate.cs`, `Inventory.cs`, `InventoryType.cs`, `Multitool.cs`, `MultitoolType.cs`, `Ship.cs`, `ShipClass.cs`, `ShipType.cs` |
| **Files Modified** | `NMSE.csproj` (add ProjectReference, exclude Lib folder), `NMSE.Lib/NMSE.Lib.csproj` |
| **Effort** | ~1 hour |
| **Verification** | `dotnet build` (full solution), `dotnet test NMSE.Tests/ --no-build` (960 pass), `dotnet test NMSE.Extractor.Tests/ --no-build` (178 pass) |
| **Acceptance** | All builds pass, all 1,138 tests pass, no namespace changes |

### Lot 1.3 - Move Config Layer

| Item | Detail |
|------|--------|
| **Work** | Move `Config/AppConfig.cs` to `NMSE.Lib/Config/`. |
| **Files Moved** | 1 file: `AppConfig.cs` |
| **Effort** | ~20 minutes |
| **Verification** | Full build + tests |
| **Acceptance** | All builds pass, all tests pass |

### Lot 1.4 - Move IO Layer

| Item | Detail |
|------|--------|
| **Work** | Move all 12 files from `IO/` to `NMSE.Lib/IO/`. |
| **Files Moved** | 12 files: `SaveFileManager.cs`, `SaveSlotManager.cs`, `BinaryIO.cs`, `Lz4Compressor.cs`, `Lz4CompressorStream.cs`, `Lz4DecompressorStream.cs`, `Lz4BufferedCompressorStream.cs`, `Lz4ChunkedCompressorStream.cs`, `MetaCrypto.cs`, `MetaFileWriter.cs`, `MemoryDatManager.cs`, `ContainersIndexManager.cs` |
| **Effort** | ~30 minutes |
| **Verification** | Full build + tests |
| **Acceptance** | All builds pass, all tests pass |

### Lot 1.5 - Move Core Layer

| Item | Detail |
|------|--------|
| **Work** | Move all 21 files from `Core/` to `NMSE.Lib/Core/`. Note: `MxmlRewardEditor.cs` uses `OperatingSystem.IsWindows()` and `[SupportedOSPlatform("windows")]` for Registry access - this is already correctly guarded and compiles on `net10.0`. |
| **Files Moved** | 21 files: `AccountLogic.cs`, `BaseLogic.cs`, `CompanionLogic.cs`, `DiscoveryLogic.cs`, `ExocraftLogic.cs`, `ExosuitLogic.cs`, `ExportConfig.cs`, `FileNameHelper.cs`, `FreighterLogic.cs`, `FrigateLogic.cs`, `InventoryImportHelper.cs`, `MainStatsLogic.cs`, `MilestoneLogic.cs`, `MultitoolLogic.cs`, `MxmlRewardEditor.cs`, `RawJsonLogic.cs`, `SeedHelper.cs`, `SettlementLogic.cs`, `SquadronLogic.cs`, `StarshipLogic.cs`, `StatHelper.cs` |
| **Effort** | ~30 minutes |
| **Verification** | Full build + tests |
| **Acceptance** | All builds pass, all tests pass |

### Lot 1.6 - Move Data Layer (with IconManager Abstraction)

| Item | Detail |
|------|--------|
| **Work** | Move all 24 Data files to `NMSE.Lib/Data/`. This is the most complex lot because: |
| | 1. **`IconManager.cs`** - uses `System.Drawing.Image`, `Bitmap`, `Graphics`. Must be abstracted behind an `IIconProvider` interface. The concrete `IconManager` stays in the WinForms project (or a Windows-specific assembly). |
| | 2. **`CoordinateHelper.cs`** - already has `#if WINFORMS` guards. The WINFORMS block stays excluded in NMSE.Lib (no `WINFORMS` define). |
| | 3. All other 22 Data files have no WinForms dependencies and move as-is. |
| **Interface Created** | `NMSE.Lib/Data/IIconProvider.cs` - platform-agnostic icon loading contract |
| **Files Moved** | 22 of 24 Data files move to `NMSE.Lib/Data/` as-is |
| **Files Remaining in NMSE.csproj** | `Data/IconManager.cs` (stays as WinForms-specific implementation of `IIconProvider`) |
| **Files Modified** | `CoordinateHelper.cs` (verify `#if WINFORMS` guards are sufficient), any Core/UI files that reference `IconManager` directly (update to use `IIconProvider`) |
| **Effort** | ~2–3 hours (most complex lot in Phase 1) |
| **Verification** | Full build + tests |
| **Acceptance** | NMSE.Lib builds on `net10.0` with 0 errors. WinForms app builds and works unchanged. All tests pass. `IIconProvider` interface is clean and usable by Eto.Forms later. |

#### IIconProvider Interface Design

```csharp
namespace NMSE.Data;

/// <summary>
/// Platform-agnostic interface for loading and caching item icons.
/// Each UI framework provides its own implementation using its native image types.
/// </summary>
public interface IIconProvider : IDisposable
{
    /// <summary>Gets an icon image for the given filename. Returns null if not found.</summary>
    object? GetIcon(string? iconFilename);

    /// <summary>Pre-loads icons for all items in the database in parallel.</summary>
    void PreloadIcons(GameItemDatabase database);

    /// <summary>Gets an icon for an item by looking up the icon filename from the database.</summary>
    object? GetIconForItem(string? itemId, GameItemDatabase? database);
}
```

The `object?` return type allows each platform to return its native image type (`System.Drawing.Image` for WinForms, `Eto.Drawing.Bitmap` for Eto.Forms) without introducing framework dependencies in the shared library.

### Lot 1.7 - Move Resources

| Item | Detail |
|------|--------|
| **Work** | Move `Resources/` directory to `NMSE.Lib/Resources/`. Update `NMSE.Lib.csproj` with Content/EmbeddedResource entries. Update `NMSE.csproj` to remove resource entries and instead rely on the runtime output from NMSE.Lib (resources copied to output on build). |
| **Directories Moved** | `Resources/json/`, `Resources/icons/`, `Resources/images/`, `Resources/map/`, `Resources/ui/` |
| **Embedded Resources** | `Resources/app/NMSE.ico`, `Resources/app/NMSGeoSans_Kerned.ttf` - stay in the UI project (app icon and embedded font are UI-specific) |
| **Effort** | ~1 hour |
| **Verification** | Full build. Verify resource files appear in output directory. Run app to confirm icons, JSON databases, and localisation files load correctly. |
| **Acceptance** | All resources load at runtime. App functions identically. Tests pass. |

### Lot 1.8 - Update Test Projects

| Item | Detail |
|------|--------|
| **Work** | Update `NMSE.Tests/NMSE.Tests.csproj` to replace all 60+ `<Compile Include="..\..\*.cs" Link="..." />` entries with a single `<ProjectReference Include="..\NMSE.Lib\NMSE.Lib.csproj" />`. Update `NMSE.Extractor.Tests` similarly if needed. |
| **Files Modified** | `NMSE.Tests/NMSE.Tests.csproj`, potentially `NMSE.Extractor.Tests/NMSE.Extractor.Tests.csproj` |
| **Effort** | ~1 hour |
| **Verification** | `dotnet test NMSE.Tests/` (960 pass), `dotnet test NMSE.Extractor.Tests/` (178 pass) |
| **Acceptance** | All 1,138 tests pass. No linked source files remain in test projects - they use ProjectReference instead. |

### Lot 1.9 - Final Verification & Cleanup

| Item | Detail |
|------|--------|
| **Work** | Final verification pass. Ensure: NMSE.Lib has no WinForms/System.Drawing references (except guarded). NMSE.csproj only contains UI/, Program.cs, and IconManager. Clean up any orphaned files. Update `Directory.Build.props` if needed for new project layout. Run full test suite. |
| **Effort** | ~1 hour |
| **Verification** | Full build, full test suite, manual inspection of project files |
| **Acceptance** | Clean build, clean tests, no orphaned files, `NMSE.Lib.csproj` targets `net10.0` without `-windows` |

### Phase 1 Summary

| Lot | Description | AI Effort | Cumulative |
|-----|-------------|-----------|------------|
| 1.1 | Lib project skeleton | 30 min | 30 min |
| 1.2 | Move Models (20 files) | 1 hr | 1.5 hr |
| 1.3 | Move Config (1 file) | 20 min | ~2 hr |
| 1.4 | Move IO (12 files) | 30 min | ~2.5 hr |
| 1.5 | Move Core (21 files) | 30 min | ~3 hr |
| 1.6 | Move Data + IconManager abstraction | 2–3 hr | ~5.5 hr |
| 1.7 | Move Resources | 1 hr | ~6.5 hr |
| 1.8 | Update test projects | 1 hr | ~7.5 hr |
| 1.9 | Final verification & cleanup | 1 hr | **~8.5 hr** |

**Phase 1 Total: ~8.5 hours across ~9 lots**

**Invariant after every lot:** `dotnet build` succeeds, all tests pass, app runs correctly.

---

## 5. Phase 2 - Eto.Forms UI Conversion

**Goal:** Replace the WinForms UI layer entirely with Eto.Forms, producing a single cross-platform application.

**Branch:** `eto-forms` (branched from `main` after Phase 1 is merged)

**Approach:** The Eto.Forms project replaces the WinForms project entirely. There is no period where both coexist - the branch is the Eto.Forms version, and `main` remains WinForms until the branch is merged.

### Lot 2.1 - Eto.Forms Project Setup

| Item | Detail |
|------|--------|
| **Work** | Create new `NMSE/NMSE.csproj` (replaces old WinForms csproj) targeting `net10.0` with Eto.Forms NuGet packages. Set up platform-specific launcher projects or conditional platform handlers. Create `Program.cs` with Eto.Forms `Application` entry point. |
| **NuGet Packages** | `Eto.Forms` (core), `Eto.Platform.Wpf` or `Eto.Platform.WinForms` (Windows), `Eto.Platform.Gtk` (Linux), `Eto.Platform.Mac64` (macOS) |
| **Files Created** | `NMSE/NMSE.csproj`, `NMSE/Program.cs` |
| **Effort** | ~1–2 hours |
| **User Testing** | `dotnet build` succeeds. `dotnet run` opens an empty Eto.Forms window on Windows. |
| **Acceptance** | Empty Eto.Forms app launches on Windows with correct platform backend |

### Lot 2.2 - EtoIconProvider Implementation

| Item | Detail |
|------|--------|
| **Work** | Implement `IIconProvider` using `Eto.Drawing.Bitmap` for icon loading, caching, and downscaling. Port the parallel preload logic from the WinForms `IconManager`. |
| **Files Created** | `NMSE/UI/Util/EtoIconProvider.cs` |
| **Effort** | ~1–2 hours |
| **Verification** | Unit test that loads an icon via EtoIconProvider and verifies non-null return |
| **Acceptance** | Icons load correctly from Resources/images/ using Eto.Drawing types |

### Lot 2.3 - FontManager Port

| Item | Detail |
|------|--------|
| **Work** | Port `UI/Util/FontManager.cs` from `PrivateFontCollection` (GDI+) to Eto.Forms font loading. Eto supports loading fonts from files/streams via `Eto.Drawing.Font`. |
| **Files Created** | `NMSE/UI/Util/FontManager.cs` (Eto.Forms version) |
| **Effort** | ~1 hour |
| **Acceptance** | NMSGeoSans font loads and can be applied to Eto.Forms Labels |

### Lot 2.4 - MainForm Shell (Menu, Toolbar, Tabs, Status)

| Item | Detail |
|------|--------|
| **Work** | Translate `UI/MainForm.cs` (1,882 lines) to Eto.Forms. This is the application shell: MenuBar, ToolBar (buttons + combos for directory/slot/file), TabControl with 20 TabPages, and a status bar. At this stage, tabs contain placeholder "Panel Name - TODO" labels. |
| **WinForms -> Eto Translations** | `MenuStrip` -> `MenuBar`, `ToolStrip` -> `ToolBar`, `TabControl` -> `TabControl`, `StatusStrip` -> custom `TableLayout` at bottom, `ToolStripComboBox` -> `DropDown` in toolbar |
| **Localisation** | Wire up `UiStrings` calls - the localisation system (`Data/UiStrings.cs`) is in NMSE.Lib and is framework-agnostic. `ApplyUiLocalisation()` translates directly. |
| **Files Created** | `NMSE/UI/MainForm.cs` |
| **Effort** | ~3–4 hours |
| **User Testing** | App launches, menus render, tabs show placeholders, status bar shows "Ready", localisation can be changed via menu |
| **Acceptance** | Full menu structure, toolbar with combos, 20 named tabs, status bar, language switching works |

### Lot 2.5 - Save Load/Save Infrastructure

| Item | Detail |
|------|--------|
| **Work** | Wire up the directory combo, slot combo, file combo, Load button, Save button to `SaveFileManager`, `SaveSlotManager`, `ContainersIndexManager`, etc. Port the file dialog calls (`OpenFileDialog`, `SaveFileDialog`) to Eto equivalents. Port the deferred panel loading system. |
| **Effort** | ~2–3 hours |
| **User Testing** | Can browse to a save directory, select a slot, load a save file, see status update. Save button enables after load. |
| **Acceptance** | Full save load/save cycle works. JSON data is available for panels to consume. |

### Lot 2.6 - Utility Classes Port

| Item | Detail |
|------|--------|
| **Work** | Port `ItemPickerDialog.cs` (modal item selector) and `RedrawHelper.cs` (paint suspension) to Eto.Forms. `ColorEmojiLabel.cs` (17 lines) becomes unnecessary - Eto/GTK/Cocoa all render color emoji natively. Port `DoubleBufferedTabControl` if needed (Eto.Forms TabControl may already handle flicker). |
| **Files Created** | `NMSE/UI/Util/ItemPickerDialog.cs`, `NMSE/UI/Util/RedrawHelper.cs` (if needed) |
| **Effort** | ~1–2 hours |
| **Acceptance** | Item picker launches, search works, item selection works |

### Lots 2.7–2.26 - Panel-by-Panel Translation

Each WinForms panel is translated to an Eto.Forms equivalent. Panels are ordered from simplest to most complex to build confidence and establish patterns early.

#### Translation Pattern

For each panel:
1. Create `NMSE/UI/Panels/{PanelName}.cs` using Eto.Forms controls
2. Translate the WinForms `.Designer.cs` layout code to Eto layout (using `TableLayout`, `StackLayout`, `DynamicLayout`, etc.)
3. Translate the `.cs` event handlers - most calls to `*Logic` classes are unchanged
4. Wire up `ApplyUiLocalisation()` using `UiStrings.Get()`
5. Connect to the `IIconProvider` where icons are displayed

#### Panel Order and Effort Estimates

| Lot | Panel | WinForms Lines (code + designer) | Complexity | AI Effort |
|-----|-------|----------------------------------|------------|-----------|
| 2.7 | **FleetPanel** | 68 + 59 = 127 | Trivial (container for 3 sub-panels) | 30 min |
| 2.8 | **ExosuitPanel** | 68 + 123 = 191 | Simple | 30 min |
| 2.9 | **RecipePanel** | 158 + 127 = 285 | Simple (read-only display) | 45 min |
| 2.10 | **AccountPanel** | 378 + 518 = 896 | Simple–Medium | 1 hr |
| 2.11 | **MilestonePanel** | 225 + 163 = 388 | Simple | 45 min |
| 2.12 | **ExocraftPanel** | 428 + 261 = 689 | Medium | 1.5 hr |
| 2.13 | **ByteBeatPanel** | 361 + 295 = 656 | Medium | 1.5 hr |
| 2.14 | **MultitoolPanel** | 473 + 239 = 712 | Medium (inventory grid use) | 2 hr |
| 2.15 | **FreighterPanel** | 359 + 693 = 1,052 | Medium (many controls) | 2 hr |
| 2.16 | **ExportConfigPanel** | 282 + 278 = 560 | Medium | 1.5 hr |
| 2.17 | **SquadronPanel** | 392 + 231 = 623 | Medium | 1.5 hr |
| 2.18 | **StarshipPanel** | 899 + 313 = 1,212 | Medium–Hard | 2.5 hr |
| 2.19 | **FrigatePanel** | 735 + 695 = 1,430 | Medium–Hard | 2.5 hr |
| 2.20 | **SettlementPanel** | 686 + 341 = 1,027 | Medium | 2 hr |
| 2.21 | **MainStatsPanel** | 1,178 + 674 = 1,852 | Hard (many combos, grids, sections) | 3–4 hr |
| 2.22 | **RawJsonPanel** | 929 + 192 = 1,121 | Medium–Hard (tree view + editing) | 2.5 hr |
| 2.23 | **CompanionPanel** | 1,020 + 511 = 1,531 | Hard (complex data + images) | 3 hr |
| 2.24 | **DiscoveryPanel** | 1,520 + 552 = 2,072 | Hard (tabs within tab, tree views, galaxy map) | 3–4 hr |
| 2.25 | **BasePanel** | 1,412 + 81 = 1,493 | Hard (dynamic layouts, scrolling) | 3 hr |
| 2.26 | **InventoryGridPanel** | 3,115 + 432 = 3,547 | **Very Hard** (custom rendering, SlotCell, adjacency borders, marquee labels, context menus) | 5–8 hr |

**Panel Translation Total: ~38–44 hours**

#### InventoryGridPanel - Special Considerations (Lot 2.26)

This is the hardest panel to port. The WinForms version uses:

- **Custom `SlotCell` inner class** (extending `Panel`) with custom `Paint` handlers
- **GDI+ rendering** (`Graphics.DrawImage`, `Graphics.DrawString`, `GraphicsPath` for badge shapes)
- **`MarqueeLabel`** (custom scrolling text label)
- **Adjacency border overlay** (`OnPaintBackground`)
- **Context menus** per cell
- **Drag-select** across cells

The Eto.Forms approach:
- Use `Drawable` for the entire grid (custom painting via `Drawable.Paint` event)
- Eto's `Graphics` class provides `DrawImage()`, `DrawText()`, `DrawRectangle()`, `FillPath()` equivalents
- MarqueeLabel -> custom animation or static truncated text
- Context menus -> Eto `ContextMenu`
- Mouse tracking -> `Drawable.MouseDown`, `Drawable.MouseMove`, `Drawable.MouseUp` events

### Lot 2.27 - CoordinateHelper Glyph Rendering (Eto.Forms)

| Item | Detail |
|------|--------|
| **Work** | Create Eto.Forms equivalent of the `#if WINFORMS` block in `CoordinateHelper.cs`. The shared lib keeps the coordinate math; the UI project provides glyph panel creation using Eto controls. |
| **Files Created** | `NMSE/UI/Util/GlyphRenderer.cs` (Eto.Forms glyph panel) |
| **Effort** | ~1 hour |
| **Acceptance** | Portal glyph images render correctly in the coordinate display |

### Lot 2.28 - Integration Testing

| Item | Detail |
|------|--------|
| **Work** | Full integration test: load a save, navigate all 20 tabs, verify data displays correctly, edit values, save, reload, verify changes persisted. Test all 16 localisation languages. Test inventory grid interactions (click, right-click, context menu, add item, remove item). |
| **Effort** | ~2–3 hours |
| **User Testing** | Full manual testing on Windows. If Linux/macOS is available, test there too. |
| **Acceptance** | Feature parity with WinForms version. All 20 panels load and function. Localisation works. Save/load cycle works. |

### Phase 2 Summary

| Category | Lots | AI Effort |
|----------|------|-----------|
| Project setup + infrastructure | 2.1–2.6 | ~9–14 hr |
| Simple panels (5) | 2.7–2.11 | ~3.5 hr |
| Medium panels (7) | 2.12–2.18 | ~12.5 hr |
| Hard panels (5) | 2.19–2.25 | ~17 hr |
| InventoryGridPanel (Very Hard) | 2.26 | ~5–8 hr |
| Glyph rendering | 2.27 | ~1 hr |
| Integration testing | 2.28 | ~2–3 hr |
| **Phase 2 Total** | **28 lots** | **~50–60 hours** |

---

## 6. Phase 3 - Polish, Packaging & Release

**Goal:** Prepare the Eto.Forms application for release. Cross-platform packaging, documentation, and final polish.

**Branch:** `eto-forms` (continues from Phase 2, then merge to `main`)

### Lot 3.1 - Cross-Platform Publishing Profiles

| Item | Detail |
|------|--------|
| **Work** | Set up `dotnet publish` profiles for: Windows x64, Linux x64, macOS x64, macOS ARM64 (Apple Silicon). Create publish scripts/CI configuration. |
| **Deliverables** | Publish profiles in `NMSE/Properties/PublishProfiles/`, build scripts |
| **Effort** | ~2 hours |
| **Acceptance** | `dotnet publish -r win-x64`, `linux-x64`, `osx-x64`, `osx-arm64` all produce working executables |

### Lot 3.2 - Linux Packaging

| Item | Detail |
|------|--------|
| **Work** | Create a `.desktop` file, icon, and AppImage build script. Document GTK3 dependency. Create a Flatpak manifest if desired. |
| **Deliverables** | `scripts/linux/nmse.desktop`, `scripts/linux/build-appimage.sh`, `docs/linux-install.md` |
| **Effort** | ~2 hours |
| **Acceptance** | AppImage runs on Ubuntu 22.04+ / Fedora 38+. GTK dependency documented. |

### Lot 3.3 - macOS Packaging

| Item | Detail |
|------|--------|
| **Work** | Create a `.app` bundle structure, `Info.plist`, icon set. Document Mono/Cocoa dependencies. Create a DMG build script if desired. |
| **Deliverables** | `scripts/macos/`, `docs/macos-install.md` |
| **Effort** | ~2 hours |
| **Acceptance** | `.app` bundle launches on macOS. Dependencies documented. |

### Lot 3.4 - Windows Packaging Update

| Item | Detail |
|------|--------|
| **Work** | Update the existing Windows publish/zip workflow for the new Eto.Forms project structure. Ensure the ReadyToRun, build versioning, and zip packaging still work. |
| **Effort** | ~1 hour |
| **Acceptance** | Windows build produces a zip identical in structure to current releases |

### Lot 3.5 - Documentation Update

| Item | Detail |
|------|--------|
| **Work** | Update README.md with cross-platform install instructions. Update architecture docs. Deprecate Wine guides (native app now available). Update CONTRIBUTING.md with Eto.Forms development setup. |
| **Effort** | ~1–2 hours |
| **Acceptance** | Documentation reflects new architecture |

### Lot 3.6 - Merge to Main

| Item | Detail |
|------|--------|
| **Work** | Final review of `eto-forms` branch. Resolve any merge conflicts with `main`. Create PR, review, merge. Tag release. |
| **Effort** | ~1 hour |
| **Acceptance** | `main` now contains the Eto.Forms application. WinForms code is fully removed. |

### Phase 3 Summary

| Lot | Description | AI Effort |
|-----|-------------|-----------|
| 3.1 | Publish profiles | 2 hr |
| 3.2 | Linux packaging | 2 hr |
| 3.3 | macOS packaging | 2 hr |
| 3.4 | Windows packaging | 1 hr |
| 3.5 | Documentation | 1–2 hr |
| 3.6 | Merge to main | 1 hr |
| **Phase 3 Total** | | **~9–10 hours** |

---

## 7. Working Methodology

### Cadence

```
Programmer: Produces Lot N -> commits to branch -> notifies user
Maintainer: Tests Lot N -> provides feedback -> approves or requests changes
Programmer: Addresses feedback (if any) -> moves to Lot N+1
```

### What "Effort" Means

The effort estimates represent the time an AI coding agent spends actively working on the lot, including:
- Reading/understanding existing code
- Writing new code
- Running builds and tests
- Fixing issues found during build/test
- Committing and pushing

Estimates assume:
- Single-session focus per lot
- No external blockers
- Clean build environment
- Ability to reference the WinForms source and translate mechanically

### Branching

```
main
 │
 ├── Phase 0: Wine guides (docs only)
 ├── Phase 1: NMSE.Lib extraction (Lots 1.1–1.9)
 │     ↓ (merge each lot to main as verified)
 │
 └── eto-forms (branch from main after Phase 1)
       ├── Phase 2: UI conversion (Lots 2.1–2.28)
       ├── Phase 3: Polish & packaging (Lots 3.1–3.6)
       └── -> merge to main when ready
```

### Testing Gates

Every lot must pass before the next begins:

| Gate | Criteria |
|------|----------|
| **Build gate** | `dotnet build` succeeds with 0 errors |
| **Test gate** | All existing tests pass (960 + 178 = 1,138) |
| **Smoke test** | App launches and basic functionality works |
| **User approval** | User confirms the lot's deliverables |

### Rollback

- Phase 1 (on `main`): Each lot is a separate commit. If a lot introduces a regression, `git revert` the commit.
- Phase 2–3 (on `eto-forms`): The branch is independent. If the entire approach fails, `main` is unaffected.

---

## 8. Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Eto.Forms `Drawable` performance** - custom painting for InventoryGridPanel may be slow on GTK/Cocoa | Medium | High | Profile early in Lot 2.26. If too slow, consider bitmap caching (draw to offscreen bitmap, blit once). |
| **Eto.Forms GTK3 rendering differences** - controls may look/behave differently on GTK vs WinForms | Medium | Medium | Test each lot on Linux (GTK) as well as Windows. Document platform-specific quirks. |
| **Eto.Forms macOS Cocoa issues** - MonoMac bindings may have gaps for certain controls | Medium | Medium | Test on macOS early. If specific controls don't work, provide platform-specific fallbacks. |
| **Embedded font loading** - `PrivateFontCollection` is Windows-only; Eto's cross-platform font loading may differ | Low | Low | Eto supports loading fonts from files. Fall back to system font if custom font loading fails. |
| **Eto.Forms NuGet version stability** - library may have breaking changes | Low | Medium | Pin to a specific Eto.Forms version. Only upgrade deliberately. |
| **Large merge conflict** - `eto-forms` branch diverges significantly from `main` | Medium | Medium | Keep `main` stable during Phase 2 (minimal changes). Rebase `eto-forms` regularly. |
| **GTK dependency on Linux** - users may not have GTK3+ installed | Low | Low | Document dependency. Consider AppImage bundling. Most modern Linux desktops include GTK3. |
| **Test project adaptation** - some tests may depend on WinForms types indirectly | Low | Low | Tests currently target `net10.0` (not windows) and link source files. After Lot 1.8, they use ProjectReference to NMSE.Lib which has no WinForms types. |
| **InventoryGridPanel complexity** - this panel is 3,547 lines of complex custom rendering | High | High | Dedicate the most time to this lot (5–8 hours). Consider splitting into sub-lots if needed. |
| **Clipboard operations** - WinForms `Clipboard` class may not translate directly | Low | Low | Eto has `Clipboard` support. Test copy/paste operations on all platforms. |

---

## Appendix A - File Inventory

### Files Moving to NMSE.Lib (Phase 1)

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
Moving as-is (22 files):
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

Stays in UI project (1 file):
  IconManager.cs (178) - WinForms implementation of IIconProvider

New in NMSE.Lib (1 file):
  IIconProvider.cs (~20 lines) - platform-agnostic interface
```

#### Resources/ (~4,950 files)
```
Moving to NMSE.Lib:
  Resources/json/     (24 files - game databases)
  Resources/json/lang/ (16 files - game localisation)
  Resources/icons/    (some icon files)
  Resources/images/   (4,892 files - item icons)
  Resources/map/      (1 file - galaxy map data)
  Resources/ui/lang/  (16 files - UI localisation)

Staying in UI project:
  Resources/app/NMSE.ico              (app icon - embedded)
  Resources/app/NMSGeoSans_Kerned.ttf (custom font - embedded)
```

### Files Being Translated in Phase 2 (WinForms -> Eto.Forms)

#### MainForm (1 file -> 1 file)
```
UI/MainForm.cs (1,882 lines) -> NMSE/UI/MainForm.cs
UI/MainForm.Designer.cs (63 lines) -> (layout embedded in MainForm.cs)
```

#### Panels (20 panels × 2 files each -> 20 new files)
```
Each panel: {Panel}.cs + {Panel}.Designer.cs -> single {Panel}.cs in Eto.Forms
Total WinForms: 40 files, ~19,500 lines
Expected Eto.Forms: 20 files, ~12,000–15,000 lines (designer code becomes more concise)
```

#### Controls (1 file -> 0 files)
```
UI/Controls/ColorEmojiLabel.cs (17 lines) -> not needed (Eto/GTK/Cocoa render emoji natively)
```

#### Utilities (3 files -> 3 files)
```
UI/Util/FontManager.cs (103 lines) -> NMSE/UI/Util/FontManager.cs (Eto version)
UI/Util/ItemPickerDialog.cs (154 lines) -> NMSE/UI/Util/ItemPickerDialog.cs (Eto version)
UI/Util/RedrawHelper.cs (32 lines) -> NMSE/UI/Util/RedrawHelper.cs (if needed)
```

#### New Files
```
NMSE/UI/Util/EtoIconProvider.cs - IIconProvider implementation using Eto.Drawing
NMSE/UI/Util/GlyphRenderer.cs - Portal glyph rendering using Eto controls
NMSE/Program.cs - Eto.Forms entry point
```

---

## Appendix B - WinForms -> Eto.Forms Control Map

This is the detailed translation reference for Phase 2.

### Layout

| WinForms | Eto.Forms | Notes |
|----------|-----------|-------|
| `Form` | `Form` | Nearly identical API |
| `UserControl` | `Panel` | Eto `Panel` is the base container |
| `Panel` | `Panel` / `Drawable` | Use `Drawable` for custom painting |
| `FlowLayoutPanel` | `StackLayout` (with `Orientation.Horizontal` + wrapping) or `WrapPanel` | |
| `TableLayoutPanel` | `TableLayout` | Similar concept |
| `SplitContainer` | `Splitter` | Similar |
| `GroupBox` | `GroupBox` | Identical concept |
| `TabControl` + `TabPage` | `TabControl` + `TabPage` | Nearly identical |
| `ScrollableControl` | `Scrollable` | Similar |

### Common Controls

| WinForms | Eto.Forms | Notes |
|----------|-----------|-------|
| `Label` | `Label` | Nearly identical |
| `TextBox` | `TextBox` | Nearly identical |
| `Button` | `Button` | Nearly identical |
| `CheckBox` | `CheckBox` | Nearly identical |
| `ComboBox` | `DropDown` (non-editable) / `ComboBox` (editable) | Different naming |
| `NumericUpDown` | `NumericStepper` | Different naming |
| `RadioButton` | `RadioButton` | Nearly identical |
| `ProgressBar` | `ProgressBar` | Nearly identical |
| `PictureBox` | `ImageView` | Different naming |
| `RichTextBox` | `RichTextArea` | Different naming |
| `TreeView` | `TreeGridView` | Different data model |
| `DataGridView` | `GridView` | Different data model - uses `IDataStore<T>` |
| `ListView` | `GridView` (with columns) | Eto doesn't have a separate ListView |
| `ToolTip` | `ToolTip` property on controls | Set via `control.ToolTip = "text"` |

### Menus and Toolbars

| WinForms | Eto.Forms | Notes |
|----------|-----------|-------|
| `MenuStrip` | `MenuBar` | |
| `ToolStripMenuItem` | `ButtonMenuItem` / `SubMenuItem` | |
| `ToolStripSeparator` | `SeparatorMenuItem` | |
| `ToolStrip` | Custom `TableLayout` with `Button`s | Eto has no exact ToolStrip equivalent |
| `ToolStripComboBox` | `DropDown` in a layout | |
| `ToolStripButton` | `Button` in a layout | |
| `StatusStrip` | Custom `TableLayout` docked to bottom | |
| `ContextMenuStrip` | `ContextMenu` | |

### Dialogs

| WinForms | Eto.Forms | Notes |
|----------|-----------|-------|
| `OpenFileDialog` | `OpenFileDialog` | Nearly identical API |
| `SaveFileDialog` | `SaveFileDialog` | Nearly identical API |
| `FolderBrowserDialog` | `SelectFolderDialog` | Different naming |
| `MessageBox.Show()` | `MessageBox.Show()` | Nearly identical |
| `ColorDialog` | `ColorDialog` | Nearly identical |
| Custom dialog (`Form`) | `Dialog` | Eto has a dedicated `Dialog` class for modal dialogs |

### Drawing / Custom Painting

| WinForms (GDI+) | Eto.Forms (Eto.Drawing) | Notes |
|------------------|------------------------|-------|
| `Graphics g` | `Graphics g` (from `PaintEventArgs`) | Same concept |
| `g.DrawImage(img, x, y, w, h)` | `g.DrawImage(img, x, y, w, h)` | Nearly identical |
| `g.DrawString(text, font, brush, x, y)` | `g.DrawText(font, color, x, y, text)` | Parameter order differs |
| `g.FillRectangle(brush, rect)` | `g.FillRectangle(color, rect)` | Uses Color instead of Brush |
| `g.DrawRectangle(pen, rect)` | `g.DrawRectangle(color, rect)` | Uses Color instead of Pen |
| `GraphicsPath` | `GraphicsPath` | Similar (Eto has its own) |
| `g.FillPath(brush, path)` | `g.FillPath(color, path)` | |
| `SolidBrush(Color)` | Just use `Color` directly | Eto simplifies brush usage |
| `Pen(Color, width)` | `Pen(Color, width)` or just `Color` with `g.DrawLine()` | |
| `Bitmap(width, height)` | `new Bitmap(width, height, PixelFormat.Format32bppRgba)` | |
| `Image.FromStream(stream)` | `new Bitmap(stream)` | |
| `g.InterpolationMode = ...` | `g.ImageInterpolation = ...` | Enum differs |
| `e.Graphics.MeasureString()` | `Font.MeasureString()` | Called on Font, not Graphics |
| `StringFormat` | `FormattedText` (if needed) | |

### Events

| WinForms | Eto.Forms | Notes |
|----------|-----------|-------|
| `Click += handler` | `Click += handler` | Identical |
| `Paint += handler` | (on `Drawable`) `Paint += handler` | `PaintEventArgs` wraps Eto `Graphics` |
| `MouseDown += handler` | `MouseDown += handler` | Similar |
| `MouseMove += handler` | `MouseMove += handler` | Similar |
| `SelectedIndexChanged` | `SelectedIndexChanged` | Similar |
| `TextChanged` | `TextChanged` | Identical |
| `KeyDown` | `KeyDown` | Similar |
| `DragDrop` | `DragDrop` | Similar |
| `Shown` | `Shown` | Identical |
| `FormClosing` | `Closing` | Different name |

---

## Appendix C - Eto.Forms Dependencies by Platform

### NuGet Packages

```xml
<!-- Core (all platforms) -->
<PackageReference Include="Eto.Forms" Version="2.8.*" />

<!-- Windows backend (choose one) -->
<PackageReference Include="Eto.Platform.WinForms" Version="2.8.*" />
<!-- OR -->
<PackageReference Include="Eto.Platform.Wpf" Version="2.8.*" />

<!-- Linux backend -->
<PackageReference Include="Eto.Platform.Gtk" Version="2.8.*" />

<!-- macOS backend -->
<PackageReference Include="Eto.Platform.Mac64" Version="2.8.*" />
```

### Platform Detection in Program.cs

```csharp
using Eto;
using Eto.Forms;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Eto.Forms auto-detects the platform, or it can be specified:
        // new Application(Platforms.WinForms)  - Windows (WinForms backend)
        // new Application(Platforms.Wpf)       - Windows (WPF backend)
        // new Application(Platforms.Gtk)       - Linux (GTK3 backend)
        // new Application(Platforms.Mac64)     - macOS (Cocoa backend)

        new Application().Run(new MainForm());
    }
}
```

### Runtime Dependencies

| Platform | Runtime Dependency | How Users Get It |
|----------|-------------------|-----------------|
| **Windows** | .NET 10 Runtime | Bundled (self-contained publish) or separate install |
| **Linux** | .NET 10 Runtime + GTK3 | GTK3 is pre-installed on most desktops (GNOME, XFCE, Cinnamon, MATE). .NET bundled or separate. |
| **macOS** | .NET 10 Runtime | Bundled (self-contained publish). Cocoa is built into macOS. |

### Self-Contained Publish Commands

```bash
# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

---

## Overall Timeline Summary

```
Phase 0: Wine/Compatibility Quick-Ship
  Lot 0.1  Wine testing + Linux bundle         ~1–2 hr    ─┐
  Lot 0.2  macOS setup guides                  ~1–2 hr    ─┘ Total: ~2–4 hr

Phase 1: NMSE.Lib Extraction (on main)
  Lot 1.1  Lib project skeleton                ~30 min    ─┐
  Lot 1.2  Move Models (20 files)              ~1 hr       │
  Lot 1.3  Move Config (1 file)                ~20 min     │
  Lot 1.4  Move IO (12 files)                  ~30 min     │
  Lot 1.5  Move Core (21 files)                ~30 min     │
  Lot 1.6  Move Data + IconManager abstraction ~2–3 hr     │
  Lot 1.7  Move Resources                      ~1 hr       │
  Lot 1.8  Update test projects                ~1 hr       │
  Lot 1.9  Final verification & cleanup        ~1 hr      ─┘ Total: ~8.5 hr

Phase 2: Eto.Forms UI Conversion (on eto-forms branch)
  Lot 2.1  Eto.Forms project setup             ~1–2 hr    ─┐
  Lot 2.2  EtoIconProvider                     ~1–2 hr     │
  Lot 2.3  FontManager port                    ~1 hr       │
  Lot 2.4  MainForm shell                      ~3–4 hr     │
  Lot 2.5  Save load/save infrastructure       ~2–3 hr     │
  Lot 2.6  Utility classes port                ~1–2 hr     │
  Lots 2.7–2.11  Simple panels (5)             ~3.5 hr     │
  Lots 2.12–2.18 Medium panels (7)             ~12.5 hr    │
  Lots 2.19–2.25 Hard panels (5)               ~17 hr      │
  Lot 2.26 InventoryGridPanel                  ~5–8 hr     │
  Lot 2.27 Glyph rendering                    ~1 hr       │
  Lot 2.28 Integration testing                 ~2–3 hr    ─┘ Total: ~50–60 hr

Phase 3: Polish, Packaging & Release (on eto-forms branch)
  Lot 3.1  Publish profiles                    ~2 hr      ─┐
  Lot 3.2  Linux packaging                     ~2 hr       │
  Lot 3.3  macOS packaging                     ~2 hr       │
  Lot 3.4  Windows packaging update            ~1 hr       │
  Lot 3.5  Documentation                       ~1–2 hr     │
  Lot 3.6  Merge to main                       ~1 hr      ─┘ Total: ~9–10 hr

══════════════════════════════════════════════════════════════
GRAND TOTAL (Effort):  ~70–83 hours across ~40 lots
══════════════════════════════════════════════════════════════
```

### In Terms of Sessions

Assuming each session is ~2–4 hours of focused work:

| Phase | Sessions | Calendar Time (1 session/day) |
|-------|----------|-------------------------------|
| Phase 0 | 1 session | 1 day |
| Phase 1 | 3–4 sessions | 3–4 days |
| Phase 2 | 15–20 sessions | 3–4 weeks |
| Phase 3 | 3–4 sessions | 3–4 days |
| **Total** | **~22–29 sessions** | **~4–6 weeks** |

---

*This document is the single source of truth for the NMSE cross-platform migration. All architectural decisions, effort estimates, file inventories, and acceptance criteria are maintained here. Update this document as work progresses.*
