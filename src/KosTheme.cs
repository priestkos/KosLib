#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

/// <summary>Accent presets, pushed for one window's Draw() and popped when the returned scope ends.</summary>
internal static class KosTheme
{
    private static KosAccent lastAccent;
    private static Vector4 lastBg, lastText;
    private static (KosSlot Slot, Vector4 Color)[] last = [];
    private static KosFlair? lastFlair;

    /// <summary>The drawn extras of the innermost pushed preset; null outside any push and under Dalamud style.</summary>
    public static KosFlair? Flair { get; private set; }

    public static string Label(KosAccent a) => a == KosAccent.Dalamud ? "Dalamud style" : a.ToString();

    /// <summary>
    /// <c>using var _ = KosTheme.Push(config.Accent);</c> at the top of Draw(). Dalamud style pushes nothing.
    /// Colours (and, for KosMos, frame/grab/tab rounding) are popped and the outer flair restored on dispose.
    /// </summary>
    public static Scope Push(KosAccent accent)
    {
        var previous = Flair;
        var colors = new ImRaii.ColorDisposable();
        var styles = new ImRaii.StyleDisposable();
        Flair = null;
        if (accent != KosAccent.Dalamud)
        {
            var style = ImGui.GetStyle();
            var bg = style.Colors[(int)ImGuiCol.WindowBg];
            var text = style.Colors[(int)ImGuiCol.Text];
            if (accent != lastAccent || bg != lastBg || text != lastText) // derive once per change, not per frame
                (lastAccent, lastBg, lastText, last, lastFlair) = (accent, bg, text, KosColor.Derive(accent, bg, text), KosColor.Flair(accent, bg, text));
            foreach (var (slot, color) in last) colors.Push(Col(slot), color);
            if (lastFlair?.Rounding is { } r)
            {
                var px = r * ImGuiHelpers.GlobalScale; // all three are float style vars (a Vector2 one would assert)
                styles.Push(ImGuiStyleVar.FrameRounding, px);
                styles.Push(ImGuiStyleVar.GrabRounding, px);
                styles.Push(ImGuiStyleVar.TabRounding, px);
            }
            Flair = lastFlair;
        }

        return new Scope(colors, styles, previous);
    }

    /// <summary>Pops what <see cref="Push"/> pushed and puts the outer preset's flair back.</summary>
    internal readonly struct Scope(ImRaii.ColorDisposable colors, ImRaii.StyleDisposable styles, KosFlair? previous) : IDisposable
    {
        public void Dispose()
        {
            styles.Dispose();
            colors.Dispose();
            Flair = previous;
        }
    }

    private static ImGuiCol Col(KosSlot s) => s switch
    {
        KosSlot.Button => ImGuiCol.Button,
        KosSlot.ButtonHovered => ImGuiCol.ButtonHovered,
        KosSlot.ButtonActive => ImGuiCol.ButtonActive,
        KosSlot.Tab => ImGuiCol.Tab,
        KosSlot.TabHovered => ImGuiCol.TabHovered,
        KosSlot.TabActive => ImGuiCol.TabActive,
        KosSlot.TabUnfocused => ImGuiCol.TabUnfocused,
        KosSlot.TabUnfocusedActive => ImGuiCol.TabUnfocusedActive,
        KosSlot.Header => ImGuiCol.Header,
        KosSlot.HeaderHovered => ImGuiCol.HeaderHovered,
        KosSlot.HeaderActive => ImGuiCol.HeaderActive,
        KosSlot.CheckMark => ImGuiCol.CheckMark,
        KosSlot.SliderGrab => ImGuiCol.SliderGrab,
        KosSlot.SliderGrabActive => ImGuiCol.SliderGrabActive,
        KosSlot.FrameBg => ImGuiCol.FrameBg,
        KosSlot.FrameBgHovered => ImGuiCol.FrameBgHovered,
        _ => ImGuiCol.FrameBgActive,
    };
}
