using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P2Trines;

namespace AnoMech.Tests;

public class UmadP2TrinesStateTests
{
    private static UmadP2TrinesState Roll(int seed, UmadP2TrinesStateOverrides? overrides = null)
    {
        var world = new SimWorld(new EventScheduler());
        world.Reseed(seed);
        return new UmadP2TrinesState(world.Rng, overrides ?? new UmadP2TrinesStateOverrides());
    }

    [Test]
    public void TheFirstSetIsAnyThreeOfTheSixOuterTriangles()
    {
        Assert.That(UmadP2TrinesState.FirstSets, Has.Count.EqualTo(20));
        Assert.That(UmadP2TrinesState.FirstSets.Select(UmadP2TrinesState.Describe).Distinct().Count(), Is.EqualTo(20));
        Assert.That(UmadP2TrinesState.FirstSets.All(s => s.Length == 3 && s.All(t => t is >= 1 and <= 6) && s.Distinct().Count() == 3));
    }

    [Test]
    public void EveryTriangleDropsExactlyOnce()
    {
        var seen = new HashSet<string>();
        for (var seed = 1; seed <= 400; seed++)
        {
            var state = Roll(seed);
            var all = state.FirstSet.Append(state.SecondSet).Concat(state.ThirdSet).ToList();
            Assert.That(all.Order(), Is.EqualTo(Enumerable.Range(0, 7)), $"seed {seed}");
            Assert.That(state.ThirdSet, Has.Count.EqualTo(3).And.Contains(0), $"seed {seed}");
            Assert.That(state.PartyTriangle, Is.EqualTo(state.FirstSet.Min()), $"seed {seed}");
            Assert.That(state.TankTriangle, Is.EqualTo(state.FirstSet.Max()), $"seed {seed}");
            seen.Add(UmadP2TrinesState.Describe(state.FirstSet));
        }
        Assert.That(seen, Has.Count.EqualTo(20));
    }

    [Test]
    public void TheCentreMostlyPointsEastAndEitherWingComesUp()
    {
        var states = Enumerable.Range(1, 600).Select(seed => Roll(seed)).ToList();
        Assert.That(states.Count(s => s.CentrePointsEast), Is.InRange(330, 470));
        Assert.That(states.Count(s => s.WingsRight), Is.InRange(240, 360));
    }

    [Test]
    public void OverridesAreHonoured()
    {
        var overrides = new UmadP2TrinesStateOverrides { FirstSet = 7, SecondSet = 6, CentrePointsEast = false, WingsRight = true };
        var first = UmadP2TrinesState.FirstSets[7];
        Assume.That(first, Does.Not.Contain(6));
        for (var seed = 1; seed <= 20; seed++)
        {
            var state = Roll(seed, overrides);
            Assert.That(state.FirstSet, Is.EqualTo(first));
            Assert.That(state.SecondSet, Is.EqualTo(6));
            Assert.That(state.CentrePointsEast, Is.False);
            Assert.That(state.WingsRight, Is.True);
        }
    }

    [Test]
    public void ASecondTriangleAlreadyInTheFirstSetIsLeftToTheRoll()
    {
        var overrides = new UmadP2TrinesStateOverrides { FirstSet = 0, SecondSet = UmadP2TrinesState.FirstSets[0][0] };
        Assert.That(overrides.Validate().Any);
        for (var seed = 1; seed <= 20; seed++)
            Assert.That(Roll(seed, overrides).FirstSet, Does.Not.Contain(Roll(seed, overrides).SecondSet));
    }

    [Test]
    public void TheNetworkReplayRejectsImpossibleLayouts()
    {
        Assert.That(UmadP2TrinesState.FromNetworkReplay([3, 1, 5], 2, true, false), Is.Not.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([1, 1, 5], 2, true, false), Is.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([1, 3], 2, true, false), Is.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([0, 3, 5], 2, true, false), Is.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([1, 3, 7], 2, true, false), Is.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([1, 3, 5], 3, true, false), Is.Null);
        Assert.That(UmadP2TrinesState.FromNetworkReplay([1, 3, 5], 7, true, false), Is.Null);
    }
}
