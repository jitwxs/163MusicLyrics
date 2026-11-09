# 跨平台桌面版开发

本目录是 .NET 10 + Avalonia 12.1 + TKCode.Arc 桌面应用，采用 CommunityToolkit.Mvvm。
仓库中的 `archive-winform/` 是旧版 Windows Forms 实现，`prototype/` 是界面原型，均不参与本解决方案编译。

## 代码入口

- `MusicLyricApp/Program.cs` → `App.axaml.cs` → `Views/MainWindow.axaml.cs`：应用启动和主窗口。`Program.cs` 中的 `UseArc(...)` 注册 TKCode.Arc 控件、主题与字体。
- `Views/`、`ViewModels/`：单窗口界面。`MainWindow` 是带侧边栏的 `ArcWindow`，按 `MainWindowViewModel.CurrentPageKey` 切换四个页面：
  `SearchView`（歌词搜索，模糊搜索结果 `BlurSearchView` 在页内显示）、`BatchSearchView`（批量任务）、`FormatConvertView`（格式转换）、`SettingView`（设置）。
  对话框与提示统一使用 Arc 的 `Dialog`（`Core/DialogHelper.cs`）和 Toast；颜色、字号、圆角取自 Arc 设计令牌，不写固定色值。
- `Core/Service/`：搜索、文件保存、本地缓存、更新检查；`Music/` 对接网易云和 QQ 音乐，`Translate/` 对接翻译服务。
- `Core/Utils/`、`Models/`：歌词处理、时间戳转换、网络工具与数据模型。
- `MusicLyricApp.Tests/`：xUnit 测试。

应用直接访问音乐服务接口，无需单独启动后端或数据库。在线查询需要网络，部分接口需要在设置中配置 Cookie；翻译功能需要相应服务凭据。

## 本地编译与测试

需要 .NET 10 SDK。macOS Apple Silicon 使用 arm64 SDK。
若 SDK 安装在用户目录 `~/.dotnet`，先在终端执行：

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

从仓库根目录执行：

```sh
cd cross-platform
dotnet restore MusicLyricApp.sln
dotnet build MusicLyricApp.sln -c Debug --no-restore
dotnet test MusicLyricApp.sln -c Debug --no-build
```

也可以直接使用一键脚本（在 `cross-platform/` 目录）：

```sh
./app.sh run                    # 编译并启动 Debug 版
./app.sh build                  # 编译 Release 版
./app.sh package                # 打当前平台独立包
./app.sh dist                   # 在 Mac 上一键打包 Windows、Linux、macOS 共 5 个分发包，不启动程序
./app.sh all                    # 编译、打包并启动当前平台发布版
```

版本统一通过 MSBuild 读取 `MusicLyricApp/MusicLyricApp.csproj` 的 `<Version>`，不需要传版本参数，也不使用 `APP_VERSION` 环境变量。当前配置为 `7.5.0`（界面显示 v7.5）。可以使用 `bash app.sh ...` 或 `sh app.sh ...`，后者会在需要时自动切换到 Bash。

原始发布文件放在 `publish/<版本>/<RID>/`，并在 `publish/` 下生成归档：Windows 使用 `.zip`，Linux 使用 `.tar.gz`。macOS 额外生成 `publish/MusicLyricApp-<版本>-<RID>.app`，最终 `.tar.gz` 中包含完整 `.app`，可解压后放入“应用程序”目录；`./app.sh run-package` 从该应用包启动。

默认 RID 自动按当前操作系统和 CPU 架构选择；也可手动指定，例如 `./app.sh package win-x64`。跨平台发布目标为 `osx-arm64`、`osx-x64`、`win-x64`、`win-x86`、`win-arm64`、`linux-x64` 和 `linux-arm64`。macOS 应用包封装必须在 Mac 上完成，并安装 Xcode Command Line Tools（提供 `codesign`）。

## 启动

从 `cross-platform` 目录执行：

```sh
cd MusicLyricApp
dotnet run -c Debug --no-build
```

