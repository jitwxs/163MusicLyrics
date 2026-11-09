#!/usr/bin/env bash
set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "❌ This script must be run on macOS."
    exit 1
fi
if [[ $# -ne 1 || ! "$1" =~ ^[A-Za-z0-9._-]+$ ]]; then
    echo "❌ Usage: $0 <version>" >&2
    exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
version="$1"
app_name="MusicLyricApp"
output_root="$script_dir/publish"
any_processed=false
extract_dir=""
trap 'if [[ -n "$extract_dir" ]]; then rm -rf "$extract_dir"; fi' EXIT

for target in osx-x64 osx-arm64; do
    archive_mid_path="$output_root/$app_name-$version-$target-mid.tar.gz"
    if [[ ! -f "$archive_mid_path" ]]; then
        echo "⚠️ Archive not found for $target, skipping..."
        continue
    fi

    extract_dir="$(mktemp -d "$output_root/.macos-extract.XXXXXX")"
    tar -xzf "$archive_mid_path" -C "$extract_dir"
    bash "$script_dir/package-macos-app.sh" "$version" "$target" "$extract_dir" "$output_root"
    rm -rf "$extract_dir"
    extract_dir=""
    rm "$archive_mid_path"
    any_processed=true
done

if [[ "$any_processed" != true ]]; then
    echo "❌ No valid archives found. Nothing was processed." >&2
    exit 1
fi
echo "✅ Final macOS apps and archives are available in '$output_root/'."
