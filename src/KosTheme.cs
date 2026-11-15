#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

/// <summary>
/// Accent presets. <see cref="Push"/> colours KosLib's slots for one window's Draw(); the opt-in
/// <see cref="PushWindow"/> colours the whole window and has to run in PreDraw, before ImGui begins the window.
/// </summary>
internal static class KosTheme
{
    private static KosAccent lastAccent;
    private static Vector4 lastBg, lastText;
    private static (KosSlot Slot, Vector4 Color)[] last = [];
    private static KosFlair? lastFlair;

    private static KosAccent winAccent;
    private static bool winMaterial;
    private static Vector4 winBg, winText;
    private static KosWindowPalette? winLast;

    /// <summary>The accent of the whole-window scope in force, or null outside one.</summary>
    private static KosAccent? windowAccent;

    /// <summary>The drawn extras of the innermost pushed preset; null outside any push and under Dalamud style.</summary>
    public static KosFlair? Flair { get; private set; }

    public static string Label(KosAccent a) => a == KosAccent.Dalamud ? "Dalamud style" : a.ToString();

    /// <summary>
    /// <c>using var _ = KosTheme.Push(config.Accent);</c> at the top of Draw(). Dalamud style pushes nothing.
    /// Colours (and, for KosMos, frame/grab/tab rounding) are popped and the outer flair restored on dispose.
    /// Inside a whole-window scope for the same accent it pushes nothing, because that scope already pushed these slots.
    /// </summary>
    public static Scope Push(KosAccent accent)
    {
        var previous = Flair;
        var colors = new ImRaii.ColorDisposable();
        var styles = new ImRaii.StyleDisposable();
        if (accent != KosAccent.Dalamud && windowAccent == accent)
            return new Scope(colors, styles, previous, windowAccent);

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

        return new Scope(colors, styles, previous, windowAccent);
    }

    /// <summary>
    /// Opt-in: colours the whole window, background, title bar and text included, from <paramref name="accent"/>.
    /// Call it from the window's <c>PreDraw()</c> and dispose the scope in <c>PostDraw()</c>: WindowBg, the title bar
    /// and the window rounding only take effect when they are pushed before ImGui begins the window. Dalamud's window
    /// host calls PostDraw whenever it called PreDraw, so the pair stays balanced even if Draw throws.
    /// Dalamud style pushes nothing. The window keeps the user's background alpha and light/dark choice.
    /// <paramref name="material"/> = MaterialKos: M3 tonal colours from the same accent and rounder windows.
    /// </summary>
    public static Scope PushWindow(KosAccent accent, bool material = false)
    {
        var previous = Flair;
        var previousWindow = windowAccent;
        var colors = new ImRaii.ColorDisposable();
        var styles = new ImRaii.StyleDisposable();
        if (accent == KosAccent.Dalamud) return new Scope(colors, styles, previous, previousWindow);

        var style = ImGui.GetStyle();
        var bg = style.Colors[(int)ImGuiCol.WindowBg];
        var text = style.Colors[(int)ImGuiCol.Text];
        if (winLast is null || accent != winAccent || material != winMaterial || bg != winBg || text != winText) // derive once per change
            (winAccent, winMaterial, winBg, winText, winLast) =
                (accent, material, bg, text, material ? MaterialKos.Window(accent, bg, text) : KosColor.Window(accent, bg, text));
        if (winLast is not { } p) return new Scope(colors, styles, previous, previousWindow);

        foreach (var (slot, color) in p.Window) colors.Push(WinCol(slot), color);
        foreach (var (slot, color) in p.Components) colors.Push(Col(slot), color);
        var s = ImGuiHelpers.GlobalScale;
        var frame = (p.Flair.Rounding ?? 5f) * s; // every var here is a float style var (a Vector2 one would assert)
        styles.Push(ImGuiStyleVar.WindowRounding, (material ? 12f : 8f) * s);
        styles.Push(ImGuiStyleVar.ChildRounding, (material ? 12f : 6f) * s);
        styles.Push(ImGuiStyleVar.PopupRounding, (material ? 8f : 6f) * s);
        styles.Push(ImGuiStyleVar.ScrollbarRounding, (material ? 12f : 8f) * s);
        styles.Push(ImGuiStyleVar.FrameRounding, frame);
        styles.Push(ImGuiStyleVar.GrabRounding, material ? 12f * s : frame); // M3 slider handles are capsules (ImGui clamps the radius)
        styles.Push(ImGuiStyleVar.TabRounding, frame);
        Flair = p.Flair;
        windowAccent = accent;
        return new Scope(colors, styles, previous, previousWindow);
    }

