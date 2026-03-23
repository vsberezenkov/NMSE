#!/usr/bin/env bash
# ================================================================
# NMSE Wine Launch Script for Linux
#
# Launches NMSE (No Man's Save Editor) using Wine on Linux.
# Detects system Wine or a bundled Wine prefix and configures
# the environment for best WinForms compatibility.
#
# Usage:
#   ./nmse.sh                    # Normal launch
#   ./nmse.sh --debug            # Launch with Wine debug output
#   ./nmse.sh --reset-prefix     # Delete and recreate Wine prefix
#   ./nmse.sh --help             # Show help
#
# Requirements:
#   - Wine 9.0+ (wine-stable) or bundled Wine
#   - x86_64 Linux (Wine does not support ARM Linux)
#
# See docs/wine-linux-guide.md for full setup instructions.
# ================================================================

set -euo pipefail

# Constants
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/app"
NMSE_EXE="$APP_DIR/NMSE.exe"
PREFIX_DIR="$SCRIPT_DIR/.nmse-wineprefix"

# Minimum Wine version (major.minor)
MIN_WINE_MAJOR=9
MIN_WINE_MINOR=0

# Helpers
log()   { echo "[NMSE] $*"; }
warn()  { echo "[NMSE] WARNING: $*" >&2; }
die()   { echo "[NMSE] ERROR: $*" >&2; exit 1; }

usage() {
    cat <<EOF
NMSE - No Man's Save Editor (Wine Launcher for Linux)

Usage: $(basename "$0") [OPTIONS]

Options:
  --debug          Enable Wine debug logging (written to nmse-wine.log)
  --reset-prefix   Delete and recreate the Wine prefix
  --winecfg        Open Wine configuration dialog
  --help           Show this help message

The launcher will:
  1. Detect Wine (system-installed or bundled)
  2. Create/reuse a dedicated Wine prefix at .nmse-wineprefix/
  3. Launch NMSE.exe with WinForms-friendly settings

Requirements:
  - Wine 9.0 or later (install via your package manager)
  - x86_64 Linux system

For full setup instructions, see: docs/wine-linux-guide.md
EOF
    exit 0
}

# Version check
check_wine_version() {
    local version_str
    version_str="$("$WINE_BIN" --version 2>/dev/null)" || die "Cannot determine Wine version"

    # Extract version number (e.g. "wine-9.0" -> "9.0", "wine-9.21 (Staging)" -> "9.21")
    local ver
    ver="$(echo "$version_str" | sed -n 's/^wine-\([0-9]*\.[0-9]*\).*/\1/p')"
    if [[ -z "$ver" ]]; then
        warn "Could not parse Wine version from: $version_str"
        return
    fi

    local major minor
    major="${ver%%.*}"
    minor="${ver#*.}"

    if (( major < MIN_WINE_MAJOR )) || { (( major == MIN_WINE_MAJOR )) && (( minor < MIN_WINE_MINOR )); }; then
        warn "Wine $ver detected - Wine $MIN_WINE_MAJOR.$MIN_WINE_MINOR+ recommended."
        warn "Older versions may have rendering quirks with .NET WinForms apps."
    else
        log "Wine $ver detected ✓"
    fi
}

