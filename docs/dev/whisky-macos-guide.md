# Running NMSE on macOS with Gcenx Wine Builds

A step-by-step guide to running NMSE (No Man's Save Editor) on macOS using Gcenx Wine Builds from [Gcenx/macOS_Wine_builds](https://github.com/Gcenx/macOS_Wine_builds).

> **Note:** This is an interim solution. A native cross-platform version using Eto.Forms is planned - see the [Cross-Platform Work Plan](cross-platform-workplan.md) for details.

---

## Table of Contents

1. [Overview](#overview)
2. [System Requirements](#system-requirements)
3. [Installation](#installation)
4. [Setting Up NMSE with Gcenx Wine Builds](#setting-up-nmse-with-gcenx-wine-builds)
5. [Finding Your NMS Save Files](#finding-your-nms-save-files)
6. [Homebrew Cask Installation (Advanced)](#homebrew-cask-installation-advanced)
7. [Troubleshooting](#troubleshooting)
8. [Known Limitations](#known-limitations)

---

## Overview

Gcenx Wine Builds are community-built Wine packages for macOS. They provide Wine app bundles that can run Windows applications and manage Wine prefixes on macOS.

**Why Gcenx Wine Builds?**
- **Free and open-source** (GPL-3.0)
- **Apple Silicon support** via Rosetta 2 (M1/M2/M3/M4 Macs)
- **Intel Mac support** (native Wine)
- **Clean macOS UI** built with SwiftUI
- **Easy bottle management** - each app gets its own environment
- **Active development** and community

---

## System Requirements

| Requirement | Detail |
|------------|--------|
| **macOS version** | macOS 13 Ventura or later |
| **Chip** | Apple Silicon (M1/M2/M3/M4) or Intel |
| **Rosetta 2** | Required on Apple Silicon (Gcenx Wine Builds prompts to install if missing) |
| **Disk space** | ~1 GB (Gcenx Wine Builds + Wine + NMSE) |
| **Homebrew** | Recommended for installation (optional - Gcenx Wine Builds also has a direct download) |

---

## Installation

### Step 1: Install Gcenx Wine Builds

**Via Homebrew (recommended):**
```bash
# Download and install a Gcenx Wine package from GitHub releases
```

**Via direct download:**
1. Visit https://github.com/Gcenx/macOS_Wine_builds
2. Download the desired package
3. Install the app bundle on macOS

### Step 2: Download NMSE

1. Go to https://github.com/vectorcmdr/NMSE/releases
2. Download the latest `NMSE-<version>.zip` file (listed under the "Latest Build" release)
3. Extract it - you'll get a folder containing `NMSE.exe` and the `Resources/` directory
4. Move this folder somewhere convenient (e.g. `~/Applications/NMSE/`)

```bash
# Or via terminal (uses GitHub API to find the latest zip):
cd ~/Applications
mkdir -p NMSE && cd NMSE
DOWNLOAD_URL=$(curl -s https://api.github.com/repos/vectorcmdr/NMSE/releases/tags/latest \
  | grep -o '"browser_download_url": "[^"]*\.zip"' \
  | head -1 | cut -d'"' -f4)
curl -L -o NMSE-latest.zip "$DOWNLOAD_URL"
unzip NMSE-latest.zip
rm NMSE-latest.zip
```

---

## Setting Up NMSE with Gcenx Wine Builds

### Step 3: Create a Bottle

1. Open the Gcenx Wine app bundle from Applications
2. Click the **+** button (or "Create Bottle")
3. Configure the bottle:
   - **Name:** `NMSE`
   - **Windows Version:** Windows 10
   - **Path:** Leave as default (or choose a location)
4. Click **Create**

<!-- Screenshot placeholder: Gcenx Wine Builds create bottle dialog -->

### Step 4: Add NMSE to the Bottle

1. Select the **NMSE** bottle from the sidebar
2. Click **Pin Program** (or "Add Program")
3. Navigate to where you extracted NMSE and select `NMSE.exe`
4. Click **Open**

<!-- Screenshot placeholder: Gcenx Wine Builds add program dialog -->

### Step 5: Launch NMSE

1. In the NMSE bottle, click on `NMSE.exe` in the programs list
2. Click **Run** (▶)
3. NMSE will launch - first launch may take 20–30 seconds as Wine initialises

<!-- Screenshot placeholder: NMSE running with Gcenx Wine Builds on macOS -->

### Optional: Bottle Configuration

For best compatibility, you can adjust these settings in the bottle:

1. Select the NMSE bottle
2. Click **Settings** (⚙)
3. Recommended settings:
   - **DPI:** Adjust if the UI looks too small/large on Retina displays (try 144 for Retina)
   - **Enhanced Sync:** Enable (ESync) for better performance
   - **Metal HUD:** Off (not needed for a WinForms app)

---

## Finding Your NMS Save Files

### macOS Native Steam Installation

If you play NMS via Steam on macOS:
```
~/Library/Application Support/HelloGames/NMS/<profile_id>/
```

To navigate to this folder:
1. In **Finder**, press `Cmd+Shift+G` (Go to Folder)
2. Paste: `~/Library/Application Support/HelloGames/NMS`
3. Select your profile folder

### In NMSE's Directory Browser

When NMSE opens its directory browser, the macOS filesystem appears under Wine's `Z:\` drive:
```
Z:\Users\<username>\Library\Application Support\HelloGames\NMS\
```

Alternatively, you can use the Gcenx Wine prefix's `drive_c` to place a shortcut:
1. In the Gcenx Wine app bundle, click **Open C: Drive** on your NMSE prefix
2. Navigate to `users/<username>/Desktop/`
3. Create a symbolic link to your NMS saves:
   ```bash
   ln -s ~/Library/Application\ Support/HelloGames/NMS ~/Library/Containers/com.gcenx.app/.../drive_c/users/$(whoami)/Desktop/NMS-Saves
   ```

---

## Homebrew Cask Installation (Advanced)

A Homebrew Cask formula is provided for automated installation:

```bash
# From the NMSE repository:
brew install --cask scripts/macos/nmse-whisky.rb
```

This will:
1. Download the latest NMSE Windows build
2. Install it to `~/Applications/NMSE/`
3. Create a launch script at `~/Applications/NMSE/nmse-launch.sh`

After installation:
```bash
~/Applications/NMSE/nmse-launch.sh
```

The launch script automatically finds Gcenx Wine Builds' bundled Wine and uses it.

---

## Troubleshooting

### Rosetta 2 Not Installed (Apple Silicon)

If you see a Rosetta 2 prompt:
```bash
softwareupdate --install-rosetta --agree-to-license
```

### Wine Crashes on Launch

1. In Gcenx Wine Builds, delete the NMSE bottle
2. Create a new bottle with Windows 10 selected
3. Re-add NMSE.exe

### Font Issues

If fonts look wrong in NMSE:
1. In Gcenx Wine Builds, select the NMSE bottle
2. Click **Open Terminal**
3. Run: `winetricks corefonts`

### DPI / Retina Display Issues

On Retina displays, NMSE may look very small:
1. In Gcenx Wine Builds, select the NMSE bottle -> **Settings**
2. Adjust **DPI** to 144 (for standard Retina) or 192 (for scaled Retina)
3. Restart NMSE

### Slow First Launch

The first time you launch NMSE in a new bottle, Wine needs to initialise the prefix. This can take 20–30 seconds. Subsequent launches will be much faster (2–5 seconds).

### Application Window Doesn't Appear

Wait 10–15 seconds - on Apple Silicon, Rosetta 2 translation adds initial overhead. If still nothing:
1. Check the Gcenx Wine Build's console output (click **Show Log** in the bottle)
2. Ensure NMSE.exe is a valid Windows executable (should be ~5–10 MB for the main exe)

### Save Files Not Found

- macOS saves: `~/Library/Application Support/HelloGames/NMS/`
- In NMSE, navigate via `Z:\Users\<username>\Library\Application Support\HelloGames\NMS\`
- If you can't find `Z:\`, try navigating to `My Computer` -> `Z:` in the file dialog

---

## Known Limitations

1. **Windows-style UI** - NMSE looks like a Windows application, not a native macOS app
2. **File paths** - Shown in Windows format (`Z:\Users\...`) rather than macOS format (`/Users/...`)
3. **Performance** - Slightly slower than native due to Wine + Rosetta 2 translation (on Apple Silicon)
4. **Retina displays** - May need manual DPI configuration for crisp rendering
5. **macOS integration** - No Dock icon, menu bar integration, or native keyboard shortcuts (Cmd key)
6. **Bundle size** - Gcenx Wine Builds + Wine + NMSE uses ~1 GB of disk space

---

## Alternative: CrossOver (Paid)

If you prefer a more polished experience with commercial support, see the [CrossOver macOS Guide](crossover-macos-guide.md). CrossOver ($74/year) offers:
- Better Apple Silicon performance (dedicated translation layer)
- Pre-configured application profiles
- Commercial technical support
- Automatic updates

---

## Future: Native macOS Support

The Wine compatibility layer is an interim solution. The planned native cross-platform version will:

- Use **Eto.Forms** for the UI (native Cocoa on macOS)
- Share all business logic via **NMSE.Lib** (platform-independent shared library)
- Look and feel native on macOS (native menu bar, Cmd shortcuts, Retina support)
- Be a ~50 MB `.app` bundle instead of requiring Wine

See the [Cross-Platform Work Plan](cross-platform-workplan.md) for the full migration roadmap.
