#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;

namespace KosLib;

// Pure colour maths and preset derivation: no Dalamud or ImGui types, so the offline tests compile this file alone.

/// <summary>Accent presets. Saved in plugin configs as numbers, so values are pinned and only ever appended.</summary>
/// <remarks>Public because a plugin's public configuration class stores it.</remarks>
public enum KosAccent
{
    Dalamud = 0, // pushes nothing: the user's own Dalamud style
    Teal = 1,
    Violet = 2,
    Amber = 3,
    Crimson = 4,
    Ocean = 5,
    KosMos = 6, // the signature look, after the KosMos profile site: crimson to foxfire gold
}

/// <summary>
/// The drawn extras a preset adds to KosLib's own components (cards, section headers, dots, primary buttons).
/// Null for Dalamud style, so those components draw in plain theme colours.
/// </summary>
/// <param name="EdgeA">Left colour of a card's top edge line and of a section rule.</param>
/// <param name="EdgeB">Right colour the edge line runs into (the same hue as EdgeA for the plain accents).</param>
/// <param name="Tag">Section title text.</param>
/// <param name="TagNumber">Section number text and the section diamond.</param>
/// <param name="CardFill">A card's own fill, or null to keep the theme's FrameBg.</param>
/// <param name="CardBorder">A card's frame when the caller gives no edge colour.</param>
/// <param name="Glow">Soft halo round primary buttons and status dots.</param>
/// <param name="Rounding">Frame/grab/tab rounding in unscaled pixels, or null to keep the theme's.</param>
/// <param name="Primary">Idle, hovered and active fill of a primary (call-to-action) button.</param>
internal readonly record struct KosFlair(
    Vector4 EdgeA, Vector4 EdgeB, Vector4 Tag, Vector4 TagNumber, Vector4? CardFill, Vector4 CardBorder, bool Glow, float? Rounding,
    (Vector4 Idle, Vector4 Hovered, Vector4 Active) Primary = default);

/// <summary>The style colours a preset may change. Names match <c>ImGuiCol</c>; WindowBg and Text are never here.</summary>
internal enum KosSlot
{
    Button, ButtonHovered, ButtonActive,
    Tab, TabHovered, TabActive, TabUnfocused, TabUnfocusedActive,
    Header, HeaderHovered, HeaderActive,
    CheckMark, SliderGrab, SliderGrabActive,
    FrameBg, FrameBgHovered, FrameBgActive,
}

internal static class KosColor
{
    private static readonly (KosSlot, Vector4)[] None = [];

    /// <summary>Linear mix: <paramref name="t"/> = 0 gives <paramref name="a"/>, 1 gives <paramref name="b"/>.</summary>
    public static Vector4 Mix(Vector4 a, Vector4 b, float t) => Vector4.Lerp(a, b, t);

    /// <summary>WCAG relative luminance of the RGB part.</summary>
    public static float Luminance(Vector4 c) => 0.2126f * Lin(c.X) + 0.7152f * Lin(c.Y) + 0.0722f * Lin(c.Z);

    /// <summary>WCAG contrast ratio, 1..21.</summary>
    public static float Contrast(Vector4 a, Vector4 b)
    {
        float la = Luminance(a), lb = Luminance(b);
        return (MathF.Max(la, lb) + 0.05f) / (MathF.Min(la, lb) + 0.05f);
    }

    /// <summary><paramref name="top"/> composited over an opaque <paramref name="under"/>.</summary>
    public static Vector4 Over(Vector4 top, Vector4 under) => Mix(under, top, top.W) with { W = 1 };

    public static Vector4 Accent(KosAccent a) => a switch
    {
        KosAccent.Teal => new(0.18f, 0.72f, 0.66f, 1),
        KosAccent.Violet => new(0.62f, 0.45f, 0.95f, 1),
        KosAccent.Amber => new(0.98f, 0.70f, 0.20f, 1),
        KosAccent.Crimson => new(0.86f, 0.20f, 0.30f, 1),
        KosAccent.Ocean => new(0.20f, 0.50f, 0.92f, 1),
        KosAccent.KosMos => Crimson,
        _ => default,
    };

    // KosMos profile site palette (assets/style.css): --crimson #ff2a44, --foxfire-gold #f5b041,
    // .chapter-tag #ffc56a, --bg-card rgba(8, 11, 18, 0.96), --border-subtle = gold at 0.16.
    public static readonly Vector4 Crimson = new(1f, 0.165f, 0.267f, 1);
    public static readonly Vector4 FoxfireGold = new(0.961f, 0.690f, 0.255f, 1);
    public static readonly Vector4 TagGold = new(1f, 0.773f, 0.416f, 1);
    public static readonly Vector4 NightCard = new(0.031f, 0.043f, 0.071f, 0.96f);

