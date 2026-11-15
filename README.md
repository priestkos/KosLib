# KosLib

A small shared ImGui style library for the PriestKos Dalamud plugins: cards, section headers, badges, joined toggles and
optional window presets (among them **KosMos**, after the K'os Mos profile site), with the user's own Dalamud theme left in charge.

`source-included` · Dalamud API 15 · .NET 10 · AGPL-3.0

> **No support is offered.** KosLib exists for the PriestKos plugin family; it is public so the plugins' release builds can check it out.

## What it does

- **`Kos.Card(id, edge, body)`**: a full-width framed panel round whatever `body` draws, with an optional coloured edge.
  The frame is drawn on a background draw-list channel after the body is laid out. A card inside a card draws its body
  without a second frame instead of nesting the channel split (which ImGui asserts on).
- **`Kos.Badge(text, color)`**: a pill with dark text on a solid semantic colour, one text line tall so `SameLine` text
  stays aligned.
- **`Kos.Section(title, number?)`**: a section header, a drawn diamond, `01 / TITLE` in capitals and a rule to the
  right edge. It uses the monospaced font when the host sets `Kos.Mono = UiBuilder.MonoFontHandle`.
- **`Kos.Segmented(id, ref index, options)`**: 2–4 joined toggle buttons for one choice, in place of a radio-button row.
- **`Kos.KeyValue(label, value)`**: a read-out row, muted label on the left and the value right-aligned.
- **`Kos.Callout(text, color)`**: wrapped text with a coloured bar down its left edge, for alerts and notes.
- **`Kos.Dot(color)`** and **`Kos.Pips(filled, total, color)`**: drawn status marks (a draw-list overload of `Pips`
  serves custom widgets).
- **`Kos.IconButton`** / **`Kos.PrimaryButton`**: a Font Awesome button whose tooltip also shows while it's disabled,
  and the accent-filled main action of a view.
- **`Kos.HelpMarker(text)`**, **`Kos.EmptyState(text)`**: a `(?)` with a wrapped tooltip; centred muted text for an empty list.
- **`KosTheme.Push(accent)`**: a preset for one window. `using var _ = KosTheme.Push(config.Accent);` at the top of
  `Draw()` pushes it and pops it at the end of the scope. **Dalamud style** (the default) pushes nothing. Five accents
  (Teal, Violet, Amber, Crimson, Ocean) colour the controls and give KosLib's own components an accent edge. **KosMos**
  is the signature look, taken from the profile site's stylesheet: crimson controls, foxfire-gold checkmarks and section
  tags, a crimson-to-gold line along the top of each card, night-blue card fills, a glow on the primary button, and
  6 px frame rounding.
- **`KosTheme.PushWindow(accent)`** (opt-in): the same preset applied to the **whole window**: background, title bar,
  text, borders, separators, scrollbars, popups and table colours all come from the accent, plus softer window, child,
  popup and frame rounding. It has to be pushed before ImGui begins the window, so the host calls it in `PreDraw()` and
  disposes the scope in `PostDraw()` (snippet below). The window stays light or dark like the user's theme and keeps its
  background alpha; Dalamud style still pushes nothing.
- **MaterialKos** (opt-in): a Material Design 3 look from the same accent. `KosTheme.PushWindow(accent, material: true)`
  colours the window with M3 tonal roles (surfaces, containers, outline, state layers) and rounder windows, light or dark
  like the user's theme. `MaterialKos.Button` (filled, tonal, outlined, text; optional leading Font Awesome icon), `MaterialKos.Switch` and `MaterialKos.Chip`
  are the M3 controls, with two lively ones: `MaterialKos.GlowButton` (its edge catches a light that sits nearest the mouse
  and travels slowly round at rest; `MaterialKos.Motion = false` stops the travel) and `MaterialKos.Slide` (slide to
  confirm, for actions that must not happen by accident: drag the knob across, or hold Space/Enter on it for a second); they take their colours from whatever is pushed, so they also work under a plain accent or
  Dalamud style. Tonal palettes use CIELAB LCh rather than M3's HCT, so hues can drift slightly from Google's tools.
- **`KosGallery.Draw()`**: every component in every state and every preset, for the in-game visual check. The host plugin
  calls it from its own debug section.

### What it is not meant to do

- Replace the user's theme unasked: by default a preset changes only `Button*`, `Tab*`, `Header*`, `CheckMark`,
  `SliderGrab*` and `FrameBg*` (and, for KosMos only, frame/grab/tab rounding), never `WindowBg` or `Text`, and only
  inside the window that pushes it. The whole-window palette is a separate call a plugin offers as a setting, off by
  default. KosMos's night card fill is drawn by `Kos.Card` itself, and only on a dark theme.
- Touch the game: no hooks, signatures, `ClientStructs`, IPC or game state. Pure UI.
- Ship fonts, textures, a window base class or a navigation framework.
- Register commands. Each plugin carries its own copy, so two plugins registering the same command would collide.

## How it works

KosLib is **source-included**: a plugin adds it as a git submodule at `KosLib/` and compiles `KosLib/src/*.cs` into its
own assembly. There is no `KosLib.dll`, so nothing extra goes into the plugin zip and two plugins can't clash over an
assembly version. Each plugin pins its own KosLib commit.

Preset colours are derived from the live `WindowBg` and `Text` of the user's style: surfaces under text are the accent
mixed 54–86% toward the window background, the header family is a mid grey (`Text` mixed 60% toward `WindowBg`) at
alpha 0.31 / 0.75 / 0.90, and checkmarks and slider grabs use the accent (darkened toward the text colour on a light
theme). The derivation is a pure function (`KosColor.Derive`), re-run only when the accent or the theme changes.

The whole-window palette (`KosColor.Window`) keeps the accent's hue at low saturation for the surfaces (KosMos uses the
profile site's night blue instead, so its windows aren't red-brown), picks light or dark surfaces to match the user's
theme, tints the text slightly toward the same hue, and then derives the component slots and flair again over the new
background and text. Inside a whole-window scope, `KosTheme.Push` for the same accent pushes nothing, so a window that
calls both doesn't push every colour twice.