    /// <summary>Pops what <see cref="Push"/> or <see cref="PushWindow"/> pushed and puts the outer state back.</summary>
    /// <remarks>Disposing twice is harmless: the ImRaii disposables pop only what is still pushed.</remarks>
    internal readonly struct Scope(ImRaii.ColorDisposable colors, ImRaii.StyleDisposable styles, KosFlair? previous, KosAccent? previousWindow)
        : IDisposable
    {
        public void Dispose()
        {
            styles.Dispose();
            colors.Dispose();
            Flair = previous;
            windowAccent = previousWindow;
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

    private static ImGuiCol WinCol(KosWindowSlot s) => s switch
    {
        KosWindowSlot.WindowBg => ImGuiCol.WindowBg,
        KosWindowSlot.ChildBg => ImGuiCol.ChildBg,
        KosWindowSlot.PopupBg => ImGuiCol.PopupBg,
        KosWindowSlot.MenuBarBg => ImGuiCol.MenuBarBg,
        KosWindowSlot.TitleBg => ImGuiCol.TitleBg,
        KosWindowSlot.TitleBgActive => ImGuiCol.TitleBgActive,
        KosWindowSlot.TitleBgCollapsed => ImGuiCol.TitleBgCollapsed,
        KosWindowSlot.Text => ImGuiCol.Text,
        KosWindowSlot.TextDisabled => ImGuiCol.TextDisabled,
        KosWindowSlot.TextSelectedBg => ImGuiCol.TextSelectedBg,
        KosWindowSlot.Border => ImGuiCol.Border,
        KosWindowSlot.BorderShadow => ImGuiCol.BorderShadow,
        KosWindowSlot.Separator => ImGuiCol.Separator,
        KosWindowSlot.SeparatorHovered => ImGuiCol.SeparatorHovered,
        KosWindowSlot.SeparatorActive => ImGuiCol.SeparatorActive,
        KosWindowSlot.ScrollbarBg => ImGuiCol.ScrollbarBg,
        KosWindowSlot.ScrollbarGrab => ImGuiCol.ScrollbarGrab,
        KosWindowSlot.ScrollbarGrabHovered => ImGuiCol.ScrollbarGrabHovered,
        KosWindowSlot.ScrollbarGrabActive => ImGuiCol.ScrollbarGrabActive,
        KosWindowSlot.ResizeGrip => ImGuiCol.ResizeGrip,
        KosWindowSlot.ResizeGripHovered => ImGuiCol.ResizeGripHovered,
        KosWindowSlot.ResizeGripActive => ImGuiCol.ResizeGripActive,
        KosWindowSlot.TableHeaderBg => ImGuiCol.TableHeaderBg,
        KosWindowSlot.TableBorderStrong => ImGuiCol.TableBorderStrong,
        KosWindowSlot.TableBorderLight => ImGuiCol.TableBorderLight,
        KosWindowSlot.TableRowBgAlt => ImGuiCol.TableRowBgAlt,
        KosWindowSlot.PlotLines => ImGuiCol.PlotLines,
        KosWindowSlot.PlotLinesHovered => ImGuiCol.PlotLinesHovered,
        KosWindowSlot.PlotHistogram => ImGuiCol.PlotHistogram,
        KosWindowSlot.PlotHistogramHovered => ImGuiCol.PlotHistogramHovered,
        _ => ImGuiCol.NavHighlight,
    };
}
