# cross-platform 功能回归报告

## 2026-11-09：7.5.0 分支回归加固

范围：当前分支相对 v7.4 的 #339、#340、#338、版本与分发优化、#314，以及工作区中的保存目录按钮、取消处理和设置窗口归属修复。环境为 macOS arm64、.NET SDK 9.0.320 / Runtime 9.0.22。

| 检查 | 实际结果 |
| --- | --- |
| Debug 全量 .NET（含 Coverlet） | 296 项：291 通过，0 失败，5 项真实服务探测按设计跳过 |
| Release 全量 .NET | 296 项：291 通过，0 失败，5 项真实服务探测按设计跳过 |
| Python 发布脚本回归 | 40 项全部通过 |
| 四个 Shell 脚本 `bash -n` | 通过 |
| `git diff --check` | 通过 |

相对本轮开始前的工作区，.NET 参数化用例由 259 增至 296，Python 由 32 增至 40。此前两项保存状态断言预期固定英文 `Saved`，现已改为分别覆盖中英文并按实际本地化键精确断言；同时保留文件、进度、计数和跳过原因验证，不是删除失败断言。

### 关键防回归链路

- **QQ 搜索（#339）**：锁定桌面客户端参数，使用独立原始 JSON 贯通 native API、业务 API 和模糊搜索；覆盖单曲/专辑/歌单字段映射、三层错误码、空列表与缺失分类节点。唯一关键词防止进程缓存掩盖调用。
- **文件日志（#340）**：独立 LogFactory 验证启用/禁用启动、重启读取关闭设置、关闭时旧文件内容和修改时间不变、重复切换、真实文件监控触发重载，以及文件 INFO 过滤不影响其他目标。
- **分发（#338 / 版本优化）**：真实 `sh` 入口、MSBuild 版本及失败、五目标顺序和 ZIP/tar 内容、连续两次 dist 清理、无效调用不删产物、失败不继续发布、helper 失败保留旧有效产物、两种主机上的旧发布入口和损坏中间包。
- **复制链接（#314）**：真实 XAML 菜单绑定、剪贴板等待/失败/重试、命令忙碌状态、切歌后使用新 URL、签名参数和 Unicode 不被修改；无网络、无目录选择、无保存副作用；断言重复复制按钮已移除。
- **目录交互**：从实际 XAML 按钮取命令并执行 `ICommand.Execute`，而不只等待 `ExecuteAsync`；捕获 UI Dispatcher 上的未处理异常。检查两个选择器的延迟返回、空结果、取消/异常、配置不变，以及设置窗口 owner、复用、关闭后重建和 owner 关闭生命周期。成功持久化使用可注入临时配置的存储/主窗口链路，避免写用户配置。
- **版本一致性**：应用标题的主/次版本与实际程序集版本一致；分发测试确认只使用 MSBuild 返回版本，不接受环境变量覆盖。

### 覆盖率与边界

本轮 Debug Coverlet 原始结果：行覆盖 **53.78%（4753 / 8837）**，分支覆盖 **46.02%（1244 / 2703）**。范围包含生成代码和真实协议适配层，没有通过排除业务源码提高数字。

| 类 | 行覆盖 | 分支覆盖 |
| --- | ---: | ---: |
| LoggingService | 100% | 90% |
| SearchService | 94.57% | 95.65% |
| SettingWindow | 90% | 100% |
| SettingViewModel | 55.22% | 41.66% |
| MainWindowViewModel | 36.78% | 29.8% |

这些数字不代表所有功能或原生交互已完整验证。原生目录弹窗、系统剪贴板、真实签名、公证及 Windows/Linux 实机启动仍需人工/专用环境验收；Python 中的签名工具为模拟，线上服务用例本轮未启用。未宣称本轮重新进行了全平台真实打包。

### 本轮记录、未扩展修改的风险

- 旧 `publish.sh` 在检查版本参数之前清理 `publish/*`。在临时副本中确认：缺少版本参数时会失败但已删除旧产物；此问题不在已修复的 `app.sh dist` 参数检查链路中。
- `QQMusicApi.Search0` 对整个响应、`Req_1`、`Data` 或 `Body` 为 null 缺少防护；本轮覆盖的是有业务结构的失败与空结果，没有将畸形响应误标成正常空结果。
- 关闭的模糊搜索视图仍可能收到全局语言变化通知；语言测试在 Avalonia UI 上下文执行并恢复语言，但不等于已修复该订阅生命周期。

### 本地产物及复跑

- `TestResults/release75-hardening-debug.trx`
- `TestResults/release75-hardening-release.trx`
- `TestResults/51823060-2b2d-4820-b809-d5e1d42ca0ae/coverage.cobertura.xml`

以上均被 Git 忽略。完整命令与人工验收清单见 [README.md](README.md)。

---

以下为保留的历史回归记录，计数和结果不代表当前版本。

执行日期：2026-10-31。环境：macOS arm64，.NET SDK 9.0.320、.NET Runtime 9.0.22。

## 结果

| 检查 | 结果 |
| --- | --- |
| Debug 离线回归 | 203 通过，0 失败，3 个在线探测按设计跳过 |
| Release 离线回归 | 203 通过，0 失败 |
| 单独启用真实服务探测 | 3 通过，0 失败 |
| `git diff --check` | 通过 |

