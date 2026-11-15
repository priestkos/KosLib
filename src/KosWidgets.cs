#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

// The M2 common set. Every colour comes from the live style or from KosTheme.Flair (null under Dalamud style, where
// these draw in plain theme colours). Status marks are drawn shapes, never Unicode glyphs.
internal static partial class Kos
{
    /// <summary>
    /// Optional monospaced font for section tags, like the profile site's JetBrains Mono chapter tags. The host sets
    /// it to <c>UiBuilder.MonoFontHandle</c> (Dalamud owns that handle: never dispose it) and clears it on Dispose.
    /// </summary>
    public static IFontHandle? Mono { get; set; }

    /// <summary>Section header: a drawn diamond, an optional number, the title in capitals and a rule to the right edge.</summary>
    public static void Section(string title, string? number = null)
    {
        var flair = KosTheme.Flair;
        var scale = ImGuiHelpers.GlobalScale;
        var draw = ImGui.GetWindowDrawList();
        ImGui.Spacing();
        var start = ImGui.GetCursorScreenPos();
        var right = start.X + ImGui.GetContentRegionAvail().X;

        IDisposable? font = Mono is { Available: true } mono ? mono.Push() : null; // pushed only once its atlas is built
        try
        {
            var line = ImGui.GetTextLineHeight();
            var d = MathF.Round(line * 0.24f);
            var mid = new Vector2(start.X + d + scale, start.Y + line / 2);
            var markColor = ImGui.GetColorU32(flair?.TagNumber ?? ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
            // The site's diamond bullet: an outline with a filled centre.
            draw.AddQuad(mid - new Vector2(0, d), mid + new Vector2(d, 0), mid + new Vector2(0, d), mid - new Vector2(d, 0), markColor, scale);
            var inner = d * 0.45f;
            draw.AddQuadFilled(mid - new Vector2(0, inner), mid + new Vector2(inner, 0), mid + new Vector2(0, inner), mid - new Vector2(inner, 0), markColor);
            ImGui.Dummy(new Vector2(2 * d + 7 * scale, line));
            ImGui.SameLine(0, 0);
            if (number is not null)
            {
                ImGui.TextColored(flair?.TagNumber ?? ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled], number);
                ImGui.SameLine(0, 0);
                ImGui.TextDisabled(" / ");
                ImGui.SameLine(0, 0);
            }
            if (flair is { } f) ImGui.TextColored(f.Tag, title.ToUpperInvariant());
            else ImGui.TextUnformatted(title.ToUpperInvariant());
        }
        finally
        {
            font?.Dispose();
        }

        var textEnd = ImGui.GetItemRectMax();
        var y = MathF.Round((ImGui.GetItemRectMin().Y + textEnd.Y) / 2);
        var x0 = textEnd.X + 8 * scale;
        if (right - x0 > 4 * scale)
        {
            if (flair is { } f) EdgeLine(draw, new Vector2(x0, y), right - x0, scale, f.EdgeA, f.EdgeB with { W = 0 }, fadeIn: false);
            else draw.AddLine(new Vector2(x0, y), new Vector2(right, y), ImGui.GetColorU32(ImGuiCol.Separator), scale);
        }
    }

