using System.Numerics;
using KosLib;
using Xunit;

namespace KosLib.Tests;

// The opt-in whole-window palette replaces the window background and text, so everything drawn on it is re-checked.
public class KosWindowTests
{
    private static readonly (string Name, Vector4 Bg, Vector4 Text)[] Themes =
    [
        ("Dalamud Standard", new(0.06f, 0.06f, 0.06f, 0.93f), new(1, 1, 1, 1)),
        ("ImGui light", new(0.94f, 0.94f, 0.94f, 1), new(0, 0, 0, 1)),
    ];

    private static readonly KosSlot[] UnderText =
    [
        KosSlot.Button, KosSlot.ButtonHovered, KosSlot.ButtonActive,
        KosSlot.Tab, KosSlot.TabHovered, KosSlot.TabActive, KosSlot.TabUnfocused, KosSlot.TabUnfocusedActive,
        KosSlot.Header, KosSlot.HeaderHovered, KosSlot.HeaderActive,
        KosSlot.FrameBg, KosSlot.FrameBgHovered, KosSlot.FrameBgActive,
    ];

    public static TheoryData<KosAccent> Accents()
    {
        var data = new TheoryData<KosAccent>();
        foreach (var a in Enum.GetValues<KosAccent>()) if (a != KosAccent.Dalamud) data.Add(a);
        return data;
    }

    private static Dictionary<KosWindowSlot, Vector4> Win(KosWindowPalette p) => p.Window.ToDictionary(x => x.Slot, x => x.Color);

    [Fact]
    public void DalamudStyleHasNoWindowPalette()
    {
        foreach (var (_, bg, text) in Themes) Assert.Null(KosColor.Window(KosAccent.Dalamud, bg, text));
        Assert.Null(KosColor.Window((KosAccent)99, Themes[0].Bg, Themes[0].Text));
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void EveryWindowSlotOnceAndEveryComponentSlot(KosAccent accent)
    {
        foreach (var (_, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            Assert.Equal(Enum.GetValues<KosWindowSlot>().OrderBy(s => s), p.Window.Select(x => x.Slot).OrderBy(s => s));
            Assert.Equal(Enum.GetValues<KosSlot>().OrderBy(s => s), p.Components.Select(x => x.Slot).OrderBy(s => s));
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void KeepsTheUsersLightnessAndWindowAlpha(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            Assert.Equal(KosColor.IsLight(bg), KosColor.IsLight(p.Bg));
            Assert.Equal(bg.W, Win(p)[KosWindowSlot.WindowBg].W);
            Assert.Equal(1f, p.Bg.W);
            Assert.True(KosColor.Contrast(p.Text, p.Bg) >= 12f, $"{accent} {name}: text on window {KosColor.Contrast(p.Text, p.Bg):F2}:1");
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void TextKeeps7To1OnEveryPanelSurface(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            var w = Win(p);
            foreach (var slot in new[] { KosWindowSlot.PopupBg, KosWindowSlot.MenuBarBg, KosWindowSlot.TitleBg, KosWindowSlot.TableHeaderBg })
            {
                var ratio = KosColor.Contrast(p.Text, KosColor.Over(w[slot], p.Bg));
                Assert.True(ratio >= 7f, $"{accent} {slot}, {name}: {ratio:F2}:1");
            }
            var alt = KosColor.Contrast(p.Text, KosColor.Over(w[KosWindowSlot.TableRowBgAlt], p.Bg));
            Assert.True(alt >= 7f, $"{accent} TableRowBgAlt, {name}: {alt:F2}:1");
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void TitleSelectionAndDisabledTextKeep45To1(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            var w = Win(p);
            var checks = new (string What, Vector4 Ink, Vector4 Under)[]
            {
                ("title (active)", p.Text, KosColor.Over(w[KosWindowSlot.TitleBgActive], p.Bg)),
                ("selected text", p.Text, KosColor.Over(w[KosWindowSlot.TextSelectedBg], p.Bg)),
                ("disabled text", w[KosWindowSlot.TextDisabled], p.Bg),
            };
            foreach (var (what, ink, under) in checks)
            {
                var ratio = KosColor.Contrast(ink, under);
                Assert.True(ratio >= 4.5f, $"{accent} {what}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void ControlLabelsKeep45To1OnTheNewWindow(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            var c = p.Components.ToDictionary(x => x.Slot, x => x.Color);
            foreach (var slot in UnderText)
            {
                var ratio = KosColor.Contrast(p.Text, KosColor.Over(c[slot], p.Bg));
                Assert.True(ratio >= 4.5f, $"{accent} {slot}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void MarksKeep3To1AgainstEveryFrameState(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            var c = p.Components.ToDictionary(x => x.Slot, x => x.Color);
            foreach (var mark in new[] { KosSlot.CheckMark, KosSlot.SliderGrab, KosSlot.SliderGrabActive })
            foreach (var frame in new[] { KosSlot.FrameBg, KosSlot.FrameBgHovered, KosSlot.FrameBgActive })
            {
                var ratio = KosColor.Contrast(c[mark], KosColor.Over(c[frame], p.Bg));
                Assert.True(ratio >= 3f, $"{accent} {mark} on {frame}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void FlairStaysReadableOnTheNewWindow(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Window(accent, bg, text)!;
            var f = p.Flair;
            var card = f.CardFill is { } night ? KosColor.Over(night, p.Bg) : p.Bg;
            foreach (var (what, ink) in new[] { ("Tag", f.Tag), ("TagNumber", f.TagNumber) })
            foreach (var (surface, under) in new[] { ("window", p.Bg), ("card", card) })
            {
                var ratio = KosColor.Contrast(ink, under);
                Assert.True(ratio >= 4.5f, $"{accent} {what} on {surface}, {name}: {ratio:F2}:1");
            }
            foreach (var (state, fill) in new[] { ("idle", f.Primary.Idle), ("hovered", f.Primary.Hovered), ("active", f.Primary.Active) })
            {
                var ratio = KosColor.Contrast(p.Text, fill);
                Assert.True(ratio >= 4.5f, $"{accent} primary {state}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Fact]
    public void HslRoundTrips()
    {
        foreach (var a in Enum.GetValues<KosAccent>())
        {
            if (a == KosAccent.Dalamud) continue;
            var c = KosColor.Accent(a);
            var (h, s, l) = KosColor.ToHsl(c);
            var back = KosColor.FromHsl(h, s, l);
            Assert.True(Vector4.Distance(c, back) < 1e-4f, $"{a}: {c} -> {back}");
        }
        Assert.Equal(new Vector4(0.5f, 0.5f, 0.5f, 1), KosColor.FromHsl(0.3f, 0, 0.5f));
    }
}
