---
name: musiclyric-ui
description: Conventions for changing MusicLyricApp's cross-platform UI (cross-platform/MusicLyricApp, Avalonia 12 + TKCode.Arc 1.0) - where each page and helper lives, how localization, shortcuts, toasts, confirmations, settings rows and the download manager are wired, which Arc behaviours were verified the hard way, and how to test and verify a change. Use together with the tkcode-arc skill whenever you add or change a page, control, dialog, setting, shortcut or UI text in this repo, or review such a change.
---

# MusicLyricApp UI

The app is one `ArcWindow` with an Arc `Sidebar` and five pages. Use the `tkcode-arc` skill for Arc itself
(components, tokens, the API-reference-only rule). This skill adds what is specific to this repo, and
[arc-pitfalls.md](arc-pitfalls.md) records Arc and Avalonia behaviours the docs do not spell out. Read that file
before using a component listed in it.

Paths below are relative to `cross-platform/MusicLyricApp/`.

## Map

| Area | Files |
|---|---|
| Shell, sidebar, page switching, global shortcuts, exit confirmation | `Views/MainWindow.axaml(.cs)`, `ViewModels/MainWindowViewModel.cs` (page keys `PageSearch`, `PageBatch`, `PageConvert`, `PageLocal`, `settings/<section>`) |
| Online lyrics, 在线歌词 (query row, song strip, preview, fuzzy results, output bar) | `Views/SearchView.axaml(.cs)`, `Views/BlurSearchView.axaml(.cs)`, `ViewModels/SearchResultViewModel.cs`, `BlurSearchViewModel.cs` |
| Download manager (UI name 下载管理 / Download manager; code name `Batch*`) | `Views/BatchSearchView.axaml(.cs)`, `ViewModels/BatchSearchViewModel.cs`, `BatchSearchItemViewModel.cs` |
| Format conversion | `Views/FormatConvertView.axaml(.cs)`, `ViewModels/FormatConvertViewModel.cs` |
| Sign-in state of the cookies (settings rows, online lyrics status button, "needs login" toast action) | `ViewModels/LoginStatusViewModel.cs` (shared `MainWindowViewModel.Login`), `Core/Service/Login/LoginProbe.cs` |
| Local search, 本地搜索 (scan a folder, match files to songs, save lyrics beside them) | `Views/LocalMusicScanView.axaml(.cs)`, `ViewModels/LocalMusicScanViewModel.cs`, `Core/Service/LocalMusic/` |
| Settings (5 sections chosen from the sidebar) | `Views/SettingView.axaml(.cs)`, `ViewModels/SettingViewModel.cs`, `SettingParamViewModel.cs`, `Models/SettingBase.cs` |
| Shortcuts (single source of truth) | `Core/AppShortcuts.cs`, `Views/ShortcutsDialog.cs` |
| Dialogs and confirmations | `Core/DialogHelper.cs` |
| Helpers that work around Arc behaviour | `Views/TitleBarDrag.cs`, `Views/SelectLocalization.cs` |
| Theme | `Core/Service/ThemeService.cs`, registration in `Program.cs` (Violet accent) |
| Local search cache | `Core/Service/LocalSongCacheService.cs` |
| UI text | `Resources/Localization/zh-CN.json`, `Resources/Localization/en-US.json` |

## Rules

**Text.** Every visible string, accessible name and tooltip comes from both localization files, added to both in
the same change. XAML uses `{loc:Loc Key=...}` and code uses `LocalizationService.Get/Format`. Name new keys by area:
`Nav.*`, `Search.*`, `Blur.*`, `Batch.*`, `Convert.*`, `Settings.*`, `Shortcuts.*`, `Dialog.*`, `Status.*`, `Table.*`. Old keys
(`UI.ExtraNN`) stay as they are. Arc controls ship English defaults, so set them on every use: `Table` (`Caption`,
`EmptyMessage`, `ItemNameOne/Other`, `SelectedNoun`, `SelectAllLabel`, `ClearSelectionLabel`), `SearchField.ClearText`,
`Alert` and `Dialog` button texts. Use `Table.*` and `Search.Clear` where they fit. Chinese copy is plain and short:
浅色/深色, 删除所选, 保存歌词. Do not mix registers (亮色 next to 暗黑).

**Language changes.** Bound `{loc:Loc}` text updates by itself. Text built in code must be rebuilt: table columns
are rebuilt on `LocalizationService.Instance.PropertyChanged`, and labels computed in a view model raise
`OnPropertyChanged`. `Select` triggers are relabelled by `SelectLocalization.Attach` (see pitfalls).

