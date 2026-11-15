#nullable enable
using System;
using System.Numerics;

namespace KosLib;

// MaterialKos: a Material Design 3 look built from one KosLib accent, per Google's public M3 colour system
// (tonal palettes, role tones, state layers). Pure: no Dalamud or ImGui types, so the offline tests compile it.
// Shortcut: tonal palettes use CIELAB LCh, not M3's HCT (CAM16); tone is L* in both, hue drifts a little in blues.
// Upgrade to HCT only if a palette looks off in game.

/// <summary>The M3 colour roles MaterialKos uses, for one accent on a light or dark window.</summary>
internal sealed record MaterialScheme(
    bool Light,
    Vector4 Primary, Vector4 OnPrimary, Vector4 PrimaryContainer, Vector4 OnPrimaryContainer,
    Vector4 SecondaryContainer, Vector4 OnSecondaryContainer, Vector4 Tertiary,
    Vector4 Surface, Vector4 SurfaceContainerLow, Vector4 SurfaceContainer, Vector4 SurfaceContainerHigh, Vector4 SurfaceContainerHighest,
    Vector4 OnSurface, Vector4 OnSurfaceVariant, Vector4 Outline, Vector4 OutlineVariant);

internal static partial class MaterialKos
{
    /// <summary>M3 state-layer opacities: the content colour laid over a container when hovered / pressed.</summary>
    public const float Hover = 0.08f, Press = 0.12f;

    /// <summary>
    /// The M3 "tonal spot" scheme seeded from <paramref name="accent"/>'s hue; null for Dalamud style or an unknown
    /// value. Tertiary sits 60° round the hue (KosMos: crimson seeds a gold tertiary).
    /// </summary>
    public static MaterialScheme? Scheme(KosAccent accent, bool light)
    {
        if (accent == KosAccent.Dalamud || !Enum.IsDefined(accent)) return null;
        var hue = ToLch(KosColor.Accent(accent)).H;
        var third = (hue + 60) % 360;
        Vector4 R(float h, float chroma, float dark, float lightTone) => Tone(h, chroma, light ? lightTone : dark);
        return new MaterialScheme(light,
            R(hue, 48, 80, 40), R(hue, 48, 20, 100), R(hue, 48, 30, 90), R(hue, 48, 90, 10),
            R(hue, 20, 30, 90), R(hue, 20, 90, 10), R(third, 32, 80, 40),
            R(hue, 6, 6, 98), R(hue, 6, 10, 96), R(hue, 6, 12, 94), R(hue, 6, 17, 92), R(hue, 6, 22, 90),
            R(hue, 6, 90, 10), R(hue, 10, 80, 30), R(hue, 10, 60, 50), R(hue, 10, 30, 80));
    }

    /// <summary>
    /// The whole-window palette in Material style: same contract as <see cref="KosColor.Window"/> (null for Dalamud
    /// style; light or dark like the user's theme; the user's background alpha kept), with M3 roles and state layers.
    /// </summary>
    public static KosWindowPalette? Window(KosAccent accent, Vector4 windowBg, Vector4 text)
    {
        if (Scheme(accent, KosColor.IsLight(windowBg)) is not { } s) return null;
        Vector4 State(Vector4 under, Vector4 ink, float a) => KosColor.Over(ink with { W = a }, under);
        Vector4 Clear(Vector4 c) => c with { W = 0 };
        var (bg, ink) = (s.Surface, s.OnSurface);

        (KosWindowSlot, Vector4)[] window =
        [
            (KosWindowSlot.WindowBg, bg with { W = windowBg.W }),
            (KosWindowSlot.ChildBg, Clear(bg)),
            (KosWindowSlot.PopupBg, s.SurfaceContainer with { W = 0.98f }),
            (KosWindowSlot.MenuBarBg, s.SurfaceContainer),
            (KosWindowSlot.TitleBg, s.SurfaceContainerLow),
            (KosWindowSlot.TitleBgActive, s.SurfaceContainerHigh),
            (KosWindowSlot.TitleBgCollapsed, s.SurfaceContainerLow with { W = 0.75f }),
            (KosWindowSlot.Text, ink),
            (KosWindowSlot.TextDisabled, s.OnSurfaceVariant), // ImGui's "disabled" text is mostly hint text: M3's variant role
            (KosWindowSlot.TextSelectedBg, s.Primary with { W = 0.35f }),
            (KosWindowSlot.Border, s.OutlineVariant),
            (KosWindowSlot.BorderShadow, Clear(bg)),
            (KosWindowSlot.Separator, s.OutlineVariant),
            (KosWindowSlot.SeparatorHovered, s.Primary with { W = 0.65f }),
            (KosWindowSlot.SeparatorActive, s.Primary),
            (KosWindowSlot.ScrollbarBg, Clear(bg)),
            (KosWindowSlot.ScrollbarGrab, s.Outline with { W = 0.50f }),
            (KosWindowSlot.ScrollbarGrabHovered, s.Outline with { W = 0.75f }),
            (KosWindowSlot.ScrollbarGrabActive, s.Primary),
            (KosWindowSlot.ResizeGrip, Clear(s.Primary)),
            (KosWindowSlot.ResizeGripHovered, s.Primary with { W = 0.35f }),
            (KosWindowSlot.ResizeGripActive, s.Primary with { W = 0.65f }),
            (KosWindowSlot.TableHeaderBg, s.SurfaceContainerHigh),
            (KosWindowSlot.TableBorderStrong, s.OutlineVariant),
            (KosWindowSlot.TableBorderLight, s.OutlineVariant with { W = 0.5f }),
            (KosWindowSlot.TableRowBgAlt, ink with { W = 0.04f }),
            (KosWindowSlot.PlotLines, s.Primary),
            (KosWindowSlot.PlotLinesHovered, s.Tertiary),
            (KosWindowSlot.PlotHistogram, s.Tertiary),
            (KosWindowSlot.PlotHistogramHovered, s.Primary),
            (KosWindowSlot.NavHighlight, s.Primary),
        ];
        var sc = s.SecondaryContainer;
        var tabIdle = s.SurfaceContainer;
        (KosSlot, Vector4)[] components =
        [
            (KosSlot.Button, sc), // ImGui buttons are M3 tonal buttons
            (KosSlot.ButtonHovered, State(sc, s.OnSecondaryContainer, Hover)),
            (KosSlot.ButtonActive, State(sc, s.OnSecondaryContainer, Press)),
            (KosSlot.Tab, tabIdle),
            (KosSlot.TabHovered, State(tabIdle, ink, Hover)),
            (KosSlot.TabActive, sc),
            (KosSlot.TabUnfocused, tabIdle),
            (KosSlot.TabUnfocusedActive, KosColor.Mix(sc, tabIdle, 0.4f)),
            (KosSlot.Header, sc with { W = 0.55f }),
            (KosSlot.HeaderHovered, State(s.SurfaceContainerHigh, ink, Hover)),
            (KosSlot.HeaderActive, sc),
            (KosSlot.CheckMark, s.Primary),
            (KosSlot.SliderGrab, s.Primary),
            (KosSlot.SliderGrabActive, KosColor.Mix(s.Primary, ink, 0.3f)),
            (KosSlot.FrameBg, s.SurfaceContainerHighest),
            (KosSlot.FrameBgHovered, State(s.SurfaceContainerHighest, ink, Hover)),
            (KosSlot.FrameBgActive, State(s.SurfaceContainerHighest, ink, Press)),
        ];
        var pc = s.PrimaryContainer; // KosLib's primary button keeps the theme text colour, so it sits on the container
        var flair = new KosFlair(s.Primary, s.Tertiary, s.Primary, s.Tertiary, s.SurfaceContainerLow, s.OutlineVariant, false, 6,
            (pc, State(pc, s.OnPrimaryContainer, Hover), State(pc, s.OnPrimaryContainer, Press)));
        return new KosWindowPalette(bg, ink, window, components, flair);
    }

