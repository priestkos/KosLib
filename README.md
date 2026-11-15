# KosLib

A small shared ImGui style library for the PriestKos Dalamud plugins: framed cards, badges and optional window accent
presets, with the user's own Dalamud theme left in charge.

`source-included` · Dalamud API 15 · .NET 10 · AGPL-3.0

> **No support is offered.** KosLib exists for the PriestKos plugin family; it is public so the plugins' release builds can check it out.

## What it does

- **`Kos.Card(id, edge, body)`**: a full-width framed panel round whatever `body` draws, with an optional coloured edge.
  The frame is drawn on a background draw-list channel after the body is laid out. A card inside a card draws its body
  without a second frame instead of nesting the channel split (which ImGui asserts on).
- **`Kos.Badge(text, color)`**: a pill with dark text on a solid semantic colour, one text line tall so `SameLine` text
  stays aligned.
- **`KosTheme.Push(accent)`**: an accent preset for one window. `using var _ = KosTheme.Push(config.Accent);` at the top
  of `Draw()` pushes the colours and pops them at the end of the scope. Five accents (Teal, Violet, Amber, Crimson, Ocean)
  plus **Dalamud style**, the default, which pushes nothing.
- **`KosGallery.Draw()`**: every component in every state and every preset, for the in-game visual check. The host plugin
  calls it from its own debug section.

### What it is not meant to do

- Replace the user's theme: a preset changes only `Button*`, `Tab*`, `Header*`, `CheckMark`, `SliderGrab*` and `FrameBg*`,
  never `WindowBg` or `Text`, and only inside the window that pushes it.
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

Every `actions/checkout` step that builds the plugin needs `with: submodules: true`. Clone with
`git clone --recurse-submodules`, and run `git submodule update --init` after a pull that moves the pin.

The files spell out their own `using`s and `#nullable enable`, so they compile whether or not the plugin turns on
`ImplicitUsings` or `Nullable`. `KosAccent` is public (a plugin's public configuration class stores it); every other
type is `internal`. `KosAccent` values are saved as numbers, so they are pinned and only ever appended.

## Tests

```bash
dotnet test tests/KosLib.Tests/KosLib.Tests.csproj -c Release
```

20 xUnit checks on the pure colour code (`src/KosColor.cs`, compiled without Dalamud). Every preset is checked over
Dalamud Standard and ImGui's stock light style:

- Dalamud style (and an unknown value) derives an empty list, so it pushes nothing.
- Text keeps at least 4.5:1 contrast on every button, tab, header and frame state.
- Checkmarks and slider grabs keep at least 3:1 against every frame state.
- Every slot is derived exactly once, and none is `WindowBg` or `Text`.
- The contrast and compositing maths match WCAG reference points.

The ImGui-drawing parts (`Kos.cs`, `KosTheme.cs`, `KosGallery.cs`) can't run outside the game. They are checked by
building an adopting plugin, and visually through the gallery in game.

## Limits

- Not verified in game yet: the look and the gallery have only been built and unit-tested, not seen in a live client.
- A table inside a card should be safe, because ImGui tables split the draw list with their own splitter instance
  (read in `goatcorp/gc-imgui` 1.88). The gallery has an opt-in checkbox to confirm it in game; until then, keep tables out of cards.
- Contrast is guaranteed for the two tested themes. A custom Style Editor theme feeds its own `WindowBg` and `Text`
  into the same mix, which usually keeps contrast but isn't checked at run time.
- A KosLib fix reaches a plugin only when that plugin moves its submodule pin.

## Layout

| Path | What |
|---|---|
| `src/Kos.cs` | `Card`, `Badge` |
| `src/KosColor.cs` | `KosAccent`, colour maths and preset derivation (pure) |
| `src/KosTheme.cs` | `KosTheme.Push`, `Label`, the slot → `ImGuiCol` map |
| `src/KosGallery.cs` | The in-game gallery |
| `tests/KosLib.Tests/` | xUnit tests for `KosColor.cs` |
| `plan.md` | Design plan and milestones |

## Licence

AGPL-3.0, see [LICENSE](LICENSE). A plugin that includes KosLib's source and is distributed by someone other than its
copyright holder must be AGPL-3.0-compatible.
