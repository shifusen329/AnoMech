using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P1TeleTrouncing;

namespace AnoMech.Tests;

public class UmadP1TeleTrouncingPlaybookTests
{
    [Test]
    public void TeleTrouncingExplainsEveryMove()
    {
        foreach (var seed in Enumerable.Range(1, 12))
        {
            var state = new UmadP1TeleTrouncingState(new Rng(seed), new UmadP1TeleTrouncingStateOverrides());
            foreach (var cue in new[]
            {
                UmadP1TeleTrouncingPlaybook.Arrow(state, first: true), UmadP1TeleTrouncingPlaybook.Arrow(state, first: false),
                UmadP1TeleTrouncingPlaybook.Confetti(state), UmadP1TeleTrouncingPlaybook.Tethers,
                UmadP1TeleTrouncingPlaybook.MysteryMagic3(state),
            })
                PlaybookAssert.ExplainsEveryRole(cue, $"seed {seed}");
        }
    }
}
