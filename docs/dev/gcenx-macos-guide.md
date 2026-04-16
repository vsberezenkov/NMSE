# Running NMSE on macOS with Gcenx Wine Builds

A step-by-step guide to running NMSE (No Man's Save Editor) on macOS using the Gcenx Wine packages from [Gcenx/macOS_Wine_builds](https://github.com/Gcenx/macOS_Wine_builds).

> **Note:** This is an interim solution. A native cross-platform version using Eto.Forms is planned - see the [Cross-Platform Work Plan](cross-platform-workplan.md) for details.

---

## Table of Contents

1. [Overview](#overview)
2. [Why Gcenx Wine Builds?](#why-gcenx-wine-builds)
3. [System Requirements](#system-requirements)
4. [Installation](#installation)
5. [Setting Up NMSE](#setting-up-nmse)
6. [Finding Your NMS Save Files](#finding-your-nms-save-files)
7. [Troubleshooting](#troubleshooting)
8. [Known Limitations](#known-limitations)
9. [Alternative: CrossOver (Paid)](#alternative-crossover-paid)
10. [Future: Native macOS Support](#future-native-macos-support)

---

## Overview

Gcenx Wine Builds are community-built Wine packages for macOS that provide a straightforward way to run Windows applications without using a virtual machine. This guide walks through installing a Gcenx Wine package, creating a Wine prefix for NMSE, and opening your save files.

**Why Gcenx Wine Builds?**
- **Free and open-source** Wine packages for macOS
- **Wine-Staging 11.6** package available and recommended
- **Gecko and Mono included** to reduce prefix size
- Works with **macOS Intel** and **Apple Silicon** via Rosetta 2
- Supports the Windows applications NMSE requires

---

## Why Gcenx Wine Builds?

Gcenx Wine builds are a strong free alternative to commercial macOS Wine wrappers. They are packaged as native macOS app bundles and can be installed directly from GitHub releases.

Advantages:
- No need for a separate Wine wrapper app like CrossOver
- Official WineHQ configuration options included
- Manual install from the released tarball
- Includes both 32-bit and 64-bit Wine support

---

## System Requirements

| Requirement | Detail |
|------------|--------|
| **macOS version** | macOS 12 Monterey or later |
| **Chip** | Apple Silicon (M1/M2/M3/M4) or Intel |
| **Rosetta 2** | Required on Apple Silicon |
| **Disk space** | ~1 GB (Wine + NMSE + save files) |

---

## Installation

### Step 1: Install the Gcenx Wine-Staging 11.6 package

Download the tested Gcenx Wine-Staging 11.6 bundle for macOS and install it manually.

1. Visit https://github.com/Gcenx/macOS_Wine_builds/releases
2. Download the `Wine-Staging 11.6` package
3. Extract the `.tar.xz`
4. Move the resulting `Wine *.app` bundle to `/Applications`

The Gcenx Wine-Staging 11.6 build includes `wine-gecko` and `wine-mono 11.0.0`. The Wine-Mono package initializes automatically when the prefix is created, so no separate Mono install is required. This build is known to work well for NMSE on Apple Silicon Macs such as M4 Pro with macOS Tahoe.

### Step 2: Download NMSE

1. Go to https://github.com/vectorcmdr/NMSE/releases
2. Download the latest `NMSE-<version>.zip` file
3. Extract it to a convenient folder, for example:

```bash
mkdir -p ~/Applications/NMSE
unzip ~/Downloads/NMSE-*.zip -d ~/Applications/NMSE
```

You should now have `NMSE.exe` and a `Resources/` folder in your NMSE directory.

---

## Setting Up NMSE

### Step 3: Launch NMSE

With Wine-Staging 11.6 installed, you can launch NMSE directly from the Wine bundle. Wine will automatically create and initialise a prefix the first time it runs NMSE.

From the NMSE install folder:

```bash
cd ~/Applications/NMSE
/Applications/Wine-Staging\ 11.6.app/Contents/Resources/bin/wine NMSE.exe
```

If your Gcenx bundle has a different app name, replace `Wine-Staging 11.6.app` with the installed app bundle name.

On first launch, Wine will create the prefix automatically and may take 20–30 seconds to initialise.

---

## Finding Your NMS Save Files

### Native macOS Steam Saves

No Man's Sky save files are usually stored here on macOS:

```bash
~/Library/Application Support/HelloGames/NMS/<profile_id>/
```

To open the folder in Finder:
1. Press `Cmd+Shift+G`
2. Paste: `~/Library/Application Support/HelloGames/NMS`

### Open Saves from NMSE

When NMSE shows the file browser, the macOS filesystem is available under Wine's `Z:\` drive:

```bash
Z:\Users\<username>\Library\Application Support\HelloGames\NMS\
```

If you prefer, you can also open the save folder using a Wine file manager:

```bash
export WINEPREFIX="$HOME/wineprefixes/nmse"
/Applications/Wine\ Stable.app/Contents/Resources/bin/winefile
```

---

## Troubleshooting

### Rosetta 2 Not Installed (Apple Silicon)

If NMSE fails to start on an Apple Silicon Mac, install Rosetta 2:

```bash
softwareupdate --install-rosetta --agree-to-license
```

### Wine Package Not Found

If the `wine` or `winecfg` command cannot be found, verify the app bundle is in `/Applications` and use the full path to the bundle's `bin` folder.

### Wine Crashes on Launch

1. Delete the Wine prefix: `rm -rf "$HOME/wineprefixes/nmse"`
2. Recreate it with `wineboot`
3. Run `winecfg` again
4. Launch NMSE again

### Font or UI Problems

Install Windows core fonts:

```bash
/Applications/Wine\ Stable.app/Contents/Resources/bin/winetricks corefonts
```

### DPI / Retina Display Issues

If the UI appears too small on Retina screens:
1. Run `winecfg`
2. Open the **Graphics** tab
3. Set a custom DPI value such as **144** or **192**
4. Restart NMSE

### Save Files Not Found

- macOS saves: `~/Library/Application Support/HelloGames/NMS/`
- In NMSE, browse to `Z:\Users\<username>\Library\Application Support\HelloGames\NMS\`
- If the `Z:` drive is missing, open `winecfg` and verify that the drives tab maps your home folder

---

## Known Limitations

1. **Windows-style UI** - NMSE is still a Windows application running under Wine.
2. **File paths** - Wine exposes macOS paths with Windows-style drive letters (for example `Z:\`).
3. **Performance** - May be slower than native due to Wine translation and Rosetta 2 on Apple Silicon.
4. **Retina support** - May require manual DPI tuning for crisp text.
5. **macOS integration** - No native Dock or Cmd-key support in the Wine app.
6. **Wine package size** - Wine and NMSE together use around 1 GB of disk space.

---

## Alternative: CrossOver (Paid)

If you prefer a more polished, supported experience, see the [CrossOver macOS Guide](crossover-macos-guide.md). CrossOver offers:
- Better Apple Silicon performance in many cases
- Commercial support and updates
- A more automated setup flow

---

## Future: Native macOS Support

The Wine compatibility layer is an interim solution. The planned native cross-platform version will:

- Use **Eto.Forms** for a native macOS UI
- Share business logic through **NMSE.Lib**
- Support native menu bar and Cmd shortcuts
- Ship as a small `.app` bundle instead of requiring Wine

See the [Cross-Platform Work Plan](cross-platform-workplan.md) for the full migration roadmap.
