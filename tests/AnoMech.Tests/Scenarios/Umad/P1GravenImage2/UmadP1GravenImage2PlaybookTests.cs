using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P1GravenImage2;

namespace AnoMech.Tests;

public class UmadP1GravenImage2PlaybookTests
{
    [Test]
    public void GravenImage2ExplainsEveryMove()
    {
        foreach (var purpleDps in new[] { false, true })
            foreach (var west in new[] { false, true })
                foreach (var ice in new[] { 0, 1 })
                {
                    var state = UmadP1GravenImage2State.FromNetworkReplay(new UmadP1GravenImage2AiReplayStateMessage(
                        [purpleDps, !purpleDps], ice, ice == 1, [west, !west], PartyRole.ShieldHealer, PartyRole.MeleeDpsA))!;
                    var context = $"purpleDps {purpleDps} west {west} ice {ice}";
                    foreach (var cue in new[]
                    {
                        UmadP1GravenImage2Playbook.Uptime, UmadP1GravenImage2Playbook.FirstStack(state),
                        UmadP1GravenImage2Playbook.Spreads(state, 0), UmadP1GravenImage2Playbook.Buster,
                        UmadP1GravenImage2Playbook.FirstCleave(state), UmadP1GravenImage2Playbook.SecondStack,
                        UmadP1GravenImage2Playbook.Spreads(state, 1), UmadP1GravenImage2Playbook.SecondCleave(state),
                        UmadP1GravenImage2Playbook.Confetti(state), UmadP1GravenImage2Playbook.Soak(state),
                        UmadP1GravenImage2Playbook.LightOfJudgment,
                    })
                        PlaybookAssert.ExplainsEveryRole(cue, context);
                }
    }

    // The spread text names the side the strat actually sends each yellow role to.
    [Test]
    public void GravenImage2SpreadTextMatchesTheBotsSide()
    {
        var state = UmadP1GravenImage2State.FromNetworkReplay(new UmadP1GravenImage2AiReplayStateMessage(
            [false, false], 0, false, [true, true], PartyRole.MainTank, PartyRole.MeleeDpsA))!;
        foreach (var set in new[] { 0, 1 })
            foreach (var role in UmadP1Roles.Dps)
            {
                var exit = UmadP1GravenImage2Ai.TetherExit(state, set, role);
                var side = exit.X > 0f ? "east" : "west";
                Assert.That(UmadP1GravenImage2Playbook.Spreads(state, set).Why(role), Does.Contain(side), $"{role} set {set} at {exit}");
            }
    }
}