    /// <summary>Black or white, whichever reads better on <paramref name="fill"/> (always 4.58:1 or more).</summary>
    public static Vector4 OnColor(Vector4 fill)
    {
        Vector4 black = new(0, 0, 0, 1), white = new(1, 1, 1, 1);
        return KosColor.Contrast(black, fill with { W = 1 }) >= KosColor.Contrast(white, fill with { W = 1 }) ? black : white;
    }

    // ---- CIELAB LCh (D65) --------------------------------------------------------------------------------------

    /// <summary>The sRGB colour at <paramref name="tone"/> (L*, 0..100), hue in degrees; chroma is lowered until it fits sRGB.</summary>
    public static Vector4 Tone(float hue, float chroma, float tone)
    {
        var c = FromLch(tone, chroma, hue, out var inGamut);
        if (inGamut) return Clamp(c);
        float lo = 0, hi = chroma;
        for (var i = 0; i < 16; i++)
        {
            var mid = (lo + hi) / 2;
            FromLch(tone, mid, hue, out var ok);
            if (ok) lo = mid; else hi = mid;
        }
        return Clamp(FromLch(tone, lo, hue, out _));
    }

    /// <summary>L* (0..100), chroma and hue (degrees) of the RGB part.</summary>
    public static (float L, float C, float H) ToLch(Vector4 c)
    {
        float r = Lin(c.X), g = Lin(c.Y), b = Lin(c.Z);
        var fx = F((0.4124564f * r + 0.3575761f * g + 0.1804375f * b) / 0.95047f);
        var fy = F(0.2126729f * r + 0.7151522f * g + 0.0721750f * b);
        var fz = F((0.0193339f * r + 0.1191920f * g + 0.9503041f * b) / 1.08883f);
        float l = 116 * fy - 16, a = 500 * (fx - fy), bb = 200 * (fy - fz);
        var h = MathF.Atan2(bb, a) * 180 / MathF.PI;
        return (l, MathF.Sqrt(a * a + bb * bb), h < 0 ? h + 360 : h);

        static float F(float t) => t > 216f / 24389 ? MathF.Cbrt(t) : t * 24389 / 3132 + 4f / 29;
    }

    private static Vector4 FromLch(float l, float chroma, float hue, out bool inGamut)
    {
        var rad = hue * MathF.PI / 180;
        var fy = (l + 16) / 116;
        var fx = fy + chroma * MathF.Cos(rad) / 500;
        var fz = fy - chroma * MathF.Sin(rad) / 200;
        float x = 0.95047f * Inv(fx), y = Inv(fy), z = 1.08883f * Inv(fz);
        var r = 3.2404542f * x - 1.5371385f * y - 0.4985314f * z;
        var g = -0.9692660f * x + 1.8760108f * y + 0.0415560f * z;
        var b = 0.0556434f * x - 0.2040259f * y + 1.0572252f * z;
        const float e = 1e-4f;
        inGamut = r >= -e && r <= 1 + e && g >= -e && g <= 1 + e && b >= -e && b <= 1 + e;
        return new Vector4(Gamma(r), Gamma(g), Gamma(b), 1);

        static float Inv(float t) => t > 6f / 29 ? t * t * t : 3132f / 24389 * (t - 4f / 29);
        static float Gamma(float v) => v <= 0.0031308f ? 12.92f * v : 1.055f * MathF.Pow(MathF.Max(v, 0), 1 / 2.4f) - 0.055f;
    }

    private static Vector4 Clamp(Vector4 c) => Vector4.Clamp(c, Vector4.Zero, Vector4.One);

    private static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
}
