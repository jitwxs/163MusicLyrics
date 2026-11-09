# TKCode.Arc review checklist

Run this against every screen you wrote or changed. Fix each line that fails, then run the list again until every line
passes. Each line names what to look for in the code.

## Names and API

- [ ] Every Arc element has the `arc:` prefix, and the file declares `xmlns:arc="https://github.com/AIDotNet/TKCode.Arc"`.
- [ ] Every property, event and enum value on an Arc control is in that component's API reference (https://docs.tkcode.si/components/<slug>.md).
- [ ] C# files that use Arc and `Avalonia.Controls` together alias one of the clashing names (`using ArcButton = TKCode.Arc.Button;`).
- [ ] No WPF or WinUI API: `Visibility`, `DataTemplateSelector`, `x:Array`, `Trigger`.
- [ ] Packages are pinned to 1.0.0, the application references Avalonia 12.1.4, and `AppBuilder.UseArc(arc => arc.UseArcControls())` is called (plus `UseArcBlocks()` or `UseArcAdvancedCharts()` in the same callback when those are used); the theme is changed with `ArcTheme.Apply(...)`.

## Tokens and theme

- [ ] No hex color, `Colors.*` or `Brushes.*` in views or styles; colors come from `{arc:ArcDesignTokenResource ...}` or `ArcTokens.Bind`.
- [ ] Corner radii, spacing and font sizes come from tokens (`RadiusControl`, `RadiusPanel`, `Space4`, `TypeSM`, ...), not literal numbers.
- [ ] Text on the accent uses `AccentForeground`; `TextMuted` holds only hints and captions.
- [ ] Values that stay on screen are bound (`ArcTokens.Bind`), not read once (`ArcTokens.Get`), so they follow a theme change.
- [ ] The screen reads correctly in light and dark, and with at least two accents.

## Layout and composition

- [ ] One `Primary` button per surface; the others are `Secondary` or `Ghost`, and destructive ones `Danger`.
- [ ] Controls in one row use the same `Size` where they have one, so their heights line up.
- [ ] An existing Arc component or block is used wherever one fits, instead of a hand-built look-alike.
- [ ] Every list or table has an empty state (`EmptyState`) and a loading state (`Skeleton` or `Progress`).
- [ ] Cards are not nested in cards.

## Copy

- [ ] Labels, titles and buttons are sentence case.
- [ ] Each button says what it does, as a verb and an object: "Save changes", "Create invoice", not "OK" or "Submit".
- [ ] Each error message says how to fix the value, and each description states a real constraint.
- [ ] Confirmations state a fact ("Changes saved") without exclamation marks.

## Accessibility

- [ ] Every field has a `Label`; a hidden label (`IsLabelVisible="False"`) only where something else on screen names the field.
- [ ] Every icon-only button has an accessible name (`AutomationProperties.Name`).
- [ ] No state is shown by color alone; status colors come with words or icons.
- [ ] Everything can be reached and used with the keyboard, in a sensible tab order.

## Motion

- [ ] No hand-written animation or timer loop for a change an Arc control already animates.
- [ ] Custom transitions sit in a ControlTheme under `^:motion-full` and `^:motion-reduced`, with `arc:ArcMotion.Tracks="True"`.
- [ ] Code-driven motion uses `MotionValue<T>`, with a comment that says why a theme cannot express it.
- [ ] With `ArcMotion.Level` set to `Off`, every end state is the same and nothing is missing.

## Binding and threading

- [ ] Two-way properties (`Text`, `IsChecked`, `SelectedItem`, `Value`) are bound to settable view model properties; read-only ones are only read.
- [ ] Values from background work are set on the UI thread (`Dispatcher.UIThread`).
- [ ] Validation runs through `ErrorMessage` or data validation on the bound property, not through ad-hoc text blocks.

## Build

- [ ] The project builds without new warnings: a XAML property that does not exist fails at build time.
- [ ] Every Lucide icon chosen at run time (a bound `Kind`, an icon from data) is declared with `<LucideIcon Include="..." />`; no `ARCL002` warning is left.
- [ ] `InvariantGlobalization` is off, so numbers, money and dates format with culture data.
