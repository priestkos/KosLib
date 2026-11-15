#nullable enable
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

/// <summary>Accent presets, pushed for one window's Draw() and popped when the returned disposable ends.</summary>
internal static class KosTheme
{
    private static KosAccent lastAccent;
    private static System.Numerics.Vector4 lastBg, lastText;
    private static (KosSlot Slot, System.Numerics.Vector4 Color)[] last = [];

    public static string Label(KosAccent a) => a == KosAccent.Dalamud ? "Dalamud style" : a.ToString();

    /// <summary><c>using var _ = KosTheme.Push(config.Accent);</c> at the top of Draw(). Dalamud style pushes nothing.</summary>
    public static ImRaii.ColorDisposable Push(KosAccent accent)
    {
        var colors = new ImRaii.ColorDisposable();
        if (accent == KosAccent.Dalamud) return colors;
        var style = ImGui.GetStyle();
        var bg = style.Colors[(int)ImGuiCol.WindowBg];
        var text = style.Colors[(int)ImGuiCol.Text];
        if (accent != lastAccent || bg != lastBg || text != lastText) // derive once per change, not per frame
            (lastAccent, lastBg, lastText, last) = (accent, bg, text, KosColor.Derive(accent, bg, text));
        foreach (var (slot, color) in last) colors.Push(Col(slot), color);
        return colors;
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
