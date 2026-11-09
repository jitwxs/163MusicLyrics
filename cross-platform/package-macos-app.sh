#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 4 ]]; then
    echo "Usage: $0 <version> <osx-arm64|osx-x64> <publish-dir> <output-dir>" >&2
    exit 2
fi

version="$1"
rid="$2"
if [[ ! "$version" =~ ^[A-Za-z0-9._-]+$ ]]; then
    echo "Invalid version: $version" >&2
    exit 2
fi
case "$rid" in
    osx-arm64|osx-x64) ;;
    *) echo "Unsupported macOS RID: $rid" >&2; exit 2 ;;
esac
if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "macOS app packaging requires macOS and codesign." >&2
    exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source_dir="$(cd "$3" && pwd)"
mkdir -p "$4"
output_dir="$(cd "$4" && pwd)"
case "$output_dir/" in
    "$source_dir/"*) echo "Output directory must be outside the publish directory." >&2; exit 2 ;;
esac
app_name="MusicLyricApp"
bundle_name="$app_name-$version-$rid.app"
archive_name="$app_name-$version-$rid.tar.gz"

for required in "$app_name" NLog.config; do
    if [[ ! -f "$source_dir/$required" ]]; then
        echo "Missing publish file: $source_dir/$required" >&2
        exit 1
    fi
done
icon_source="$source_dir/Resources/app-logo.icns"
if [[ ! -f "$icon_source" ]]; then
    icon_source="$script_dir/MusicLyricApp/Resources/app-logo.icns"
fi
if [[ ! -f "$icon_source" ]]; then
    echo "Missing macOS icon: $icon_source" >&2
    exit 1
fi

# Stage everything before replacing final artifacts, including signing and archive validation.
staging_dir="$(mktemp -d "$output_dir/.macos-package.XXXXXX")"
trap 'rm -rf "$staging_dir"' EXIT
bundle="$staging_dir/$bundle_name"
macos_dir="$bundle/Contents/MacOS"
resources_dir="$bundle/Contents/Resources"
mkdir -p "$macos_dir" "$resources_dir"
cp -R "$source_dir/." "$macos_dir/"
chmod +x "$macos_dir/$app_name"

# Non-code files in MacOS receive extended-attribute signatures that tar can lose.
# Store assets in Resources and preserve app-relative lookup paths with symlinks.
for item in "$macos_dir"/* "$macos_dir"/.[!.]* "$macos_dir"/..?*; do
    [[ -e "$item" || -L "$item" ]] || continue
    name="$(basename "$item")"
    if [[ "$name" != "$app_name" && "$name" != *.dylib ]]; then
        mv "$item" "$resources_dir/$name"
        ln -s "../Resources/$name" "$item"
    fi
done
cp "$icon_source" "$resources_dir/app-logo.icns"

cat > "$bundle/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>$app_name</string>
  <key>CFBundleIdentifier</key>
  <string>com.github.jitwxs.$app_name</string>
  <key>CFBundleName</key>
  <string>$app_name</string>
  <key>CFBundleVersion</key>
  <string>$version</string>
  <key>CFBundleShortVersionString</key>
  <string>$version</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleIconFile</key>
  <string>app-logo.icns</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
EOF

codesign --force --deep --timestamp=none --sign - "$bundle"
codesign --verify --deep --strict "$bundle"
COPYFILE_DISABLE=1 tar -czf "$staging_dir/$archive_name" -C "$staging_dir" "$bundle_name"
mkdir "$staging_dir/verify"
tar -xzf "$staging_dir/$archive_name" -C "$staging_dir/verify"
codesign --verify --deep --strict "$staging_dir/verify/$bundle_name"

rm -rf "$output_dir/$bundle_name"
mv "$bundle" "$output_dir/$bundle_name"
mv "$staging_dir/$archive_name" "$output_dir/$archive_name"
echo "macOS app: $output_dir/$bundle_name"
echo "Archive: $output_dir/$archive_name"
