---
name: tkcode-arc
description: Builds and reviews Avalonia interfaces with TKCode.Arc, the Arc design language (uiarc.dev) as native Avalonia controls. Picks the right Arc component or block, installs and registers the packages, wires only documented properties, and applies Arc's rules for design tokens, motion, copy, accessibility and layout. Use when a project references a TKCode.Arc package, uses the XAML namespace https://github.com/AIDotNet/TKCode.Arc or calls UseArc(...), or when asked to build or polish an Avalonia screen such as a settings page, form, dashboard, data table, dialog or command palette with Arc.
---

# TKCode.Arc

TKCode.Arc brings Arc to Avalonia: 113 components in Arc's 22 groups, 78 page-level blocks, a data grid
and a streaming Markdown renderer. Every control derives from an Avalonia base type (`Input` is a `TextBox`, `Button`
is a `Button`), so binding, commands, styles and data validation work as usual, and every control reads one set of
design tokens. The quality bar is Arc's: a calm, neutral interface where type, spacing and one considered interaction
carry the character.

This skill is for TKCode.Arc 1.0.0 on Avalonia 12.1.4. The documentation index is
https://docs.tkcode.si/llms.txt.

## Workflow

Copy this checklist and tick it off:

```
Arc task:
- [ ] 1. Find: an Arc component or block for each region of the screen (components.md, then choosing.md)
- [ ] 2. Read: the page of every component you use, including "When not to use"
- [ ] 3. Install: packages added, UseArc(arc => arc.UseArcControls()) registered
- [ ] 4. Wire: documented properties and events only, bound to a view model
- [ ] 5. Style: tokens for every color, radius, size and font; Arc's motion, not your own
- [ ] 6. Review: run checklist.md, fix, and run it again until every line passes
```

**Starting a new app or control library?** Create it from a template instead of wiring packages by hand; it builds and
runs as soon as it is created, with Arc registered and the theme in place. See https://docs.tkcode.si/docs/templates.md.

```bash
dotnet new install TKCode.Arc.Templates
dotnet new tkcode-arc -n MyApp --layout sidebar   # blank, sidebar, header or dock; --mvvm toolkit, --di, --i18n, --tests
dotnet new tkcode-arc-xplat -n MyApp              # desktop and browser (WebAssembly) hosts
dotnet new tkcode-arc-admin -n MyApp              # finished apps: -admin, -ai-chat, -devtool, -workflow, -projects, -analytics
dotnet new tkcode-arc-lib -n Acme.Controls        # a control library with tests and a sample
```

Inside an existing project, `tkcode-arc-page`, `tkcode-arc-window`, `tkcode-arc-dialog` and `tkcode-arc-control` add
those pieces. The finished apps have English UI text.

**Find.** Start from [components.md](components.md) and shortlist by purpose; each line also gives the names other
libraries use (modal, snackbar, autocomplete, ...), so search it for the word in the request. Then read the shortlisted
classes in [choosing.md](choosing.md): for each component it lists the nearby ones to choose instead and when, so you
settle on one before fetching its page. Prefer a block when one covers the whole region (a sidebar, a settings page, a
command palette). Note the package of each pick: components outside `TKCode.Arc` (the AI components in
`TKCode.Arc.Special`, the phone controls in `TKCode.Arc.Mobile`, the DataGrid) need their package and registration
call. The phone controls have their own XAML namespace,
`xmlns:arcm="https://github.com/AIDotNet/TKCode.Arc.Mobile"`, and drop the prefix of their page name: `MobileButton`
is `<arcm:Button>`. Skip the groups marked decorative (text effects, cursor effects, showpieces) unless the brief asks for the effect. Compose screens from Arc controls and Avalonia layout panels
(`Grid`, `StackPanel`, `WrapPanel`, `DockPanel`); do not rebuild a control Arc already has.

**Read.** Fetch each component's short reference first: `https://docs.tkcode.si/components/<slug>.api.md` (blocks:
`https://docs.tkcode.si/blocks/<slug>.api.md`), with the first example, the alternatives and the API reference. Fetch the full page,
the same address without `.api`, when you need its usage sections, more examples or the accessibility notes. The full
page says when to use the component and when not to, with the better choice for each nearby case; shows usage in XAML
and C# and the source of every live example; and ends with the API reference, the complete list of properties (type
and default), events and pseudo-classes. A name that is not in the API reference does not exist.

**Install.**

```bash
dotnet add package TKCode.Arc --version 1.0.0
dotnet add package TKCode.Arc.Fonts --version 1.0.0   # optional: Geist and Geist Mono
```

```csharp
AppBuilder.Configure<App>()
          .UsePlatformDetect()
          .WithInterFont()    // Avalonia.Fonts.Inter
          .WithArcFonts()     // TKCode.Arc.Fonts
          .UseArc(arc => arc
              .UseArcControls()
              .WithTheme(t => t.Appearance(ArcAppearance.System)));

// Theme changes at run time name only what changes:
ArcTheme.Apply(t => t.Appearance(ArcAppearance.Dark).Accent(ArcAccentPreset.Violet));
```

