# KosLib — plan

A small, shared ImGui style library for the PriestKos Dalamud plugin family (StripDeathroll14, Babel14, Calc14, Aetherpath, FrontlineAdvisor, …).
It does the job RebornMaterial does for GatherBuddy Reborn (one consistent look across windows), but sized for our plugins, and with the user's Dalamud theme staying in charge.

Status (2026-11-15, second pass): **M0, M1 and M3 pushed (StripDeathroll14 `c379f21` on GitHub); M2 built on decision 6, plus the KosMos preset; local commits wait on the in-game look check.** M2 adds `Section`, `Segmented`, `KeyValue`, `Callout`, `Dot`, `Pips`, `IconButton`, `PrimaryButton`, `HelpMarker` and `EmptyState`, and StripDeathroll14 uses all but `KeyValue`, `Dot` and `HelpMarker`. There are 44 offline tests.

### Decisions (user, 2026-11-15)
| # | Question | Answer |
|---|---|---|
| 1 | Licence | AGPL-3.0 |
| 2 | Repo | Public `priestkos/KosLib` on GitHub, plus a Tower Forgejo mirror |
| 3 | Adoption | StripDeathroll14 only, for now |
| 4 | Presets | 5 accents (Teal, Violet, Amber, Crimson, Ocean) plus Dalamud style |
| 5 | Go-ahead | Full plan approved. Built through a local commit in StripDeathroll14; its push and release come after the in-game check |
| 7 | Whole-window theming | "Incorporate theming of the whole window as an option we can explore in our library" (2026-11-15). Built as `KosTheme.PushWindow`, opt-in per plugin setting, off by default; our own colour scheme, derived from the existing accents |
| 6 | Next pass | "Push it and progress to M2 and further, let us tweak the look and style", taking inspiration from other plugins and the KosMos profile site. This overrides §1.7's two-plugin rule for the M2 set. The look became a 7th preset, **KosMos** (value 6), so Dalamud style stays the default and pushes nothing |

