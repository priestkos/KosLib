using System.Numerics;
using KosLib;
using Xunit;

namespace KosLib.Tests;

public class KosColorTests
{
    // Dalamud Standard (StyleModelV1.DalamudStandard) and ImGui's stock light style.
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

    [Fact]
    public void DalamudStylePushesNothing()
    {
        foreach (var (_, bg, text) in Themes) Assert.Empty(KosColor.Derive(KosAccent.Dalamud, bg, text));
        Assert.Empty(KosColor.Derive((KosAccent)99, Themes[0].Bg, Themes[0].Text)); // a hand-edited config value
    }

    [Fact]
    public void PresetsKeepTheirPinnedValues()
    {
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], Enum.GetValues<KosAccent>().Select(a => (int)a));
    }

    [Fact]
    public void NeverTouchesWindowBgOrText()
    {
        Assert.DoesNotContain(Enum.GetNames<KosSlot>(), n => n is "WindowBg" or "Text");
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void EverySlotOnceAndOpaqueOrHeaderAlpha(KosAccent accent)
    {
        var d = KosColor.Derive(accent, Themes[0].Bg, Themes[0].Text);
        Assert.Equal(Enum.GetValues<KosSlot>().OrderBy(s => s), d.Select(x => x.Slot).OrderBy(s => s));
        foreach (var (slot, c) in d)
            Assert.Equal(slot switch { KosSlot.Header => 0.31f, KosSlot.HeaderHovered => 0.75f, KosSlot.HeaderActive => 0.90f, _ => 1f }, c.W);
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void LabelsKeep45To1OnEveryStateSurface(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var opaqueBg = bg with { W = 1 };
            var d = KosColor.Derive(accent, bg, text).ToDictionary(x => x.Slot, x => x.Color);
            foreach (var slot in UnderText)
            {
                var ratio = KosColor.Contrast(text, KosColor.Over(d[slot], opaqueBg));
                Assert.True(ratio >= 4.5f, $"{accent} {slot} on {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void MarksKeep3To1AgainstTheirFrame(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var opaqueBg = bg with { W = 1 };
            var d = KosColor.Derive(accent, bg, text).ToDictionary(x => x.Slot, x => x.Color);
            foreach (var mark in new[] { KosSlot.CheckMark, KosSlot.SliderGrab, KosSlot.SliderGrabActive })
            foreach (var frame in new[] { KosSlot.FrameBg, KosSlot.FrameBgHovered, KosSlot.FrameBgActive })
            {
                var ratio = KosColor.Contrast(d[mark], KosColor.Over(d[frame], opaqueBg));
                Assert.True(ratio >= 3f, $"{accent} {mark} on {frame}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Fact]
    public void ContrastMatchesWcagReferencePoints()
    {
        Assert.Equal(21f, KosColor.Contrast(new(0, 0, 0, 1), new(1, 1, 1, 1)), 2);
        Assert.Equal(1f, KosColor.Contrast(new(0.5f, 0.5f, 0.5f, 1), new(0.5f, 0.5f, 0.5f, 1)), 3);
        Assert.Equal(4.48f, KosColor.Contrast(new(0.4667f, 0.4667f, 0.4667f, 1), new(1, 1, 1, 1)), 1); // #777 on white
    }

    [Fact]
    public void OverCompositesByAlpha()
    {
        var c = KosColor.Over(new(1, 1, 1, 0.25f), new(0, 0, 0, 1));
        Assert.Equal(new Vector4(0.25f, 0.25f, 0.25f, 1), c);
    }
}
