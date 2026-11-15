#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace KosLib;

/// <summary>The four M3 button styles, most to least emphasis.</summary>
internal enum MaterialButton { Filled, Tonal, Outlined, Text }

// MaterialKos's signature controls. Colours come from the live style, so they follow whatever is pushed: M3 roles
// inside PushWindow(accent, material: true), the accent under KosTheme.Push, the user's own theme under Dalamud style.
// Primary = CheckMark, container = Button, track = FrameBg. GetColorU32 applies style alpha, so ImRaii.Disabled dims them.
// Shortcut: no hover/toggle animation and no keyboard focus ring (same as Kos.Segmented); add them if asked.
internal static partial class MaterialKos
{
    private static Vector4 Col(ImGuiCol c) => ImGui.GetStyle().Colors[(int)c];

    private static string Shown(string label) => label.Split("##")[0];

    /// <summary>A pill-shaped M3 button; returns true when clicked. Use <c>"Label##id"</c> for repeated labels.</summary>
    public static bool Button(string label, MaterialButton kind = MaterialButton.Filled)
    {
        var text = ImGui.CalcTextSize(Shown(label));
        var h = ImGui.GetFrameHeight();
        var padX = MathF.Round(h * 0.6f); // M3: 24 dp side padding on a 40 dp button
        var size = new Vector2(text.X + 2 * padX, h);
        var min = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, size);
        var layer = ImGui.IsItemActive() ? Press : ImGui.IsItemHovered() ? Hover : 0;

