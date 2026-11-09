# TKCode.Arc components

Every component and block of TKCode.Arc 1.0.0, by group: its page, its class, what it is for and the other names
people use for it (in other UI libraries, or in plain words). Components are in the `TKCode.Arc` package unless their
group or line names another one, with the call that registers it in `UseArc(...)`; blocks are in `TKCode.Arc.Blocks`
(`UseArcBlocks()`). Groups marked decorative hold effects and showpieces, not everyday controls.

To decide between close candidates, read their sections in [choosing.md](choosing.md), then the page of the one you
pick: it shows usage and examples and lists every property and event.

## Components

### Buttons

- [Button](https://docs.tkcode.si/components/button.md): `Button`. The default button for one action, in primary, secondary, ghost and danger weights, three sizes and a loading state that keeps its width. Also called: btn, command button, primary action.
- [Action button](https://docs.tkcode.si/components/action-button.md): `ActionButton`. A submit button that shows the work on itself: a pending word and a spinner while the task runs, then a success word with a check, then back to idle. Also called: submit button, async button, loading button, busy button.
- [Action swap](https://docs.tkcode.si/components/action-swap.md): `ActionSwap`. One pill that cycles through related states, such as Follow and Following, changing its width, tone, icon and letters in place.
- [Split button](https://docs.tkcode.si/components/split-button.md): `SplitButton`. A primary action with a menu of nearby alternatives. Also called: dropdown button, menu button.
- [Button group](https://docs.tkcode.si/components/button-group.md): `ButtonGroup`. Related actions joined into one surface with hairline dividers: a hover highlight glides between segments, the pressed one answers in place, and an attached menu can close the row. Also called: toolbar, button bar, joined buttons.
- [Floating button group](https://docs.tkcode.si/components/floating-button-group.md): `FloatingButtonGroup`. A small tray of actions that floats over a canvas, document, chart or map, with one highlight that morphs from button to button.
- [Expanding button group](https://docs.tkcode.si/components/expanding-button-group.md): `ExpandingButtonGroup`. Icon buttons in a compact group: the one you point at or focus grows to reveal its label while its neighbours slide aside, and an action confirms in place.
- [Copy button](https://docs.tkcode.si/components/copy-button.md): `CopyButton`. Copy a value with immediate confirmation. Also called: copy to clipboard.
- [Icon morph](https://docs.tkcode.si/components/icon-morph.md): `IconMorph`. Icons that morph between states: menu to close, play to pause, copy to check, and more.
- [Confirm morph](https://docs.tkcode.si/components/confirm-morph.md): `ConfirmMorph`. A destructive button that morphs into an inline confirmation, a spinner, and a result with undo. Also called: delete confirmation, inline confirm, undo delete.
- [Theme switcher](https://docs.tkcode.si/components/theme-switch.md): `ThemeSwitcher`. A button that flips the whole window between light and dark, with four styles of page transition. Also called: dark mode toggle, light and dark toggle.

### Gestures

- [Hold to confirm](https://docs.tkcode.si/components/hold-to-confirm.md): `HoldToConfirm`. Confirm a destructive action by holding, not tapping. Also called: long press, press and hold, destructive confirm.
- [Swipe actions](https://docs.tkcode.si/components/swipe-actions.md): `SwipeActions`. Reveal row actions with a swipe, or from the same actions in a menu. Also called: swipe to delete, swipe row actions.
- [Reactions](https://docs.tkcode.si/components/reactions.md): `Reactions`. Acknowledge a post in one tap, with counts that roll. Also called: emoji reactions, likes.

### Menus

- [Dropdown menu](https://docs.tkcode.si/components/dropdown-menu.md): `DropdownMenu`. A menu of commands behind a labelled trigger button: choosing an item runs an action and closes the menu. Also called: menu, actions menu, overflow menu, kebab menu, more menu, menu flyout.
- [Context menu](https://docs.tkcode.si/components/context-menu.md): `ContextMenu`. A right-click or long-press menu of the actions for one object, opened at the pointer or under the target. Also called: right-click menu.
- [User menu](https://docs.tkcode.si/components/user-menu.md): `UserMenu`. Your account, settings, theme, and sign out behind the avatar. Opens as a bottom sheet on phones. Also called: account menu, profile menu, avatar menu.

### Text fields

- [Input](https://docs.tkcode.si/components/input.md): `Input`. The text box for a short typed value, with its label, helper text and error row built in. Also called: text box, textbox, text field, text input, edit box.
- [Textarea](https://docs.tkcode.si/components/textarea.md): `Textarea`. A multiline field for notes, descriptions, and longer text. Also called: multiline text box, text area, memo.
- [Password field](https://docs.tkcode.si/components/password-field.md): `PasswordField`. A masked Input for a password or secret that already exists, with a reveal toggle inside the field. Also called: password box, secret input.
- [Password strength](https://docs.tkcode.si/components/password-strength.md): `PasswordStrength`. Show how strong a new password is while it is typed. Also called: password meter, strength meter.
- [Search field](https://docs.tkcode.si/components/search-field.md): `SearchField`. A search box that narrows the items already on the view as people type, with a clear button that does not shift the toolbar. Also called: search box, search bar, search input.
- [Expanding search](https://docs.tkcode.si/components/expanding-search.md): `ExpandingSearch`. An icon that morphs into a search field with results beneath it. Also called: collapsible search, search icon expand.
- [Inline edit](https://docs.tkcode.si/components/inline-edit.md): `InlineEdit`. Rename in place: the text becomes a field without moving. Also called: rename in place, editable label, click to edit.

### Special inputs

- [Number field](https://docs.tkcode.si/components/number-field.md): `NumberField`. Enter a bounded number with clear increment controls. Also called: numeric up down, spin box, stepper input, number input.
- [Money input](https://docs.tkcode.si/components/money-input.md): `MoneyInput`. A currency field with live grouping, stable width, rolling digits, and minor-unit output. Also called: currency input, amount field, price input.
- [Phone input](https://docs.tkcode.si/components/phone-input.md): `PhoneInput`. A phone field with a country picker, formatting as you type, and E.164 output. Also called: telephone input, phone number field.
- [Scrub input](https://docs.tkcode.si/components/scrub-input.md): `ScrubInput`. A Figma-style number field: drag the label to scrub, or type values, units, and arithmetic. Also called: drag number, draggable value, unit input.
- [Tag input](https://docs.tkcode.si/components/tag-input.md): `TagInput`. Turn short text values into removable tags. Also called: tags, chips input, token input.
- [Mention input](https://docs.tkcode.si/components/mention-input.md): `MentionInput`. A textarea with @people and #channel mentions that act as single tokens, with suggestions at the caret. Also called: at mentions, @mention, hashtag input.
- [Shortcut recorder](https://docs.tkcode.si/components/shortcut-recorder.md): `ShortcutRecorder`. Record key combinations into key caps, with conflict warnings, Kbd, and a searchable cheatsheet. Also called: hotkey input, keybinding editor, key capture.

### Selects

- [Select](https://docs.tkcode.si/components/select.md): `Select`. The dropdown field for one value from a short, fixed list, with a label and a keyboard friendly menu. Also called: dropdown, combo box (fixed list), picker, select box, option list.
- [Combobox](https://docs.tkcode.si/components/combobox.md): `Combobox`. Search and select from a list without leaving the field. Also called: autocomplete, typeahead, auto complete box, searchable select, filterable dropdown.
- [Multi-select](https://docs.tkcode.si/components/multi-select.md): `MultiSelect`. A dropdown field for several values from a fixed list, shown as chips inside the field. Also called: multiple select, multi-select dropdown, checkbox dropdown.
- [Morph select](https://docs.tkcode.si/components/morph-select.md): `MorphSelect`. A select whose trigger grows into the list, with a gliding highlight and type-ahead.
- [Chip group](https://docs.tkcode.si/components/chip-group.md): `ChipGroup`. Filter by a few facets with chips that morph as you pick them. Also called: filter chips, choice chips, tag filter, pills.

### Toggles

- [Radio cards](https://docs.tkcode.si/components/radio-cards.md): `RadioCards`. Selectable option cards with a sliding selection ring, price and description slots, and radio keyboard behavior. Also called: option cards, plan picker, selectable cards.
- [Billing toggle](https://docs.tkcode.si/components/billing-toggle.md): `BillingToggle`. A monthly and yearly switch with a savings badge and prices that roll to the new amount. Also called: monthly yearly toggle, pricing period switch.
- [Checkbox](https://docs.tkcode.si/components/checkbox.md): `Checkbox`. A labelled yes or no choice in a form, applied when the form is saved, with an indeterminate state for partly selected groups. Also called: check box, tick box.
- [Radio group](https://docs.tkcode.si/components/radio-group.md): `RadioGroup`. Choose one option from a visible set. Also called: radio buttons, option buttons.
- [Switch](https://docs.tkcode.si/components/switch.md): `Switch`. A tactile toggle for settings that take effect immediately. Also called: toggle, toggle switch, toggle button setting.
- [Segmented control](https://docs.tkcode.si/components/segmented-control.md): `SegmentedControl`. Switch between a small set of related views. Also called: segmented button, toggle button group, view switcher, tab-like choice.

### Sliders

- [Slider](https://docs.tkcode.si/components/slider.md): `Slider`. Pick a value or a range on a track that follows your finger. Also called: range slider, track bar.
- [Elastic slider](https://docs.tkcode.si/components/elastic-slider.md): `ElasticSlider`. A slider that stretches like rubber past its ends, with a swaying value tag and detents the thumb clicks into.

### Pickers

- [Calendar](https://docs.tkcode.si/components/calendar.md): `Calendar`. Browse dates in a clear, compact month view. Also called: month view, date grid.
- [Date picker](https://docs.tkcode.si/components/date-picker.md): `DatePicker`. A date field: type a date or pick it from a calendar popup, with the same label and error rows as the other fields. Also called: date input, calendar date picker.
- [Date range picker](https://docs.tkcode.si/components/date-range-picker.md): `DateRangePicker`. A range picker that grows from its trigger into two months with presets and a stretching range highlight. Also called: date range, period picker, from to dates.
- [Time picker](https://docs.tkcode.si/components/time-picker.md): `TimePicker`. A time-of-day field that picks from an evenly spaced list, every 15 minutes by default, shown in the person's culture. Also called: time input, clock picker.
- [Color picker](https://docs.tkcode.si/components/color-picker.md): `ColorPicker`. A swatch that grows into a picker with format morphing, eyedropper, saved swatches, and contrast readout. Also called: color chooser, swatch picker.

### Editors

- [Rich text editor](https://docs.tkcode.si/components/rich-text-editor.md): `RichTextEditor`. A lightweight editor with markdown shortcuts, a floating toolbar, a slash menu, and HTML and markdown output. Also called: WYSIWYG editor, rich text box, formatted text editor.
- [Signature pad](https://docs.tkcode.si/components/signature-pad.md): `SignaturePad`. Smooth ink that thins with speed, with undo, replay, and PNG or SVG export. Also called: signature capture, draw signature.
- [File dropzone](https://docs.tkcode.si/components/file-dropzone.md): `FileDropzone`. A file drop target with the system file picker, paste, and a list that shows each file's upload state. Also called: file upload, drag and drop upload, file picker area.

### Navigation

- [Tabs](https://docs.tkcode.si/components/tabs.md): `Tabs`. A tab list that switches between peer views of one object, such as Overview, Activity and Settings, each with its own panel. Also called: tab control, tab view, tab bar, tab strip.
- [Breadcrumb](https://docs.tkcode.si/components/breadcrumb.md): `Breadcrumb`. Show where a page sits in a hierarchy. Also called: breadcrumbs, path bar.
- [Pagination](https://docs.tkcode.si/components/pagination.md): `Pagination`. Previous, next and page number buttons for a collection the app loads one page at a time. Also called: pager, page numbers.

### Expand

- [Scroll area](https://docs.tkcode.si/components/scroll-area.md): `ScrollArea`. A native scroll container with thin overlay scrollbars and edge fades that appear only when content overflows. Also called: scroll viewer, scrollable panel.
- [Accordion](https://docs.tkcode.si/components/accordion.md): `Accordion`. A column of headed rows, such as questions in an FAQ, whose content opens in place one at a time or together. Also called: expander, collapse, collapsible sections, disclosure.
- [Expandable card](https://docs.tkcode.si/components/expandable-card.md): `ExpandableCard`. A summary card whose header opens a details panel below it, or grows sideways for wide details. Also called: expanding card, card to detail.
- [Resizable panels](https://docs.tkcode.si/components/resizable-panels.md): `ResizablePanels`. Trade space between panes by dragging the divider between them. Also called: splitter, split view, grid splitter, split pane.
- [Expandable text](https://docs.tkcode.si/components/expandable-text.md): `ExpandableText`. Long text clamped to a few lines with a soft fade that glides open with Read more and closes with Show less, shown only when the text overflows. Also called: read more, show more text, truncated text.

### Overlays

- [Dialog](https://docs.tkcode.si/components/dialog.md): `Dialog`. A centred modal panel for a decision or a short task that must be finished before the person returns to the page. Also called: modal, modal dialog, popup window, message box, content dialog, confirm dialog.
- [Drawer](https://docs.tkcode.si/components/drawer.md): `Drawer`. A modal panel that slides in from an edge of the window for focused work on the current view, such as filters or one record. Also called: side sheet, off-canvas panel, slide-over, flyout panel.
- [Bottom sheet](https://docs.tkcode.si/components/bottom-sheet.md): `BottomSheet`. A sheet that rests at a peek or full height and follows your finger. Also called: action sheet, bottom drawer, mobile sheet.
- [Popover](https://docs.tkcode.si/components/popover.md): `Popover`. A small non-modal panel of interactive content anchored to the control that opens it, such as share settings or a filter form. Also called: flyout, popup, callout.
- [Hover card](https://docs.tkcode.si/components/hover-card.md): `HoverCard`. Preview a person or link on hover or focus without leaving the page. Also called: hover preview, peek card.
- [Tooltip](https://docs.tkcode.si/components/tooltip.md): `Tooltip`. A short label shown on hover or keyboard focus for a control that does not explain itself, such as an icon-only button. Also called: hint, info tip, title tip.

### Messages

- [Alert](https://docs.tkcode.si/components/alert.md): `Alert`. An inline notice that stays on the page next to what it concerns until its situation is resolved. Also called: banner, info bar, callout message, notice, inline message.
- [Toast](https://docs.tkcode.si/components/toast.md): `Toast`. A short message a page shows for a few seconds after something it started has finished, such as a saved form. Also called: snackbar, notification, flash message, toast notification.
- [Toast stack](https://docs.tkcode.si/components/toast-stack.md): `ToastStack`. An app-wide stack of toasts at the window edge that any control can add a result to, newest in front. Also called: notification stack, toaster, notification manager.
- [Announcement bar](https://docs.tkcode.si/components/announcement-bar.md): `AnnouncementBar`. A top banner that rotates messages, counts down, and collapses smoothly when dismissed. Also called: top banner, promo bar, site-wide notice.
- [Cookie consent](https://docs.tkcode.si/components/cookie-consent.md): `CookieConsent`. A consent bar that morphs into preferences and folds into a reopen button. Also called: cookie banner, GDPR consent.

### Progress

- [Progress](https://docs.tkcode.si/components/progress.md): `Progress`. A progress bar for a task with a known total, such as an upload or an import, with an optional count and a completion check. Also called: progress bar, spinner, loading indicator, progress ring, activity indicator.
- [Skeleton](https://docs.tkcode.si/components/skeleton.md): `Skeleton`. Placeholder shapes that hold the layout of a block of content, such as an avatar and lines of text, while its data loads. Also called: placeholder, shimmer, loading placeholder, ghost content.
- [Stepper](https://docs.tkcode.si/components/stepper.md): `Stepper`. Show where a person is in a multi-step flow and what is done. Also called: steps, wizard steps, progress steps.
- [Countdown](https://docs.tkcode.si/components/countdown.md): `Countdown`. A launch countdown with rolling digits that morphs into a live state at zero. Also called: timer, countdown timer.
- [Usage meter](https://docs.tkcode.si/components/usage-meter.md): `UsageMeter`. Show what fills an allowance and how close it is to the limit. Also called: quota bar, usage bar, limit meter.

### Avatars

- [Avatar](https://docs.tkcode.si/components/avatar.md): `Avatar`. A compact identity marker for people and accounts. Also called: profile picture, user image, initials.
- [Avatar group](https://docs.tkcode.si/components/avatar-group.md): `AvatarGroup`. Show a team or set of contributors in a small space. Also called: avatar stack, facepile.
- [Badge](https://docs.tkcode.si/components/badge.md): `Badge`. A small label for status, category, or metadata. Also called: tag, label, status pill, chip, count badge.

### Cards

- [Card](https://docs.tkcode.si/components/card.md): `Card`. A tile for one item of a browsable grid, with a visual, title, owner and status, that can grow into a quick look. Also called: panel, tile, container.
- [Metric card](https://docs.tkcode.si/components/metric-card.md): `MetricCard`. A dashboard tile with one number, the label that names it, a comparison and an optional change chip. Also called: KPI card, stat card, statistic, number tile.
- [Empty state](https://docs.tkcode.si/components/empty-state.md): `EmptyState`. A useful next step when there is nothing to show yet. Also called: no data, blank slate, zero state.

### Charts

- [Line chart](https://docs.tkcode.si/components/line-chart.md): `LineChart`. A multi-series line chart with a gliding crosshair, legend toggles, and paths that morph between ranges. Also called: line graph, trend chart, time series.
- [Bar chart](https://docs.tkcode.si/components/bar-chart.md): `BarChart`. Compare one measure across days and scrub any bar for its value. Also called: column chart, histogram, bar graph.
- [Donut chart](https://docs.tkcode.si/components/donut-chart.md): `DonutChart`. A donut chart of how a whole divides into two to six parts, with a legend and the total or the pointed part in the center. Also called: pie chart, ring chart.
- [Streamgraph](https://docs.tkcode.si/components/streamgraph.md): `Streamgraph`. Layered streams on a wiggle baseline that morph between ranges, with a layer you can isolate and read week by week.
- [Brush chart](https://docs.tkcode.si/components/brush-chart.md): `BrushChart`. A dense time series with an overview strip: drag a window to zoom, resize it by its handles, and read events in place.
- [Waffle chart](https://docs.tkcode.si/components/waffle-chart.md): `WaffleChart`. A ten by ten unit chart where every cell is one percent, and cells fly to their new group when the data changes.
- [Slope chart](https://docs.tkcode.si/components/slope-chart.md): `SlopeChart`. Before and after on two axes: lines draw in, rank moves sit beside each value, and switching datasets slides every line to its new slope.
- [Sparkline](https://docs.tkcode.si/components/sparkline.md): `Sparkline`. Show a compact trend beside a value. Also called: mini chart, inline chart.
- [Gauge](https://docs.tkcode.si/components/gauge.md): `Gauge`. A 270 degree ring that shows where a reading sits in a known range, with the percentage in the center and named bands. Also called: dial, meter, radial progress.
- [Activity heatmap](https://docs.tkcode.si/components/activity-heatmap.md): `ActivityHeatmap`. See a year of activity at a glance, one square per day. Also called: contribution graph, calendar heatmap.
- [Animated counter](https://docs.tkcode.si/components/animated-counter.md): `AnimatedCounter`. A large number whose changed digits roll to the new value, like an odometer, without changing its width. Also called: rolling number, number ticker, count up.
- [Ridgeline](https://docs.tkcode.si/components/ridgeline.md): `Ridgeline`. Overlapping distributions, one ridge per group: hover to lift a ridge and read its quartiles, switch datasets and every curve morphs.
- [Treemap](https://docs.tkcode.si/components/treemap.md): `Treemap`. A squarified treemap: click to drill and the tiles grow to fill the view, with a breadcrumb back and metrics that morph every tile. Also called: hierarchy chart, area breakdown.

### Advanced charts

Package: `TKCode.Arc.AdvancedCharts`, `UseArcAdvancedCharts()`.

- [Data grid](https://docs.tkcode.si/components/data-grid.md): `DataGrid`. A spreadsheet grid with range selection, inline editing, a fill handle, clipboard, undo, sorting, filters, grouping, totals and export. Also called: spreadsheet, editable grid, data table with editing, excel-like grid.
- [Sunburst](https://docs.tkcode.si/components/sunburst.md): `Sunburst`. A hierarchy drawn as concentric rings: click a segment to make it the new centre, with a breadcrumb back, a rolling total and a legend of shares. Also called: radial treemap, ring hierarchy, multi-level pie chart, zoomable hierarchy.
- [Sankey flow](https://docs.tkcode.si/components/sankey-flow.md): `SankeyFlow`. Flows between stages drawn as ribbons with drifting particles: hover or focus a node or ribbon to trace its share through the whole journey. Also called: sankey diagram, flow diagram, alluvial chart, funnel flow, user journey chart.
- [Funnel chart](https://docs.tkcode.si/components/funnel-chart.md): `FunnelChart`. A tapered conversion funnel with the drop-off at each step: split or isolate segments on a spring, and watch dots leave the band at the real rates. Also called: conversion funnel, sales funnel, drop-off chart, pipeline chart.
- [Radar chart](https://docs.tkcode.si/components/radar-chart.md): `RadarChart`. Compare several profiles on one web of measures: swap a profile and its shape morphs, sweep the axes and each value reads out. Also called: spider chart, web chart, star plot, skill radar, capability chart.
- [Realtime stream](https://docs.tkcode.si/components/realtime-stream.md): `RealtimeStream`. A live line chart that scrolls as readings arrive, holds still under the pointer so a point can be read, and marks readings flagged as anomalies. Also called: live line chart, streaming chart, real-time chart, telemetry chart, scrolling time series.
- [Race bar chart](https://docs.tkcode.si/components/race-bar-chart.md): `RaceBarChart`. A ranking that changes over time: bars slide past each other as their values climb, and a timeline plays, pauses or scrubs the periods. Also called: bar chart race, racing bars, animated ranking, ranking over time, leaderboard animation.

### Workflow

Package: `TKCode.Arc.Workflow`, `UseArcWorkflow()`.

- [Workflow canvas](https://docs.tkcode.si/components/workflow-canvas.md): `WorkflowCanvas`. Show a workflow on a dotted canvas and play its runs: each step lights up in turn, the data draws along the edges, and every step reports its result and time. Also called: node editor, flow chart, pipeline diagram, DAG view.

### Dock

Package: `TKCode.Arc.Dock`, `UseArcDock()`, not on NuGet yet.

- [Dock layout](https://docs.tkcode.si/components/dock-layout.md): `DockLayout`. An IDE-style layout of tabbed panes in nested, resizable splits around a document area: drag panes to dock them, float them in their own windows, auto-hide tools to the edges, bind documents to a collection, switch panes from the keyboard, and save layouts to JSON. Also called: docking panels, IDE layout, tool windows, docking manager.

### Video

Package: `TKCode.Arc.Video`, `UseArcControls()`, not on NuGet yet.

- [Video player](https://docs.tkcode.si/components/video-player.md): `VideoPlayer`. Play a video with controls over the picture: seek with chapters, volume, speed, captions and full screen, drawn in Arc's style and decoded by the platform's own player.
- [Video surface](https://docs.tkcode.si/components/video-surface.md): `VideoSurface`. Show a video's picture with nothing over it, as a background or under controls of your own; it clips, fades and transforms like any control.

### PDF

Package: `TKCode.Arc.Pdf`, `UseArcPdf()`, not on NuGet yet.

- [PDF viewer](https://docs.tkcode.si/components/pdf-viewer.md): `PdfViewer`. View a PDF with thumbnails, a table of contents, search, text selection, links and password prompts, rendered by PDFium on desktop and pdf.js in a browser. Also called: pdf reader, pdf preview, document viewer, pdf.js, pdfium.

### Tables

- [Table](https://docs.tkcode.si/components/table.md): `Table`. Compare structured records with sortable columns. Also called: data table, list view with columns, grid view, sortable table.
- [Tree table](https://docs.tkcode.si/components/tree-table.md): `TreeTable`. A table with nested rows, tri-state selection, hierarchy-preserving sort, and lazy-loaded children. Also called: tree grid, tree list view, hierarchical table.
- [Tree view](https://docs.tkcode.si/components/tree-view.md): `TreeView`. Navigate nested folders and structured content. Also called: tree, folder tree, outline, hierarchy.
- [Reorderable list](https://docs.tkcode.si/components/reorderable-list.md): `ReorderableList`. Organize items with direct drag and drop. Also called: sortable list, drag to reorder, drag and drop list.
- [Filter toolbar](https://docs.tkcode.si/components/filter-toolbar.md): `FilterToolbar`. The strip above a table or list with the applied filters as removable chips, an Add filter menu, search and a reset. Also called: filter bar, query builder, faceted filters.
- [Code block](https://docs.tkcode.si/components/code-block.md): `CodeBlock`. A read-only code viewer with a file-name header, syntax colors and a copy button. Also called: code viewer, syntax highlighting, source view.
- [JSON viewer](https://docs.tkcode.si/components/json-viewer.md): `JsonViewer`. A collapsible JSON tree with search, paging for long arrays, and copy value or path. Also called: JSON tree, object inspector.

### Activity

- [Timeline](https://docs.tkcode.si/components/timeline.md): `Timeline`. Follow what happened, newest first, grouped by day. Also called: activity feed, history, audit log, changelog list.
- [Comment thread](https://docs.tkcode.si/components/comment-thread.md): `CommentThread`. Threaded comments with replies, reactions, mentions, inline edit, and resolve. Also called: comments, discussion, replies.
- [Chat thread](https://docs.tkcode.si/components/chat-thread.md): `ChatThread`. A chat thread with grouped messages, reactions, read receipts, typing, and a composer with attachments. Also called: chat, messages, conversation view, message list.

### Media

- [Image compare](https://docs.tkcode.si/components/image-compare.md): `ImageCompare`. Drag a divider across two images to see what changed. Also called: before and after, image slider compare.
- [Carousel](https://docs.tkcode.si/components/carousel.md): `Carousel`. Browse a row of slides by dragging, flicking, or arrowing through them. Also called: slideshow, image gallery, slider of cards.
- [Card stack](https://docs.tkcode.si/components/card-stack.md): `CardStack`. Review a deck one card at a time, with a throw and an undo.
- [Page viewer](https://docs.tkcode.si/components/page-viewer.md): `PageViewer`. Zoom, pan and page through pictures or pages rendered on demand. Also called: document viewer, image viewer, zoom and pan, pdf pages, page reader.

### Text effects

Decorative: effects and showpieces. Use one only when the brief asks for that effect, not for ordinary controls.

- [Text reveal](https://docs.tkcode.si/components/text-reveal.md): `TextReveal`. A headline or short paragraph whose words rise in once when it appears on screen.
- [In-view title](https://docs.tkcode.si/components/in-view-title.md): `InViewTitle`. Bring a section title in as it scrolls into view.
- [Text morph](https://docs.tkcode.si/components/text-morph.md): `TextMorph`. Morph a label into its next state, letter by letter.
- [Text shimmer](https://docs.tkcode.si/components/text-shimmer.md): `TextShimmer`. Show ongoing work with a calm light across the words.

### Layout

- [Waterfall panel](https://docs.tkcode.si/components/waterfall-panel.md): `WaterfallPanel`. A masonry panel: cards of different heights flow into equal columns, each to the shortest one, so they pack without gaps.
- [Tile panel](https://docs.tkcode.si/components/tile-panel.md): `TilePanel`. A start-screen tile grid: tiles span rows and columns, take the first free place without overlapping, and resize with the panel.
- [Cyclic panel](https://docs.tkcode.si/components/cyclic-panel.md): `CyclicPanel`. A list that loops without a seam: after the last item comes the first, scrolled by wheel, touchpad or code, with the visible items reported.

### Extras

- [Markdown](https://docs.tkcode.si/components/markdown.md): `Markdown`. Render Markdown documents and streamed replies in Arc's style, with fenced code highlighted in Arc's syntax palette. Also called: Markdown viewer, Markdown renderer, streaming text.
- [Token text editor](https://docs.tkcode.si/components/token-text-editor.md): `TokenTextEditor`. A multi-line text editor whose tokens, such as commands and mentions, behave as single units, with triggers that open a query for your own list.
- [Cascader](https://docs.tkcode.si/components/cascader.md): `Cascader`. Pick one node of a hierarchy level by level, such as province, city and district, with a field and menu that match Select. Also called: cascading select, nested dropdown, region picker.
- [Transfer box](https://docs.tkcode.si/components/transfer-box.md): `TransferBox`. Move items between two lists, one by one, as a selection or all at once, with select all, search and the bound collections edited in place.
- [Property grid](https://docs.tkcode.si/components/property-grid.md): `PropertyGrid`. Edit the properties of one or more objects in rows of label and editor, grouped by category, with search, nested values and lists. Also called: property inspector, settings inspector, object editor.
- [Window](https://docs.tkcode.si/components/arc-window.md): `ArcWindow`. A desktop window with an Arc title bar: left, center and right areas for your controls, native dragging, snapping and resizing, and caption buttons that follow the theme. Also called: custom title bar window, chromeless window.
- [Number filter box](https://docs.tkcode.si/components/number-filter-box.md): `NumberFilterBox`. One number filter condition: a checkbox to switch it on, an operator shown as a symbol and a value, with an optional tolerance.
- [Text filter box](https://docs.tkcode.si/components/text-filter-box.md): `TextFilterBox`. One text filter condition: a checkbox to switch it on, contains, starts with, ends with, equals or a regular expression, and case sensitivity.
- [Enum selector](https://docs.tkcode.si/components/enum-selector.md): `EnumSelector`. Pick from the members of an enum: radio options for a plain enum, and checkboxes that combine flags for a [Flags] enum, described at compile time without reflection.
- [Fluid tab control](https://docs.tkcode.si/components/fluid-tab-control.md): `FluidTabControl`. Tabs beside one scroll area that holds every tab's content in order. Picking a tab scrolls to its section, and scrolling selects the tab of the section in view. Also called: scroll spy, scroll-linked tabs, anchor navigation, table of contents tabs, settings navigation.

### Loaders

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

- [Stretch refresh](https://docs.tkcode.si/components/stretch-refresh.md): `StretchRefresh`. A feed you pull down to refresh: the rows spread apart, a line stretches across the gap until it is time to let go, then runs while new items arrive.
- [Morph loader](https://docs.tkcode.si/components/morph-loader.md): `MorphLoader`. A tiny loader whose four strokes morph between dots, bars, a ring and a square, then fold into a check or a cross when the work finishes.
- [Skeleton morph](https://docs.tkcode.si/components/skeleton-morph.md): `SkeletonMorph`. Loading placeholders that stretch into the real content block by block, while the card eases to its new height.
- [Black hole](https://docs.tkcode.si/components/black-hole.md): `BlackHole`. A delete transition that spirals the chosen items into a small singularity while the rest of the list bends around it, and spins them back out on undo.
- [Charge session](https://docs.tkcode.si/components/charge-session.md): `ChargeSession`. A live charging card: energy flows from the charger along the cable into the battery, which fills toward a draggable limit while power, energy, time and cost update.

### Playful

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

Decorative: effects and showpieces. Use one only when the brief asks for that effect, not for ordinary controls.

- [Sticker peel](https://docs.tkcode.si/components/sticker-peel.md): `StickerPeel`. A surface of stickers with a real peel. Hovering lifts a corner, pulling folds the sticker back with its paper backing showing, and letting go sticks it down where you dropped it.
- [Page curl](https://docs.tkcode.si/components/page-curl.md): `PageCurl`. A small book that opens on its cover, with pages that fold along a line that follows the pointer.
- [Orbit logos](https://docs.tkcode.si/components/orbit-logos.md): `OrbitLogos`. An integrations orbit: your product mark in the centre, logos on two slowly turning rings, and a card that morphs out of the logo you pick.
- [Ticket tear](https://docs.tkcode.si/components/ticket-tear.md): `TicketTear`. An event ticket with a stub you tear off. It rips along the perforation one hole at a time, following your hand, and once free it falls away.

### Audio and video

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

- [Voice orb](https://docs.tkcode.si/components/voice-orb.md): `VoiceOrb`. A glowing voice-mode sphere that breathes at rest, ripples while it listens, swirls while it thinks and pulses as it speaks, with live captions and call controls.
- [Now playing](https://docs.tkcode.si/components/now-playing.md): `NowPlaying`. A mini player bar that grows into a full player in one continuous morph, with a waveform scrubber and artwork that slides the way you skip.
- [Voice recorder](https://docs.tkcode.si/components/voice-recorder.md): `VoiceRecorder`. A voice note composer: record into a live waveform, review and scrub the take, then send it into the thread as a voice bubble.

### Surfaces

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

- [Sheet stack](https://docs.tkcode.si/components/sheet-stack.md): `SheetStack`. Drill-down panels that rise over one another, keep the level below in view, drag away to close and turn into stacked dialogs on wide screens.
- [Wallet stack](https://docs.tkcode.si/components/wallet-stack.md): `WalletStack`. A wallet of payment cards in real materials: fan the stack, flick through it and lift a card out to see its balance and recent activity.
- [Control center](https://docs.tkcode.si/components/control-center.md): `ControlCenter`. A quick settings panel whose tiles grow into their details, with a focus-session dial and volume and text-size meters that stretch at their ends.
- [Holo card](https://docs.tkcode.si/components/holo-card.md): `HoloCard`. A collectible card drawn in perspective that follows the pointer with a shifting rainbow sheen, then turns over to reveal a second side.
- [Card to article](https://docs.tkcode.si/components/card-to-article.md): `CardToArticle`. Story cards that grow into full articles in one continuous morph and shrink back into their place when you pull them down.
- [Link unfurl](https://docs.tkcode.si/components/link-unfurl.md): `LinkUnfurl`. A message composer that turns a pasted address into a link, shows it loading, then unrolls it into a preview card you can fold back or remove.

### Cursor

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

Decorative: effects and showpieces. Use one only when the brief asks for that effect, not for ordinary controls.

- [Dot grid](https://docs.tkcode.si/components/dot-grid.md): `DotGrid`. A field of dots that swells away from the pointer like a lens and sends a ripple out from every click.
- [Gravity well](https://docs.tkcode.si/components/gravity-well.md): `GravityWell`. Tiles that lean toward the pointer, shrink and dim as if pulled into a well, with a click that rolls a shockwave through the grid.
- [Ink fluid](https://docs.tkcode.si/components/ink-fluid.md): `InkFluid`. A fluid simulation behind your content: the pointer stirs soft ink that swirls and fades, and a slow stir keeps it moving when nobody does.

### AI

Package: `TKCode.Arc.Special`, `UseArcSpecial()`.

- [Agent status](https://docs.tkcode.si/components/agent-status.md): `AgentStatus`. A live status pill for a running agent: its mark in a spinning ring, the current step and a timer, opening into the recent steps with pause, stop and approval controls. Also called: AI status, agent running indicator.
- [Tool call](https://docs.tkcode.si/components/tool-call.md): `ToolCall`. A tool call row for an agent transcript: the tool, what it did and how it went, opening into the call's input and output, with approval and retry in place. Also called: function call, tool invocation, tool use card.
- [Agent plan](https://docs.tkcode.si/components/agent-plan.md): `AgentPlan`. An agent's plan as an editable checklist: reorder, rename and add steps while it is a draft, then follow the run step by step once it is approved. Also called: task plan, todo list for agent.
- [Agent timeline](https://docs.tkcode.si/components/agent-timeline.md): `AgentTimeline`. A replay of a multi-agent run on one time axis: each agent's thinking, tool calls and waiting, the handoffs between them, and the time, tokens and cost.
- [Context window](https://docs.tkcode.si/components/context-window.md): `ContextWindow`. A model's context window as a grid of 200 cells that fills part by part, with a compaction threshold, a legend and actions that show what still fits. Also called: token usage, context meter.
- [Reasoning block](https://docs.tkcode.si/components/reasoning-block.md): `ReasoningBlock`. The collapsible thinking block of an AI reply: a card whose label counts the seconds while the model thinks, with the latest reasoning streaming under it, and the full reasoning as steps on a quiet rail once it has finished. Also called: thinking block, chain of thought, reasoning trace.
- [Source cards](https://docs.tkcode.si/components/source-cards.md): `SourceCards`. A row of cited sources under an AI answer: cards stream into reserved slots while the model searches, the overflow collapses into a stack of site marks, hovering lifts a preview with the passage, and an inline [n] citation highlights its card. Also called: citations, references, sources.
- [Model card](https://docs.tkcode.si/components/model-card.md): `ModelCard`. A model details card where switching models rolls every price, limit and score in place, a volume slider estimates the monthly cost, benchmark bars show each model's rank, and Compare crossfades to a side by side table against any rival. Also called: model info, model details.
- [AI workflow](https://docs.tkcode.si/components/ai-workflow.md): `AiWorkflow`. An automation card where a trigger, an AI step, a branch and actions sit on one rail, a test run glides down it opening each step's output, and steps are added from a compact command menu.
- [Code suggestion](https://docs.tkcode.si/components/code-suggestion.md): `CodeSuggestion`. An AI code edit suggestion where the proposed changes stream into a small editor panel as inline diff hunks, and the person accepts or rejects each one, or all at once with Cmd or Ctrl + Enter.
- [Ghost text](https://docs.tkcode.si/components/ghost-text.md): `GhostText`. An inline AI autocomplete where muted ghost text continues the sentence at the caret: Tab accepts it in place, Cmd+Right takes one word, Escape dismisses it, and a calm chip shows the model behind it. Also called: inline suggestion, autocomplete suggestion, copilot text.
- [Image generation](https://docs.tkcode.si/components/image-generation.md): `ImageGeneration`. An image generation card where four images resolve as a pixel mosaic, from coarse colour blocks to full detail, each with its own progress ring; pick one to upscale, vary or download.
- [Dictation button](https://docs.tkcode.si/components/dictation-button.md): `DictationButton`. A chat composer with voice input: the mic springs into a recording pill with a live waveform, a timer, cancel and Insert, and words stream into the field as you speak, muted until the recognizer settles them. Also called: voice input, speech to text, microphone button.
- [Speech player](https://docs.tkcode.si/components/speech-player.md): `SpeechPlayer`. A read-aloud player for AI replies: Read aloud turns into a compact player with play, a scrubbable progress bar, speed and a voice picker, and a highlight follows the spoken word through the reply. Also called: text to speech, audio playback.
- [Meeting summary](https://docs.tkcode.si/components/meeting-summary.md): `MeetingSummary`. AI notes for a call: the platform, attendees and length, then Summary, Decisions and Action items that stream in, each line linked to its transcript quote and each task ready to check off or send to a tracker.
- [Reply draft](https://docs.tkcode.si/components/reply-draft.md): `ReplyDraft`. An AI reply drafter under a received email: quick replies steer a fresh streaming draft, tone and length rewrite it with one crossfade, and the reader edits it before Insert.
- [Agent handoff](https://docs.tkcode.si/components/agent-handoff.md): `AgentHandoff`. A support chat where an AI assistant hands off to a person: a card shows the summary it passes to the team and the typical wait, then the person joins, with a thin timeline marking each step.
- [PII redaction](https://docs.tkcode.si/components/pii-redaction.md): `PiiRedaction`. A guardrail card where personal data is redacted before the prompt reaches the model, and the reply comes back with the original values put back.
- [Response feedback](https://docs.tkcode.si/components/response-feedback.md): `ResponseFeedback`. A feedback control for AI replies: thumbs down opens an inline panel with reason chips and a note, thumbs up asks what was good, and a submit shows a short thank-you before folding back into the row. Also called: thumbs up down, rate response.
- [LLM cost calculator](https://docs.tkcode.si/components/llm-cost-calculator.md): `LlmCostCalculator`. A pricing calculator for LLM workloads: set tokens per request, requests per day and prompt caching, and the models re-rank by monthly cost as bars glide and prices roll.
- [Provider keys](https://docs.tkcode.si/components/provider-keys.md): `ProviderKeys`. A settings card for bring-your-own-key AI products: each provider's masked key, status and last use, with reveal, copy, rotate and remove with undo, and an inline form that tests a new key before saving.
- [Generation stats](https://docs.tkcode.si/components/generation-stats.md): `GenerationStats`. A live stats strip for a streaming model response: time to first token, speed with a sparkline, output tokens and cost count up, then settle and open into a latency and token cost breakdown.
- [AI translate](https://docs.tkcode.si/components/ai-translate.md): `AiTranslate`. A message card where picking a language streams the AI translation in place, sentence by sentence over the dimmed original, with a detected-language chip, a Show original toggle and a status line.
- [TL;DR toggle](https://docs.tkcode.si/components/tldr-toggle.md): `TldrToggle`. An article card that streams an AI summary or key points in place of the article, with a length slider and key points that jump back to their paragraphs.
- [Connector setup](https://docs.tkcode.si/components/connector-setup.md): `ConnectorSetup`. A "Connect your tools" card for an AI assistant: each app connects inline through redirect, authorize and a first sync with a progress hairline, then lists the scopes it was granted and offers Disconnect with an inline confirmation.
- [Smart form fill](https://docs.tkcode.si/components/smart-form-fill.md): `SmartFormFill`. A paste-to-fill form card: AI reads an email signature or messy notes and fills a contact form field by field, with a confidence dot per value, its source lit up in the text, review for guesses and Undo.
- [Enrich column](https://docs.tkcode.si/components/enrich-column.md): `EnrichColumn`. An AI enrichment column for a data table, where one prompt runs down every row and each cell shimmers while it works, then settles into a value with its source or a failure you can retry.
- [Ticket router](https://docs.tkcode.si/components/ticket-router.md): `TicketRouter`. Support messages arrive one at a time, an AI classifier labels intent, sentiment and confidence, and each ticket travels into a team lane with an assignee or waits in a review lane for one click.
- [Ask your data](https://docs.tkcode.si/components/ask-your-data.md): `AskYourData`. A natural language to chart card: the question goes in, the SQL streams into a collapsible, syntax tinted panel, the query runs with a rolling row count, and the answer builds as stacked bars, lines or a table with a one line insight underneath.
- [Agent cursor](https://docs.tkcode.si/components/agent-cursor.md): `AgentCursor`. A live document where an AI agent edits like a collaborator: its labeled cursor selects, rewrites and fixes text while a teammate reads along, and the changes stay tinted until someone accepts or undoes them.
- [Provider status](https://docs.tkcode.si/components/provider-status.md): `ProviderStatus`. A status card for the AI providers your product depends on: health as an icon and label, median latency and a 30 day uptime bar with a tooltip per day, incidents that open into their updates, and a visible countdown to the next check.
- [Training run](https://docs.tkcode.si/components/training-run.md): `TrainingRun`. A fine-tuning job card where a live train and validation loss curve draws across a fixed step axis that doubles as the epoch progress bar, with the learning rate, elapsed time, ETA, recent events, pause and cancel, and results with Deploy.
- [Design to code](https://docs.tkcode.si/components/design-to-code.md): `DesignToCode`. A screenshot-to-code card: a scan line finds the layout regions of a dropped screenshot or design file frame, then code streams in beside it, linked region by region, with a live preview one tab away.
- [AI command](https://docs.tkcode.si/components/ai-command.md): `AiCommandPalette`. A Cmd+K command palette with an AI mode: search pages and actions, or press Tab to ask, and the answer streams in place with numbered sources and next steps that run from the list.
- [Starter prompts](https://docs.tkcode.si/components/starter-prompts.md): `StarterPrompts`. A greeting and starter cards for the empty state of an AI app, with category tabs, a shuffle that crossfades the cards, and a composer that a picked card fills. Also called: suggested prompts, prompt suggestions.
- [AI mascot](https://docs.tkcode.si/components/ai-mascot.md): `AiMascot`. A small assistant character that shows what the AI is doing: it breathes when idle, leans in to listen, watches its dot orbit while thinking, reads along as it writes, squints when done and shakes once on an error.
- [Image reveal](https://docs.tkcode.si/components/image-reveal.md): `ImageReveal`. An image generation canvas where a soft, blurred preview made from the image's own colors sharpens top to bottom behind a soft-edged band, with fine film grain, a translucent progress pill and calm queued and error states.
- [AI spark](https://docs.tkcode.si/components/ai-spark.md): `AiSpark`. An animated spark mark for AI status: four tapered rays and four lighter diagonals breathe at rest, ripple while thinking, pulse like a heartbeat while writing, then gather and settle when done.
- [Sketch to image](https://docs.tkcode.si/components/sketch-to-image.md): `SketchToImage`. An image generation visual that paints the way an artist does: pencil strokes sketch the scene, a round brush blocks in flat colors region by region, grain and warm light refine it, and it resolves into the finished picture.
- [Building blocks](https://docs.tkcode.si/components/building-blocks.md): `BuildingBlocks`. A loading visual for AI app builders: rounded blocks snap into a tiny app layout with no bounce, fill with content and pick up the accent while the label steps from Laying out to Ready.
- [Pixel mascot](https://docs.tkcode.si/components/pixel-mascot.md): `PixelMascot`. A pixel-art robot that shows what an AI assistant is doing. It idles, thinks, types, celebrates and errors in frame-stepped sprite animations, and stays crisp at every integer size from 24px to 192px.
- [Ghost mascot](https://docs.tkcode.si/components/ghost-mascot.md): `GhostMascot`. A floating ghost mascot whose hem ripples while it hovers: it blinks when idle, listens, thinks under a swirl, writes with a tiny pen, sparkles when done and wobbles on an error.
- [Cube mascot](https://docs.tkcode.si/components/cube-mascot.md): `CubeMascot`. An isometric cube mascot whose shaded faces and painted-on face show what the assistant is doing: it turns in eased quarter turns while thinking, prints lines onto the floor while writing, settles with a sparkle when done and shakes once on error.
- [Tone tuner](https://docs.tkcode.si/components/tone-tuner.md): `ToneTuner`. A brand voice card where two sliders (formality and detail) and voice chips rewrite a customer reply: each changed sentence crossfades in place, new words can be highlighted against the original, and Copy takes the current version.
- [Upgrade prompt](https://docs.tkcode.si/components/upgrade-prompt.md): `UpgradePrompt`. An upgrade nudge for the moment someone hits a limit, with a usage meter, a rolling price with a monthly and yearly toggle, three concrete unlocks and a success state, in a card that never changes size.
- [Referral card](https://docs.tkcode.si/components/referral-card.md): `ReferralCard`. A give-and-get referral card where people copy or share their invite link, invite friends by email as chips, and watch invites move from invited to signed up to earned, with the credit total counting up and a milestone reward unlocking.
- [Trial banner](https://docs.tkcode.si/components/trial-banner.md): `TrialBanner`. A trial status banner where a ring counts down the days left, the copy names what the workspace loses, and the upgrade path is one click away, as a slim banner, a sidebar card or a header pill.
- [Team switcher](https://docs.tkcode.si/components/team-switcher.md): `TeamSwitcher`. A workspace switcher: the trigger shows the current workspace's logo and plan, and the menu searches your workspaces, shows your role and a Cmd/Ctrl+number shortcut for each, and creates or joins one inline. Also called: workspace switcher, organization switcher.
- [Live visitors](https://docs.tkcode.si/components/live-visitors.md): `LiveVisitors`. A realtime visitors card where the live count rolls digit by digit, new arrivals ping in on a dotted world map, the top pages reorder with bars, and signups and upgrades tick in.
- [Social proof toast](https://docs.tkcode.si/components/social-proof-toast.md): `SocialProofToast`. A stack of social proof notifications for landing and pricing pages, where recent trials, upgrades and verified purchases from people and companies arrive every few seconds; it pauses and fans out on hover, and visitors can dismiss any toast.
- [Install snippet](https://docs.tkcode.si/components/install-snippet.md): `InstallSnippet`. A quickstart card where framework tabs, three numbered steps and highlighted snippets take a developer from install to the first event, and Generate key wipes a fresh key into place.

### Illustrations

Decorative: effects and showpieces. Use one only when the brief asks for that effect, not for ordinary controls.

- [Data flow](https://docs.tkcode.si/components/data-flow.md): `DataFlow`. Events stream from their sources through a queue and a batch transform into a warehouse and live dashboards.
- [Payment flow](https://docs.tkcode.si/components/payment-flow.md): `PaymentFlow`. A card payment from checkout through tokenization, authorization and the issuer's decision to settlement.
- [Multi-region failover](https://docs.tkcode.si/components/multi-region-failover.md): `MultiRegionFailover`. A global load balancer shifts traffic to a standby region and promotes its database when the primary region goes down.
- [Vector search](https://docs.tkcode.si/components/vector-search.md): `VectorSearch`. A question is embedded as a vector and answered with its nearest documents by cosine similarity.
- [Agent loop](https://docs.tkcode.si/components/agent-loop.md) (`TKCode.Arc.Special`, `UseArcSpecial()`): `AgentLoop`. An agent gathers context, calls tools and checks the result, going round the loop until the task is done.
- [Mixture of experts](https://docs.tkcode.si/components/mixture-of-experts.md) (`TKCode.Arc.Special`, `UseArcSpecial()`): `MixtureOfExperts`. A router scores every expert for each token and sends it to its top choices, whose outputs blend into the result.
- [Speculative decoding](https://docs.tkcode.si/components/speculative-decoding.md) (`TKCode.Arc.Special`, `UseArcSpecial()`): `SpeculativeDecoding`. A small draft model guesses a few tokens ahead and the large model checks them in one pass, keeping the ones it agrees with.
- [Prompt cache](https://docs.tkcode.si/components/prompt-cache.md) (`TKCode.Arc.Special`, `UseArcSpecial()`): `PromptCache`. Requests that share a prompt prefix read it from the cache instead of processing it again, and the savings add up.
- [Reasoning tree](https://docs.tkcode.si/components/reasoning-tree.md) (`TKCode.Arc.Special`, `UseArcSpecial()`): `ReasoningTree`. A search over candidate thoughts: each is scored, weak branches are pruned, the best expand and the winning path draws in.

### Mobile

Package: `TKCode.Arc.Mobile`, `UseArcMobile()`.

XAML namespace: `xmlns:arcm="https://github.com/AIDotNet/TKCode.Arc.Mobile"`. The controls are `TKCode.Arc.Mobile.Button`, `.Page` and so on, written `arcm:Button`; the API tables call them `MobileButton` so they do not clash with the desktop names.

- [Mobile page](https://docs.tkcode.si/components/mobile-page.md): `arcm:Page`. A phone screen: a header, the content and a footer laid out inside the device's safe area.
- [Mobile navbar](https://docs.tkcode.si/components/mobile-navbar.md): `arcm:Navbar`. The 44 px top bar of a page: a leading slot, a centered title and a trailing slot for actions. Also called: navigation bar, app bar, top bar.
- [Mobile toolbar](https://docs.tkcode.si/components/mobile-toolbar.md): `arcm:Toolbar`. The 44 px bottom action bar of a page: plain actions that share the width, or leading, center and trailing slots.
- [Mobile tabbar](https://docs.tkcode.si/components/mobile-tabbar.md): `arcm:Tabbar`. The bottom tab bar of a page: icon-over-label tabs that share the width, with the selected one highlighted. Also called: bottom navigation, tab bar, bottom tabs.
- [Mobile navigation stack](https://docs.tkcode.si/components/mobile-navigation-stack.md): `arcm:NavigationStack`. A stack of pages: a pushed page slides in over the one below, and a swipe from the left edge or a back button slides it away. Also called: navigation stack, page stack, push navigation.
- [Mobile list](https://docs.tkcode.si/components/mobile-list.md): `arcm:ListGroup`. Grouped rows for settings and menus: icon, title, subtitle, a value or control at the end and a chevron. Also called: grouped list, table view, settings list.
- [Mobile pull to refresh](https://docs.tkcode.si/components/mobile-pull-to-refresh.md): `arcm:PullToRefresh`. Pull scrollable content down at its top to refresh it, with rubber-band resistance, a spinner and a spring back. Also called: pull to refresh, refresh control.
- [Mobile dialog](https://docs.tkcode.si/components/mobile-dialog.md): `arcm:Dialog`. A centered alert or confirmation dialog with a title, a message and up to three actions, over a scrim.
- [Mobile toast](https://docs.tkcode.si/components/mobile-toast.md): `arcm:Toast`. A transient message that slides up near the bottom, leaves by itself and can be swiped away.
- [Mobile sheet](https://docs.tkcode.si/components/mobile-sheet.md): `arcm:Sheet`. A bottom sheet that rises over a page with a scrim and can rest at several heights, closed by tapping the scrim, pulling the handle down or flicking it.
- [Mobile action sheet](https://docs.tkcode.si/components/mobile-action-sheet.md): `arcm:ActionSheet`. A bottom list of actions with an optional destructive row and a separate Cancel row, closed by choosing a row, tapping the scrim or pulling it down. Also called: action sheet, bottom menu.
- [Mobile button](https://docs.tkcode.si/components/mobile-button.md): `arcm:Button`. A touch-first button with a press state instead of a hover state and a 44 px minimum touch target.
- [Mobile switch](https://docs.tkcode.si/components/mobile-switch.md): `arcm:Switch`. A 51 by 31 toggle for settings that take effect at once, with a spring thumb and a 44 px hit area.
- [Mobile text field](https://docs.tkcode.si/components/mobile-text-field.md): `arcm:TextField`. A 44 px filled text field with an optional icon and a clear button, whose border takes the accent color while focused. Also called: mobile text input.
- [Mobile search bar](https://docs.tkcode.si/components/mobile-searchbar.md): `arcm:Searchbar`. A pill-shaped text field with a search glyph, a clear button and a Search key that raises Submitted. Also called: mobile search bar.
- [Mobile checkbox](https://docs.tkcode.si/components/mobile-checkbox.md): `arcm:Checkbox`. A 22 px rounded checkbox in a 44 px row, with a check, an indeterminate dash and no hover state.
- [Mobile radio](https://docs.tkcode.si/components/mobile-radio.md): `arcm:Radio`. A 22 px radio in a 44 px row whose accent dot springs in, one choice per group, with arrow-key navigation and no hover state.
- [Mobile icon button](https://docs.tkcode.si/components/mobile-icon-button.md): `arcm:IconButton`. A 44 by 44 icon-only button with plain, tinted, filled and floating variants and a spring press state.
- [Mobile segmented control](https://docs.tkcode.si/components/mobile-segmented.md): `arcm:Segmented`. A short row of segments that share the width, one selected, for switching between views of the same content. Also called: mobile segmented control.

## Blocks

### Analytics

- [Metrics dashboard](https://docs.tkcode.si/blocks/metrics-dashboard.md): `MetricsDashboard`. A one-screen analytics overview with a range switch, metric tabs that count to their totals, an inspectable area chart that can overlay the previous period, and two ranked lists. Also called: dashboard, analytics overview, KPI dashboard.
- [KPI drilldown](https://docs.tkcode.si/blocks/kpi-drilldown.md): `KpiDrilldown`. KPI cards with sparklines and period changes that grow in place into a detail view with a trend chart, a prior-period comparison, a readout you can drive by keyboard and a breakdown by dimension.
- [Metric explorer](https://docs.tkcode.si/blocks/metric-explorer.md): `MetricExplorer`. A row of KPI cards that each open into a chart you can inspect by dragging: the card's surface grows into the panel, the tabs morph the line from metric to metric point by point, the range switch moves a camera window over the daily data, and this period is drawn against the one before it.
- [Revenue explorer](https://docs.tkcode.si/blocks/revenue-explorer.md): `RevenueExplorer`. Monthly recurring revenue as stacked new, expansion and churn layers with three headline numbers and an overview strip: drag its window or handles and the paths, axes and numbers slide to the new range, with range presets, series chips, a previous-period comparison and a crosshair readout for the pointer and the keyboard.
- [MRR waterfall](https://docs.tkcode.si/blocks/mrr-waterfall.md): `MrrWaterfall`. A recurring-revenue bridge from the start of a period to its end: floating bars for new business, expansion, reactivation, contraction and churn, a side panel that lists the accounts behind any bar, and period and unit switches that move every bar, tick and number on one spring.
- [Cohort retention](https://docs.tkcode.si/blocks/cohort-retention.md): `CohortRetention`. How many of each starting group stayed, month by month: a heatmap triangle that morphs into retention curves, headline numbers that compare the newest cohorts with the oldest, segments that repaint every cell, and a cell cursor for the pointer and the keyboard.
- [Usage forecast](https://docs.tkcode.si/blocks/usage-forecast.md): `UsageForecast`. A billing cycle against a budget: measured usage and a dashed forecast with its likely range, a budget line you drag or step with the keys while the headline, tiles, red overage and alert chips react, and a metric switch that morphs every path.
- [Journey flow](https://docs.tkcode.si/blocks/journey-flow.md): `JourneyFlow`. A journey as a Sankey chart: point at a step or a flow to trace that cohort through every stage, select a step to see where its people came from and went next, and switch segments to watch every flow morph.
- [Uptime status](https://docs.tkcode.si/blocks/uptime-status.md): `UptimeStatus`. A status page for a set of services: 90 days of daily bars with uptime figures and a hover card per day, a response-time chart with p50 and p95 curves against a target for the chosen service, and an incident timeline, all derived from the services' samples and the incidents, with keyboard paging by a week.
- [Choropleth explorer](https://docs.tkcode.si/blocks/choropleth-explorer.md): `ChoroplethExplorer`. A map shaded by a metric in quantile steps with a linked ranked list; choose a region to fly into it and rank its counties, switch the metric to sweep new colours across the map, or change the country.
- [Connected scatter](https://docs.tkcode.si/blocks/connected-scatter.md): `ConnectedScatter`. Two measures that move together, drawn as one path: a head point you scrub or play along the months, readouts that count with it, a target line that tints the healthy side, and a switch that morphs the whole path from one series to another.
- [Horizon chart](https://docs.tkcode.si/blocks/horizon-chart.md): `HorizonChart`. Many daily series compared by how far each strays from its own baseline, folded into one thin band row each: the band count folds and unfolds every row on one spring, rows re-sort by name, peak or latest value, and one crosshair reads the same day across every row for the pointer, touch and the keyboard.

### Finance

- [Market terminal](https://docs.tkcode.si/blocks/market-terminal.md): `MarketTerminal`. A trading screen with a candlestick chart you zoom and pan, a live watchlist with sparklines and flashing prices, and a market and limit order ticket with its order list.
- [Budget variance](https://docs.tkcode.si/blocks/budget-variance.md): `BudgetVariance`. Budget against actual spend by department as bullet charts on one shared scale, with signed variances, a period switch that springs every bar, budget lines that open in place and a total row.

### Line charts

- [Small multiples](https://docs.tkcode.si/blocks/small-multiples.md): `SmallMultiples`. A grid of small line charts, one per group, read as a single picture: one crosshair moves through every panel, the y axes switch between each group's own range and a common one, panels re-sort by value, growth or name, and flat groups can be faded.
- [Line replay](https://docs.tkcode.si/blocks/line-replay.md): `LineReplay`. Watch a day of data unfold: the line draws itself as a clock runs, events you mark pop up on the chart when it reaches them, and you can pause, drag to any moment or change the speed.

### AI

- [Model picker](https://docs.tkcode.si/blocks/model-picker.md): `ModelPicker`. Choose a model and its reasoning effort from one compact pill: featured models with a note each, an effort setting, every other model by provider, and a summary of what the choice costs. Also called: model selector, LLM picker.
- [AI side panel](https://docs.tkcode.si/blocks/ai-side-panel.md): `AiSidePanel`. An assistant beside a page the person reads: it reads the whole page, answers about it or about a selection, and proposes changes as a word diff that the person applies, discards or undoes, with a sweep on the page and an Undo pill. Also called: copilot panel, assistant sidebar.
- [Prompt studio](https://docs.tkcode.si/blocks/prompt-studio.md): `PromptStudio`. A card to edit a prompt template with colored variable chips, try it on saved test cases, and compare and ship versions: version and model menus, variable fields, a Run button and an output panel for the reply.
- [Document extraction](https://docs.tkcode.si/blocks/document-extraction.md): `DocumentExtraction`. Turns a document into typed data and shows where each value came from: a quiet document viewer beside an editable form whose fields fill in as the model reads, with tinted source text, curved links, low-confidence review and a typed JSON result.
- [Model arena](https://docs.tkcode.si/blocks/model-arena.md): `ModelArena`. A blind test of two models: one prompt goes to two anonymous models, both answers stream in at the same pace, and the models are revealed after the vote, with time to first token, speed and cost per answer and a standings list.
- [AI answer](https://docs.tkcode.si/blocks/ai-answer.md): `AiAnswer`. A page that answers one question like an article: reasoning, source cards, a streamed answer with headings, lists, a table, code and numbered citations that light the sentence they support, then actions, related questions and a follow-up box.
- [AI chat](https://docs.tkcode.si/blocks/ai-chat.md): `AiChat`. A chat with an assistant that fills its container: a title menu, a model button and a new chat button on top, a thread with streamed Markdown replies, collapsible reasoning, tool chips, numbered citations and message actions, and a message box at the bottom. Also called: chatbot, assistant chat, chat UI.
- [AI composer](https://docs.tkcode.si/blocks/ai-composer.md): `AiComposer`. The prompt box of an AI app: a text area that grows, slash commands and @ mentions that become tokens, attachments that upload in place, tool toggles, a model and effort menu, a character counter and a send button that becomes a stop button. Also called: prompt input, chat input, message composer.
- [Agent memory](https://docs.tkcode.si/blocks/agent-memory.md): `AgentMemory`. A card that shows what an assistant remembers: filter by group, search, edit a sentence in place, forget with a few seconds to undo, file a just-noted memory and add one by hand.

### App shell

- [Sidebar](https://docs.tkcode.si/blocks/sidebar.md): `Sidebar`. Workspace navigation with sections, nested pages, counts and a gliding selection marker; collapses to icons. Also called: navigation pane, side navigation, nav drawer, navigation view, app sidebar.
- [Page header](https://docs.tkcode.si/blocks/page-header.md): `PageHeader`. A project page header that folds into a compact bar as you scroll, with tabs whose counts roll. Also called: page title bar, header with actions.
- [Command palette](https://docs.tkcode.si/blocks/command-palette.md): `CommandPalette`. A complete keyboard driven action surface with search, grouped results, and shortcuts. Also called: command bar, quick open, spotlight, Cmd+K menu, launcher.
- [Notification center](https://docs.tkcode.si/blocks/notification-center.md): `NotificationCenter`. A home for updates with read state, grouped information, and animated disclosure. Also called: notification inbox, notifications panel, bell menu.
- [Empty states](https://docs.tkcode.si/blocks/empty-states.md): `EmptyStates`. Four empty states in one illustration whose shapes morph between scenes as you switch tabs.
- [Search results](https://docs.tkcode.si/blocks/search-results.md): `SearchResults`. A workspace search page with a suggestions popup, filters whose counts follow the query, a date range, sorting, removable filter chips, highlighted results and a detail sheet. Also called: search page, results list.

### Auth

- [Login](https://docs.tkcode.si/blocks/login.md): `Login`. Sign in and sign up pages, from a compact code card to full screen split and immersive layouts. Also called: sign in, log in page, authentication form.
- [Onboarding illustration](https://docs.tkcode.si/blocks/onboarding-illustration.md): `OnboardingIllustration`. A compact first-run card whose line drawing morphs from an empty box to files, a team and a badge as people step through it.
- [OTP input](https://docs.tkcode.si/blocks/otp-input.md): `OtpInput`. A six digit verification flow with paste support and keyboard navigation. Also called: verification code, one-time code, PIN input, 2FA code.
- [Multi-step form](https://docs.tkcode.si/blocks/multi-step-form.md): `MultiStepForm`. A short form asked one step at a time in a card, with a progress list, Back and Continue, checks before each step, and a success state at the end. Also called: wizard, multi-page form, onboarding form.

### Settings

- [Settings page](https://docs.tkcode.si/blocks/settings-page.md): `SettingsPage`. A full settings page: section navigation with an account card, the chosen section beside it, and a save bar for unsaved changes. Also called: preferences, options page, settings screen.
- [Settings command](https://docs.tkcode.si/blocks/settings-command.md): `SettingsCommand`. Find any setting from one search field and change it in place: toggles flip, choices cycle and links open, all from the keyboard. Also called: settings search, find a setting.
- [Security settings](https://docs.tkcode.si/blocks/security-settings.md): `SecuritySettings`. A security checkup, password change, two-step verification and signed-in sessions, each handled in place. Also called: two-factor settings, sessions, password settings.
- [Billing overview](https://docs.tkcode.si/blocks/billing-overview.md): `BillingOverview`. The current plan, next charge, payment method and invoice history on one page, with the actions next to each. Also called: billing page, subscription, plan and invoices.
- [Usage billing](https://docs.tkcode.si/blocks/usage-billing.md): `UsageBilling`. A metered billing period: spend so far, a projected total, daily spend, overage per meter and a spending limit. Also called: metered billing, usage charges.
- [Cancel flow](https://docs.tkcode.si/blocks/cancel-flow.md): `CancelFlow`. A considerate cancellation: ask why, offer a fitting alternative, show what ends, hold to confirm, and allow a quick return. Also called: cancel subscription, churn flow.

### People

- [Team members](https://docs.tkcode.si/blocks/team-members.md): `TeamMembers`. The members page of a workspace: invite by typing or pasting addresses that fly into pending invites as the seat count rolls, change roles, search, and remove people with an inline confirm. Also called: members list, user management.
- [Invite people](https://docs.tkcode.si/blocks/invite-people.md): `InvitePeople`. Invite people by name or email into a workspace, with a role for each, a seat meter against the plan, and avatars that fly from the suggestions into chips and on into a pending list. Also called: invite users, share dialog.
- [Team directory](https://docs.tkcode.si/blocks/team-directory.md): `TeamDirectory`. A company directory with search, team filters and the picked person's profile beside the list, from bio and status to an email address to copy.
- [Roles and permissions](https://docs.tkcode.si/blocks/roles-permissions.md): `RolesPermissions`. A matrix of roles and permissions with group checkboxes that ripple, locked owner access, role copies and a review bar that saves every change at once. Also called: access control, RBAC, permissions matrix.
- [Profile page](https://docs.tkcode.si/blocks/profile-page.md): `ProfilePage`. A person's profile as one page: a cover and avatar, an about column with stats, Activity and Projects tabs, and an edit sheet whose saved fields glow once. Also called: account page, user profile.

### Developer

- [API keys](https://docs.tkcode.si/blocks/api-keys.md): `ApiKeys`. API key management where a Create key pill unfolds into a form, the new secret is shown once, rows open into usage details and a hold revokes a key. Also called: API tokens, access keys.
- [Webhooks](https://docs.tkcode.si/blocks/webhooks.md): `Webhooks`. A webhooks console where you check each endpoint's health and traffic, inspect what was sent and what came back, retry failed deliveries in place and add endpoints with a one-time signing secret.
- [Integrations](https://docs.tkcode.si/blocks/integrations.md): `Integrations`. A directory of apps to connect, with search, counted category filters, a connect pill that spins, connects or offers a retry, and a drawer for permissions, sync frequency and hold to disconnect.
- [Env variables](https://docs.tkcode.si/blocks/env-variables.md): `EnvVariables`. A project's environment variables as a matrix of switches per environment, with masked values, inline editing, .env import, a fingerprint comparison and a redeploy banner. Also called: environment variables, secrets editor.
- [Domain verify](https://docs.tkcode.si/blocks/domain-verify.md): `DomainVerify`. A custom domain setup card where customers add a domain and copy the DNS records, then a live checker marks each record valid, not found or misconfigured with a one-line fix and finishes with the domain verified and its SSL certificate issued.
- [Webhook tester](https://docs.tkcode.si/blocks/webhook-tester.md): `WebhookTester`. A webhook tester where you pick an event, edit its JSON payload and send a signed test event, watch it go from signed to sent to the endpoint's answer, read the response and retry failed deliveries from a recent list.

### Pricing

- [Plan comparison](https://docs.tkcode.si/blocks/plan-comparison.md): `PlanComparison`. Plan cards with a monthly or yearly switch that rolls every price to the new period, a highlighted plan, and a feature matrix that shows what each tier adds.
- [Pricing calculator](https://docs.tkcode.si/blocks/pricing-calculator.md): `PricingCalculator`. Plan cards priced by a seat slider and a billing switch: prices roll digit by digit as you drag, and the recommended badge glides to the plan that fits the team.
- [Usage pricing](https://docs.tkcode.si/blocks/usage-pricing.md): `UsagePricing`. A pricing calculator that picks the plan for you: drag seats and monthly traffic and the plan name morphs, the estimate counts to the new total and the call to action follows.
- [Cart drawer](https://docs.tkcode.si/blocks/cart-drawer.md): `CartDrawer`. A product grid whose items fly into the cart button, a count that rolls, and a side drawer with quantity steppers, a free-shipping progress bar, a rolling subtotal and checkout.
- [Checkout summary](https://docs.tkcode.si/blocks/checkout-summary.md): `CheckoutSummary`. An order summary where an accepted promo code turns into a removable chip, discount and tax lines slide in, totals roll to the new amount and the pay button walks through processing to paid.
- [Live checkout](https://docs.tkcode.si/blocks/checkout-live.md): `LiveCheckout`. A subscription checkout whose summary recalculates with every change to plan, billing period, add-ons and details, then moves to the centre and unfolds into a receipt.
- [Seat manager](https://docs.tkcode.si/blocks/seat-manager.md): `SeatManager`. A team seats card with an avatar row and a seat meter for members, invites and open seats, a stepper that rolls today's prorated charge and the new monthly total, a confirm step before any charge, invites you can resend or revoke with undo, and a full-team state that offers another seat.
- [Credit top-up](https://docs.tkcode.si/blocks/credit-topup.md): `CreditTopUp`. A card for buying usage credits: the balance with a two-week usage chart and days left, preset packs with bonus badges, a custom amount slider with a live price, auto top-up with a threshold, the saved payment card, and a new balance that rolls in like an odometer.

### Media

- [File upload](https://docs.tkcode.si/blocks/file-upload.md): `FileUpload`. A complete file selection flow with constraints, progress, and error feedback. Also called: upload list, attachments, file uploader.
- [Media player](https://docs.tkcode.si/blocks/media-player.md): `MediaPlayer`. An audio player with cover art, a playlist, a seek bar, previous and next, and volume with mute; skipping wraps around and keeps playing or paused, and the playing row shows moving bars. Also called: video player, audio player.

### Projects

- [Week calendar](https://docs.tkcode.si/blocks/week-calendar.md): `WeekCalendar`. A week planner where you drag to add, move and resize events and undo the last change, with a month and calendar toggles beside the grid, an all-day row, and three-day or agenda views when narrow. Also called: scheduler, week view, agenda.
- [Availability picker](https://docs.tkcode.si/blocks/availability-picker.md): `AvailabilityPicker`. Find a meeting time across several people's calendars, see who is free at each time, and book the one that works. Also called: booking slots, time slot picker, appointment picker.
- [Project board](https://docs.tkcode.si/blocks/project-board.md): `ProjectBoard`. A compact sprint board: arrow buttons step each card to the next or previous stage and it flies to its new column, under a sprint line, the team and an All work / My work switch. Also called: project overview, board view.
- [Gantt timeline](https://docs.tkcode.si/blocks/gantt-timeline.md): `GanttTimeline`. A project timeline of phases and tasks on a calendar: drag bars to move or resize them, dependency arrows that turn red on conflicts, day, week and month zoom, folding phases and undo. Also called: Gantt chart, project timeline, schedule chart.
- [Release readiness](https://docs.tkcode.si/blocks/release-readiness.md): `ReleaseReadiness`. A release gate where a short list of checks unlocks one action, with a progress bar, the owners, a notify switch and the result in place.
- [Kanban board](https://docs.tkcode.si/blocks/kanban-board.md): `KanbanBoard`. A team board whose cards lift, tilt with the pointer and spring into the slot that opens for them, with swimlanes by person, live work-in-progress limits, quick add and a details drawer. Also called: kanban, task board, card board.

### Headers and footers

- [Site header](https://docs.tkcode.si/blocks/site-header.md): `SiteHeader`. A sticky website header that turns solid on scroll, with a gliding active link, mega menu panels, and a mobile sheet. Also called: website header, navbar, top navigation.
- [Site footer](https://docs.tkcode.si/blocks/site-footer.md): `SiteFooter`. A website footer with link columns and newsletter, a minimal layout, and a large fading Arc mark. Also called: website footer.
- [CTA section](https://docs.tkcode.si/blocks/cta-section.md): `CtaSection`. A call to action as a centered closing section, a split beside a setup card that completes itself, or a dismissible banner.
- [Newsletter signup](https://docs.tkcode.si/blocks/newsletter-signup.md): `NewsletterSignup`. An email signup framed by a stack of past issues; subscribing drops the next issue, addressed to you, onto the front. Also called: email signup, subscribe form.

### Showpieces

Decorative: effects and showpieces. Use one only when the brief asks for that effect, not for ordinary controls.

- [Activity terrain](https://docs.tkcode.si/blocks/activity-terrain.md): `ActivityTerrain`. A week of hourly activity as a 3D terrain: orbit and zoom it, hover an hour for its numbers, fly to a bar, press it flat into a heat map, and read the total, the peak hours and the busiest day beside it.
- [Revenue globe](https://docs.tkcode.si/blocks/revenue-globe.md): `RevenueGlobe`. A live payments map: arcs fly from every city to headquarters on a dotted globe, with a day timeline to replay or scrub and the revenue, regions and top cities beside it.
- [Customer galaxy](https://docs.tkcode.si/blocks/customer-galaxy.md): `CustomerGalaxy`. Every customer as a dot sized by revenue and colored by health, turning as a galaxy; group them into clusters, fly in, and read the totals.
- [Semantic zoom](https://docs.tkcode.si/blocks/semantic-zoom.md): `SemanticZoom`. An organization map you zoom through from teams to projects to a single task, each level showing new detail, with breadcrumbs, a minimap, a guided tour and the numbers of whatever is in view.
- [Layout morph](https://docs.tkcode.si/blocks/layout-morph.md): `LayoutMorph`. One set of cards in six views, from a grid and a list to a fanned stack, a 3D ring and a helix, with every card flying to its new place.
- [Exploded view](https://docs.tkcode.si/blocks/exploded-view.md): `ExplodedView`. A screen that comes apart as you scroll: its layers tip back, separate with labels, take turns in focus with a caption each, and close into one screen again.