    /// <summary>
    /// The most a frosted window's background covers. The blur needs the scene to show through, and text on the window
    /// still has to read over a white or a black scene with no blur at all (the tests check both).
    /// </summary>
    public const float FrostMaxAlpha = 0.85f;

    /// <summary>A frosted window's background: the same colour, at most <see cref="FrostMaxAlpha"/> opaque.</summary>
    public static Vector4 Frosted(Vector4 windowBg) => windowBg with { W = MathF.Min(windowBg.W, FrostMaxAlpha) };

    /// <summary>A light theme is one whose window background is brighter than mid grey (so its text is dark).</summary>
    public static bool IsLight(Vector4 windowBg) => Luminance(windowBg) > 0.5f;

    /// <summary>
    /// The drawn extras for <paramref name="accent"/> over the live theme; null for Dalamud style or an unknown value.
    /// On a light theme every colour is darkened toward the text and the KosMos night card fill is dropped, so
    /// nothing turns into dark-on-dark.
    /// </summary>
    public static KosFlair? Flair(KosAccent accent, Vector4 windowBg, Vector4 text)
    {
        if (accent == KosAccent.Dalamud || !Enum.IsDefined(accent)) return null;
        var light = IsLight(windowBg);
        var a = Accent(accent);
        var bg = windowBg with { W = 1 };
        Vector4 Ink(Vector4 c, float toText) => Mix(c, text, toText) with { W = 1 };
        KosFlair f;
        if (accent == KosAccent.KosMos)
            f = light
                ? new KosFlair(Ink(Crimson, 0.30f), Ink(FoxfireGold, 0.45f), Ink(Crimson, 0.45f), Ink(Crimson, 0.30f), null,
                    Ink(FoxfireGold, 0.45f) with { W = 0.55f }, true, 6)
                : new KosFlair(Crimson, FoxfireGold, TagGold, Crimson, NightCard, FoxfireGold with { W = 0.28f }, true, 6);
        // Light: the number at 0.40 toward the text left Amber at 4.16:1, so title and number both take 0.50.
        else
            f = light
                ? new KosFlair(Ink(a, 0.30f), Ink(a, 0.30f) with { W = 0.25f }, Ink(a, 0.50f), Ink(a, 0.50f), null, Ink(a, 0.30f) with { W = 0.55f }, false, null)
                : new KosFlair(a, Ink(a, 0.35f) with { W = 0.25f }, Ink(a, 0.35f), Ink(a, 0.15f), null, a with { W = 0.40f }, false, null);
        return f with { Primary = PrimaryFill(a, bg, text) };
    }

    /// <summary>
    /// The brightest accent fill that keeps the label at 4.5:1: step from the raw accent toward the window background
    /// until the label passes. Hovered is that fill; idle and active sit a little further toward the background.
    /// The tests check all three states on both test themes (the step alone doesn't prove the other two).
    /// </summary>
    public static (Vector4 Idle, Vector4 Hovered, Vector4 Active) PrimaryFill(Vector4 accent, Vector4 windowBg, Vector4 text)
    {
        var bg = windowBg with { W = 1 };
        var t = 0f;
        while (t < 0.9f && Contrast(text, Mix(accent, bg, t) with { W = 1 }) < 4.6f) t += 0.02f;
        return (Mix(accent, bg, t + 0.08f) with { W = 1 }, Mix(accent, bg, t) with { W = 1 }, Mix(accent, bg, t + 0.14f) with { W = 1 });
    }