XAML uses `xmlns:arc="https://github.com/AIDotNet/TKCode.Arc"` for controls, token resources and markup
extensions. Blocks (`TKCode.Arc.Blocks`, `UseArcBlocks()`), the AI components, illustrations and effects
(`TKCode.Arc.Special`, `UseArcSpecial()`), phone controls (`TKCode.Arc.Mobile`, `UseArcMobile()`), the DataGrid
(`TKCode.Arc.AdvancedCharts`, `UseArcAdvancedCharts()`), the workflow canvas (`TKCode.Arc.Workflow`, `UseArcWorkflow()`)
and the dock layout (`TKCode.Arc.Dock`, `UseArcDock()`) are separate packages, registered in the same `UseArc` callback. Full steps:
https://docs.tkcode.si/docs/installation.md.

Icons: Arc's built-in glyphs are `{x:Static arc:ArcIcons.Name}`. Any other Lucide icon comes from
`TKCode.Arc.Icons.Lucide` (`<lucide:LucideIcon Kind="Bell" />`), whose generator compiles in only the icons written in
C# or XAML. An icon that comes from a binding or from data is declared in the project file, or it draws nothing:

```xml
<ItemGroup>
    <LucideIcon Include="Bell;ArrowRight" />    <!-- or <LucideIconsIncludeAll>true</LucideIconsIncludeAll> -->
</ItemGroup>
```

**Wire.** Bind two-way properties (`Text`, `IsChecked`, `SelectedItem`, `Value`) to settable view model properties and
only read the read-only ones. Fields are named by `Label`, explained by `Description` when there is a constraint, and
show errors through `ErrorMessage` or the bound property's data validation. Set values that come from background work
on the UI thread (`Dispatcher.UIThread`).

**Style.** Every color, radius, spacing, shadow and font size comes from a token: `{arc:ArcDesignTokenResource Name}`
in XAML, `ArcTokens.Bind(control, property, ArcDesignTokenKind.Name)` in code. The common ones:

| Need | Tokens |
|---|---|
| Surfaces | `Background` (page), `Surface` (cards, fields), `SurfaceMuted` (wells), `SurfaceRaised` (menus, popups) |
| Text | `Foreground` (titles, primary), `TextSecondary` (descriptions), `TextMuted` (hints only) |
| Lines | `Border`, `BorderStrong`, `BorderSubtle` |
| Accent | `Accent`, `AccentStrong`, `AccentSubtle`, `AccentForeground` (text on the accent) |
| Status | `Success`, `Warning`, `Danger` and their `…Subtle` tints |
| Shape | `RadiusControl` (fields, buttons), `RadiusPanel` (cards), `RadiusSurface` (large surfaces), `RadiusPill` |
| Spacing | `Space1` (4) to `Space24` (96), on a 4 px scale |
| Type | `TypeXS` (12), `TypeSM` (14), `TypeBase` (16), `TypeLG` (18) up to `Type5XL` (72) |
| Elevation | `ShadowResting`, `ShadowRaised`, `ShadowFloating` |

Every token, with themes and accents: https://docs.tkcode.si/docs/theming.md. Arc controls animate themselves; a control of your own
declares transitions under `^:motion-full` and `^:motion-reduced` with `arc:ArcMotion.Tracks="True"` and uses
`MotionValue<T>` only for what a theme cannot express: https://docs.tkcode.si/docs/motion.md.

## Core principles

1. **One purpose per screen.** One `Primary` button per surface; the accent marks the one action or state that matters
   and everything else stays neutral.
2. **Native first.** Use the Avalonia way (binding, commands, styles, data validation, the keyboard) through Arc's
   controls; the controls already follow it.
3. **Tokens, not values.** A screen built from tokens follows light and dark, the eight accents and token overrides
   with no extra work.
4. **Words that say what happens.** Sentence case everywhere; a button is a verb and an object ("Save changes",
   "Create invoice"); an error says how to fix the value.
5. **Motion explains a change.** Move what moved, fade what appeared; the end state reads the same under the Reduced
   and Off motion levels.
6. **Accessible by default.** Every field has a label, every icon-only button an accessible name, every state a word or
   an icon and not only a color.

## Never do

