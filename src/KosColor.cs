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
}

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
        _ => default,
    };

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
        var mark = Luminance(bg) > 0.5f ? Mix(a, text, 0.45f) with { W = 1 } : a;
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
