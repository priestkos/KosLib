#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

// Small drawn pieces: a framed card and a badge. Colours come from the live style or the caller (semantic
// ImGuiColors), so the user's Dalamud theme stays in charge; nothing is pushed window-wide.
internal static class Kos
{
    private static int cardDepth; // ImGui draws on one thread; a second ChannelsSplit on the same draw list asserts

    /// <summary>A pill with dark text on a solid colour (all the colours used are bright, so it reads on any theme).</summary>
    public static void Badge(string text, Vector4 color)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = ImGui.CalcTextSize(text);
        var padX = 6 * scale;
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(size.X + 2 * padX, size.Y)); // one text line tall, so SameLine text stays aligned
        var draw = ImGui.GetWindowDrawList();
        var grow = new Vector2(0, scale);
        draw.AddRectFilled(pos - grow, pos + new Vector2(size.X + 2 * padX, size.Y) + grow, ImGui.GetColorU32(color), size.Y / 2 + scale);
        draw.AddText(pos + new Vector2(padX, 0), 0xFF000000, text);
    }

    /// <summary>
    /// Full-width framed panel round whatever body draws. The frame goes on a background channel after the body
    /// is laid out. A card inside a card draws its body without a frame instead of nesting the channel split.
    /// Tables use their own splitter, so a table in the body should be safe: confirm in the gallery before relying on it.
    /// </summary>
    public static void Card(string id, Vector4? edge, Action body)
    {
        if (cardDepth > 0)
        {
            using (ImRaii.PushId(id)) body();
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var pad = new Vector2(8, 6) * scale;
        var style = ImGui.GetStyle();
        var draw = ImGui.GetWindowDrawList();
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;

        draw.ChannelsSplit(2);
        cardDepth++;
        Vector2 end;
        try
        {
            draw.ChannelsSetCurrent(1);
            ImGui.SetCursorScreenPos(start + pad);
            using (ImRaii.PushId(id))
            using (ImRaii.Group()) body();
            end = ImGui.GetItemRectMax();
            draw.ChannelsSetCurrent(0);
            var max = new Vector2(start.X + width, end.Y + pad.Y);
            var rounding = MathF.Max(style.FrameRounding, 4 * scale);
            draw.AddRectFilled(start, max, ImGui.GetColorU32(ImGuiCol.FrameBg, 0.6f), rounding);
            draw.AddRect(start, max, edge is { } e ? ImGui.GetColorU32(e with { W = 0.7f }) : ImGui.GetColorU32(ImGuiCol.Border), rounding, ImDrawFlags.None, scale);
        }
        finally
        {
            cardDepth--;
            draw.ChannelsMerge(); // always, or the window's draw list is left split
        }

        ImGui.SetCursorScreenPos(new Vector2(start.X, end.Y + pad.Y));
        ImGui.Dummy(new Vector2(width, 0));
    }
}
