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

    private static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
}
