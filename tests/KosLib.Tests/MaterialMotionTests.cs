using System.Numerics;
using Xunit;

namespace KosLib.Tests;

public class MaterialMotionTests
{
    [Fact]
    public void PillPointWalksTheEdgeClockwise()
    {
        Vector2 min = new(10, 20), size = new(100, 30);
        const float r = 15;
        Vector2 left = new(25, 35), right = new(95, 35);
        for (var k = 0; k < 200; k++)
        {
            var p = MaterialKos.PillPoint(min, size, k / 200f);
            var axis = new Vector2(Math.Clamp(p.X, left.X, right.X), 35); // nearest point on the pill's middle line
            Assert.Equal(r, Vector2.Distance(p, axis), 3); // every point is on the edge
        }
        Assert.Equal(new Vector2(25, 20), MaterialKos.PillPoint(min, size, 0));
        var flat = 70f; var lap = 2 * flat + 2 * MathF.PI * r;
        Assert.Equal(110, MaterialKos.PillPoint(min, size, (flat + MathF.PI * r / 2) / lap).X, 3); // right end, clockwise
        Assert.True(Vector2.Distance(MaterialKos.PillPoint(min, size, 0.3f), MaterialKos.PillPoint(min, size, 1.3f)) < 1e-3f); // whole turns wrap
        Assert.Equal(MaterialKos.PillPoint(min, size, 0.3f).X, MaterialKos.PillPoint(min, size, -0.7f).X, 3);
    }

    [Fact]
    public void SlideConfirmsOnceAtTheEndAndSpringsBackOtherwise()
    {
        var (k, ok) = MaterialKos.SlideStep(0, 0.016f, 0.5f, false);
        Assert.Equal((0.5f, false), (k, ok));
        (k, ok) = MaterialKos.SlideStep(k, 0.1f, null, false); // let go early
        Assert.False(ok);
        Assert.Equal(0.1f, k, 3);
        (k, _) = MaterialKos.SlideStep(k, 0.1f, null, false);
        Assert.Equal(0, k);

        (k, ok) = MaterialKos.SlideStep(0.9f, 0.016f, 1.4f, false); // dragged past the end
        Assert.True(ok);
        (k, ok) = MaterialKos.SlideStep(k, 0.016f, 1.4f, false); // still held: no second confirm
        Assert.False(ok);
        Assert.Equal(-1, k);
        (k, ok) = MaterialKos.SlideStep(k, 0.016f, null, false); // released: home from the end
        Assert.Equal((1f, false), (k, ok));

        k = 0; // a held key creeps across in about a second
        var frames = 0;
        do { (k, ok) = MaterialKos.SlideStep(k, 0.016f, null, true); frames++; } while (!ok && frames < 200);
        Assert.True(ok);
        Assert.InRange(frames, 55, 65);
    }

    [Fact]
    public void ReducedMotionSlideIsHomeAtOnceButAHeldKeyStillCreeps()
    {
        var (k, ok) = MaterialKos.SlideStep(0.7f, 0.016f, null, false, still: true); // let go early
        Assert.Equal((0f, false), (k, ok));
        (k, ok) = MaterialKos.SlideStep(-1, 0.016f, null, false, still: true); // released after confirming
        Assert.Equal((0f, false), (k, ok));
        (k, ok) = MaterialKos.SlideStep(-1, 0.016f, 1.4f, false, still: true); // still held after confirming
        Assert.Equal((-1f, false), (k, ok));

        k = 0;
        var frames = 0;
        do { (k, ok) = MaterialKos.SlideStep(k, 0.016f, null, true, still: true); frames++; } while (!ok && frames < 200);
        Assert.True(ok);
        Assert.InRange(frames, 55, 65);
    }
}