**Shortcuts.** Add or change a shortcut only in `AppShortcuts` (id, scope, gesture, label key). Key bindings,
tooltips, placeholder hints and the shortcuts dialog all read from it. Show keys the way the platform does: ⌘ on
macOS and Ctrl elsewhere, and "Enter" (never "Return" or ↩).

**Feedback.** Status messages go through the view model's `TipRaised` event, and `MainWindow` shows them as a
`Toast`. Do not add status text blocks to pages. Questions use `DialogHelper.ConfirmOnAsync(owner, ...)`; pass
`isDanger: true` for destructive actions and say in the message what will be lost. A view model never opens dialogs
itself. It exposes a hook such as `BatchSearchViewModel.ConfirmDeleteAsync`, the view sets the hook, and tests leave
it unset.

**Frames.** `Table`, `CodeBlock`, `Textarea` and `Input` draw their own frame. Never put them inside a card
(`Border.pane`), because that gives two stacked panels. Only an `EmptyState` sits in a card. This applies to the lyric
preview, fuzzy results, the download manager table and the conversion output.

**Settings rows.** Each setting is an `arc:SettingsRow` inside an `arc:SettingsGroup`, with `Description` for a real
constraint. Inputs, selects and number fields are 280 wide so every section lines up. Path rows use
`Classes="path"`, which always stacks, with `Width="NaN"` on the input. A numeric setting follows the existing
pattern: a `...Text` property, a `...Error` property bound to `ErrorMessage`, range validation in the `On...Changed`
partial, and the value written to `SettingBean` only when it is valid. Add the field with a default to
`Models/SettingBase.cs`; old config files then get the default. Settings are saved when the user leaves the settings
area or quits, so killing the process does not save them.

**Download manager.** `Items` is the full list, newest first. New and re-searched songs move to the top in the
order they were asked for. The table binds to `VisibleItems` (filtered by `FilterText`), and rows the filter hides
are unticked so bulk actions only touch visible rows. Selection is mirrored between `Table.SelectedItems` and
`IsSelected` in `BatchSearchView.axaml.cs`. A row's own action buttons act on that row only and never change the
selection. On startup `LoadFromLocalCacheAsync` restores up to `SearchCacheStartupLoadLimit` cached songs. Deleting
selected songs also deletes their cache files.

**Cache files.** Names use the source enum (`NET_EASE_MUSIC_<id>.json`), never a localized name. Names written by
older versions (网易云/NetEase Music/QQ音乐/QQ Music) are still read and are migrated on the next save. Read or
delete a song's cache only through `LocalSongCacheService`.

## Workflow

```
UI change:
- [ ] 1. Read the tkcode-arc skill and each Arc component's API page; check arc-pitfalls.md for those components
- [ ] 2. Make the change following the rules above; text added to both localization files
- [ ] 3. Add or update a regression test (patterns below)
- [ ] 4. dotnet test cross-platform/MusicLyricApp.Tests: all green, no new warnings
- [ ] 5. See it in the real window with the run-musiclyricapp skill, in light and dark, zh-CN and en-US when text changed
- [ ] 6. Run the tkcode-arc checklist.md, then report what changed and what you saw
```

Tests use .NET 10 from `~/.dotnet`:
`export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH; dotnet test cross-platform/MusicLyricApp.Tests`.

## Test patterns

Tests are xunit v3 with `Avalonia.Headless.XUnit`, under `cross-platform/MusicLyricApp.Tests/Regression/`:

- `TestWorkspace` (in `TestSupport.cs`) gives a temp settings folder and cache, mocked window and folder, and helpers:
  `Song(id)`, `Input(id)`, `Storage()`, `Search(mock)`, and `Host(view, vm)` to put a view in a window.
- Page tests build the view, host it, call `Window.Show()` and `Dispatcher.UIThread.RunJobs()`, then query
  `GetVisualDescendants()`. A test that uses Avalonia types is `[AvaloniaFact]` or `[AvaloniaTheory]`.
- Assert what the user sees: `IsEffectivelyVisible`, not `IsVisible`, because a parent card may hide the control.
- Arc dialogs render on the `OverlayLayer`, so assert on its `TextBlock`s. Menu item commands bind only after the
  menu is opened.
- Tests that switch language restore it in `finally` (see `WithLanguage` in `DownloadManagerCacheRegressionTests`).
- Async commands: `await vm.SomeCommand.ExecuteAsync(...)`.
