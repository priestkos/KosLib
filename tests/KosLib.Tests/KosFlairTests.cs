using System.Numerics;
using KosLib;
using Xunit;

namespace KosLib.Tests;

// The drawn extras (card edge, section tag, card fill) must stay readable on both test themes.
public class KosFlairTests
{
    private static readonly (string Name, Vector4 Bg, Vector4 Text)[] Themes =
    [
        ("Dalamud Standard", new(0.06f, 0.06f, 0.06f, 0.93f), new(1, 1, 1, 1)),
        ("ImGui light", new(0.94f, 0.94f, 0.94f, 1), new(0, 0, 0, 1)),
    ];

    public static TheoryData<KosAccent> Accents()
    {
        var data = new TheoryData<KosAccent>();
        foreach (var a in Enum.GetValues<KosAccent>()) if (a != KosAccent.Dalamud) data.Add(a);
        return data;
    }

    [Fact]
    public void DalamudStyleHasNoFlair()
    {
        foreach (var (_, bg, text) in Themes) Assert.Null(KosColor.Flair(KosAccent.Dalamud, bg, text));
        Assert.Null(KosColor.Flair((KosAccent)99, Themes[0].Bg, Themes[0].Text));
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void SectionTextKeeps45To1OnTheWindowAndOnACard(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var opaqueBg = bg with { W = 1 };
            var f = KosColor.Flair(accent, bg, text)!.Value;
            var card = f.CardFill is { } fill ? KosColor.Over(fill, opaqueBg) : opaqueBg;
            foreach (var (what, ink) in new[] { ("Tag", f.Tag), ("TagNumber", f.TagNumber) })
            foreach (var (surface, under) in new[] { ("window", opaqueBg), ("card", card) })
            {
                var ratio = KosColor.Contrast(ink, under);
                Assert.True(ratio >= 4.5f, $"{accent} {what} on {surface}, {name}: {ratio:F2}:1");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void ACardFillOnlyEverRaisesTextContrast(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var opaqueBg = bg with { W = 1 };
            if (KosColor.Flair(accent, bg, text)!.Value.CardFill is not { } fill) continue;
            var onCard = KosColor.Contrast(text, KosColor.Over(fill, opaqueBg));
            Assert.True(onCard >= KosColor.Contrast(text, opaqueBg), $"{accent} card on {name}: {onCard:F2}:1");
            Assert.True(onCard >= 7f, $"{accent} card on {name}: {onCard:F2}:1");
        }
    }

    [Theory]
    [MemberData(nameof(Accents))]
    public void PrimaryButtonLabelsKeep45To1InEveryState(KosAccent accent)
    {
        foreach (var (name, bg, text) in Themes)
        {
            var p = KosColor.Flair(accent, bg, text)!.Value.Primary;
            foreach (var (state, fill) in new[] { ("idle", p.Idle), ("hovered", p.Hovered), ("active", p.Active) })
            {
                Assert.Equal(1f, fill.W);
                var ratio = KosColor.Contrast(text, fill);
                Assert.True(ratio >= 4.5f, $"{accent} primary {state} on {name}: {ratio:F2}:1");
            }
        }
    }

    [Fact]
    public void OnlyKosMosDrawsANightCardAndOnlyOnADarkTheme()
    {
        foreach (var a in Enum.GetValues<KosAccent>())
        {
            Assert.Null(KosColor.Flair(a, Themes[1].Bg, Themes[1].Text)?.CardFill);
            Assert.Equal(a == KosAccent.KosMos, KosColor.Flair(a, Themes[0].Bg, Themes[0].Text)?.CardFill is not null);
        }
    }

    [Fact]
    public void KosMosUsesTheProfileSitePaletteOnADarkTheme()
    {
        var f = KosColor.Flair(KosAccent.KosMos, Themes[0].Bg, Themes[0].Text)!.Value;
        Assert.Equal(KosColor.Crimson, f.EdgeA);
        Assert.Equal(KosColor.FoxfireGold, f.EdgeB);
        Assert.Equal(KosColor.TagGold, f.Tag);
        Assert.True(f.Glow);
        var marks = KosColor.Derive(KosAccent.KosMos, Themes[0].Bg, Themes[0].Text).ToDictionary(x => x.Slot, x => x.Color);
        Assert.Equal(KosColor.FoxfireGold, marks[KosSlot.CheckMark]);
    }
}
