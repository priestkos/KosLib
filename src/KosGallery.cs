#nullable enable
using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

namespace KosLib;

/// <summary>Every component in every state and every preset: the in-game visual check. The host plugin calls
/// <see cref="Draw"/> from its own debug section (KosLib registers no command: each plugin carries its own copy).</summary>
internal static class KosGallery
{
    private static bool tableInCard;
    private static bool check = true;
    private static float slider = 0.4f;

    public static void Draw()
    {
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

        Kos.Card("plain", null, () => ImGui.TextUnformatted("Card with the theme's border."));
        Kos.Card("edged", ImGuiColors.HealerGreen, () =>
        {
            Kos.Badge("Turns", ImGuiColors.HealerGreen);
            ImGui.SameLine();
            ImGui.TextUnformatted("Card with a coloured edge and a badge.");
        });
        Kos.Card("outer", ImGuiColors.TankBlue, () =>
        {
            ImGui.TextUnformatted("Outer card.");
            Kos.Card("inner", ImGuiColors.DalamudRed, () => ImGui.TextUnformatted("Card inside a card: drawn without its own frame."));
        });

        ImGui.Checkbox("Table inside a card (not yet confirmed in game: tick it once, then look in /xllog for an ImGui assert)", ref tableInCard);
        if (tableInCard)
        {
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

        ImGui.Separator();
        foreach (var accent in Enum.GetValues<KosAccent>())
        {
            using var id = ImRaii.PushId((int)accent);
            using var colors = KosTheme.Push(accent);
            ImGui.TextUnformatted(KosTheme.Label(accent));
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
    }
}
