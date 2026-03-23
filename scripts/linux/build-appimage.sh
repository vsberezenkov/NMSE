#!/usr/bin/env bash
# ──────────────────────────────────────────────────────────────
# build-appimage.sh - Build an NMSE AppImage for Linux
#
# Creates a self-contained AppImage that bundles:
#   1. The NMSE Windows build (self-contained .NET, win-x64)
#   2. A minimal Wine installation for running the WinForms app
#
# Prerequisites (build host):
#   - Linux x86_64
#   - Wine 9.0+ installed (the script copies it into the AppImage)
#   - wget or curl
#   - NMSE published build (self-contained win-x64)
#
# Usage:
#   # First, publish NMSE on Windows (or cross-compile):
#   dotnet publish NMSE.csproj -c Release -r win-x64 --self-contained
#
#   # Then on Linux, build the AppImage:
#   ./build-appimage.sh /path/to/nmse-publish-output
#
# Output:
#   NMSE-x86_64.AppImage  (~300-500 MB)
#
# Users just: chmod +x NMSE-x86_64.AppImage && ./NMSE-x86_64.AppImage
# ──────────────────────────────────────────────────────────────

set -euo pipefail

# ── Configuration ─────────────────────────────────────────────
APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# ── Input validation ──────────────────────────────────────────
if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <path-to-nmse-publish-output> [output-file]"
    echo ""
    echo "  <path-to-nmse-publish-output>  Directory containing NMSE.exe and Resources/"
    echo "                                  (output of 'dotnet publish -r win-x64 --self-contained')"
    echo "  [output-file]                  Optional output path (default: NMSE-x86_64.AppImage)"
    echo ""
    echo "Prerequisites:"
    echo "  - Linux x86_64 build host"
    echo "  - Wine 9.0+ installed system-wide (will be copied into AppImage)"
    echo "  - wget"
    exit 1
fi

NMSE_PUBLISH_DIR="$1"

if [[ ! -f "$NMSE_PUBLISH_DIR/NMSE.exe" ]]; then
    echo "ERROR: NMSE.exe not found in $NMSE_PUBLISH_DIR"
    echo "Run 'dotnet publish NMSE.csproj -c Release -r win-x64 --self-contained' first."
    exit 1
fi

# ── Verify Wine is available ──────────────────────────────────
# Accept 'wine' or 'wine64' (Ubuntu 24.04+ only ships wine64/wine32)
WINE_CMD=""
if command -v wine >/dev/null 2>&1; then
    WINE_CMD="wine"
elif command -v wine64 >/dev/null 2>&1; then
    WINE_CMD="wine64"
else
    echo "ERROR: Wine not found. Install Wine 9.0+ to build the AppImage."
    echo "  Ubuntu/Debian:  sudo apt install wine64 wine32"
    echo "  Fedora:         sudo dnf install wine"
    echo "  Arch:           sudo pacman -S wine"
    exit 1
fi

WINE_VERSION="$("$WINE_CMD" --version 2>/dev/null | sed 's/wine-//')"
echo "[BUILD] Using Wine $WINE_VERSION (via $WINE_CMD)"

# ── Create AppDir structure ───────────────────────────────────
BUILD_DIR="$(mktemp -d)"
APPDIR="$BUILD_DIR/NMSE.AppDir"
echo "[BUILD] Working directory: $BUILD_DIR"

mkdir -p "$APPDIR"/{app,wine,usr/share/icons/hicolor/256x256/apps}

# ── Copy NMSE application ────────────────────────────────────
echo "[BUILD] Copying NMSE application ..."
cp -r "$NMSE_PUBLISH_DIR"/. "$APPDIR/app/"

# ── Copy Wine installation ────────────────────────────────────
echo "[BUILD] Copying Wine installation ..."
# Detect Wine prefix location (varies by distro)
WINE_PREFIX_DIR=""
for candidate in /usr /usr/lib/wine /opt/wine-stable /opt/wine; do
    if [[ -x "$candidate/bin/wine" ]] || [[ -x "$candidate/bin/wine64" ]]; then
        WINE_PREFIX_DIR="$candidate"
        break
    fi
done

if [[ -z "$WINE_PREFIX_DIR" ]]; then
    # Fall back to copying from PATH
    WINE_BIN_DIR="$(dirname "$(command -v "$WINE_CMD")")"
    WINE_PREFIX_DIR="$(dirname "$WINE_BIN_DIR")"
fi

echo "[BUILD] Wine installation root: $WINE_PREFIX_DIR"

# Copy Wine binaries
if [[ -d "$WINE_PREFIX_DIR/bin" ]]; then
    cp -r "$WINE_PREFIX_DIR/bin" "$APPDIR/wine/"