# Locate Wine
find_wine() {
    # 1. Bundled Wine (if building an AppImage or portable bundle)
    if [[ -x "$SCRIPT_DIR/wine/bin/wine" ]]; then
        WINE_BIN="$SCRIPT_DIR/wine/bin/wine"
        log "Using bundled Wine: $WINE_BIN"
        return
    fi
    if [[ -x "$SCRIPT_DIR/wine/bin/wine64" ]]; then
        WINE_BIN="$SCRIPT_DIR/wine/bin/wine64"
        log "Using bundled Wine (wine64): $WINE_BIN"
        return
    fi

    # 2. System Wine (try 'wine', then 'wine64' for Ubuntu 24.04+)
    if command -v wine >/dev/null 2>&1; then
        WINE_BIN="$(command -v wine)"
        log "Using system Wine: $WINE_BIN"
        return
    fi

    if command -v wine64 >/dev/null 2>&1; then
        WINE_BIN="$(command -v wine64)"
        log "Using system Wine (wine64): $WINE_BIN"
        return
    fi

    # 3. Ubuntu 24.04 system packages put wine64 in /usr/lib/wine/ (not PATH)
    if [[ -x /usr/lib/wine/wine64 ]]; then
        WINE_BIN="/usr/lib/wine/wine64"
        log "Using system Wine: $WINE_BIN"
        return
    fi

    die "Wine not found. Install Wine 9.0+ via your package manager:
    Ubuntu/Debian:  sudo apt install wine
    Fedora:         sudo dnf install wine
    Arch:           sudo pacman -S wine
    openSUSE:       sudo zypper install wine

  Or visit: https://wiki.winehq.org/Download"
}

# Wine prefix setup
setup_prefix() {
    export WINEPREFIX="$PREFIX_DIR"
    export WINEARCH=win64

    if [[ ! -d "$PREFIX_DIR/drive_c" ]]; then
        log "Creating Wine prefix at $PREFIX_DIR ..."
        "$WINE_BIN" wineboot --init 2>/dev/null || true
        log "Wine prefix created ✓"
    fi
}

# NMS save directory symlink hint
show_save_hint() {
    local steam_save="$HOME/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS"
    local flatpak_save="$HOME/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS"

    if [[ -d "$steam_save" ]]; then
        log "NMS saves found at: $steam_save"
        log "In NMSE, browse to: Z:$(echo "$steam_save" | sed 's|/|\\|g')"
    elif [[ -d "$flatpak_save" ]]; then
        log "NMS saves found (Flatpak Steam): $flatpak_save"
        log "In NMSE, browse to: Z:$(echo "$flatpak_save" | sed 's|/|\\|g')"
    else
        log "NMS save directory not found. You can manually browse to your saves."
        log "Wine maps Linux / as Z:\\ - look under Z:\\ for your save path."
    fi
}

# Main
main() {
    local debug=false
    local reset=false
    local winecfg=false

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --debug)        debug=true; shift ;;
            --reset-prefix) reset=true; shift ;;
            --winecfg)      winecfg=true; shift ;;
            --help|-h)      usage ;;
            *)              warn "Unknown option: $1"; shift ;;
        esac
    done

    # Find Wine binary
    find_wine
    check_wine_version

    # Handle prefix reset
    if $reset && [[ -d "$PREFIX_DIR" ]]; then
        log "Removing existing Wine prefix ..."
        rm -rf "$PREFIX_DIR"
    fi

    # Set up Wine prefix
    setup_prefix

    # Wine configuration dialog
    if $winecfg; then
        log "Opening Wine configuration ..."
        "$WINE_BIN" winecfg
        exit 0
    fi

    # Verify NMSE.exe exists
    if [[ ! -f "$NMSE_EXE" ]]; then
        die "NMSE.exe not found at: $NMSE_EXE
    Expected layout:
      $(basename "$0")
      app/
        NMSE.exe
        Resources/
        ...

    Download the latest Windows build from:
      https://github.com/vectorcmdr/NMSE/releases"
    fi

    # Show save directory hint on first run
    show_save_hint

    # Wine environment for best WinForms compatibility
    # Disable Gecko/Mono installers (not needed for .NET self-contained builds)
    export WINEDLLOVERRIDES="mscoree=d;mshtml=d"

    # Better font rendering
    export FREETYPE_PROPERTIES="truetype:interpreter-version=40"

    # Debug logging
    if $debug; then
        export WINEDEBUG="+all"
        log "Debug mode ON - logging to nmse-wine.log"
        "$WINE_BIN" "$NMSE_EXE" "$@" > "$SCRIPT_DIR/nmse-wine.log" 2>&1
    else
        # Suppress most Wine debug output for clean terminal
        export WINEDEBUG="-all"
        "$WINE_BIN" "$NMSE_EXE" "$@" 2>/dev/null
    fi
}

main "$@"