    /// <summary>
    /// The colours a preset pushes over the live <paramref name="windowBg"/> and <paramref name="text"/>. Empty for
    /// <see cref="KosAccent.Dalamud"/> (or an unknown value), which is the "pushes nothing" guarantee.
    /// Surfaces under text are the accent mixed 54-86% toward the window background; the Header family is a mid grey.
    /// </summary>
    public static (KosSlot Slot, Vector4 Color)[] Derive(KosAccent accent, Vector4 windowBg, Vector4 text)
    {
        if (accent == KosAccent.Dalamud || !Enum.IsDefined(accent)) return None;
        var a = Accent(accent);
        var bg = windowBg with { W = 1 };
        Vector4 Solid(float t) => Mix(a, bg, t) with { W = 1 };
        var grey = Mix(text, bg, 0.6f); // 0.5 left HeaderActive at 4.22:1 under white text
        var tab = Mix(text, bg, 0.83f) with { W = 1 };
        // A light theme (dark text) needs the marks darker than the accent to stand out from a pale frame.
        // KosMos marks are foxfire gold on a dark theme, like the site's badge dots.
        var mark = IsLight(bg) ? Mix(a, text, 0.45f) with { W = 1 } : accent == KosAccent.KosMos ? FoxfireGold : a;
        return
        [
            (KosSlot.Button, Solid(0.66f)),
            (KosSlot.ButtonHovered, Solid(0.60f)),
            (KosSlot.ButtonActive, Solid(0.54f)),
            (KosSlot.Tab, tab),
            (KosSlot.TabHovered, Solid(0.60f)),
            (KosSlot.TabActive, Solid(0.54f)),
            (KosSlot.TabUnfocused, tab),
            (KosSlot.TabUnfocusedActive, Solid(0.66f)),
            (KosSlot.Header, grey with { W = 0.31f }),
            (KosSlot.HeaderHovered, grey with { W = 0.75f }),
            (KosSlot.HeaderActive, grey with { W = 0.90f }),
            (KosSlot.CheckMark, mark),
            (KosSlot.SliderGrab, mark),
            (KosSlot.SliderGrabActive, Mix(mark, text, 0.3f) with { W = 1 }),
            (KosSlot.FrameBg, Solid(0.86f)),
            (KosSlot.FrameBgHovered, Solid(0.80f)),
            (KosSlot.FrameBgActive, Solid(0.74f)),
        ];
    }

    // ---- Whole-window palette (opt-in) ----------------------------------------------------------------------------

    /// <summary>
    /// The opt-in whole-window palette for <paramref name="accent"/>, built over the user's live
    /// <paramref name="windowBg"/> and <paramref name="text"/>; null for Dalamud style or an unknown value.
    /// Surfaces keep the accent's hue at a low saturation (KosMos takes the profile site's night blue instead, so its
    /// windows aren't red-brown). A light theme gets a light window and a dark one a dark window, the window keeps the
    /// user's background alpha, and the component slots and flair are derived again over the new background and text.
    /// </summary>
    public static KosWindowPalette? Window(KosAccent accent, Vector4 windowBg, Vector4 text)
    {
        if (accent == KosAccent.Dalamud || !Enum.IsDefined(accent)) return null;
        var light = IsLight(windowBg);
        var a = Accent(accent);
        var kosmos = accent == KosAccent.KosMos;
        var (h, s, _) = ToHsl(kosmos ? NightCard with { W = 1 } : a);
        var tint = MathF.Min(s, kosmos ? 0.45f : 0.30f);
        Vector4 Surface(float l) => FromHsl(h, tint, l);

        var bg = Surface(light ? 0.955f : 0.085f);
        var ink = light ? FromHsl(h, tint * 0.6f, 0.10f) : FromHsl(h, tint * 0.5f, 0.94f);
        var mark = light ? Mix(a, ink, 0.45f) with { W = 1 } : kosmos ? FoxfireGold : a;
        var border = kosmos ? (light ? Mix(FoxfireGold, ink, 0.45f) with { W = 0.55f } : FoxfireGold with { W = 0.22f })
            : light ? Mix(a, ink, 0.30f) with { W = 0.45f } : a with { W = 0.30f };
        Vector4 Toward(Vector4 c, float t) => Mix(c, bg, t) with { W = 1 };
        Vector4 Clear(Vector4 c) => c with { W = 0 };

        (KosWindowSlot, Vector4)[] colors =
        [
            (KosWindowSlot.WindowBg, bg with { W = windowBg.W }),
            (KosWindowSlot.ChildBg, Clear(bg)), // children show the window's own tint
            (KosWindowSlot.PopupBg, Surface(light ? 0.975f : 0.11f) with { W = 0.97f }),
            (KosWindowSlot.MenuBarBg, Surface(light ? 0.92f : 0.12f)),
            (KosWindowSlot.TitleBg, Surface(light ? 0.90f : 0.07f)),
            (KosWindowSlot.TitleBgActive, Toward(a, light ? 0.78f : 0.64f)),
            (KosWindowSlot.TitleBgCollapsed, Surface(light ? 0.90f : 0.07f) with { W = 0.75f }),
            (KosWindowSlot.Text, ink),
            (KosWindowSlot.TextDisabled, Mix(ink, bg, light ? 0.34f : 0.42f) with { W = 1 }), // light at 0.42 was 4.10-4.25:1
            (KosWindowSlot.TextSelectedBg, a with { W = 0.35f }),
            (KosWindowSlot.Border, border),
            (KosWindowSlot.BorderShadow, Clear(bg)),
            (KosWindowSlot.Separator, border),
            (KosWindowSlot.SeparatorHovered, mark with { W = 0.65f }),
            (KosWindowSlot.SeparatorActive, mark),
            (KosWindowSlot.ScrollbarBg, Surface(light ? 0.92f : 0.06f) with { W = 0.53f }),
            (KosWindowSlot.ScrollbarGrab, Toward(a, 0.58f)),
            (KosWindowSlot.ScrollbarGrabHovered, Toward(a, 0.42f)),
            (KosWindowSlot.ScrollbarGrabActive, Toward(a, 0.25f)),
            (KosWindowSlot.ResizeGrip, mark with { W = 0.20f }),
            (KosWindowSlot.ResizeGripHovered, mark with { W = 0.60f }),
            (KosWindowSlot.ResizeGripActive, mark with { W = 0.90f }),
            (KosWindowSlot.TableHeaderBg, Surface(light ? 0.88f : 0.16f)),
            (KosWindowSlot.TableBorderStrong, border),
            (KosWindowSlot.TableBorderLight, border with { W = border.W * 0.5f }),
            (KosWindowSlot.TableRowBgAlt, ink with { W = 0.04f }),
            (KosWindowSlot.PlotLines, mark),
            (KosWindowSlot.PlotLinesHovered, Mix(mark, ink, 0.3f) with { W = 1 }),
            (KosWindowSlot.PlotHistogram, mark),
            (KosWindowSlot.PlotHistogramHovered, Mix(mark, ink, 0.3f) with { W = 1 }),
            (KosWindowSlot.NavHighlight, mark),
        ];
        return new KosWindowPalette(bg, ink, colors, Derive(accent, bg, ink), Flair(accent, bg, ink)!.Value);
    }

