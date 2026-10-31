# cross-platform 功能回归报告

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