fi

# Ensure a 'wine' binary exists in the bundled bin/ for the AppRun launcher.
# Ubuntu 24.04+ ships wine64/wine32 but no 'wine' wrapper.
if [[ -d "$APPDIR/wine/bin" ]] && [[ ! -e "$APPDIR/wine/bin/wine" ]]; then
    if [[ -x "$APPDIR/wine/bin/wine64" ]]; then
        ln -s wine64 "$APPDIR/wine/bin/wine"
        echo "[BUILD] Created wine -> wine64 symlink in AppDir"
    fi
fi

# Copy Wine libraries (needed for WoW64 and PE DLLs)
for libdir in lib lib64 lib/wine lib64/wine lib/x86_64-linux-gnu/wine lib/i386-linux-gnu/wine share/wine; do
    if [[ -d "$WINE_PREFIX_DIR/$libdir" ]]; then
        mkdir -p "$APPDIR/wine/$libdir"
        cp -r "$WINE_PREFIX_DIR/$libdir"/. "$APPDIR/wine/$libdir/" 2>/dev/null || true
    fi
done

# ── Desktop integration files ─────────────────────────────────
echo "[BUILD] Adding desktop integration files ..."

# Desktop entry
cp "$SCRIPT_DIR/nmse.desktop" "$APPDIR/nmse.desktop"

# AppRun entry point
cp "$SCRIPT_DIR/AppRun" "$APPDIR/AppRun"
chmod +x "$APPDIR/AppRun"

# Icon - use NMSE icon if available, otherwise create a placeholder
if [[ -f "$REPO_ROOT/Resources/app/NMSE.ico" ]]; then
    # Extract PNG from ICO if possible (ImageMagick), otherwise use a placeholder
    if command -v convert >/dev/null 2>&1; then
        convert "$REPO_ROOT/Resources/app/NMSE.ico[0]" "$APPDIR/nmse.png" 2>/dev/null || \
        cp "$REPO_ROOT/Resources/app/NMSE.ico" "$APPDIR/nmse.png" 2>/dev/null || true
    else
        # Just copy the ico - not ideal but AppImage tools can sometimes handle it
        cp "$REPO_ROOT/Resources/app/NMSE.ico" "$APPDIR/nmse.png" 2>/dev/null || true
    fi
else
    # Create a simple placeholder SVG icon
    cat > "$APPDIR/nmse.svg" <<'SVGEOF'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256">
  <rect width="256" height="256" rx="32" fill="#1a1a2e"/>
  <text x="128" y="150" font-family="sans-serif" font-size="72" font-weight="bold"
        fill="#e94560" text-anchor="middle">NMSE</text>
</svg>
SVGEOF
fi

# Copy icon to hicolor location for desktop integration
if [[ -f "$APPDIR/nmse.png" ]]; then
    cp "$APPDIR/nmse.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/nmse.png"
fi

# ── Download appimagetool ────────────────────────────────────
APPIMAGETOOL="$BUILD_DIR/appimagetool"
if [[ ! -x "$APPIMAGETOOL" ]]; then
    echo "[BUILD] Downloading appimagetool ..."
    wget -q -O "$APPIMAGETOOL" "$APPIMAGETOOL_URL"
    chmod +x "$APPIMAGETOOL"
fi

# ── Build AppImage ────────────────────────────────────────────
echo "[BUILD] Building AppImage ..."
OUTPUT_FILE="${2:-$SCRIPT_DIR/NMSE-x86_64.AppImage}"

# appimagetool requires FUSE; if running in CI without FUSE, extract and run
if "$APPIMAGETOOL" --appimage-extract-and-run "$APPDIR" "$OUTPUT_FILE" 2>/dev/null; then
    echo "[BUILD] ✓ AppImage created: $OUTPUT_FILE"
elif "$APPIMAGETOOL" "$APPDIR" "$OUTPUT_FILE" 2>/dev/null; then
    echo "[BUILD] ✓ AppImage created: $OUTPUT_FILE"
else
    echo "[BUILD] WARNING: appimagetool failed. Creating tar.gz bundle instead ..."
    OUTPUT_FILE="${OUTPUT_FILE%.AppImage}.tar.gz"
    tar -czf "$OUTPUT_FILE" -C "$BUILD_DIR" "NMSE.AppDir"
    echo "[BUILD] ✓ Bundle created: $OUTPUT_FILE"
fi

echo "[BUILD] Size: $(du -sh "$OUTPUT_FILE" | cut -f1)"

# ── Cleanup ───────────────────────────────────────────────────
rm -rf "$BUILD_DIR"
echo "[BUILD] Done."