    /// <summary>Hue (0..1), saturation and lightness of the RGB part.</summary>
    public static (float H, float S, float L) ToHsl(Vector4 c)
    {
        float max = MathF.Max(c.X, MathF.Max(c.Y, c.Z)), min = MathF.Min(c.X, MathF.Min(c.Y, c.Z));
        var l = (max + min) / 2;
        var d = max - min;
        if (d < 1e-6f) return (0, 0, l);
        var s = l > 0.5f ? d / (2 - max - min) : d / (max + min);
        var h = max == c.X ? (c.Y - c.Z) / d + (c.Y < c.Z ? 6 : 0) : max == c.Y ? (c.Z - c.X) / d + 2 : (c.X - c.Y) / d + 4;
        return (h / 6, s, l);
    }

    /// <summary>An opaque colour from hue (0..1), saturation and lightness.</summary>
    public static Vector4 FromHsl(float h, float s, float l)
    {
        if (s <= 0) return new(l, l, l, 1);
        var q = l < 0.5f ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        return new(Channel(p, q, h + 1f / 3), Channel(p, q, h), Channel(p, q, h - 1f / 3), 1);

        static float Channel(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            return t < 1f / 6 ? p + (q - p) * 6 * t : t < 0.5f ? q : t < 2f / 3 ? p + (q - p) * (2f / 3 - t) * 6 : p;
        }
    }

    private static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
}

/// <summary>Window-level style colours, used only by the opt-in whole-window palette. Names match <c>ImGuiCol</c>.</summary>
internal enum KosWindowSlot
{
    WindowBg, ChildBg, PopupBg, MenuBarBg,
    TitleBg, TitleBgActive, TitleBgCollapsed,
    Text, TextDisabled, TextSelectedBg,
    Border, BorderShadow, Separator, SeparatorHovered, SeparatorActive,
    ScrollbarBg, ScrollbarGrab, ScrollbarGrabHovered, ScrollbarGrabActive,
    ResizeGrip, ResizeGripHovered, ResizeGripActive,
    TableHeaderBg, TableBorderStrong, TableBorderLight, TableRowBgAlt,
    PlotLines, PlotLinesHovered, PlotHistogram, PlotHistogramHovered,
    NavHighlight,
}

/// <summary>Everything the whole-window mode pushes: window colours, the component slots and flair over the new surfaces.</summary>
/// <param name="Bg">The new window background, opaque (the pushed WindowBg keeps the user's alpha).</param>
/// <param name="Text">The new text colour.</param>
internal sealed record KosWindowPalette(
    Vector4 Bg, Vector4 Text, (KosWindowSlot Slot, Vector4 Color)[] Window, (KosSlot Slot, Vector4 Color)[] Components, KosFlair Flair);
