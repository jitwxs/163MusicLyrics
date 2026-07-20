# Batch song-name search design

## Goal

Extend the existing download manager so users can paste one song name per line and resolve multiple songs without first collecting provider-specific IDs. The existing ID, link, album, playlist, folder scan, lyric rendering, and save flows remain unchanged.

## User flow

The download manager input becomes editable. A user pastes song titles, optionally using `title | artist`, and clicks **按歌名批量搜索**. The app searches NetEase Cloud Music and QQ Music for every title, keeps only normalized exact-title matches, and prefers the requested artist. If no artist is supplied, the optional preferred-artist field is used as a ranking hint rather than a hard filter.

For each query, the app retrieves the best exact match from both providers when available. It prefers a successful result containing original lyrics and an existing translation, then an artist match, then NetEase as a stable tie-breaker. A matched item is selected automatically and appears in the existing download manager table. Missing or non-exact results are displayed as unresolved rows and are never silently substituted with a fuzzy title.

The existing **保存选中** command performs the actual save using the user's current lyric format and output settings. The new feature does not change translation-provider credentials or invent missing translations.

## Components

- `BatchSongMatcher`: pure parsing, normalization, exact-title filtering, and deterministic ranking. It accepts plain lines, Markdown table rows, and `title | artist` input.
- `BatchSearchViewModel`: orchestrates provider searches, lyric retrieval, translation-aware provider selection, progress, cancellation-safe UI state, and result insertion.
- `BatchSearchItemViewModel`: exposes requested title and translation availability.
- `BatchSearchView`: adds editable multiline input, preferred artist, a batch-search button, and result columns.

## Error handling

Each query is isolated. A network error or missing title creates a visible unresolved row while later queries continue. Duplicate input lines are removed. Starting another run clears only the batch-name results created by the current manager session, while existing cached lyric data remains reusable.

## Verification

Unit tests cover input parsing, Unicode/punctuation normalization, exact-title enforcement, artist preference, and deterministic source ranking. The full solution must build for `net9.0`; tests must pass. A macOS arm64 app bundle is then produced and inspected through the running UI before the branch is pushed.
