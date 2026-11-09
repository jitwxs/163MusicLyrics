#!/usr/bin/env bash
# Allow `sh app.sh ...` while keeping the implementation in Bash.
if [ -z "${BASH_VERSION:-}" ]; then
    exec bash "$0" "$@"
fi
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/MusicLyricApp"
PROJECT_FILE="$PROJECT_DIR/MusicLyricApp.csproj"
SOLUTION_FILE="$SCRIPT_DIR/MusicLyricApp.sln"
OUTPUT_ROOT="$SCRIPT_DIR/publish"
CONFIGURATION="${CONFIGURATION:-Release}"
APP_VERSION=""

find_dotnet() {
    if command -v dotnet >/dev/null 2>&1; then
        DOTNET_EXE="$(command -v dotnet)"
    elif [[ -x "$HOME/.dotnet/dotnet" ]]; then
        DOTNET_EXE="$HOME/.dotnet/dotnet"
        export DOTNET_ROOT="$HOME/.dotnet"
        export PATH="$DOTNET_ROOT:$PATH"
    else
        echo "找不到 .NET SDK。请安装 .NET 9 SDK，或将 dotnet 放入 PATH。" >&2
        exit 1
    fi

    local installed_sdks
    installed_sdks="$("$DOTNET_EXE" --list-sdks)"
    if [[ ! "$installed_sdks" =~ (^|$'\n')9\.[0-9] ]]; then
        echo "未检测到 .NET 9 SDK。当前项目目标框架为 net9.0。" >&2
        exit 1
    fi
}

load_project_version() {
    APP_VERSION="$("$DOTNET_EXE" msbuild "$PROJECT_FILE" -nologo -verbosity:quiet -getProperty:Version)"
    APP_VERSION="${APP_VERSION//$'\r'/}"
    if [[ ! "$APP_VERSION" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
        echo "项目 Version 配置为空或无效：$PROJECT_FILE" >&2
        exit 2
    fi
    echo "项目版本：$APP_VERSION"
}

detect_rid() {
    local os arch
    os="$(uname -s)"
    arch="$(uname -m)"
    case "$os:$arch" in
        Darwin:arm64|Darwin:aarch64) echo "osx-arm64" ;;
        Darwin:x86_64) echo "osx-x64" ;;
        Linux:x86_64|Linux:amd64) echo "linux-x64" ;;
        Linux:arm64|Linux:aarch64) echo "linux-arm64" ;;
        MINGW*:x86_64|MSYS*:x86_64|CYGWIN*:x86_64) echo "win-x64" ;;
        MINGW*:aarch64|MSYS*:aarch64|CYGWIN*:aarch64) echo "win-arm64" ;;
        *)
            echo "无法根据当前系统识别目标平台：$os / $arch。请通过参数指定 RID，例如 osx-arm64。" >&2
            exit 2
            ;;
    esac
}

ensure_restore() {
    if [[ ! -f "$PROJECT_DIR/obj/project.assets.json" || ! -f "$SCRIPT_DIR/MusicLyricApp.Tests/obj/project.assets.json" ]]; then
        "$DOTNET_EXE" restore "$SOLUTION_FILE"
    fi
}

build_app() {
    ensure_restore
    "$DOTNET_EXE" build "$SOLUTION_FILE" -c "$CONFIGURATION" --no-restore
}

publish_app() {
    local rid="$1"
    local output_dir="$OUTPUT_ROOT/$APP_VERSION/$rid"
    local archive

    mkdir -p "$OUTPUT_ROOT"
    ensure_restore
    "$DOTNET_EXE" publish "$PROJECT_FILE" \
        -c Release \
        -r "$rid" \
        --self-contained true \
        -p:CleanPublishDirectory=true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$output_dir"

    if [[ "$rid" == osx-* ]]; then
        archive="$OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$rid.tar.gz"
        bash "$SCRIPT_DIR/package-macos-app.sh" "$APP_VERSION" "$rid" "$output_dir" "$OUTPUT_ROOT"
    elif [[ "$rid" == win-* ]]; then
        archive="$OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$rid.zip"
        (cd "$output_dir" && zip -qr "$archive" .)
    else
        archive="$OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$rid.tar.gz"
        tar -czf "$archive" -C "$output_dir" .
    fi
    echo "打包完成：$archive"
    echo "发布目录：$output_dir"
}

