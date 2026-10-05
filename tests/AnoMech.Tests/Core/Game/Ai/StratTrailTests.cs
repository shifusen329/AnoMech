using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Tests;

public class StratTrailTests
{
    private static StratTarget At(PartyRole role, float issuedAt, float x, float? deadline = null)
        => new(role, new Vector3(x, 0f, 0f), issuedAt, deadline, $"m{issuedAt}", null, null);

    [Test]
    public void DueForPicksTheLastSpotIssuedByThatTime()
    {
        var trail = new StratTrail(new EventScheduler());
        trail.Record(At(PartyRole.MainTank, 1f, 1f));
        trail.Record(At(PartyRole.OffTank, 2f, 9f));
        trail.Record(At(PartyRole.MainTank, 5f, 5f));
        trail.Record(At(PartyRole.MainTank, 9f, 9f));

        Assert.That(trail.DueFor(PartyRole.MainTank, 6f)?.Spot.X, Is.EqualTo(5f));
        Assert.That(trail.DueFor(PartyRole.MainTank, 5f)?.Spot.X, Is.EqualTo(5f));
        Assert.That(trail.DueFor(PartyRole.MainTank, 0.5f), Is.Null);
        Assert.That(trail.DueFor(PartyRole.OffTank, 20f)?.Spot.X, Is.EqualTo(9f));
        Assert.That(trail.DueFor(PartyRole.CasterDps, 20f), Is.Null);
    }

    // Forsaken's timing: the bait move goes out at 33.0s, before the tower's stacks and spreads
    // land at 33.17s, so a death there needed the tower spot, not the bait.
    [Test]
    public void ASpotNotDueYetGivesWayToTheOneThatWas()
    {
        var trail = new StratTrail(new EventScheduler());
        trail.Record(At(PartyRole.MainTank, 25.17f, 1f, deadline: 32.16f));
        trail.Record(At(PartyRole.MainTank, 33f, 2f, deadline: 37f));

        Assert.That(trail.DueFor(PartyRole.MainTank, 33.17f)?.Spot.X, Is.EqualTo(1f));
        Assert.That(trail.DueFor(PartyRole.MainTank, 37.5f)?.Spot.X, Is.EqualTo(2f));
        Assert.That(trail.DueFor(PartyRole.MainTank, 30f)?.Spot.X, Is.EqualTo(1f), "nothing due yet: the upcoming spot");
    }

    [Test]
    public void NoteTimesTheSpotOnTheEventClockAndCarriesTheCue()
    {
        var events = new EventScheduler();
        var trail = new StratTrail(events);
        events.Advance(12.5f);
        var cue = new StratCue("Towers", role => $"{role} soaks", "NAUR 1.3");
        trail.Note(PartyRole.RegenHealer, new Vector2(3f, -4f), cue, deadline: 14f);

        var target = trail.DueFor(PartyRole.RegenHealer, 13f);
        Assert.That(target, Is.Not.Null);
        Assert.That(target!.IssuedAt, Is.EqualTo(12.5f));
        Assert.That(target.Spot, Is.EqualTo(new Vector3(3f, 0f, -4f)));
        Assert.That(target.Deadline, Is.EqualTo(14f));
        Assert.That((target.Mechanic, target.Why, target.Source), Is.EqualTo(("Towers", "RegenHealer soaks", "NAUR 1.3")));
    }

    [Test]
    public void ClearForgetsTheRun()
    {
        var trail = new StratTrail(new EventScheduler());
        trail.Record(At(PartyRole.MainTank, 1f, 1f));
        trail.Clear();
        Assert.That(trail.DueFor(PartyRole.MainTank, 10f), Is.Null);
    }
}
