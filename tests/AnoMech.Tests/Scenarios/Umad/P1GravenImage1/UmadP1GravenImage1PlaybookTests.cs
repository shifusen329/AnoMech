using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P1GravenImage1;

namespace AnoMech.Tests;

public class UmadP1GravenImage1PlaybookTests
{
    [Test]
    public void GravenImage1ExplainsEveryMove()
    {
        foreach (var tetherDps in new[] { false, true })
            foreach (var stack in new[] { false, true })
                foreach (var lie in new[] { false, true })
                    foreach (var ice in new[] { 0, 1 })
                    {
                        var state = UmadP1GravenImage1State.FromNetworkReplay(new UmadP1GravenImage1AiReplayStateMessage(
                            tetherDps, ice, lie, stack, lie, PartyRole.OffTank, PartyRole.MeleeDpsB,
                            [PartyRole.MainTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.CasterDps],
                            PartyRole.RegenHealer, PartyRole.PhysRangedDps, 1, 2, 1 - ice, lie, ice, lie, lie))!;
                        var context = $"tetherDps {tetherDps} stack {stack} lie {lie} ice {ice}";
                        foreach (var cue in new[]
                        {
                            UmadP1GravenImage1Playbook.Uptime, UmadP1GravenImage1Playbook.KnockbackTether(state),
                            UmadP1GravenImage1Playbook.MysteryMagic1(state), UmadP1GravenImage1Playbook.WaveCannonLineup,
                            UmadP1GravenImage1Playbook.Towers(state), UmadP1GravenImage1Playbook.Confetti(state),
                            UmadP1GravenImage1Playbook.MysteryMagic2(state), UmadP1GravenImage1Playbook.LightOfJudgment,
                        })
                            PlaybookAssert.ExplainsEveryRole(cue, context);
                    }
    }
}