        var primary = Col(ImGuiCol.CheckMark) with { W = 1 };
        var (fill, ink) = kind switch
        {
            MaterialButton.Filled => (primary, OnColor(primary)),
            MaterialButton.Tonal => (Col(ImGuiCol.Button), Col(ImGuiCol.Text)),
            _ => (Vector4.Zero, primary),
        };
        var draw = ImGui.GetWindowDrawList();
        var max = min + size;
        var r = h / 2;
        if (fill.W > 0) draw.AddRectFilled(min, max, ImGui.GetColorU32(fill), r);
        if (layer > 0) draw.AddRectFilled(min, max, ImGui.GetColorU32(ink with { W = layer }), r);
        if (kind == MaterialButton.Outlined) draw.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), r, ImDrawFlags.None, ImGuiHelpers.GlobalScale);
        draw.AddText(min + new Vector2(padX, (h - text.Y) / 2), ImGui.GetColorU32(ink), Shown(label));
        return clicked;
    }

    /// <summary>An M3 switch with its label to the right (clicking the label toggles too); true on the frame it changes.</summary>
    public static bool Switch(string label, ref bool value)
    {
        var style = ImGui.GetStyle();
        var shown = Shown(label);
        var text = ImGui.CalcTextSize(shown);
        var h = ImGui.GetFrameHeight();
        var track = new Vector2(MathF.Round(h * 1.625f), h); // M3: 52 x 32 dp
        var size = new Vector2(track.X + (shown.Length > 0 ? style.ItemInnerSpacing.X + text.X : 0), h);
        var min = ImGui.GetCursorScreenPos();
        var changed = ImGui.InvisibleButton(label, size);
        if (changed) value = !value;
        var held = ImGui.IsItemActive();
        var layer = held ? Press : ImGui.IsItemHovered() ? Hover : 0;

        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var r = h / 2;
        var primary = Col(ImGuiCol.CheckMark) with { W = 1 };
        var outline = Col(ImGuiCol.TextDisabled);
        draw.AddRectFilled(min, min + track, ImGui.GetColorU32(value ? primary : Col(ImGuiCol.FrameBg)), r);
        if (!value) draw.AddRect(min, min + track, ImGui.GetColorU32(outline), r, ImDrawFlags.None, 2 * scale);
        // Thumb: 16 dp off, 24 dp on, 28 dp while pressed, centred r from its end of the track.
        var thumb = h * (held ? 0.4375f : value ? 0.375f : 0.25f);
        var centre = new Vector2(value ? min.X + track.X - r : min.X + r, min.Y + r);
        if (layer > 0) draw.AddCircleFilled(centre, r * 1.25f, ImGui.GetColorU32((value ? primary : Col(ImGuiCol.Text)) with { W = layer }), 24);
        draw.AddCircleFilled(centre, thumb, ImGui.GetColorU32(value ? OnColor(primary) : outline), 24);
        if (shown.Length > 0)
            draw.AddText(new Vector2(min.X + track.X + style.ItemInnerSpacing.X, min.Y + (h - text.Y) / 2), ImGui.GetColorU32(ImGuiCol.Text), shown);
        return changed;
    }

    /// <summary>An M3 filter chip: outlined when off, filled with a leading tick when on; true on the frame it changes.</summary>
    public static bool Chip(string label, ref bool selected)
    {
        var shown = Shown(label);
        var text = ImGui.CalcTextSize(shown);
        var h = ImGui.GetFrameHeight();
        var pad = MathF.Round(h * 0.5f); // M3: 16 dp on a 32 dp chip, less on the tick side
        var tick = selected ? MathF.Round(h * 0.55f) : 0;
        var size = new Vector2(text.X + 2 * pad + tick, h);
        var min = ImGui.GetCursorScreenPos();
        var changed = ImGui.InvisibleButton(label, size);
        if (changed) selected = !selected;
        var layer = ImGui.IsItemActive() ? Press : ImGui.IsItemHovered() ? Hover : 0;

        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var max = min + size;
        var r = MathF.Round(h * 0.25f); // M3: 8 dp corners
        var ink = Col(ImGuiCol.Text);
        if (selected) draw.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.Button), r);
        else draw.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), r, ImDrawFlags.None, scale);
        if (layer > 0) draw.AddRectFilled(min, max, ImGui.GetColorU32(ink with { W = layer }), r);
        var x = min.X + pad * 0.75f;
        if (selected) // a drawn tick, never a glyph
        {
            var u = tick / 10;
            var y = min.Y + h / 2;
            var c = ImGui.GetColorU32(ink);
            draw.AddLine(new Vector2(x + u, y), new Vector2(x + 3.5f * u, y + 2.5f * u), c, 1.6f * scale);
            draw.AddLine(new Vector2(x + 3.5f * u, y + 2.5f * u), new Vector2(x + 8.5f * u, y - 3f * u), c, 1.6f * scale);
            x += tick;
        }
        else x = min.X + pad;
        draw.AddText(new Vector2(x, min.Y + (h - text.Y) / 2), ImGui.GetColorU32(ink), shown);
        return changed;
    }

    /// <summary>Off: GlowButton's light rests instead of travelling round the edge when nobody points at it.</summary>
    public static bool Motion { get; set; } = true;

    /// <summary>
    /// A pill button on the container colour whose edge catches a light: it sits nearest the mouse while hovered, with a
    /// soft wash inside, and travels slowly round the edge at rest. Returns true when clicked.
    /// </summary>
    public static bool GlowButton(string label)
    {
        const int n = 64; // edge pieces
        var text = ImGui.CalcTextSize(Shown(label));
        var h = ImGui.GetFrameHeight();
        var padX = MathF.Round(h * 0.6f);
        var size = new Vector2(text.X + 2 * padX, h);
        var min = ImGui.GetCursorScreenPos();
        var clicked = ImGui.InvisibleButton(label, size);
        var hovered = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var r = h / 2;
        var primary = Col(ImGuiCol.CheckMark) with { W = 1 };
        var ink = Col(ImGuiCol.Text);
        draw.AddRectFilled(min, min + size, ImGui.GetColorU32(ImGuiCol.Button), r);
        var mouse = ImGui.GetMousePos();
        if (hovered) // the wash: rings on the pill's middle line, never outside its ends
        {
            var c = new Vector2(Math.Clamp(mouse.X, min.X + r, min.X + size.X - r), min.Y + r);
            foreach (var k in new[] { 1f, 0.7f, 0.4f }) draw.AddCircleFilled(c, r * k, ImGui.GetColorU32(primary with { W = held ? 0.09f : 0.06f }), 32);
        }
        draw.AddRect(min, min + size, ImGui.GetColorU32(ImGuiCol.Border), r, ImDrawFlags.None, scale);

        var light = Motion ? (float)(ImGui.GetTime() % 6 / 6) : 0; // one lap in 6 s
        if (hovered)
        {
            var best = float.MaxValue;
            for (var k = 0; k < n; k++)
            {
                var d = Vector2.DistanceSquared(PillPoint(min, size, (float)k / n), mouse);
                if (d < best) (best, light) = (d, (float)k / n);
            }
        }
        var lit = hovered ? 1f : 0.55f;
        for (var k = 0; k < n; k++)
        {
            float u0 = (float)k / n, u1 = (float)(k + 1) / n;
            var off = MathF.Abs((u0 + u1) / 2 - light);
            off = MathF.Min(off, 1 - off); // round the edge either way
            var a = MathF.Max(0, 1 - off / 0.18f); // the light covers about a third of the edge
            if (a <= 0) continue;
            draw.AddLine(PillPoint(min, size, u0), PillPoint(min, size, u1), ImGui.GetColorU32(primary with { W = a * a * lit }), 1.5f * scale);
        }
        draw.AddText(min + new Vector2(padX, (h - text.Y) / 2), ImGui.GetColorU32(ink), Shown(label));
        return clicked;
    }

    /// <summary>
    /// Slide to confirm, for actions that must not happen by accident: drag the knob all the way across, or hold
    /// Space/Enter on it for a second. Let go early and it springs back. True on the frame it confirms.
    /// </summary>
    public static bool Slide(string label, float width = 0)
    {
        var shown = Shown(label);
        var text = ImGui.CalcTextSize(shown);
        var h = ImGui.GetFrameHeight();
        var size = new Vector2(width > 0 ? width : MathF.Max(text.X + 3 * h, 8 * h), h);
        var min = ImGui.GetCursorScreenPos();
        var id = ImGui.GetID(label);
        var store = ImGui.GetStateStorage();
        ImGui.InvisibleButton(label, size);
        var active = ImGui.IsItemActive();
        var mouseHeld = active && ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var travel = size.X - h;
        if (ImGui.IsItemActivated()) store.SetFloat(id + 1, Math.Max(store.GetFloat(id), 0)); // where the knob was when grabbed
        float? drag = mouseHeld ? store.GetFloat(id + 1) + ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0).X / travel : null;
        var (knob, confirmed) = SlideStep(store.GetFloat(id), ImGui.GetIO().DeltaTime, drag, active && !mouseHeld);
        store.SetFloat(id, knob);
        if (ImGui.IsItemHovered() && !active) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

        var draw = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var r = h / 2;
        var at = knob < 0 ? 1 : knob;
        var primary = Col(ImGuiCol.CheckMark) with { W = 1 };
        var centre = new Vector2(min.X + r + at * travel, min.Y + r);
        draw.AddRectFilled(min, min + size, ImGui.GetColorU32(ImGuiCol.FrameBg), r);
        draw.AddRectFilled(min, new Vector2(centre.X + r, min.Y + h), ImGui.GetColorU32(ImGuiCol.Button), r); // the trail
        draw.AddText(new Vector2(min.X + h + (travel - text.X) / 2, min.Y + (h - text.Y) / 2), ImGui.GetColorU32(ImGuiCol.Text, 1 - 0.8f * at), shown);
        draw.AddCircleFilled(centre, r - 2 * scale, ImGui.GetColorU32(primary), 32);
        var u = r * 0.28f; // a drawn chevron, never a glyph
        var c = ImGui.GetColorU32(OnColor(primary));
        draw.AddLine(centre + new Vector2(-u * 0.5f, -u), centre + new Vector2(u * 0.5f, 0), c, 2 * scale);
        draw.AddLine(centre + new Vector2(u * 0.5f, 0), centre + new Vector2(-u * 0.5f, u), c, 2 * scale);
        return confirmed;
    }
}