启动前切换到项目目录，确保入口使用的相对路径 `NLog.config` 能被找到。
第一次启动前需完成上面的 Debug 编译；修改源码后去掉 `--no-build` 即可自动编译。
日志位于 `MusicLyricApp/bin/Debug/net9.0/MusicLyricApp-日期.log`。
设置保存在系统用户应用数据目录的 `MusicLyricApp/MusicLyricAppSetting.json`，默认查询缓存位于同目录下的 `cache/`。

macOS/Linux 的内置音频试听当前未启用：`MainWindowViewModel.IsPlaybackSupported` 仅对 Windows 返回 true。

## 发布

仅发布当前 Apple Silicon Mac 所需文件，可在 `cross-platform` 目录执行：

```sh
dotnet publish MusicLyricApp/MusicLyricApp.csproj -c Release -r osx-arm64 --self-contained true -o publish/osx-arm64
cd publish/osx-arm64
./MusicLyricApp
```

以上 `dotnet publish` 命令仅生成原始发布文件，不会创建 `.app`。正式分发全部平台（与 v7.4 Release 的制品矩阵一致），只需在 Mac 的 `cross-platform/` 执行：

```sh
sh app.sh dist
```

该命令通过版本、参数和运行环境检查后，会先删除并重建整个 `publish/` 目录（包含所有旧版本产物），再自动使用项目版本依次发布 Windows x86/x64、Linux x64、macOS x64/arm64 共五个包，不会启动应用；macOS 包额外完成 `.app` 封装、签名及解压后验签。完整分发命令必须在 Mac 上运行，以完成 macOS 签名。当前项目版本对应以下可上传文件：

```text
publish/MusicLyricApp-7.5.0-win-x86.zip
publish/MusicLyricApp-7.5.0-win-x64.zip
publish/MusicLyricApp-7.5.0-linux-x64.tar.gz
publish/MusicLyricApp-7.5.0-osx-x64.tar.gz
publish/MusicLyricApp-7.5.0-osx-arm64.tar.gz
```

只打当前平台包使用 `sh app.sh package`；只打指定平台包使用 `sh app.sh package osx-arm64` 或 `sh app.sh package osx-x64`；验证当前平台发布版使用 `sh app.sh run-package`。

`publish.sh <version>` 用于发布全部目标平台，并清理已有 `publish/` 输出。在 Mac 上会直接生成完整 `.app` 及最终压缩包；在其他系统上，macOS 目标只生成 `-mid.tar.gz` 中间包。将这些中间包放入 Mac 上本项目的 `cross-platform/publish/`，然后执行 `bash build-macos-app.sh <version>` 完成封装。

两个入口共用 `package-macos-app.sh`，统一生成应用元数据、复制图标、设置执行权限及进行 ad-hoc 签名。可执行文件和动态库放在 `Contents/MacOS`，其他资源放在 `Contents/Resources`，并通过相对符号链接保留原有查找路径，避免非代码文件的签名依赖扩展属性。最终归档会再次解压并验签，全部成功后才输出。正式 Release 只能上传包含 `.app` 的最终压缩包，不能将原始发布目录或 `-mid.tar.gz` 当作最终 macOS 资产。ad-hoc 签名不是 Apple Developer ID 签名或公证，下载后仍可能遇到 Gatekeeper 提示。

## macOS 打包回归

从本目录执行（仅依赖 Python 3 和 Bash，无需 .NET 发布或联网）：

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests -p 'test_macos_packaging.py' -v
```

测试使用模拟的 `uname` 和 `codesign`，检查打包结构、文件权限、元数据和失败处理；实际发布仍需在 Mac 上验证真实签名及应用启动。

## 功能回归

测试命令、覆盖范围与人工验收清单见 [MusicLyricApp.Tests/README.md](MusicLyricApp.Tests/README.md)。
本次执行结果见 [回归报告](MusicLyricApp.Tests/REGRESSION_REPORT.md)。
