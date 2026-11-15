#nullable enable
using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

/// <summary>Every component in every state and every preset: the in-game visual check. The host plugin calls
/// <see cref="Draw"/> from its own debug section (KosLib registers no command: each plugin carries its own copy).</summary>
internal static class KosGallery
{
    private static bool tableInCard;
    private static bool check = true;
    private static float slider = 0.4f;
    private static int segment;
    private static KosAccent preview = KosAccent.KosMos;

    public static void Draw()
    {
        ImGui.SetNextItemWidth(160);
        using (var combo = ImRaii.Combo("Preview preset", KosTheme.Label(preview)))
        {
            if (combo)
                foreach (var a in Enum.GetValues<KosAccent>())
                    if (ImGui.Selectable(KosTheme.Label(a), a == preview)) preview = a;
        }

        using (KosTheme.Push(preview)) Components();

        Kos.Section("Every preset", "03");
        foreach (var accent in Enum.GetValues<KosAccent>())
        {
            using var id = ImRaii.PushId((int)accent);
            using var theme = KosTheme.Push(accent);
            ImGui.TextUnformatted(accent == KosAccent.Dalamud ? "Dalamud style (pushes nothing, so this row shows the host window's own look)" : KosTheme.Label(accent));
            Kos.PrimaryButton(FontAwesomeIcon.Play, "Primary");
            ImGui.SameLine();
            ImGui.Button("Button");
            ImGui.SameLine();
            ImGui.Checkbox("Check", ref check);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(120);
            ImGui.SliderFloat("Slider", ref slider, 0, 1);
            ImGui.Selectable("Selectable (header colours)", true);
            using var bar = ImRaii.TabBar("##tabs");
            if (!bar) continue;
            using (var t = ImRaii.TabItem("Tab one")) if (t) ImGui.TextDisabled("Tab one body.");
            using (var t = ImRaii.TabItem("Tab two")) if (t) ImGui.TextDisabled("Tab two body.");
        }

        WholeWindow();
    }

    /// <summary>
    /// The opt-in whole-window palette for the preview preset, shown inside a framed child. Mid-window the WindowBg
    /// push can't repaint the host, so the child takes the palette's window colour as its own background.
    /// </summary>
    private static void WholeWindow()
    {
        Kos.Section("Whole window (opt-in)", "04");
        if (preview == KosAccent.Dalamud)
        {
            ImGui.TextDisabled("Dalamud style has no whole-window palette: pick another preview preset.");
            return;
        }

        using var theme = KosTheme.PushWindow(preview);
        var windowBg = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] with { W = 1 };
        using var bg = ImRaii.PushColor(ImGuiCol.ChildBg, windowBg);
        using var child = ImRaii.Child("##koswindow", new Vector2(-1, 190 * ImGuiHelpers.GlobalScale), true);
        if (!child) return;
        ImGui.TextUnformatted($"{KosTheme.Label(preview)}: background, text, borders and title colours all come from the accent.");
        ImGui.TextDisabled("Disabled text. The real window also keeps your background alpha.");
        Kos.PrimaryButton(FontAwesomeIcon.Play, "Primary");
        ImGui.SameLine();
        ImGui.Button("Button");
        ImGui.SameLine();
        ImGui.Checkbox("Check##win", ref check);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        ImGui.SliderFloat("Slider##win", ref slider, 0, 1);
        ImGui.Separator();
        Kos.Card("wincard", null, () => Kos.KeyValue("Card", "on the whole-window palette"));
        using var table = ImRaii.Table("##wintable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);
        if (!table) return;
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Value");
        ImGui.TableHeadersRow();
        for (var i = 1; i <= 3; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.TextUnformatted($"Row {i}");
            ImGui.TableNextColumn(); ImGui.TextUnformatted((i * 111).ToString());
        }
    }

    private static void Components()
    {
        Kos.Section("Badges and marks", "01");
        foreach (var (name, color) in new[]
        {
            ("Ok", ImGuiColors.HealerGreen), ("Warn", ImGuiColors.DalamudYellow), ("Danger", ImGuiColors.DalamudRed),
            ("Info", ImGuiColors.TankBlue), ("Muted", ImGuiColors.DalamudGrey), ("Debug", ImGuiColors.DalamudOrange),
        })
        {
            Kos.Badge(name, color);
            ImGui.SameLine();
        }
        ImGui.NewLine();
        Kos.Dot(ImGuiColors.HealerGreen);
        ImGui.SameLine();
        ImGui.TextUnformatted("Status dot");
        ImGui.SameLine();
        Kos.Pips(2, 3, ImGuiColors.DalamudYellow);
        ImGui.SameLine();
        ImGui.TextUnformatted("Pips 2 of 3");
        Kos.HelpMarker("A help marker: hover for a wrapped tooltip.");
        Kos.Segmented("seg", ref segment, "Circle", "List", "Both");
        ImGui.SameLine();
        ImGui.TextDisabled($"Segmented: {segment}");
        Kos.PrimaryButton(FontAwesomeIcon.Play, "Primary", "The one main action of a view.");
        ImGui.SameLine();
        Kos.IconButton(FontAwesomeIcon.Dice, "Icon button", "Tooltip shows even when disabled.");
        ImGui.SameLine();
        using (ImRaii.Disabled()) Kos.PrimaryButton(FontAwesomeIcon.Ban, "Disabled", "Disabled primary.");

        Kos.Section("Cards", "02");
        Kos.Card("plain", null, () =>
        {
            ImGui.TextUnformatted("Card with no edge colour: the preset's own edge.");
            Kos.KeyValue("Race", "Kitsune");
            Kos.KeyValue("Disciplines", "Courtesan, Mercenary, Bard", ImGuiColors.DalamudYellow);
        });
        Kos.Card("edged", ImGuiColors.HealerGreen, () =>
        {
            Kos.Badge("Turns", ImGuiColors.HealerGreen);
            ImGui.SameLine();
            ImGui.TextUnformatted("Card with a coloured edge and a badge.");
            Kos.Callout("A callout: wrapped text with a bar down its left edge, for alerts and notes.", ImGuiColors.DalamudRed);
        });
        Kos.Card("outer", ImGuiColors.TankBlue, () =>
        {
            ImGui.TextUnformatted("Outer card.");
            Kos.Card("inner", ImGuiColors.DalamudRed, () => ImGui.TextUnformatted("Card inside a card: drawn without its own frame."));
        });
        Kos.EmptyState("Empty state: nothing here yet.");

        ImGui.Checkbox("Table inside a card (not yet confirmed in game: tick it once, then look in /xllog for an ImGui assert)", ref tableInCard);
        if (!tableInCard) return;
        Kos.Card("table", null, () =>
        {
            using var table = ImRaii.Table("##kostable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);
            if (!table) return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();
            for (var i = 1; i <= 3; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted($"Row {i}");
                ImGui.TableNextColumn(); ImGui.TextUnformatted((i * 111).ToString());
            }
        });
    }
}
