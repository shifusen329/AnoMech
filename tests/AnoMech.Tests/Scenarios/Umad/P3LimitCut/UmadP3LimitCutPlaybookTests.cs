using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;
using Geometry = AnoMech.Scenarios.Umad.P3LimitCut.UmadP3LimitCutConstants.Geometry;

namespace AnoMech.Tests;

public class UmadP3LimitCutPlaybookTests
{
    private static UmadP3LimitCutState State(int startSpot, bool clockwise, ThunderIIIAssignment plan, int shift)
    {
        var numbers = Enumerable.Range(0, 8).Select(k => UmadP1Roles.All[(k + shift) % 8]).ToList();
        var headwinds = UmadP1Roles.All.Where((_, i) => (i + shift) % 3 == 0).ToList();
        return UmadP3LimitCutState.FromNetworkReplay(startSpot, clockwise, numbers, headwinds, 1 + 2 * (shift % 4), PartyRole.PhysRangedDps, plan)!;
    }

    private static IEnumerable<(UmadP3LimitCutState State, string Context)> States()
    {
        foreach (var plan in Enum.GetValues<ThunderIIIAssignment>())
            foreach (var clockwise in new[] { false, true })
                foreach (var start in new[] { 0, 3, 6 })
                    yield return (State(start, clockwise, plan, shift: start + (int)plan), $"plan {plan} clockwise {clockwise} start {start}");
    }

    [Test]
    public void LimitCutExplainsEveryMove()
    {
        foreach (var (state, context) in States())
        {
            var (first, second) = ThunderIIIPlanning.Roles(state.ThunderPlan);
            foreach (var cue in new[]
            {
                UmadP3LimitCutPlaybook.Stack(state), UmadP3LimitCutPlaybook.UmbraBait(state),
                UmadP3LimitCutPlaybook.TanksIntoStack, UmadP3LimitCutPlaybook.BaitBack(state),
                UmadP3LimitCutPlaybook.VacuumWave(state), UmadP3LimitCutPlaybook.Cyclones,
                UmadP3LimitCutPlaybook.Charges(state), UmadP3LimitCutPlaybook.ClearForThunder(state),
                UmadP3LimitCutPlaybook.ThunderTank(state), UmadP3LimitCutPlaybook.ThunderClearance(first, second),
                UmadP3LimitCutPlaybook.ThunderClearance(second ?? first, null), UmadP3LimitCutPlaybook.ThunderSwap(state),
                UmadP3LimitCutPlaybook.Uptime(state),
            })
                PlaybookAssert.ExplainsEveryRole(cue, context);
        }
    }

    // The charge text names the wall across from the clone and the way the strat's spot leans off
    // it (a falling heading is clockwise on the map).
    [Test]
    public void ChargeTextMatchesTheBotsWallSpot()
    {
        string[] words = ["south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"];
        foreach (var (state, context) in States())
            foreach (var role in UmadP1Roles.All)
            {
                var k = state.NumberOf(role) - 1;
                var across = (state.ChargeSpot(k) + 4) % 8;
                var lean = state.SafeHeading(k) - Geometry.SpotHeading(across);
                lean = MathF.IEEERemainder(lean, 2f * MathF.PI);
                Assert.That(MathF.Abs(lean), Is.EqualTo(MathF.PI / 8f).Within(1e-4f), context);
                var why = UmadP3LimitCutPlaybook.Charges(state).Why(role);
                Assert.That(why, Does.Contain($"half-step {(lean < 0f ? "clockwise" : "counterclockwise")} of the {words[across]} spot"), $"{role}, {context}");
            }
    }
}