    /// <summary>A read-out row: muted label on the left, value right-aligned (the site's spec rows).</summary>
    public static void KeyValue(string label, string value, Vector4? valueColor = null)
    {
        ImGui.TextDisabled(label);
        ImGui.SameLine();
        var w = ImGui.CalcTextSize(value).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0, ImGui.GetContentRegionAvail().X - w));
        if (valueColor is { } c) ImGui.TextColored(c, value);
        else ImGui.TextUnformatted(value);
    }

    /// <summary>
    /// A row of 2-4 joined toggle buttons for one choice (instead of a radio-button row). Returns true on the frame
    /// the choice changes. Draws as one group, so <c>SameLine</c> after it works.
    /// </summary>
    public static bool Segmented(string id, ref int index, params string[] options)
    {
        var flair = KosTheme.Flair;
        var style = ImGui.GetStyle();
        var scale = ImGuiHelpers.GlobalScale;
        var h = ImGui.GetFrameHeight();
        var padX = style.FramePadding.X * 1.5f;
        var draw = ImGui.GetWindowDrawList();
        var changed = false;
        using var pushId = ImRaii.PushId(id);
        using var group = ImRaii.Group();
        var start = ImGui.GetCursorScreenPos();
        var total = 0f;
        foreach (var o in options) total += ImGui.CalcTextSize(o).X + 2 * padX;
        var rounding = style.FrameRounding;
        draw.AddRectFilled(start, start + new Vector2(total, h), ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);

        var x = start.X;
        for (var i = 0; i < options.Length; i++)
        {
            var text = ImGui.CalcTextSize(options[i]);
            var w = text.X + 2 * padX;
            var pos = new Vector2(x, start.Y);
            ImGui.SetCursorScreenPos(pos);
            if (ImGui.InvisibleButton($"##seg{i}", new Vector2(w, h)) && index != i)
            {
                index = i;
                changed = true;
            }
            var selected = i == index;
            var hovered = ImGui.IsItemHovered();
            var corners = options.Length == 1 ? ImDrawFlags.RoundCornersAll
                : i == 0 ? ImDrawFlags.RoundCornersLeft
                : i == options.Length - 1 ? ImDrawFlags.RoundCornersRight : ImDrawFlags.RoundCornersNone;
            if (selected || hovered)
                draw.AddRectFilled(pos, pos + new Vector2(w, h), ImGui.GetColorU32(selected ? ImGuiCol.ButtonActive : ImGuiCol.FrameBgHovered), rounding, corners);
            if (selected && flair is { } f) // the site's active chapter pill: a gold outline
                draw.AddRect(pos, pos + new Vector2(w, h), ImGui.GetColorU32(f.Tag with { W = 0.85f }), rounding, corners, scale);
            draw.AddText(pos + new Vector2(padX, (h - text.Y) / 2), ImGui.GetColorU32(selected ? ImGuiCol.Text : ImGuiCol.TextDisabled), options[i]);
            x += w;
        }

        if (style.FrameBorderSize > 0)
            draw.AddRect(start, start + new Vector2(total, h), ImGui.GetColorU32(ImGuiCol.Border), rounding, ImDrawFlags.None, style.FrameBorderSize);
        return changed;
    }

    /// <summary>A greyed (?) after the previous item, with a wrapped tooltip.</summary>
    public static void HelpMarker(string text)
    {
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(text);
    }

    /// <summary>Centred muted text for an empty list ("No rolls yet.").</summary>
    public static void EmptyState(string text)
    {
        ImGui.Spacing();
        var w = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0, (ImGui.GetContentRegionAvail().X - w) / 2));
        ImGui.TextDisabled(text);
        ImGui.Spacing();
    }

    /// <summary>Wrapped text with a coloured bar down its left edge (the site's quote block), for alerts and notes.</summary>
    public static void Callout(string text, Vector4 color)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var bar = 3 * scale;
        var indent = bar + 8 * scale;
        var start = ImGui.GetCursorScreenPos();
        ImGui.Indent(indent);
        try
        {
            using (ImRaii.PushColor(ImGuiCol.Text, color)) ImGui.TextWrapped(text);
        }
        finally
        {
            ImGui.Unindent(indent);
        }
        var end = ImGui.GetItemRectMax();
        ImGui.GetWindowDrawList().AddRectFilled(start, new Vector2(start.X + bar, end.Y), ImGui.GetColorU32(color), bar / 2);
    }

    /// <summary>A status dot one text line tall; under a preset with glow it gets a soft halo.</summary>
    public static void Dot(Vector4 color)
    {
        var line = ImGui.GetTextLineHeight();
        var r = MathF.Round(line * 0.2f);
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(4 * r, line));
        var c = pos + new Vector2(2 * r, line / 2);
        var draw = ImGui.GetWindowDrawList();
        if (KosTheme.Flair is { Glow: true })
        {
            draw.AddCircleFilled(c, r * 2f, ImGui.GetColorU32(color with { W = 0.12f }), 20);
            draw.AddCircleFilled(c, r * 1.5f, ImGui.GetColorU32(color with { W = 0.25f }), 20);
        }
        draw.AddCircleFilled(c, r, ImGui.GetColorU32(color), 16);
    }

    /// <summary>A row of <paramref name="total"/> pips, the first <paramref name="filled"/> solid, one text line tall.</summary>
    public static void Pips(int filled, int total, Vector4 color)
    {
        var line = ImGui.GetTextLineHeight();
        var dot = MathF.Round(line * 0.18f);
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(total * 3 * dot, line));
        Pips(ImGui.GetWindowDrawList(), pos + new Vector2(0, line / 2), filled, total, ImGui.GetColorU32(color), ImGui.GetColorU32(ImGuiCol.TextDisabled), dot);
    }

    /// <summary>Pips drawn straight onto a draw list, left edge at <paramref name="at"/>, 3 radii apart.</summary>
    public static void Pips(ImDrawListPtr draw, Vector2 at, int filled, int total, uint fill, uint empty, float dot)
    {
        var scale = ImGuiHelpers.GlobalScale;
        for (var k = 0; k < total; k++)
        {
            var c = new Vector2(at.X + dot + k * 3 * dot, at.Y);
            if (k < filled) draw.AddCircleFilled(c, dot, fill, 12);
            else draw.AddCircle(c, dot, empty, 12, scale);
        }
    }

    /// <summary>Font Awesome button with text, plus a tooltip that also shows while the button is disabled.</summary>
    public static bool IconButton(FontAwesomeIcon icon, string label, string? tooltip = null)
    {
        var clicked = ImGuiComponents.IconButtonWithText(icon, label);
        if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(tooltip);
        return clicked;
    }

    /// <summary>
    /// The one main action of a view. Under a preset it is filled with the accent (label kept at 4.5:1, tested),
    /// and under KosMos it gets a soft glow; under Dalamud style it is a plain <see cref="IconButton"/>.
    /// </summary>
    public static bool PrimaryButton(FontAwesomeIcon icon, string label, string? tooltip = null)
    {
        if (KosTheme.Flair is not { } f) return IconButton(icon, label, tooltip);
        bool clicked;
        using (ImRaii.PushColor(ImGuiCol.Button, f.Primary.Idle).Push(ImGuiCol.ButtonHovered, f.Primary.Hovered).Push(ImGuiCol.ButtonActive, f.Primary.Active))
            clicked = ImGuiComponents.IconButtonWithText(icon, label);
        if (f.Glow) // GetColorU32 multiplies by style alpha, so inside ImRaii.Disabled the glow dims with the button
        {
            var scale = ImGuiHelpers.GlobalScale;
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var rounding = ImGui.GetStyle().FrameRounding;
            var draw = ImGui.GetWindowDrawList();
            for (var i = 1; i <= 3; i++)
            {
                var g = new Vector2(i * scale);
                draw.AddRect(min - g, max + g, ImGui.GetColorU32(f.EdgeA with { W = 0.30f / i }), rounding + i * scale, ImDrawFlags.None, scale);
            }
        }
        if (tooltip is not null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(tooltip);
        return clicked;
    }

    /// <summary>A horizontal gradient line: fades in from nothing to <paramref name="a"/>, runs into <paramref name="b"/>.</summary>
    private static void EdgeLine(ImDrawListPtr draw, Vector2 at, float width, float thickness, Vector4 a, Vector4 b, bool fadeIn = true)
    {
        if (width <= 0) return;
        var ca = ImGui.GetColorU32(a);
        var cb = ImGui.GetColorU32(b);
        var clear = ImGui.GetColorU32(a with { W = 0 });
        var clearB = ImGui.GetColorU32(b with { W = 0 });
        var y1 = at.Y + thickness;
        if (!fadeIn)
        {
            draw.AddRectFilledMultiColor(at, new Vector2(at.X + width, y1), ca, cb, cb, ca);
            return;
        }
        var third = width / 3;
        draw.AddRectFilledMultiColor(at, new Vector2(at.X + third, y1), clear, ca, ca, clear);
        draw.AddRectFilledMultiColor(new Vector2(at.X + third, at.Y), new Vector2(at.X + 2 * third, y1), ca, cb, cb, ca);
        draw.AddRectFilledMultiColor(new Vector2(at.X + 2 * third, at.Y), new Vector2(at.X + width, y1), cb, clearB, clearB, cb);
    }
}
