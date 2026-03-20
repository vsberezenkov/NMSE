# ──────────────────────────────────────────────────────────────
# Homebrew Cask formula: NMSE via Whisky
#
# Installs NMSE (No Man's Save Editor) as a macOS application
# that runs via Whisky (free, open-source Wine wrapper).
#
# Usage:
#   brew install --cask ./nmse-whisky.rb
#
# Prerequisites:
#   brew install --cask whisky
#
# This formula:
#   1. Downloads the latest NMSE Windows build from GitHub Releases
#   2. Extracts to ~/Applications/NMSE/
#   3. Creates a launch script that opens NMSE in Whisky
#
# Note: This is a local formula. To make it available via
# 'brew install --cask nmse', it would need to be submitted
# to the homebrew-cask tap.
#
# Note: The 'latest' tag is a rolling release created by CI.
# The zip asset name varies per build. If the download URL
# below 404s, visit the releases page manually:
#   https://github.com/vectorcmdr/NMSE/releases/tag/latest
# ──────────────────────────────────────────────────────────────

cask "nmse-whisky" do
  version "latest"

  # The CI workflow creates a rolling 'latest' tag with a zip asset.
  # The asset filename varies per build (NMSE-<version>-Release.zip).
  # This URL uses GitHub's redirect for the first asset on the tag.
  # If this fails, download manually from the releases page.
  url "https://github.com/vectorcmdr/NMSE/releases/download/latest/",
      verified: "github.com/vectorcmdr/NMSE/"
  name "NMSE - No Man's Save Editor"
  desc "Open-source save editor for No Man's Sky (runs via Whisky/Wine)"
  homepage "https://github.com/vectorcmdr/NMSE"

  # Whisky must be installed to run the Windows executable
  depends_on cask: "whisky"

  # The target directory where NMSE will be installed
  nmse_dir = "#{Dir.home}/Applications/NMSE"

  # Extract the NMSE Windows build
  artifact ".", target: nmse_dir

  # Create a helper script that launches NMSE through Whisky's wine
  postflight do
    target = "#{Dir.home}/Applications/NMSE"
    launch_script = "#{target}/nmse-launch.sh"
    File.write(launch_script, <<~BASH)
      #!/bin/bash
      # NMSE launch helper for Whisky on macOS
      #
      # This script locates Whisky's bundled Wine and launches NMSE.
      # If Whisky's Wine is not found, it falls back to system Wine.

      NMSE_DIR="#{target}"
      NMSE_EXE="$NMSE_DIR/NMSE.exe"

      if [[ ! -f "$NMSE_EXE" ]]; then
          echo "ERROR: NMSE.exe not found at $NMSE_EXE" >&2
          exit 1
      fi

      # Whisky stores its Wine in the app bundle
      WHISKY_WINE="/Applications/Whisky.app/Contents/Resources/Libraries/Wine/bin/wine64"

      # Fallback: Homebrew Wine
      HOMEBREW_WINE="/usr/local/bin/wine64"
      HOMEBREW_WINE_ARM="/opt/homebrew/bin/wine64"

      # Find available Wine
      if [[ -x "$WHISKY_WINE" ]]; then
          WINE="$WHISKY_WINE"
      elif [[ -x "$HOMEBREW_WINE" ]]; then
          WINE="$HOMEBREW_WINE"
      elif [[ -x "$HOMEBREW_WINE_ARM" ]]; then
          WINE="$HOMEBREW_WINE_ARM"
      elif command -v wine64 >/dev/null 2>&1; then
          WINE="$(command -v wine64)"
      elif command -v wine >/dev/null 2>&1; then
          WINE="$(command -v wine)"
      else
          echo "ERROR: Wine not found. Install Whisky: brew install --cask whisky" >&2
          exit 1
      fi

      # Use a dedicated prefix for NMSE
      export WINEPREFIX="$HOME/Library/Application Support/NMSE/wineprefix"
      export WINEARCH=win64
      export WINEDLLOVERRIDES="mscoree=d;mshtml=d"
      export WINEDEBUG="-all"

      # Create prefix if needed
      if [[ ! -d "$WINEPREFIX/drive_c" ]]; then
          echo "Creating Wine prefix (first run) ..."
          "$WINE" wineboot --init 2>/dev/null || true
      fi

      exec "$WINE" "$NMSE_EXE" "$@"
    BASH
    FileUtils.chmod(0o755, launch_script)
  end

  uninstall delete: "#{Dir.home}/Applications/NMSE"

  zap trash: [
    "#{Dir.home}/Library/Application Support/NMSE",
  ]

  caveats <<~EOS
    NMSE has been installed to ~/Applications/NMSE/

    To launch:
      ~/Applications/NMSE/nmse-launch.sh

    Or open Whisky, create a bottle, and add NMSE.exe as a program.

    NMS save files on macOS are typically at:
      ~/Library/Application Support/HelloGames/NMS/

    For detailed setup instructions, see:
      https://github.com/vectorcmdr/NMSE/blob/main/docs/whisky-macos-guide.md
  EOS
end
