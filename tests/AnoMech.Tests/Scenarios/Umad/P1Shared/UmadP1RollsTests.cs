using System.Numerics;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Tests;

public class UmadP1RollsTests
{
    // Slot headings run SE, NE, NW, SW; real cones at -pi/4 (SW) and 3pi/4 (NE) are offset 1.
    [Test]
    public void IceSlotsCoverTheirQuadrants()
    {
        Assert.That(IceRoll.QuadrantSigns(0), Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(IceRoll.QuadrantSigns(1), Is.EqualTo(new Vector2(1f, -1f)));
        Assert.That(IceRoll.QuadrantSigns(2), Is.EqualTo(new Vector2(-1f, -1f)));
        Assert.That(IceRoll.QuadrantSigns(3), Is.EqualTo(new Vector2(-1f, 1f)));
        var ice = new IceRoll(1, false);
        Assert.That(ice.IsReal(1) && ice.IsReal(3) && !ice.IsReal(0) && !ice.IsReal(2), Is.True);
        Assert.That(ice.HitsNorthEast, Is.True);
    }

    [TestCase(0, true, -1f, 1f)]
    [TestCase(0, false, 1f, -1f)]
    [TestCase(1, true, -1f, -1f)]
    [TestCase(1, false, 1f, 1f)]
    public void SafeQuadrantIsTheUnhitOneOnThatSide(int offset, bool west, float x, float z)
        => Assert.That(new IceRoll(offset, false).SafeQuadrant(west), Is.EqualTo(new Vector2(x, z)));

    [Test]
    public void IceClearanceIsNegativeOnlyInsideARealCone()
    {
        var ice = new IceRoll(0, false);
        Assert.That(ice.Clearance(new Vector2(5f, 3f)), Is.EqualTo(-3f).Within(1e-4));
        Assert.That(ice.Clearance(new Vector2(-5f, -1f)), Is.EqualTo(-1f).Within(1e-4));
        Assert.That(ice.Clearance(new Vector2(4f, -2f)), Is.EqualTo(2f).Within(1e-4));
        Assert.That(ice.Clearance(new Vector2(-1f, 6f)), Is.EqualTo(1f).Within(1e-4));
    }

    // The two lines a truthful offset-1, flipped set casts.
    [Test]
    public void ThunderAnchorsMatchTheRealLines()
    {
        var thunder = new ThunderRoll(1, true, false);
        Assert.That(thunder.IsReal(1) && thunder.IsReal(3), Is.True);
        var a1 = thunder.Anchor(1);
        var a3 = thunder.Anchor(3);
        Assert.That(a1.Position.X, Is.EqualTo(-17.68f).Within(0.01));
        Assert.That(a1.Position.Z, Is.EqualTo(-10.61f).Within(0.01));
        Assert.That(a3.Position.X, Is.EqualTo(-3.54f).Within(0.01));
        Assert.That(a3.Position.Z, Is.EqualTo(-24.75f).Within(0.01));
        Assert.That(a1.Rotation, Is.EqualTo(MathF.PI / 4f).Within(1e-4));
    }

    [Test]
    public void ThunderClearanceIsNegativeInsideARealLine()
    {
        var thunder = new ThunderRoll(1, true, false);
        var a1 = thunder.Anchor(1);
        var inside = new Vector2(a1.Position.X, a1.Position.Z) + new Vector2(MathF.Sin(a1.Rotation), MathF.Cos(a1.Rotation)) * 10f;
        Assert.That(thunder.Clearance(inside), Is.EqualTo(-ThunderRoll.HalfWidth).Within(1e-3));
    }
}