- WPF, WinUI or web APIs: `Visibility`, `DataTemplateSelector`, `x:Array`, `Trigger`, CSS. This is Avalonia.
- An Arc control without the `arc:` prefix (it silently becomes Avalonia's own `Button`, `Slider` or `TreeView`).
- A property, event or enum value that the API reference does not list.
- A hex color, a fixed corner radius or a font size in a view, or `#FFFFFF` for text on the accent (`AccentForeground`).
- `TextMuted` for information people must read.
- A bound `LucideIcon.Kind` (or a `LucideIconKind` from data) without declaring its icons with `<LucideIcon Include="..." />`.
- Hand-written animations, or `DispatcherTimer` loops, for a change an Arc control already animates.
- Status shown by color alone, or bounce on money, progress and errors.
- Title Case labels, "OK" or "Submit" buttons, exclamation marks in confirmations.
- Floating package versions (`0.*`), a second Avalonia version, or `InvariantGlobalization` (the inputs, pickers and
  charts format numbers and dates with culture data).

## Quick decisions

| The screen needs | Use |
|---|---|
| A single action | `Button`; `Variant="Primary"` once per surface, `Secondary` or `Ghost` beside it |
| An action whose result shows on the button (Saved, Sent) | `ActionButton` |
| One action with a few close alternatives | `SplitButton` |
| Related actions on one object | `ButtonGroup` |
| A destructive action that needs a deliberate press | `HoldToConfirm`, or `ConfirmMorph` for inline confirmation with undo |
| Single-line text with a label, hint and error | `Input`; long text `Textarea`; secrets `PasswordField` |
| A number, an amount of money, a phone number | `NumberField`, `MoneyInput`, `PhoneInput` |
| One choice from a list | `Select`; searchable `Combobox`; several `MultiSelect` |
| A setting that applies at once | `Switch` |
| One of a few views or periods | `SegmentedControl` |
| One of several options that need explaining | `RadioCards`, or `RadioGroup` for short options |
| A date, a range, a time | `DatePicker`, `DateRangePicker`, `TimePicker` |
| A decision that blocks the page | `Dialog`; focused side work `Drawer`; on phones `BottomSheet` |
| Contextual details next to a control | `Popover`; a preview on hover `HoverCard`; a short hint `Tooltip` |
| A short confirmation of finished work | `Toast`; several at once or app-wide `ToastStack` |
| A message that stays until someone acts | `Alert` |
| A number with context | `MetricCard`; a trend `Sparkline` or `LineChart`; part of a whole `DonutChart` |
| Records to compare and sort | `Table`; spreadsheet editing `DataGrid`; nested items `TreeView` |
| Filters over a collection | `FilterToolbar`, or `ChipGroup` for a few facets |
| Content still loading | `Skeleton`; a known amount of work `Progress` |
| Nothing to show yet | `EmptyState` |
| Steps of a flow | `Stepper` |
| App navigation | `Sidebar` block; within a page `Tabs`; hierarchy `Breadcrumb` |
| A settings screen | `SettingsPage` block; find any setting `SettingsCommand` |
| Search and run any action from the keyboard | `CommandPalette` block |
| Rendered Markdown or streamed AI replies | `Markdown`; source code `CodeBlock` |
| A whole AI chat screen | `AiChat` block; its input alone `AiComposer`; a message list in your own layout `ChatThread` |
| What an agent is doing | `AgentStatus` (live pill), `ToolCall` (one tool run), `AgentPlan` (steps), `ReasoningBlock` (thinking) |
| Values over time, categories, a share | `LineChart`, `BarChart`, `DonutChart`; a whole analytics page `MetricsDashboard` block |
| Tasks on a board, a schedule | `KanbanBoard`, `GanttTimeline`, `WeekCalendar` blocks |
| A node graph or pipeline | `WorkflowCanvas` (`TKCode.Arc.Workflow`); IDE-style panes `DockLayout` (`TKCode.Arc.Dock`) |
| A phone app | `arcm:Page` with `arcm:Navbar` and `arcm:Tabbar`, pages in `arcm:NavigationStack` (`TKCode.Arc.Mobile`, `xmlns:arcm`) |

## Reference files

| File | Read it when |
|---|---|
| [components.md](components.md) | Finding components: every component and block by group, with its class, package, purpose and other names |
| [choosing.md](choosing.md) | Deciding between close candidates: for each component, what to choose instead and when |
| [checklist.md](checklist.md) | Before you finish: the review checklist |
| https://docs.tkcode.si/docs/templates.md | Creating an app or control library with `dotnet new`, every template and option |
| https://docs.tkcode.si/docs/installation.md | Setting up a project, publishing, troubleshooting |
| https://docs.tkcode.si/docs/theming.md | Light and dark, accents, every design token, overriding tokens |
| https://docs.tkcode.si/docs/motion.md | Motion levels, springs, motion in your own controls |
| https://docs.tkcode.si/llms-full.txt | You need every page at once |

When these files are not next to this one, fetch them from https://docs.tkcode.si/skills/tkcode-arc/.

## Before you finish

Run [checklist.md](checklist.md) against the code, fix what fails and run it again. Then build: a XAML property that
does not exist fails at build time. Report the Arc properties you used and the page that documents each, so they are
quick to check.