原有 56 项测试保留，新增 147 项离线用例及 3 项在线探测，共 206 项。参数化测试按实际执行的独立数据行计数。

真实探测验证网易云及 QQ 的匿名歌曲信息/歌词接口，以及 GitHub 最新版本接口。首次 QQ 探测使用原链接解析测试中的 `003ODsL83GwKj4`，返回 `code=0` 但歌曲数组为空，故该次探测失败。随后对照 `QQMusicNativeApi.GetSong` 源码注明的 `001RaE0n4RrGX9` / `204422870` 样本，确认歌曲信息存在，改用该样本重新执行，3 项均通过。该结果仅代表这些样本在本次执行时可访问，不代表所有曲目和登录态接口均可用。

## 发现并修复的问题

| 问题 | 修复 | 防回归用例 |
| --- | --- | --- |
| `song?id=` 空 ID 链接被当作合法输入 | 提取后验证 ID 非空；网易云 ID 必须为数字 | `InvalidInputIsRejected` |
| 同一原文切换目标语言后返回旧语言译文 | 翻译缓存键纳入提供商类型、原语言和目标语言 | `TranslationCacheReusesSameLanguageButSeparatesTargetLanguages` |
| 普通歌词缓存被逐字模式复用 | 缓存保存/读取时验证逐字标记；标准/A2 可共享原始逐字数据 | `ChangingLyricModeMustNotReuseIncompatibleDiskCache`、`StandardAndA2ModesShareRawWordTimingsCache` |
| 批量保存时跳过项显示为 Saved | 复用保存预检规则，把跳过项标为 Skipped 并记录原因 | `BatchSkippedSongsMustNotBeMarkedSaved` |
| 切换输出格式清空歌曲元数据及音频直链 | 格式刷新只更新歌词文本 | `SearchDownloadAndSaveFlowThroughMainCommands` |
| macOS/Linux 获取歌曲直链后误报不支持播放器 | 自动准备播放仅在平台支持时执行；保留直链和下载能力 | `SearchDownloadAndSaveFlowThroughMainCommands`、平台能力断言 |
| QQ 关键字搜索缺少服务端必需的公共参数 | 在请求中加入 `comm`（ct/cv/uin/format/platform），保留各搜索类型参数 | `QQMusicNativeApiTests.KeywordSearchSendsRequiredCommAndSearchParameters` |

Issue #333 修复验证：修复前本机请求返回 `top=0; req_1=2001`，修复后同一关键词请求返回 `top=0; req_1=0`。单测捕获并检查最终序列化请求体，不依赖外部网络。

兼容性说明：历史磁盘缓存没有逐字模式标记，歌词部分会按未命中重新获取；不删除用户缓存文件。标准模式和 A2 模式仍可共享同一份原始逐字缓存。

测试隔离新增了可选依赖注入入口：音乐 API、配置文件路径、下载客户端和更新接口。默认桌面启动仍使用原实现。主窗口测试通过独立配置和无物理播放器的视图模型执行，不读取用户 Cookie，不启动真实音频设备。

## 覆盖率

Debug Coverlet/Cobertura 原始报告：整体行覆盖 **48.20%（4103 / 8512）**，分支覆盖 **42.33%（1077 / 2544）**。总体包含生成代码和真实网络适配层；没有为提高数字排除未覆盖业务文件。

以下是原始源码类的行覆盖率（不取同名正则生成类）：

| 模块 | 行覆盖率 |
| --- | ---: |
| SearchService | 94.57% |
| SrtUtils | 91.89% |
| FormatConvertViewModel | 91.56% |
| LyricUtils | 83.40% |
| BatchSearchViewModel | 83.23% |
| LocalSongCacheService | 80.85% |
| StorageService | 78.61% |
| MainWindowViewModel | 30.83% |

没有达到全代码或全功能组合覆盖。主窗口仍有较多外部程序、播放、弹窗等分支未覆盖，真实音乐/翻译协议层也不能仅凭模拟查询用例视为已验证。

## 产物

- `TestResults/regression.trx`：Debug 离线与默认跳过结果。
- `TestResults/regression-release.trx`：Release 离线结果。
- `TestResults/live.trx`：最终 3 项真实服务探测结果。
- `TestResults/4ed4d1aa-4a85-4521-b8db-23b99776fb98/coverage.cobertura.xml`：本次 Debug 覆盖率。

以上文件留在本地、已被 Git 忽略；再次执行测试可重新生成。源码和本报告可纳入版本控制。构建仍有已有的可空性、废弃 API 等警告，本次没有把这些警告当作全部已修复。

## 发布前仍需验证

Windows 实际播放/进度控制、真实系统剪贴板与文件选择器、退出确认交互、登录态受限内容、百度/彩云真实翻译、外部链接/快捷入口、不同系统/DPI 的显示，以及发布包安装和启动尚未完成实机验收。完整步骤见 [测试说明中的人工验收表](README.md#仍须人工专用环境验收)。

因此，本次结论是“已建立并通过主要离线功能回归，匿名在线样本探测通过”，不是“所有软件功能已在所有平台完整验证”。
