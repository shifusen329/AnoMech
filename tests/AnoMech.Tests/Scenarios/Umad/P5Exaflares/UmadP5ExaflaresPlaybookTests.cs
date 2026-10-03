using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P5Exaflares;

namespace AnoMech.Tests;

public class UmadP5ExaflaresPlaybookTests
{
    private static readonly int[][] Orders =
    [
        [1, 4, 2, 5, 3, 6], [1, 4, 3, 6, 2, 5], [2, 5, 1, 4, 3, 6],
        [2, 5, 3, 6, 1, 4], [3, 6, 1, 4, 2, 5], [3, 6, 2, 5, 1, 4],
    ];

    [Test]
    public void ExaflaresExplainsEveryMove()
    {
        foreach (var left in Orders)
            foreach (var right in Orders)
            {
                var state = UmadP5ExaflaresState.FromNetworkReplay(left, right, new EventScheduler());
                var context = $"left {string.Join("", left)} right {string.Join("", right)}";
                PlaybookAssert.ExplainsEveryRole(UmadP5ExaflaresPlaybook.FanOut, context);
                for (var wave = 0; wave < 6; wave++)
                    PlaybookAssert.ExplainsEveryRole(UmadP5ExaflaresPlaybook.Wave(state, wave), $"{context} wave {wave}");
                PlaybookAssert.ExplainsEveryRole(UmadP5ExaflaresPlaybook.Spread, context);
            }
    }

    // Ranged may take the open lane beyond an off-centre pair (lines 1+4 or 3+6), never beyond 2+5;
    // tanks and melee always take the gap between the two.
    [Test]
    public void ExaflaresOffersTheOuterLaneOnlyBeyondAnOffCentrePair()
    {
        var state = UmadP5ExaflaresState.FromNetworkReplay([2, 5, 1, 4, 3, 6], [3, 6, 2, 5, 1, 4], new EventScheduler());
        Assert.That(UmadP5ExaflaresPlaybook.Wave(state, 0).Why(PartyRole.CasterDps), Does.Not.Contain("open lane"));
        Assert.That(UmadP5ExaflaresPlaybook.Wave(state, 1).Why(PartyRole.CasterDps), Does.Contain("open lane"));
        Assert.That(UmadP5ExaflaresPlaybook.Wave(state, 2).Why(PartyRole.RegenHealer), Does.Contain("open lane"));
        Assert.That(UmadP5ExaflaresPlaybook.Wave(state, 3).Why(PartyRole.PhysRangedDps), Does.Not.Contain("open lane"));
        for (var wave = 0; wave < 6; wave++)
            Assert.That(UmadP5ExaflaresPlaybook.Wave(state, wave).Why(PartyRole.MeleeDpsA), Does.Not.Contain("open lane"), $"wave {wave}");
    }
}
