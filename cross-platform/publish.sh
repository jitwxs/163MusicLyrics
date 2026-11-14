#!/bin/bash
set -e

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

if [ -z "$1" ]; then
  echo "❌ Usage: $0 <version>"
  echo "👉 Example: ./publish.sh 8.0.0"
  exit 1
fi

version="${1#v}"
# The version is stamped into the app (window title, update check) as well as the archive names, so it must be a
# plain version number the project file can take.
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "❌ Version must look like 8.0.0, got: $1"
  exit 1
fi

# Only clean once the arguments are known to be good, so a typo does not delete the last build.
echo "🧹 Cleaning previous publish output..."
rm -rf publish/*

app_name="MusicLyricApp"
project_path="./MusicLyricApp/MusicLyricApp.csproj"
output_root="publish"

targets=(
  "win-x86"
  "win-x64"
  "linux-x64"
  "osx-x64"
  "osx-arm64"
)

# macOS 图标文件路径（icns）
macos_icon_source="./MusicLyricApp/Resources/app-logo.icns"

trap 'echo "❌ An error occurred. Exiting."' ERR

mkdir -p "$output_root"

# Function to create zip file (cross-platform)
create_zip() {
  local source_dir="$1"
  local output_file="$2"
  
  # Try using PowerShell (available on Windows and newer versions of macOS/Linux with PowerShell installed)
  if command -v pwsh >/dev/null 2>&1; then
    pwsh -Command "Compress-Archive -Path '$source_dir/*' -DestinationPath '$output_file' -Force"
    return 0
  elif command -v powershell >/dev/null 2>&1; then
    powershell -Command "Compress-Archive -Path '$source_dir/*' -DestinationPath '$output_file' -Force"
    return 0
  # Try using 7z if available
  elif command -v 7z >/dev/null 2>&1; then
    (cd "$source_dir" && 7z a -tzip "../$(basename "$output_file")" .)
    return 0
  # Try using python if available
  elif command -v python3 >/dev/null 2>&1; then
    python3 -c "
import zipfile
import os
with zipfile.ZipFile('$output_file', 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk('$source_dir'):
        for file in files:
            file_path = os.path.join(root, file)
            arc_path = os.path.relpath(file_path, '$source_dir')
            zf.write(file_path, arc_path)
"
    return 0
  elif command -v python >/dev/null 2>&1; then
    python -c "
import zipfile
import os
with zipfile.ZipFile('$output_file', 'w', zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk('$source_dir'):
        for file in files:
            file_path = os.path.join(root, file)
            arc_path = os.path.relpath(file_path, '$source_dir')
            zf.write(file_path, arc_path)
"
    return 0
  else
    echo "❌ No available tool to create zip file. Please install one of: PowerShell, 7-Zip, or Python"
    return 1
  fi
}

for target in "${targets[@]}"; do
  echo -e "\n-----------------------------"
  echo "📦 Publishing for $target..."

  output_dir="$output_root/$target"
  dotnet publish "$project_path" \
    -c Release \
    -r "$target" \
    --self-contained true \
    -p:DebugType=None \
    -p:PublishSingleFile=true \
    -p:ApplicationIcon=Resources\\app-logo.ico \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:Version="$version" \
    -o "$output_dir"

  if [[ "$target" == win-* ]]; then
    ext=".exe"
    original_file=$(find "$output_dir" -type f -name "*$ext" -print -quit)
    if [[ -n "$original_file" ]]; then
      new_filename="${app_name}-${version}-${target}${ext}"
      mv "$original_file" "$output_dir/$new_filename"
      echo "✅ Renamed Windows executable to: $new_filename"
    fi
  fi

  # macOS 目标单独处理图标复制
  if [[ "$target" == osx-* ]]; then
    if [ ! -f "$macos_icon_source" ]; then
      echo "❌ macOS icon file not found at '$macos_icon_source'. Please check."
      exit 1
    fi
    mkdir -p "$output_dir/Resources"
    cp "$macos_icon_source" "$output_dir/Resources/"
    echo "🎨 Copied macOS icon to $output_dir/Resources/"
  fi

  # Determine archive name and packaging method
  if [[ "$target" == win-* ]]; then
    archive_name="${app_name}-${version}-${target}.zip"
    if ! create_zip "$output_dir" "$output_root/$archive_name"; then
      echo "⚠️  Failed to create zip, falling back to tar.gz for Windows"
      archive_name="${app_name}-${version}-${target}.tar.gz"
      tar -czf "$output_root/$archive_name" -C "$output_dir" .
    fi
  elif [[ "$target" == osx-* ]]; then
    if [[ "$(uname -s)" == "Darwin" ]]; then
      archive_name="${app_name}-${version}-${target}.tar.gz"
      bash "$script_dir/package-macos-app.sh" "$version" "$target" "$output_dir" "$output_root"
    else
      archive_name="${app_name}-${version}-${target}-mid.tar.gz"
      tar -czf "$output_root/$archive_name" -C "$output_dir" .
    fi
  else
    archive_name="${app_name}-${version}-${target}.tar.gz"
    tar -czf "$output_root/$archive_name" -C "$output_dir" .
  fi

  echo "🗜️  Compressed to: $archive_name"

  echo "🧹 Removing intermediate directory: $output_dir"
  rm -rf "$output_dir"
done

echo -e "\n✅ All targets published and compressed."
echo "💡 If built outside macOS, copy the -mid tar.gz files into this project's cross-platform/publish on a Mac and run: bash build-macos-app.sh $version"
echo "💡 Only upload the final macOS tar.gz archives containing .app bundles, not the -mid archives."