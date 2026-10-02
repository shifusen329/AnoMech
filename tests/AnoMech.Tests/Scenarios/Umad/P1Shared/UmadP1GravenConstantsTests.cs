using AnoMech.Scenarios.Umad;
using AnoMech.Scenarios.Umad.P1GravenImage1;
using AnoMech.Scenarios.Umad.P1GravenImage2;
using AnoMech.Scenarios.Umad.P1Shared;
using ActionId = AnoMech.Scenarios.Umad.P1Shared.UmadP1GravenConstants.ActionId;
using KnockbackId = AnoMech.Scenarios.Umad.P1Shared.UmadP1GravenConstants.KnockbackId;

namespace AnoMech.Tests;

// The sizes the Graven Image scenarios and their bots assume, against the real sheets.
public class UmadP1GravenConstantsTests
{
    private readonly DataminingGameData data = new();

    [TestCase(ActionId.WaveCannon, 12, 100, 6)]
    [TestCase(ActionId.Explosion, 2, 4, 0)]
    [TestCase(ActionId.Gravitas, 2, 5, 0)]
    [TestCase(ActionId.Vitrophyre, 2, 5, 0)]
    [TestCase(ActionId.GravityIII, 2, 5, 0)]
    [TestCase(ActionId.Hyperdrive, 2, 5, 0)]
    [TestCase(ActionId.GravitationalWave, 13, 100, 0)]
    [TestCase(ActionId.IntemperateWill, 13, 100, 0)]
    [TestCase(ActionId.DoubleTroubleTrapStack, 2, 6, 0)]
    [TestCase(ActionId.FlagrantFireSpread, 2, 5, 0)]
    [TestCase(ActionId.FlagrantFireStack, 2, 6, 0)]
    [TestCase(ActionId.ThrummingThunderReal1, 12, 40, 10)]
    [TestCase(UmadConstants.ActionId.BlizzardIIIBlowout_Real, 13, 40, 0)]
    public void ActionShape(uint actionId, int castType, int range, int width)
    {
        var row = data.Action(actionId);
        Assert.That(row, Is.Not.Null);
        Assert.That((row!.CastType, row.EffectRange, row.XAxisModifier), Is.EqualTo(((byte)castType, (byte)range, (byte)width)));
    }

    [TestCase(KnockbackId.PulseWave, 13)]
    [TestCase(KnockbackId.DoubleTroubleTrapStack, 14)]
    public void KnockbackDistance(uint knockbackId, int distance)
        => Assert.That(data.Knockback(knockbackId)?.Distance, Is.EqualTo(distance));

    [Test]
    public void ScenarioRadiiMatchTheSheet()
    {
        Assert.That(UmadP1GravenImage1Scenario.TowerRadius, Is.EqualTo(data.Action(ActionId.Explosion)!.EffectRange));
        Assert.That(UmadP1GravenImage1Ai.PulseWaveDistance, Is.EqualTo(data.Knockback(KnockbackId.PulseWave)!.Distance));
        Assert.That(UmadP1GravenImage2Scenario.PuddleRadius, Is.EqualTo(data.Action(ActionId.GravityIII)!.EffectRange));
        Assert.That(UmadP1GravenImage2Scenario.VitrophyreRadius, Is.EqualTo(data.Action(ActionId.Vitrophyre)!.EffectRange));
    }
}
