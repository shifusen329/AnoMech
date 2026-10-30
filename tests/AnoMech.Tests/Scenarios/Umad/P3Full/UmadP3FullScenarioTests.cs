using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P3Full;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Tests;

// The three slices play as one run: one set of bosses, each slice's stand-ins left to the slice
// before, and the hits that fall in the seams resolved.
public class UmadP3FullScenarioTests
{
    private const float LimitCutAt = UmadP3FullScenario.LimitCutAt;
    private const float BlackHoleAt = UmadP3FullScenario.BlackHoleAt;

    private static (ScenarioRun Run, IReadOnlyList<string> Log) Play(int strat, int seed, Action<ScenarioProbe>? probe = null)
    {
        var run = ScenarioRun.Execute(typeof(UmadP3FullScenario), strat, seed, new ScenarioRunOptions { Probe = probe });
        return (run, DiagnosticLog.Snapshot());
    }

    [TestCase(0, 1)]
    [TestCase(5, 2)]
    public void OneKefkaChaosAndExdeathForTheWholePhase(int strat, int seed)
    {
        var bosses = new HashSet<SimEnemy>();
        var (run, _) = Play(strat, seed, p =>
        {
            foreach (var enemy in p.World.Children.OfType<SimEnemy>())
                if (enemy.BNpcBaseId is BNpcBaseId.KefkaP3 or BNpcBaseId.ChaosP3 or BNpcBaseId.Exdeath) bosses.Add(enemy);
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(bosses.GroupBy(b => b.BNpcBaseId).ToDictionary(g => g.Key, g => g.Count()),
            Is.EquivalentTo(new Dictionary<uint, int> { [BNpcBaseId.KefkaP3] = 1, [BNpcBaseId.ChaosP3] = 1, [BNpcBaseId.Exdeath] = 1 }));
    }

    [Test]
    public void LimitCutHoldsTheBossesOnBowelsOfAgonysWindCorner()
    {
        var (_, log) = Play(0, 3);
        var wind = Between(log.Single(l => l.Contains("[UmadP3BowelsOfAgony] Roll:")), "wind ", ",");
        var held = Between(log.Single(l => l.Contains("[UmadP3LimitCut] Roll:")), "bosses held ", ",");
        Assert.That(held, Is.EqualTo(wind));
    }

    // Bowels of Agony hands out the winds and both Decisive Battles the sides; Limit Cut and Black
    // Hole don't fake them again.
    [Test]
    public void TheWindsAndSidesAreHandedOutOnce()
    {
        var (_, log) = Play(0, 4);
        Assert.That(StatusesAdded(log, StatusId.Headwind) + StatusesAdded(log, StatusId.Tailwind), Is.EqualTo(8));
        Assert.That(StatusesAdded(log, StatusId.EpicHero) + StatusesAdded(log, StatusId.FatedHero), Is.EqualTo(16));
    }

    [Test]
    public void TheSeamsHitsLandAndTheRunEndsAsTheBossesFall()
    {
        var thunders = new List<int>();
        var (run, log) = Play(0, 5, p =>
        {
            foreach (var at in new[] { LimitCutAt + 55.6f, LimitCutAt + 56.1f, LimitCutAt + 58.6f, LimitCutAt + 59.1f })
                if (p.Crossed(at)) thunders.Add(Resolves(DiagnosticLog.Snapshot(), ActionId.ThunderIII_Resolve));
        });

        Assert.That(run.Passed, run.ToString);
        Assert.That(thunders, Has.Count.EqualTo(4));
        Assert.That(thunders[1] - thunders[0], Is.EqualTo(1), "the last Thunder III's first hit");
        Assert.That(thunders[3] - thunders[2], Is.EqualTo(1), "its second hit");
        Assert.That(Resolves(log, ActionId.BigBang), Is.EqualTo(2));
        Assert.That(run.Elapsed, Is.InRange(BlackHoleAt + 167f, BlackHoleAt + 170f));
    }

    // A strat's arrival times count from its slice's start, so the recap's deadlines do too.
    [Test]
    public void ALimitCutDeadlineIsOnTheRunsClock()
    {
        float? deadline = null;
        Play(0, 6, p =>
        {
            if (p.Crossed(LimitCutAt + 8f)) deadline = p.World.Strat.DueFor(PartyRole.PhysRangedDps, p.Time)?.Deadline;
        });
        Assert.That(deadline, Is.EqualTo(LimitCutAt + 7.5f).Within(0.05f));
    }

    private static string Between(string line, string from, string to)
    {
        var start = line.IndexOf(from, StringComparison.Ordinal) + from.Length;
        return line[start..line.IndexOf(to, start, StringComparison.Ordinal)];
    }

    private static int StatusesAdded(IReadOnlyList<string> log, ushort statusId)
        => log.Count(l => l.Contains("[SimCharacter] AddStatus:") && l.Contains($" gets status {statusId} "));

    private static int Resolves(IReadOnlyList<string> log, uint actionId)
    {
        var marker = $"Resolve: {Natives.Data.ActionName(actionId)} at";
        return log.Count(l => l.Contains(marker));
    }
}