## Using it in a plugin

```bash
git submodule add https://github.com/priestkos/KosLib.git KosLib
```

In the plugin csproj (the SDK's default glob would otherwise also compile `KosLib/tests` and nested `obj/` folders):

```xml
<ItemGroup>
  <Compile Remove="KosLib/**" />
  <Compile Include="KosLib/src/**/*.cs" />
</ItemGroup>
```

To offer the whole-window palette, push it around the window instead of inside `Draw()`:

```csharp
private KosTheme.Scope? windowTheme;

public override void PreDraw()
{
    windowTheme?.Dispose();
    windowTheme = config.WholeWindowTheme ? KosTheme.PushWindow(config.Accent) : null;
}

public override void PostDraw()
{
    windowTheme?.Dispose();
    windowTheme = null;
}
```

Dalamud's window host calls `PostDraw` whenever it called `PreDraw`, so the push stays balanced even if `Draw` throws.

Every `actions/checkout` step that builds the plugin needs `with: submodules: true`. Clone with
`git clone --recurse-submodules`, and run `git submodule update --init` after a pull that moves the pin.

The files spell out their own `using`s and `#nullable enable`, so they compile whether or not the plugin turns on
`ImplicitUsings` or `Nullable`. `KosAccent` is public (a plugin's public configuration class stores it); every other
type is `internal`. `KosAccent` values are saved as numbers, so they are pinned and only ever appended.

## Tests

```bash
dotnet test tests/KosLib.Tests/KosLib.Tests.csproj -c Release
```

135 xUnit checks on the pure colour and motion code (`src/KosColor.cs` and `src/MaterialKos.cs`, compiled without Dalamud); every
whole-window check runs for both the KosLib and the MaterialKos look. Every preset is checked over
Dalamud Standard and ImGui's stock light style:

- Dalamud style (and an unknown value) derives an empty list and no flair, so it pushes and draws nothing extra.
- Text keeps at least 4.5:1 contrast on every button, tab, header and frame state, and on all three primary-button states.
- Section titles and numbers keep at least 4.5:1 on the window and on a card.
- A card fill only ever raises text contrast (and keeps it at 7:1 or more); only KosMos on a dark theme has one.
- Checkmarks and slider grabs keep at least 3:1 against every frame state.
- Every slot is derived exactly once, and none is `WindowBg` or `Text`.
- KosMos uses the site's crimson, foxfire gold and tag gold.
- The whole-window palette: every window slot exactly once, light or dark like the user's theme, the user's background
  alpha kept; text at 12:1 or more on the window and 7:1 on popups, menu bars, title bars, table headers and striped
  rows; 4.5:1 for disabled text, the active title bar and selected text; and every control, mark, section-tag and
  primary-button check above repeated over the new background and text.
- The contrast and compositing maths match WCAG reference points.

The ImGui-drawing parts (`Kos.cs`, `KosWidgets.cs`, `KosTheme.cs`, `KosGallery.cs`) can't run outside the game. They
are checked by building an adopting plugin, and visually through the gallery in game.

## Limits

- Not verified in game yet: the look and the gallery have only been built and unit-tested, not seen in a live client.
- A table inside a card should be safe, because ImGui tables split the draw list with their own splitter instance
  (read in `goatcorp/gc-imgui` 1.88). The gallery has an opt-in checkbox to confirm it in game; until then, keep tables out of cards.
- The whole-window palette has only been seen as numbers: the gallery's section 04 previews it inside a framed child,
  and a plugin's setting applies it to the real window.
- Contrast is guaranteed for the two tested themes. A custom Style Editor theme feeds its own `WindowBg` and `Text`
  into the same mix, which usually keeps contrast but isn't checked at run time.
- A KosLib fix reaches a plugin only when that plugin moves its submodule pin.

## Layout

| Path | What |
|---|---|
| `src/Kos.cs` | `Card`, `Badge` |
| `src/KosWidgets.cs` | `Section`, `Segmented`, `KeyValue`, `Callout`, `Dot`, `Pips`, `IconButton`, `PrimaryButton`, `HelpMarker`, `EmptyState`, `Mono` |
| `src/KosColor.cs` | `KosAccent`, `KosFlair`, colour maths, preset, flair and whole-window derivation (pure) |
| `src/MaterialKos.cs` | M3 tonal scheme and whole-window palette, LCh colour maths, the glow and slide motion maths (pure) |
| `src/MaterialKosWidgets.cs` | M3 button, switch and filter chip |
| `src/KosTheme.cs` | `KosTheme.Push`, `KosTheme.PushWindow` (and the active `Flair`), `Label`, the slot → `ImGuiCol` maps |
| `src/KosGallery.cs` | The in-game gallery |
| `tests/KosLib.Tests/` | xUnit tests for `KosColor.cs` and `MaterialKos.cs` |
| `plan.md` | Design plan and milestones |

## Licence

AGPL-3.0, see [LICENSE](LICENSE). A plugin that includes KosLib's source and is distributed by someone other than its
copyright holder must be AGPL-3.0-compatible.