distribute_all() {
    if [[ -n "$rid" || "$(uname -s)" != "Darwin" ]]; then
        echo "dist 不接受 RID 参数，且需要在 macOS 上运行，以完成 macOS .app 签名；它会生成 Windows、Linux、macOS 共 5 个分发包。" >&2
        exit 2
    fi
    echo "清理发布目录：$OUTPUT_ROOT"
    rm -rf -- "$OUTPUT_ROOT"
    mkdir -p "$OUTPUT_ROOT"

    local target
    for target in win-x86 win-x64 linux-x64 osx-x64 osx-arm64; do
        publish_app "$target"
    done
    echo "全部 5 个分发包已生成：$OUTPUT_ROOT"
    for target in win-x86 win-x64; do
        echo "  $OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$target.zip"
    done
    for target in linux-x64 osx-x64 osx-arm64; do
        echo "  $OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$target.tar.gz"
    done
}

run_app() {
    ensure_restore
    (cd "$PROJECT_DIR" && "$DOTNET_EXE" run --project "$PROJECT_FILE" -c Debug)
}

run_published_app() {
    local rid="$1"
    local host_rid
    host_rid="$(detect_rid)"
    if [[ "$rid" != "$host_rid" ]]; then
        echo "发布程序的 RID 为 $rid，当前机器为 $host_rid；不能在当前机器直接运行该发布包。" >&2
        exit 2
    fi
    local output_dir="$OUTPUT_ROOT/$APP_VERSION/$rid"
    local executable="$output_dir/MusicLyricApp"

    if [[ "$rid" == osx-* ]]; then
        output_dir="$OUTPUT_ROOT/MusicLyricApp-$APP_VERSION-$rid.app/Contents/MacOS"
        executable="$output_dir/MusicLyricApp"
    elif [[ "$rid" == win-* ]]; then
        executable="$output_dir/MusicLyricApp.exe"
    fi
    if [[ ! -x "$executable" ]]; then
        echo "找不到发布程序：$executable。请先执行 package 或 all。" >&2
        exit 1
    fi
    (cd "$output_dir" && "./$(basename "$executable")")
}

usage() {
    cat <<EOF
用法：$0 <命令> [RID]

命令：
  run [RID]       编译 Debug 并启动开发版（默认）
  build           编译 Release 版本
  package [RID]   发布并打包（macOS 生成 .app 的 tar.gz；Windows 生成 zip）
  dist            清空 publish 后重新生成 Windows x86/x64、Linux x64、macOS x64/arm64 共 5 个分发包
  run-package [RID] 启动已经发布的独立程序
  all [RID]       编译 Release、打包，然后启动发布版

RID 可选：osx-arm64、osx-x64、win-x86、win-x64、win-arm64、linux-x64、linux-arm64。
不传 RID 时使用当前机器对应的平台。版本自动读取 MusicLyricApp.csproj 的 Version，无需指定。
dist 需要在 macOS 上运行并安装 Xcode Command Line Tools（codesign），以完成 macOS .app 封装。

示例：
  $0 run
  $0 package
  $0 dist
  $0 all osx-arm64
EOF
}

command_name="${1:-run}"
rid="${2:-}"

case "$command_name" in
    -h|--help|help) usage; exit 0 ;;
esac

find_dotnet
case "$command_name" in
    package|run-package|all|dist) load_project_version ;;
esac
case "$command_name" in
    run)
        run_app
        ;;
    build)
        build_app
        ;;
    package)
        publish_app "${rid:-$(detect_rid)}"
        ;;
    dist)
        distribute_all
        ;;
    run-package)
        run_published_app "${rid:-$(detect_rid)}"
        ;;
    all)
        target_rid="${rid:-$(detect_rid)}"
        if [[ "$target_rid" != "$(detect_rid)" ]]; then
            echo "all 会在打包后启动程序，所以只能用于当前机器平台；跨平台发布请使用 package $target_rid。" >&2
            exit 2
        fi
        build_app
        publish_app "$target_rid"
        run_published_app "$target_rid"
        ;;
    *)
        usage >&2
        exit 2
        ;;
esac
