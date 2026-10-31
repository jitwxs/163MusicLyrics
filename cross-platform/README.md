# 跨平台桌面版开发

本目录是 .NET 9 + Avalonia 11.3 桌面应用，采用 CommunityToolkit.Mvvm。
仓库中的 `archive-winform/` 是旧版 Windows Forms 实现，`prototype/` 是界面原型，均不参与本解决方案编译。

## 代码入口

- `MusicLyricApp/Program.cs` → `App.axaml.cs` → `Views/MainWindow.axaml.cs`：应用启动和主窗口。
- `Views/`、`ViewModels/`：界面、查询交互、批量任务、设置与格式转换。
- `Core/Service/`：搜索、文件保存、本地缓存、更新检查；`Music/` 对接网易云和 QQ 音乐，`Translate/` 对接翻译服务。
- `Core/Utils/`、`Models/`：歌词处理、时间戳转换、网络工具与数据模型。
- `MusicLyricApp.Tests/`：xUnit 测试。

应用直接访问音乐服务接口，无需单独启动后端或数据库。在线查询需要网络，部分接口需要在设置中配置 Cookie；翻译功能需要相应服务凭据。

## 本地编译与测试

需要 .NET 9 SDK。macOS Apple Silicon 使用 arm64 SDK。
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

现有 `publish.sh <version>` 用于发布全部目标平台，并清理已有 `publish/` 输出。
`build-macos-app.sh <version>` 将其生成的 macOS 中间压缩包包装为 `.app`、执行临时签名并压缩；它不是日常开发启动脚本。

## 功能回归

测试命令、覆盖范围与人工验收清单见 [MusicLyricApp.Tests/README.md](MusicLyricApp.Tests/README.md)。
本次执行结果见 [回归报告](MusicLyricApp.Tests/REGRESSION_REPORT.md)。
