# Arc 1.0 and Avalonia 12: behaviours verified in this repo

Each entry was found while building this app's UI and is handled in the code named. The API reference is still the
rule: if a page and this list disagree, check the installed assembly with `tools/arcdump` (below). Then fix whichever
one is wrong.

## Tokens

- **Space tokens are `double`, not `Thickness`.** `{arc:ArcDesignTokenResource Space4}` works for `Spacing`,
  `RowSpacing`, `ColumnSpacing` and `ItemSpacing`, but not for `Margin` or `Padding`. Write those as literal
  thicknesses on the 4 px scale (`Padding="16,12"`).
- **Radius tokens are `CornerRadius`** (`RadiusControl`, `RadiusPanel`, `RadiusSurface`). Bind them to
  `CornerRadius`, never to a double.

## Controls

- **`Badge.Icon` needs a control.** Give it `new ArcGlyph { Data = ArcIcons.X }`. A bare `Geometry` renders its type
  name ("Avalonia.Media.StreamGeometry") as text. See the song strip badges in `SearchView`.
- **`Select` caches each item's text.** After a language change the trigger keeps the old words. Assigning the same
  `TextSelector` again is not a change, because the delegates are equal. Clear it and set it again
  (`select.TextSelector = null; select.TextSelector = Label;`). `Views/SelectLocalization.cs` does this for every
  `Select` in the window. It walks the logical tree as well as the visual one: a section that has never been shown
  has no template yet, so its selects are missing from the visual tree, yet they cached their text when their
  `SelectedItem` was bound. Walking only the visual tree left them in the old language (start in English, switch to
  Chinese on 应用设置, then open 歌词定制). Headless tests see the stale text through `Select.SelectedText`.
- **`Table` does not filter.** Filter in the view model and pass the filtered collection as `ItemsSource`; the
  download manager uses `VisibleItems`. Selection follows the row object. Use `SelectsOnRowClick="False"` when rows
  have their own action buttons.
- **Framed controls.** `Table`, `CodeBlock`, `Textarea` and `Input` already draw a border and radius. Wrapping them in
  a card shows two stacked panels.
- **`Button` lays out its content at full width.** A long text in a button is centred and clipped, not trimmed,
  even with `TextTrimming`, because the label slot measures it without a width limit. Cap the text at the
  button's inner width (width minus `Padding` and `BorderThickness`) when the button resizes; the save folder
  button in `SearchView` does this.
- **Key bindings run before the focused control.** In Avalonia 12 a `KeyBinding` on an ancestor handles a key before
  a focused `TextBox` sees it, so a page-wide Enter binding also fires from a text area or a `SearchField` (whose
  own Enter, `SearchSubmitted`, then never comes). Give such a binding a command whose `CanExecute` is false while
  focus is where the key means something else; `SearchView` does this for the lyric editor.
- **`SearchField.ClearText`** and the `Table` labels default to English. Set them from localization.
- **Arrow glyphs such as ↩** render badly in the Arc fonts. Name keys with words ("Enter") in text; where a key
  is drawn, use an icon (`ArcIcons.CornerDownLeft` for Enter in the shortcut key caps).

## AvaloniaEdit (the lyrics editor)

- **Its Fluent and Simple themes need those themes.** They `StaticResource` Fluent or Simple keys and fail to load
  under Arc. App.axaml merges only `avares://AvaloniaEdit/Themes/Base.xaml`; `LyricEditorSetup` supplies the
  resources Base asks for (`TextAreaSelectionBrush`, `SearchPanel*`) from Arc tokens, again on theme changes.
- **Its find panel theme fails as the panel opens** (`Static resource 'ToggleButton' not found`): its buttons are
  `BasedOn="{StaticResource {x:Type ToggleButton}}"`, which Arc does not register. `LyricSearchPanelTheme.axaml`
  replaces the panel's theme with Arc controls, keeping `PART_searchTextBox`, `PART_replaceTextBox`,
  `PART_MessageView` and `PART_MessageContent`.
- **Its routed `SearchCommands` stay disabled in Arc buttons** (替换 and 全部替换 never enabled). Name the buttons and
  call the panel's methods on Click (`LyricEditorSetup.WireFindButtons`). Tests must click the buttons; calling
  `panel.ReplaceAll()` directly hides this.
- **`TextEditor` installs its own `SearchPanel`** when it gets its template. Installing another gives two panels,
  and settings go to the one never shown. Set `TextEditor.SearchResultsBrush`; the editor passes it on.
- **Arc tokens resolve only in a tree.** `ArcTokens.Get/GetColor` on a control that is not attached throws; apply
  them on `AttachedToVisualTree`.
- **Its own texts follow `AvaloniaEdit.SR.Culture`**, not the app's language ("2 of 5"). It ships zh-Hans only.
- **`TextEditor.Text = ...` clears undo; `Document.Replace(0, length, text)` is one undoable change.** The page uses
  the first for a new song or new options and the second for 还原 while editing.

## Dialogs and toasts

- **`Dialog.ShowAsync` and `Dialog.ConfirmAsync` render on the window's `OverlayLayer`**, not in the visual tree of the
  page, so tests must look there.
- **`Dialog.ConfirmAsync` leaves an empty body** under its description, which makes the dialog tall.
  `DialogHelper.ConfirmOnAsync` calls `Dialog.ShowAsync` with the message as the body and two `DialogAction`s instead.
- **Toasts:** `Toast.Show(owner, title, kind)` is a static call. The message goes in `title`; `description`, an action
  and a duration are optional.

## Window

- **Title bar dragging fails on macOS** with the Arc title bar. The log shows "Window move completed without
  beginning" and the window does not move. `Views/TitleBarDrag.cs` moves the window by setting `Position` itself
  (on platforms other than Windows). It takes the press in the Tunnel phase and marks it handled, which also silences
  the warning. Double clicks and presses on buttons or text boxes are left alone.
- **Arc owns the theme variant.** Do not set `RequestedThemeVariant`. Change the theme with `ArcTheme.Apply(...)`
  (`ThemeService.Apply`). FluentTheme is not loaded; do not add it back.

## Avalonia 12

- **`ItemsPresenter` has its own `ItemsPanel`.** In a custom `ListBox` template, write
  `<ItemsPresenter ItemsPanel="{TemplateBinding ItemsPanel}" />`, or the list silently uses a plain `StackPanel` and
  realises every row (see `LocalMusicScanView.axaml`).
- **Hidden `ScrollViewer` content reports `IsEffectivelyVisible == true`** because it is not templated into the
  visual tree yet. In tests, assert on the scroller, not on its content.
- **Clipboard calls are extension methods**: `clipboard.TryGetTextAsync()` and friends.
- **DataAnnotations validation is opt-in.** The app validates numeric settings itself and shows the message through
  `ErrorMessage`.
- **xunit v3** with `Avalonia.Headless.XUnit`. Arc must be registered in the test app (`TestApp` in
  `TestSupport.cs`), or Arc controls render without templates.

## Checking an API against the installed assembly

`tools/arcdump` is a small console app that prints the public properties, events, methods and enum values of Arc
types from the NuGet package actually referenced:

```bash
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH
dotnet run --project .claude/skills/musiclyric-ui/tools/arcdump -- Table SearchField   # members of these types
dotnet run --project .claude/skills/musiclyric-ui/tools/arcdump -- --find Toast        # full names containing "Toast"
```

Use it to settle a doubt, such as whether a value is a `double` or a `Thickness`, or which enum names exist. It does
not replace reading the component's page: the page says what a member is for and when not to use the component.