### What KosMos takes from the profile site (`priestkos/kosmos`, `assets/style.css`)
| Site | KosLib |
|---|---|
| `--crimson #ff2a44`, `--foxfire-gold #f5b041` | Crimson controls; gold checkmarks and slider grabs (dark theme) |
| `.card-edge-glow`: a 2 px top line, transparent → crimson → gold → transparent | `Kos.Card` draws the same line along its top edge |
| `.glass-panel`: `--bg-card rgba(8, 11, 18, 0.96)`, gold border at 0.16, 16 px radius | Night card fill (dark theme only), gold border at 0.28, 8 px radius |
| `.chapter-tag`: mono, `#ffc56a`, crimson `.tag-num` (`02 / DOSSIER`) | `Kos.Section("Dossier", "02")` in Dalamud's mono font, tag gold with a crimson number |
| `◈` bullet | A drawn diamond (shapes over glyphs, §1.5) |
| `.badge-dot` with a gold `box-shadow` | `Kos.Dot` with a two-ring halo |
| Crimson pill CTA with a glow | `Kos.PrimaryButton`: accent fill stepped toward the background until the label keeps 4.5:1 (the site's white-on-#ff2a44 is about 3.7:1, below that), plus a 3-ring glow |
| Active nav chapter: a gold outline pill | `Kos.Segmented` outlines the selected segment in tag gold |

From other plugins (survey in the dalamud-plugin skill): Henchman's `Underlings` call sites show `Segmented`, `Banner`, `Dot` and `Card`, which shaped `Segmented`, `Callout` and `Dot`. GatherBuddy Reborn's RebornMaterial (one seed colour, state layers) is the model for deriving every state from one accent. Neither is a dependency.

**Exception to §1.1/§1.2 on decision 7:** `KosTheme.PushWindow(accent)` replaces `WindowBg`, `Text` and the other window-level colours for the whole window, pushed in `PreDraw` and popped in `PostDraw` (Dalamud's `WindowHost` calls them as a pair). It is a separate call behind a plugin setting that defaults to off, so the default stays "the theme stays in charge". Its own rules: the window keeps the user's light/dark choice and background alpha; text holds 12:1 on the window, 7:1 on panels and 4.5:1 for disabled text, the active title bar and selection; every component check is repeated over the new surfaces (88 tests in total, measured 2026-11-15). The gallery's section 04 previews it inside a framed child.

**Exceptions to §1 this pass made, on decision 6:** KosMos pushes `FrameRounding`/`GrabRounding`/`TabRounding` (6 px scaled), the only style vars a preset touches. `Kos.Card` draws its own night fill under KosMos on a dark theme. The light-theme branch drops that fill, and the tests check that the fill only ever raises text contrast. Section tags use `UiBuilder.MonoFontHandle`, Dalamud's own font: no font is built or shipped, so §3's "no custom fonts" still holds.

---

## 1. Principles (in priority order)

Order: **correctness → safety → clarity → flair**. Flair never pays for a risk.

1. **The theme stays in charge.** Every colour comes from the live style (`ImGui.GetColorU32(ImGuiCol.*)`) or Dalamud's semantic `ImGuiColors` (HealerGreen, DalamudYellow, DalamudRed, TankBlue, …). The default preset is "Dalamud style", which pushes nothing. If a user has a Style Editor theme, it still wins.
2. **Scoped, never global.** Style pushes live only inside the component that needs them, through `ImRaii` disposables, so an exception can't leak a push. No component touches `ImGui.GetStyle()` fields directly, and nothing is pushed for a whole window.
3. **No game access.** Pure UI. No hooks, signatures, `ClientStructs`, IPC or game state. KosLib can't crash the game except through an ImGui misuse, so the rules in §5 exist to prevent exactly that.
4. **Scale-correct.** Every pixel value is multiplied by `ImGuiHelpers.GlobalScale`, except `Window.Size` and `SizeConstraints`, which Dalamud already scales.
5. **Shapes over glyphs.** Status marks (dots, pips, arrows) are drawn shapes. The game font's coverage of Unicode symbols isn't guaranteed. Icons come only from Dalamud's Font Awesome (`UiBuilder.IconFont`).
6. **Readable on any theme.** Text on a solid fill is dark on bright semantic colours. Accent-derived surfaces keep at least 4.5:1 label contrast (§4).
7. **Small.** A component goes in when two plugins need it. Target: under ~800 lines in total.

---

## 2. Packaging

| Option | For | Against |
|---|---|---|
| **A. Source-included** (each plugin compiles KosLib's `.cs` files via `<Compile Include>` from a git submodule) | No extra DLL. No assembly-version clash between plugins. Types can be `internal`. Each plugin pins its own commit. | A KosLib fix only reaches a plugin when that plugin bumps its submodule. |
| B. Project reference (separate `KosLib.dll` shipped in every plugin zip) | Normal .NET layout. | One more file per package. Every manifest/zip check has to include it. Dalamud's per-plugin load context makes it safe, but it's more surface for nothing. |
| C. NuGet package | Versioned like RebornMaterial. | Publish pipeline, versioning, and a package feed, too heavy for one author. |

**Recommendation: A**, as a **public** git submodule at `KosLib/` in each plugin repo. Each plugin's GitHub Actions release build runs a checkout, and PlogonYes points at those release zips. So the submodule must be public, and the checkout step needs `submodules: true`. Henchman's `Underlings` is private, and outsiders can't build it. We want the opposite.

The namespace is `KosLib`, and all types are `internal` (source-included, so each plugin gets its own copy). The licence follows the plugins (to decide, §8).

---

## 3. Components

### Milestone 1: extract what already works (from StripDeathroll14)
| Component | API sketch | Notes |
|---|---|---|
| `Kos.Card(id, edge?, body)` | Full-width framed panel round `body`. Frame drawn on a background draw-list channel after layout. | `body` must **not** draw tables or columns: they split channels themselves, and nested splits assert. Debug-assert this (§5). `ChannelsMerge` in `finally`. |
| `Kos.Badge(text, color)` | Pill, dark text on solid colour, one text line tall so `SameLine` text aligns. | |
| `Kos.IconButton(icon, label, tooltip?)` | Thin wrapper over `ImGuiComponents.IconButtonWithText`, plus the tooltip with `AllowWhenDisabled`. | One consistent call for the family. Keeps the `##id` convention. |
| `Kos.Semantic` | `Ok`, `Warn`, `Danger`, `Info`, `Muted`: aliases of `ImGuiColors`. | Plugins say what a colour means, not which colour it is. |

### Milestone 2: the common set (each only once a second plugin wants it)
- `Kos.Section(title, icon?)`: header line with an optional icon and a thin rule, in place of `Separator()` + `TextUnformatted`.
- `Kos.StatusDot(color, size)` and `Kos.Pips(filled, total)`: drawn dots (the Strip Deathroll circle's pieces-left pips move here).
- `Kos.KeyValue(label, value)` rows: aligned label column for read-outs.
- `Kos.Segmented(id, ref value, options)`: a row of toggle buttons for 2–4 choices, replacing radio-button rows such as Circle/List.
- `Kos.Toggle(label, ref bool, help?)`: checkbox with an optional `(?)` help marker. **No custom switch drawing until it's asked for**, since a checkbox is clearer.
- `Kos.HelpMarker(text)`: greyed `(?)` with a wrapped tooltip.
- `Kos.EmptyState(text)`: centred `TextDisabled` for "No rolls yet." and the like.

### Milestone 3: accent presets (the next session's item 4)
- `KosTheme` with presets: **Dalamud style (default, pushes nothing)**, plus a handful of accents (e.g. Teal, Violet, Amber, Crimson, Ocean).
- A preset changes **only** `Button*`, `Tab*`, `Header*`, `CheckMark`, `SliderGrab*`, `FrameBg*` (and `TitleBgActive` optionally). It never touches `WindowBg` or `Text`, so the user's theme keeps its background and text.
- Applied with one `using var _ = KosTheme.Push(config.Accent);` at the top of a window's `Draw()`, and popped at its end. It's scoped to that window, never global.
- Saved per plugin as one config enum. "Dalamud style" means zero pushes, which is the regression check.

### Explicitly out of scope
Custom fonts, a replacement window base class, a navigation sidebar framework, animations beyond `ImGui.GetTime()` pulses, snackbars/toasts, or anything that needs `OnBuildFonts` or texture loading. Revisit only on request.

---

## 4. Colour rules (for presets)

These come from the earlier style-payload work, where 20 styles passed with these rules:
- `ButtonHovered`/`ButtonActive` and `Tab*` = accent mixed **54–66% toward `WindowBg`**. Raw bright accents under white text drop to about 1.5:1.
- The `Header`/`HeaderHovered`/`HeaderActive` family = grey `mix(Text, WindowBg, 0.6)` at alpha 0.31 / 0.75 / 0.90. The test caught the earlier 0.5: it left HeaderActive at 4.22:1 under white text.
- `CheckMark`/`SliderGrab` = the accent (darkened 45% toward `Text` on a light theme); `SliderGrabActive` = that mixed 30% toward `Text`. Tested at ≥ 3:1 against every `FrameBg` state.
- `TableHeaderBg`/`Tab` = `mix(Text, WindowBg, 0.80–0.86)`.
- Light themes: mixing toward the live `WindowBg` already lightens surfaces under dark text, so only the marks need a light-theme branch (`WindowBg` luminance > 0.5). Both themes are tested.
- Pure functions: `Mix`, `Luminance`, `Contrast`, `Derive(accent, windowBg, text)`. **Unit-tested offline**: every preset over both the stock dark and a light background must keep label contrast ≥ 4.5:1 on every state surface.

---

## 5. Safety rules every component follows

An ImGui assert or a mismatched Begin/End in a plugin can take the game down. So:
1. Every `Begin*` that returns a bool goes through `ImRaii`, or through a `try/finally` that always calls the matching `End*`/`Merge`.
2. No list mutation while drawing it. Components return an action or a bool, and callers apply changes after the loop. That's the pattern already used in StripDeathroll14.
3. ~~`Card` tracks "inside a card" in a `[ThreadStatic]` depth counter; `Kos.Table` refuses inside a card.~~ **Probably not needed (review 2026-11-15, from reading the source, not tested):** the ImGui assert is per `ImDrawListSplitter` instance (`imgui_draw.cpp`: "Nested channel splitting is not supported. Please use separate instances"), and tables split with their **own** instance (`imgui_tables.cpp`: `table->DrawSplitter = &table->TempData->DrawSplitter`). `imgui.h` says separate instances can be stacked. Read from upstream `docking` 1.93.1 WIP; Dalamud's pinned copy wasn't checked. The gallery shows a table inside a card; if that holds in game, drop the counter and the "no tables" rule from `Card`.
4. Any `SetCursorScreenPos` is followed by an item (`Dummy`) before the window ends, or ImGui's boundary check asserts.
5. Unique IDs: every component takes an `id` or pushes the label's `##` part. Component internals use `PushId` so two cards with the same title don't collide.
6. No allocations in hot paths beyond what string formatting already costs. Components run every frame.

---

## 6. Testing and verification

- **Offline (CI):** an xUnit project for the pure parts: colour maths, preset derivation and contrast (§4), plus layout maths (pip spacing, label anchoring). Components that call ImGui can't run offline; we say so instead of mocking ImGui.
- **Gallery:** `KosGallery.Draw()`, which the host plugin calls from its own debug tab. KosLib doesn't register a `/koslib` command, because every adopting plugin carries its own copy and the second one to register the same command would collide. It shows every component in every state and every preset. That's the in-game visual check. The user judges it in game, and a rejected look rolls back to the last approved commit.
- Every adopting plugin keeps its own `dotnet build -c Release` (0 warnings) and its test suite green after a submodule bump.

---

## 7. Milestones

| # | Deliverable | Done when |
|---|---|---|
| M0+M1 | `src/Kos.cs` holding `Card` + `Badge`, moved from SDR `MainWindow.Style.cs` with unchanged bodies (`private` → `internal`, explicit `using System;`, `#nullable enable`, since 7 of 13 top-level plugin csprojs don't set ImplicitUsings and 3 don't set Nullable). `PhaseLabel`/`PhaseColor` stay in SDR (game-specific). `IconButton` and `Semantic` wait for a second user (§1.7). No `tests/` until M3 brings pure code. | SDR Debug+Release 0 warnings, 46 tests green (baseline at `10b9e0d`: Release 0 warnings, 46/46 passed), user sees no change in game |
| M0 wiring | SDR: `git submodule add https://github.com/priestkos/KosLib.git KosLib`; csproj `<Compile Remove="KosLib/**" />` + `<Compile Include="KosLib/src/**/*.cs" />` (the SDK glob would otherwise pick up `KosLib/tests` and nested `obj/`); `submodules: true` on the checkouts in `build.yml` and `release.yml`; `sync_dalamud_repos.py` runs `git submodule update --init` after a fast-forward, otherwise local builds compile a stale KosLib. Push order: KosLib commit public first, then the plugin, or CI checkout fails. | |
| M2 | Common set (§3, M2) as needed, plus the gallery | Built on decision 6 (originally: second plugin adopts at least 2 components). StripDeathroll14 Release 0 warnings, 46/46 tests; KosLib 44/44; the user's in-game look check is still to come |
| M3 | Accent presets plus contrast tests | All presets ≥ 4.5:1 offline; "Dalamud style" pushes nothing (`Derive` is pure and returns the `(ImGuiCol, colour)` list that `Push` loops over, so the test asserts that the list is empty, with no ImGui hook); user picks in game |
| M4 | Adoption notes for the family; release-workflow `submodules: true` in each adopting repo | One adopting plugin released through PlogonYes with KosLib inside |

---

## 8. Open questions for the user

1. **Licence:** MIT or AGPL-3.0? Checked 2026-11-15: only 2 of 18 repos carry a LICENSE file, NameplateOffset (MIT) and InventorySearchBar (AGPL, a fork). The rest carry none. With source inclusion, KosLib's licence applies inside each plugin, so AGPL would force AGPL onto every adopter. MIT can sit inside either.
2. **Repo:** a public `priestkos/KosLib` on GitHub (needed for Actions checkout), with the Tower Forgejo as a mirror?
3. **Adoption order** after StripDeathroll14: which plugin next? Babel14 is stock-styled by choice. Count of `PushColor|PushStyle|static readonly Vector4` matches in `*.cs` (2026-11-15): FrontlineAdvisor 31, GateAlert 24, Aetherpath 12, InventorySearchBar 11 (fork), huntalerts 146 (upstream fork, leave alone). Every other repo has 0.
4. **Accent preset names and count** for M3: keep 5–7 like GatherBuddy Reborn, or fewer?
