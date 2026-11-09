---
name: run-musiclyricapp
description: Launches MusicLyricApp (cross-platform/MusicLyricApp) on macOS and drives its real window - build, start, click, type, use shortcuts, scroll, drag and take screenshots - to see a UI change working or to walk through features. Covers the safety rules that matter on the user's own machine (input only to our own process, the user's settings, cookies and cache). Use when asked to run, start, screenshot or walk through the app, or to verify a UI change in the real window rather than only in tests.
---

# Run and drive MusicLyricApp

Tests run headless and miss layout, fonts, overlays and window behaviour, so confirm a UI change in the real window
too. The scripts here start one copy of the app, send input only to that copy's PID, and capture its window.

## Before the first run

- macOS must let the terminal record the screen and control the computer (System Settings → Privacy & Security →
  Screen Recording and Accessibility). If a screenshot comes back black or input does nothing, ask the user to grant
  these. Do not look for ways around the prompt.
- .NET 10 lives in `~/.dotnet`; the scripts set `DOTNET_ROOT` and `PATH`.
- Compile the input tools once: `scripts/build-tools.sh`. They go to `$MLA_STATE/bin`; the default state folder is
  `$TMPDIR/musiclyricapp-run`. Point `MLA_STATE` at the session scratchpad to keep the screenshots there.

## Loop

```bash
S=.claude/skills/run-musiclyricapp/scripts
$S/launch.sh                 # builds, starts, waits for the window: "pid 7239 window 18298 1080x760"
$S/do.sh click 75 145        # every input goes through do.sh, which checks our app is frontmost first
$S/shot.sh 01-download       # prints the path of a 1080-wide copy; open it with the Read tool
$S/stop.sh                   # stops only our copy
```

- `do.sh click <x> <y>`, `do.sh key <keycode> [cmd|ctrl]`, `do.sh scroll <x> <y> <lines>` (negative lines scroll down),
  `do.sh drag <x> <y> <dx> <dy>`. Coordinates are window points from the top-left corner. Screenshots are 2x, so
  halve pixel positions, or measure on the `-s.png` copy, which is 1:1 with points at the default 1080×760 window.
- Set `MLA_WAIT=<seconds>` for the pause after an input (default 0.6). Network searches need about 8 s.
- To type text, put it on the clipboard (`printf '晴天' | pbcopy`), click the field, then send ⌘A and ⌘V
  (`do.sh key 0 cmd`, `do.sh key 9 cmd`). This avoids keyboard layouts and input methods.
- Take one screenshot per step and look at it before taking the next step. Do not chain many inputs on a guess.

## Where things are (default window, light or dark)

| Target | Point |
|---|---|
| Sidebar: 歌词搜索, 下载管理, 格式转换 | (75,112), (75,145), (75,178) |
| Sidebar: settings sections 歌词定制 … 应用设置 | x 75, y 258 / 289 / 320 / 351 / 382 |
| Sidebar collapse button | (208,68) |
| Search: 网易云 / QQ音乐 segment | (300,78) / (372,78) |
| Search: 单曲 / 专辑 / 歌单 segment | (455,78) / (510,78) / (564,78) |
| Search field | (550,134) |
| Download manager: filter field, first row checkbox, 删除所选 | (420,240), (290,344), (880,713) |
| Settings → 应用设置: theme select | (890,199) |
| Window close button (asks to confirm the exit) | (16,19) |

Positions move when the layout changes: measure from a fresh screenshot before relying on them.

| Key | Code | Key | Code |
|---|---|---|---|
| Enter (exact search) | 36 | Escape | 53 |
| ⌘Enter (fuzzy search) | 36 cmd | ⌘F (focus search) | 3 cmd |
| ⌘1 / ⌘2 / ⌘3 (pages) | 18 / 19 / 20 cmd | ⌘, (settings) | 43 cmd |
| ⌘/ (shortcuts dialog) | 44 cmd | ⌘S (save lyrics) | 1 cmd |
| ⌘A / ⌘V | 0 / 9 cmd | | |

Sample inputs: NetEase song `186016` (晴天), album `18903` (七里香, 11 songs), QQ Music fuzzy search `晴天`. NetEase
fuzzy search needs a valid login cookie. Without one it fails with "本请求需要登陆信息", which is not a UI bug.

## Safety: this is the user's machine and data

- **Input only to our process.** Never send a click or key without `do.sh` (or `safe.sh` returning 0). If it aborts,
  stop and find out why; do not retry blindly. Never `pkill` by name: the user may have their own copy open.
- **Settings file** `~/Library/Application Support/MusicLyricApp/MusicLyricAppSetting.json` holds the user's
  NetEase and QQ Music cookies. Never print it, `cat` it or show it in a screenshot of the 扩展 section with secrets
  revealed. Read single fields only:
  `python3 -c "import json;c=json.load(open('<path>',encoding='utf-8-sig'));print(c['Config']['ThemeMode'])"`.
- **Changes persist.** Settings are written when the user leaves the settings area or quits through the exit dialog
  (a kill does not save). If you change the theme, language, search source or any setting during a walk-through,
  set it back the same way and confirm the saved field afterwards.
- **Real data.** 删除所选 in the download manager deletes the user's local cache files, and saving writes `.lrc`
  files to their save folder (usually `~/Downloads`). Open the delete dialog only to look at it and cancel it with
  Escape. Tell the user which files a save created.
- **Real network.** Searches call the NetEase and QQ Music APIs. Keep the number of searches small.

## Logs

The app writes `cross-platform/MusicLyricApp/bin/Debug/net10.0/MusicLyricApp-<yyyy-MM-dd>.log` (search, cache and save
events, warnings). The process's own output is in `$MLA_STATE/app.log`. Check both when something does not happen.

## Reporting

Say what you did, what you saw in the screenshots, and every piece of the user's state you changed and restored.
Give screenshot paths in full.
