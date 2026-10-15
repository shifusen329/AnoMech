using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3BowelsOfAgony;
using AnoMech.Scenarios.Umad.P3LimitCut;
using Strat = AnoMech.Scenarios.Umad.P3BowelsOfAgony.UmadP3BowelsOfAgonyAi.Strat;

namespace AnoMech.Tests;

public class UmadP3BowelsOfAgonyPlaybookTests
{
    private static IEnumerable<(UmadP3BowelsOfAgonyState State, string Context)> States()
    {
        var plans = Enum.GetValues<ThunderIIIAssignment>();
        var supports = UmadP1Roles.All.Where(UmadP3BowelsOfAgonyState.IsSupport).ToArray();
        var dps = UmadP1Roles.All.Where(r => !UmadP3BowelsOfAgonyState.IsSupport(r)).ToArray();
        var i = 0;
        foreach (var wind in new[] { 1, 3, 5, 7 })
            foreach (var water in new[] { (wind + 2) % 8, (wind + 6) % 8 })
                foreach (var shortElement in new[] { Element.Water, Element.Fire })
                {
                    i++;
                    PartyRole[] waterHolders = [supports[i % 4], dps[i % 4]];
                    PartyRole[] fireHolders = [supports[(i + 1) % 4], dps[(i + 2) % 4]];
                    var headwinds = UmadP1Roles.All.Where((_, k) => (k + i) % 2 == 0).ToList();
                    var state = UmadP3BowelsOfAgonyState.FromNetworkReplay(wind, water, shortElement, waterHolders, fireHolders, headwinds, i % 2 == 0, plans[i % 4])!;
                    yield return (state, $"wind {wind} water {water} short {shortElement} plan {plans[i % 4]}");
                }
    }

    [Test]
    public void EveryMoveIsExplainedForEveryRole()
    {
        foreach (var (state, context) in States())
        {
            var (first, second) = ThunderIIIPlanning.Roles(state.ThunderPlan);
            foreach (var cue in new[]
            {
                UmadP3BowelsOfAgonyPlaybook.DecisiveBattleSplit, UmadP3BowelsOfAgonyPlaybook.BossesToTheMiddle,
                UmadP3BowelsOfAgonyPlaybook.Lb3Setup(state), UmadP3BowelsOfAgonyPlaybook.Lb3ThunderAoe(state),
                UmadP3BowelsOfAgonyPlaybook.Lb3Resolve(state, state.ShortElement), UmadP3BowelsOfAgonyPlaybook.Lb3Resolve(state, state.LongElement),
                UmadP3BowelsOfAgonyPlaybook.Lb3Regroup, UmadP3BowelsOfAgonyPlaybook.Lb3PreImplosion(state),
                UmadP3BowelsOfAgonyPlaybook.Implosion(state, Strat.Lb3, firstHit: true), UmadP3BowelsOfAgonyPlaybook.Implosion(state, Strat.Lb3, firstHit: false),
                UmadP3BowelsOfAgonyPlaybook.Implosion(state, Strat.Sg3k, firstHit: true), UmadP3BowelsOfAgonyPlaybook.Implosion(state, Strat.Sg3k, firstHit: false),
                UmadP3BowelsOfAgonyPlaybook.Sg3kSetup(state), UmadP3BowelsOfAgonyPlaybook.Sg3kResolve(state, state.ShortElement),
                UmadP3BowelsOfAgonyPlaybook.Sg3kResolve(state, state.LongElement), UmadP3BowelsOfAgonyPlaybook.Sg3kCleanseFacing(state, state.ShortElement),
                UmadP3BowelsOfAgonyPlaybook.Sg3kCleanseFacing(state, state.LongElement), UmadP3BowelsOfAgonyPlaybook.Sg3kPreImplosion(state),
                UmadP3BowelsOfAgonyPlaybook.ThunderTank(state), UmadP3BowelsOfAgonyPlaybook.ThunderClearance(first, second),
                UmadP3BowelsOfAgonyPlaybook.ThunderSwap(state),
            })
                PlaybookAssert.ExplainsEveryRole(cue, context);
        }
    }

    // Vacuum Wave's facing rule: Headwind faces away from the push, Tailwind into it.
    [Test]
    public void TheCleansePairIsToldWhichWayToFace()
    {
        foreach (var (state, context) in States())
            foreach (var role in state.HoldersOf(state.LongElement))
            {
                var why = UmadP3BowelsOfAgonyPlaybook.Sg3kCleanseFacing(state, state.ShortElement).Why(role);
                Assert.That(why, Does.Contain(state.Winds[role] == Wind.Headwind ? "so face away from" : "Tailwind, so face"), $"{role}, {context}");
            }
    }
}
