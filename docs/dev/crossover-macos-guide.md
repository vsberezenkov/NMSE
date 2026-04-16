# Running NMSE on macOS with CrossOver

A step-by-step guide to running NMSE (No Man's Save Editor) on macOS using [CrossOver](https://www.codeweavers.com/crossover), a commercial Wine distribution by CodeWeavers.

> **Note:** This is an interim solution. A native cross-platform version using Eto.Forms is planned - see the [Cross-Platform Work Plan](cross-platform-workplan.md) for details.

---

## Table of Contents

1. [Overview](#overview)
2. [Why CrossOver?](#why-crossover)
3. [System Requirements](#system-requirements)
4. [Installation](#installation)
5. [Setting Up NMSE](#setting-up-nmse)
6. [Finding Your NMS Save Files](#finding-your-nms-save-files)
7. [Troubleshooting](#troubleshooting)
8. [Comparison: CrossOver vs Gcenx Wine Builds](#comparison-crossover-vs-gcenx-wine-builds)

---

## Overview

[CrossOver](https://www.codeweavers.com/crossover) is a commercial Wine distribution ($74/year or $494 lifetime) made by CodeWeavers - the primary commercial contributors to the Wine project. It provides the most polished experience for running Windows applications on macOS, with particular strength on Apple Silicon Macs.

---

## Why CrossOver?

| Feature | CrossOver | Gcenx Wine Builds (Free Alternative) |
|---------|-----------|--------------------------|
| **Cost** | $74/year | Free |
| **Apple Silicon** | Excellent (dedicated optimisation) | Good (Rosetta 2) |
| **Setup difficulty** | Very Easy | Easy |
| **Support** | Commercial (email + forum) | Community |
| **Auto-updates** | Yes | Yes |
| **Performance** | Best on Apple Silicon | Good |

**Choose CrossOver if:**
- You want the easiest, most polished experience
- You're on an Apple Silicon Mac and want best performance
- You value commercial support
- You run other Windows applications on macOS

**Choose Gcenx Wine Builds if:**
- You prefer a free solution
- You're comfortable with minor configuration
- You only need it for NMSE

---

## System Requirements

| Requirement | Detail |
|------------|--------|
| **macOS version** | macOS 12 Monterey or later |
| **Chip** | Apple Silicon (M1/M2/M3/M4) or Intel |
| **Disk space** | ~500 MB (CrossOver + NMSE) |
| **CrossOver licence** | Required ($74/year or $494 lifetime) |

---

## Installation

### Step 1: Install CrossOver

1. Visit https://www.codeweavers.com/crossover
2. Purchase a licence (or start a free 14-day trial)
3. Download and install CrossOver
4. Open CrossOver and activate your licence

### Step 2: Download NMSE

1. Go to https://github.com/vectorcmdr/NMSE/releases
2. Download the latest `NMSE-<version>-Release.zip` file (listed under the "Latest Build" release)
3. Extract the zip to a convenient location (e.g. `~/Downloads/NMSE/`)

---

## Setting Up NMSE

### Step 3: Create a Bottle

1. Open **CrossOver**
2. Click **Bottle** -> **New Bottle**
3. Configure:
   - **Bottle Name:** `NMSE`
   - **Bottle Type:** "Windows 10 64-bit"
4. Click **Create**

<!-- Screenshot placeholder: CrossOver new bottle dialog -->

### Step 4: Install NMSE into the Bottle

1. Click **Bottle** -> **Open C: Drive** for the NMSE bottle
2. Create a folder called `NMSE` inside `drive_c/`
3. Copy the contents of your extracted NMSE download into this folder
4. The structure should be:
   ```
   drive_c/NMSE/
   ├── NMSE.exe
   ├── Resources/
   │   ├── json/
   │   ├── images/
   │   ├── icons/
   │   ├── ui/
   │   └── map/
   └── ... (DLLs and other files)
   ```

### Step 5: Add NMSE as a Program

1. In CrossOver, select the **NMSE** bottle
2. Click **Run Command** (or **Configure** -> **Add Application**)
3. Browse to `C:\NMSE\NMSE.exe`
4. Click **Run** to test

### Step 6: Create a Desktop Shortcut (Optional)

1. Right-click the NMSE program entry in CrossOver
2. Select **Create Shortcut**
3. A shortcut will appear in your Dock/Launchpad

---

## Finding Your NMS Save Files

### macOS Native Steam

Save files for No Man's Sky on macOS are typically at:
```
~/Library/Application Support/HelloGames/NMS/<profile_id>/
```

### In NMSE

When browsing for saves in NMSE:
- macOS folders appear under `Z:\` (Wine's mapping of the macOS filesystem)
- Navigate to: `Z:\Users\<username>\Library\Application Support\HelloGames\NMS\`

### Creating a Shortcut

You can create a symlink inside the CrossOver bottle to make saves easier to find:
1. Open the NMSE bottle's C: drive
2. In Terminal:
   ```bash
   cd ~/Library/Application\ Support/CrossOver/Bottles/NMSE/drive_c/users/crossover/Desktop/
   ln -s ~/Library/Application\ Support/HelloGames/NMS NMS-Saves
   ```
3. In NMSE, the saves will appear on the Wine desktop as `NMS-Saves`

---

## Troubleshooting

### NMSE Doesn't Launch

1. Verify the bottle is set to "Windows 10 64-bit"
2. Ensure NMSE.exe is present at `C:\NMSE\NMSE.exe` inside the bottle
3. Try: CrossOver -> NMSE bottle -> **Run Command** -> browse to NMSE.exe

### Font Issues

If fonts look wrong:
1. Select the NMSE bottle in CrossOver
2. Click **Install Software**
3. Search for "Core Fonts" and install it

### Window Too Small / Too Large

1. Select the NMSE bottle -> **Wine Configuration** (winecfg)
2. Go to the **Graphics** tab
3. Adjust the **Screen Resolution (DPI)** slider:
   - 96 DPI = 100% (standard displays)
   - 144 DPI = 150% (Retina displays)
   - 192 DPI = 200% (HiDPI)

### Slow Performance

On Apple Silicon Macs, first-launch performance may be slower due to Rosetta 2 translation. Subsequent launches should be much faster. If performance remains poor:
1. Ensure CrossOver is updated to the latest version
2. In the bottle settings, enable **Enhanced Sync (ESync)**

---

## Comparison: CrossOver vs Gcenx Wine Builds

| Aspect | CrossOver | Gcenx Wine Builds |
|--------|-----------|--------|
| **Cost** | $74/year or $494 lifetime | Free (GPL-3.0) |
| **Ease of setup** | Easiest (guided) | Easy (manual bottle creation) |
| **Apple Silicon** | Best support | Good (Rosetta 2) |
| **Performance** | Best | Good |
| **Support** | Commercial (email) | Community (GitHub) |
| **macOS integration** | Better (shortcuts, Dock) | Basic |
| **Auto-updates** | Yes | Yes |

For most users, **Gcenx Wine Builds** (free) provides a perfectly adequate experience. CrossOver is worth considering if you want the most polished experience or use other Windows applications on your Mac.

---

## Future: Native macOS Support

See the [Cross-Platform Work Plan](cross-platform-workplan.md) for the full migration roadmap to a native Eto.Forms macOS application.
