# 功能回归测试

测试范围是 `cross-platform` 桌面版，旧版 `archive-winform` 不在本测试套件内。

## 运行

在仓库根目录执行（需要 .NET 9 SDK）：

```sh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet test cross-platform/MusicLyricApp.sln --filter 'Category!=Live'
```

生成测试报告及覆盖率：

```sh
dotnet test cross-platform/MusicLyricApp.sln \
  --logger 'trx;LogFileName=regression.trx' \
  --collect:'XPlat Code Coverage'
```

默认运行 203 项离线测试，显式跳过 3 项外部服务探测。TRX 和 Cobertura XML 输出到本目录的 `TestResults/`（已被 Git 忽略）。覆盖率反映代码执行情况，不代表所有平台、接口或业务组合均已验证。

发布配置回归：

```sh
dotnet test cross-platform/MusicLyricApp.sln -c Release --filter 'Category!=Live'
```

匿名在线探测（需要网络，可能受曲目下架、限流和服务变更影响）：

```sh
MUSICLYRIC_LIVE_TESTS=1 dotnet test cross-platform/MusicLyricApp.sln \
  --filter 'Category=Live' \
  --logger 'trx;LogFileName=live.trx' \
  --blame-hang-timeout 60s
```

在线探测读取网易云 `347230`、QQ `001RaE0n4RrGX9` 的歌曲信息与歌词，并请求 GitHub 最新版本信息。它们不读取用户 Cookie、翻译密钥或应用设置，也不下载音频。在线失败会保留失败状态，不会自动转换为跳过或成功。

## 测试结构

| 测试文件 | 主要验证行为 |
| --- | --- |
| `Regression/SearchRegressionTests.cs` | ID/链接解析、提供商识别、专辑/歌单展开、目录导入、查询错误、磁盘缓存复用、普通/逐字请求、聚合模糊搜索、结果元数据 |
| `Regression/QQMusicNativeApiTests.cs` | QQ 单曲/专辑/歌单关键词搜索请求载荷，验证服务端要求的公共 `comm` 参数和搜索类型映射 |
| `Regression/LyricsRegressionTests.cs` | 双语交错/合并/独立、分文件、译文缺失策略、时间误差匹配、简繁转换、音译、空行、纯音乐、LRC/SRT 转换 |
| `Regression/StorageRegressionTests.cs` | 实际临时文件写入、4 种编码、覆盖/编号、文件名模板、纯音乐跳过、批量统计、目录取消与记忆、剪贴板契约、封面/音频下载及失败 |
| `Regression/CacheAndSettingsRegressionTests.cs` | 设置持久化与旧配置兼容、缓存损坏/缺失/容量淘汰/来源隔离、代理配置、翻译凭据选择和语言支持、翻译缓存、更新比较与错误 |
| `Regression/ViewModelRegressionTests.cs` | 选择状态、重试去重、删除、批量保存、搜索结果选择消息、设置顺序、主题、格式转换防抖与交换、子窗口 XAML 加载与绑定 |
| `Regression/MainFlowRegressionTests.cs` | 主窗口加载/关闭、查询与保存命令、格式切换保留元数据、输入错误、平台能力、设置边界值、跳过项状态、缓存模式隔离 |
| `Regression/AdditionalFeatureRegressionTests.cs` | 网易云/QQ 逐字处理、A2 输出、拼音、歌词状态灯、保存时附带资源下载及容错、逐字缓存共享、更新说明 |
| `Regression/LiveServiceTests.cs` | 可选真实服务探测 |
| 原有 `Core/Utils/`、`Models/` 测试 | 时间戳、分享链接、A2 转换等既有 56 项测试 |

## 隔离和测试边界

- 每项文件/缓存/设置测试使用独立临时目录，结束后清理；不覆盖用户实际配置和歌词。
- 音乐查询通过 `IMusicApi` 严格模拟；下载通过可替换的 `HttpMessageHandler` 返回字节或错误。默认离线用例不访问真实服务。
- `IStorageFolder`、`IStorageFile` 和 `IWindowProvider` 的模拟把内容写入真实临时文件，但不调用系统目录选择器和剪贴板。
- Avalonia Headless XUnit 加载真实应用资源与窗口，验证命令、绑定、计时器等；不验证屏幕像素、真实键鼠输入和系统音频。
- 主流程测试禁用真实音频初始化；Windows 播放器仍须在有音频设备的 Windows 环境验收。
- `NetworkClientFactory` 和 `GlobalCache` 存在进程级共享状态，因此关闭测试并行，避免相互干扰。
- 新增可选构造参数用于替换配置路径、音乐接口、下载客户端和版本接口；正常启动沿用原默认实现。

## 仍须人工/专用环境验收

以下项目没有被标为“已通过”，应在发布前按目标环境逐项验收：

| 项目 | 步骤与预期 |
| --- | --- |
| Windows 音频 | 查询可试听歌曲，播放/暂停/继续/拖动/停止；切歌和关闭窗口后停止声音并释放资源；无直链时提示正确 |
| macOS/Linux 实机界面 | 调整窗口尺寸、切换主题、检查中文/字体/封面/表格显示；键鼠操作和不同 DPI 下无布局问题 |
| 系统剪贴板 | 启用自动读取，复制合法歌曲链接并进入输入框；关闭该设置后不自动读取；复制封面/直链实际可粘贴 |
| 系统目录选择器 | 选择、取消、记住目录、目录被删除、不可写目录；错误提示正确且不误报保存成功 |
| 登录态与受限内容 | 使用测试账户分别验证两家平台受限歌单/歌曲、有效及过期 Cookie；不把凭据提交到仓库 |
| 真实模糊查询和批量资源 | 两家平台分别查询单曲/专辑/歌单并选择多条结果；保存与下载实际内容正确；注意服务限流 |
| 百度/彩云在线翻译 | 使用测试凭据分别验证成功、鉴权失败、限流与多语言结果；当前只验证离线选择、支持范围和缓存行为 |
| 退出确认 | 确认开启时取消退出保留窗口、确认退出保存设置；关闭确认时直接退出 |
| 外部链接和快捷入口 | 主页、Wiki、Issue、打开保存目录、打开配置目录使用系统默认程序 |
| 发布产物 | 在目标系统运行 self-contained 发布产物与 macOS `.app`，检查签名、资源、设置和日志路径 |

本次实际执行结果、修复与限制见 [REGRESSION_REPORT.md](REGRESSION_REPORT.md)。
