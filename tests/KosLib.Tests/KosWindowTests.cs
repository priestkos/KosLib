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

    // Every check runs for both looks: the KosLib palette and MaterialKos.
    public static TheoryData<KosAccent, bool> Accents()
    {
        var data = new TheoryData<KosAccent, bool>();
        foreach (var a in Enum.GetValues<KosAccent>())
            if (a != KosAccent.Dalamud) { data.Add(a, false); data.Add(a, true); }
        return data;
    }

    private static KosWindowPalette P(KosAccent accent, bool material, Vector4 bg, Vector4 text) =>
        (material ? MaterialKos.Window(accent, bg, text) : KosColor.Window(accent, bg, text))!;

    private static Dictionary<KosWindowSlot, Vector4> Win(KosWindowPalette p) => p.Window.ToDictionary(x => x.Slot, x => x.Color);

    [Fact]
    public void DalamudStyleHasNoWindowPalette()
    {
        foreach (var (_, bg, text) in Themes)
        {
            Assert.Null(KosColor.Window(KosAccent.Dalamud, bg, text));
            Assert.Null(MaterialKos.Window(KosAccent.Dalamud, bg, text));
        }
        Assert.Null(KosColor.Window((KosAccent)99, Themes[0].Bg, Themes[0].Text));
        Assert.Null(MaterialKos.Window((KosAccent)99, Themes[0].Bg, Themes[0].Text));
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void EveryWindowSlotOnceAndEveryComponentSlot(KosAccent accent, bool material)
    {
        foreach (var (_, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            Assert.Equal(Enum.GetValues<KosWindowSlot>().OrderBy(s => s), p.Window.Select(x => x.Slot).OrderBy(s => s));
            Assert.Equal(Enum.GetValues<KosSlot>().OrderBy(s => s), p.Components.Select(x => x.Slot).OrderBy(s => s));
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void KeepsTheUsersLightnessAndWindowAlpha(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            Assert.Equal(KosColor.IsLight(bg), KosColor.IsLight(p.Bg));
            Assert.Equal(bg.W, Win(p)[KosWindowSlot.WindowBg].W);
            Assert.Equal(1f, p.Bg.W);
            Assert.True(KosColor.Contrast(p.Text, p.Bg) >= 12f, $"{accent}{(material ? " M3" : "")} {name}: text on window {KosColor.Contrast(p.Text, p.Bg):F2}:1");
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void TextKeeps7To1OnEveryPanelSurface(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            var w = Win(p);
            foreach (var slot in new[] { KosWindowSlot.PopupBg, KosWindowSlot.MenuBarBg, KosWindowSlot.TitleBg, KosWindowSlot.TableHeaderBg })
            {
                var ratio = KosColor.Contrast(p.Text, KosColor.Over(w[slot], p.Bg));
                Assert.True(ratio >= 7f, $"{accent}{(material ? " M3" : "")} {slot}, {name}: {ratio:F2}:1");
            }
            var alt = KosColor.Contrast(p.Text, KosColor.Over(w[KosWindowSlot.TableRowBgAlt], p.Bg));
            Assert.True(alt >= 7f, $"{accent}{(material ? " M3" : "")} TableRowBgAlt, {name}: {alt:F2}:1");
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void TitleSelectionAndDisabledTextKeep45To1(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
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
                Assert.True(ratio >= 4.5f, $"{accent}{(material ? " M3" : "")} {what}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void ControlLabelsKeep45To1OnTheNewWindow(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            var c = p.Components.ToDictionary(x => x.Slot, x => x.Color);
            foreach (var slot in UnderText)
            {
                var ratio = KosColor.Contrast(p.Text, KosColor.Over(c[slot], p.Bg));
                Assert.True(ratio >= 4.5f, $"{accent}{(material ? " M3" : "")} {slot}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void MarksKeep3To1AgainstEveryFrameState(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            var c = p.Components.ToDictionary(x => x.Slot, x => x.Color);
            foreach (var mark in new[] { KosSlot.CheckMark, KosSlot.SliderGrab, KosSlot.SliderGrabActive })
            foreach (var frame in new[] { KosSlot.FrameBg, KosSlot.FrameBgHovered, KosSlot.FrameBgActive })
            {
                var ratio = KosColor.Contrast(c[mark], KosColor.Over(c[frame], p.Bg));
                Assert.True(ratio >= 3f, $"{accent}{(material ? " M3" : "")} {mark} on {frame}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void FlairStaysReadableOnTheNewWindow(KosAccent accent, bool material)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = P(accent, material, bg, text);
            var f = p.Flair;
            var card = f.CardFill is { } night ? KosColor.Over(night, p.Bg) : p.Bg;
            foreach (var (what, ink) in new[] { ("Tag", f.Tag), ("TagNumber", f.TagNumber) })
            foreach (var (surface, under) in new[] { ("window", p.Bg), ("card", card) })
            {
                var ratio = KosColor.Contrast(ink, under);
                Assert.True(ratio >= 4.5f, $"{accent}{(material ? " M3" : "")} {what} on {surface}, {name}: {ratio:F2}:1");
            }
            foreach (var (state, fill) in new[] { ("idle", f.Primary.Idle), ("hovered", f.Primary.Hovered), ("active", f.Primary.Active) })
            {
                var ratio = KosColor.Contrast(p.Text, fill);
                Assert.True(ratio >= 4.5f, $"{accent}{(material ? " M3" : "")} primary {state}, {name}: {ratio:F2}:1");
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

    [Fact]
    public void LchRoundTripsAndToneIsLStar()
    {
        foreach (var a in Enum.GetValues<KosAccent>())
        {
            if (a == KosAccent.Dalamud) continue;
            var (l, c, h) = MaterialKos.ToLch(KosColor.Accent(a));
            var back = MaterialKos.Tone(h, c, l);
            Assert.True(Vector4.Distance(KosColor.Accent(a), back) < 2e-3f, $"{a}: {KosColor.Accent(a)} -> {back}");
        }
        foreach (var hue in new[] { 0f, 25f, 90f, 145f, 250f, 300f })
        foreach (var tone in new[] { 6f, 22f, 40f, 80f, 98f })
        {
            var col = MaterialKos.Tone(hue, 48, tone); // 48 is out of gamut for some hues: chroma is lowered, tone kept
            Assert.InRange(MaterialKos.ToLch(col).L, tone - 0.5f, tone + 0.5f);
            Assert.True(col.X is >= 0 and <= 1 && col.Y is >= 0 and <= 1 && col.Z is >= 0 and <= 1);
        }
    }

    [Fact]
    public void OnColorReadsOnEveryPrimary()
    {
        foreach (var a in Enum.GetValues<KosAccent>())
        foreach (var light in new[] { false, true })
        {
            if (MaterialKos.Scheme(a, light) is not { } s) continue;
            foreach (var fill in new[] { s.Primary, s.SecondaryContainer, KosColor.Accent(a) })
                Assert.True(KosColor.Contrast(MaterialKos.OnColor(fill), fill) >= 4.5f, $"{a} light={light} {fill}");
        }
    }
}
