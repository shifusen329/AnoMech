using AnoMech.Core.Game.PartyMit;
using AnoMech.Scenarios.Umad;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Tests;

public class MitPlanChainTests
{
    private static MitPlanData Plan(float[] hits, params MitPlanEntry[] rows)
        => new(UmadMitigation.Profile, [new MitHitDef("hit", "Hit", 1000f, null)], hits.Select(t => new MitScheduledHit(t, ["hit"])).ToArray(), rows);

    [Test]
    public void EachSliceLandsOnTheRunsClockWithItsOwnKeys()
    {
        var chained = MitPlanChain.Of(
            (Plan([10f], new MitPlanEntry(WHM, MitActionId.Temperance, 5f, "a", [10f])), 0f),
            (Plan([10f], new MitPlanEntry(MT, MitActionId.Reprisal, 5f, "b", [10f])), 100f));

        Assert.That(chained.Hits.Select(h => h.Key), Is.EqualTo(new[] { "0:hit", "1:hit" }));
        Assert.That(chained.Schedule.Select(s => (s.At, s.Keys.Single())), Is.EqualTo(new[] { (10f, "0:hit"), (110f, "1:hit") }));
        Assert.That(chained.Entries.Select(e => (e.At, e.Covers.Single())), Is.EqualTo(new[] { (5f, 10f), (105f, 110f) }));
    }

    [Test]
    public void ALaterSlicesPreStartRowIsLeftToTheSliceBefore()
    {
        var chained = MitPlanChain.Of(
            (Plan([10f]), 0f),
            (Plan([10f], new MitPlanEntry(WHM, MitActionId.Temperance, -2f, "pre", [10f])), 50f));

        Assert.That(chained.Entries, Is.Empty);
    }

    // Reprisal's recast is 60s.
    [Test]
    public void AButtonStillOnRecastGoesOutWhenItsBackOrNotAtAll()
    {
        var chained = MitPlanChain.Of(
            (Plan([10f], new MitPlanEntry(MT, MitActionId.Reprisal, 5f, "first", [10f])), 0f),
            (Plan([10f, 30f], new MitPlanEntry(MT, MitActionId.Reprisal, 0f, "delayed", [10f, 30f])), 50f),
            (Plan([10f], new MitPlanEntry(MT, MitActionId.Reprisal, 0f, "lost", [10f])), 100f));

        Assert.That(chained.Entries.Select(e => (e.Label, e.At, e.Covers.Length)),
            Is.EqualTo(new[] { ("first", 5f, 1), ("delayed", 65f, 1) }));
    }
}
