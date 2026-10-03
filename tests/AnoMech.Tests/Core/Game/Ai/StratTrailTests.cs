using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Tests;

public class StratTrailTests
{
    private static StratTarget At(PartyRole role, float issuedAt, float x)
        => new(role, new Vector3(x, 0f, 0f), issuedAt, null, $"m{issuedAt}", null, null);

    [Test]
    public void LatestForPicksTheLastSpotIssuedByThatTime()
    {
        var trail = new StratTrail(new EventScheduler());
        trail.Record(At(PartyRole.MainTank, 1f, 1f));
        trail.Record(At(PartyRole.OffTank, 2f, 9f));
        trail.Record(At(PartyRole.MainTank, 5f, 5f));
        trail.Record(At(PartyRole.MainTank, 9f, 9f));

        Assert.That(trail.LatestFor(PartyRole.MainTank, 6f)?.Spot.X, Is.EqualTo(5f));
        Assert.That(trail.LatestFor(PartyRole.MainTank, 5f)?.Spot.X, Is.EqualTo(5f));
        Assert.That(trail.LatestFor(PartyRole.MainTank, 0.5f), Is.Null);
        Assert.That(trail.LatestFor(PartyRole.OffTank, 20f)?.Spot.X, Is.EqualTo(9f));
        Assert.That(trail.LatestFor(PartyRole.CasterDps, 20f), Is.Null);
    }

    [Test]
    public void NoteTimesTheSpotOnTheEventClockAndCarriesTheCue()
    {
        var events = new EventScheduler();
        var trail = new StratTrail(events);
        events.Advance(12.5f);
        var cue = new StratCue("Towers", role => $"{role} soaks", "NAUR 1.3");
        trail.Note(PartyRole.RegenHealer, new Vector2(3f, -4f), cue, deadline: 14f);

        var target = trail.LatestFor(PartyRole.RegenHealer, 13f);
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
        Assert.That(trail.LatestFor(PartyRole.MainTank, 10f), Is.Null);
    }
}
