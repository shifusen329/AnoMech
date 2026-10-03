using System.Numerics;
using System.Reflection;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P4KefkaSays;
using GazeLayout = AnoMech.Scenarios.Umad.P4KefkaSays.UmadP4KefkaSaysAi.GazeLayout;

namespace AnoMech.Tests;

public class UmadP4KefkaSaysPlaybookTests
{
    private static UmadP4KefkaSaysState State(int roll)
    {
        bool Bit(int n) => ((roll >> n) & 1) == 1;
        var supports = UmadP1Roles.Supports.Skip(roll % 4).Concat(UmadP1Roles.Supports.Take(roll % 4));
        var dps = UmadP1Roles.Dps.Skip((roll + 1) % 4).Concat(UmadP1Roles.Dps.Take((roll + 1) % 4));
        PartyRole[] wave1 = [.. supports, .. dps];
        PartyRole[] wave2 = [wave1[3], wave1[2], wave1[0], wave1[1], wave1[6], wave1[7], wave1[5], wave1[4]];
        PartyRole[] wave3 = [.. wave1.Reverse()];
        var wounds = Enumerable.Range(0, 8).Select(i => ((roll + i) & 1) == 1 ^ i % 3 == 0).ToArray();
        return UmadP4KefkaSaysState.FromNetworkReplay(SimParty.Empty,
            Enumerable.Range(0, 5).Select(i => (roll + i) % 2).ToArray(),
            Enumerable.Range(0, 5).Select(i => (roll / 2 + i) % 2).ToArray(),
            Enumerable.Range(0, 5).Select(i => (roll / 4 + i) % 2 == 0 ? 1f : -1f).ToArray(),
            Bit(0), wave1, Bit(1), wave2, Bit(2), Bit(3), Bit(4), wave3, wounds, Bit(5), roll * MathF.PI / 4f);
    }

    [Test]
    public void KefkaSaysExplainsEveryMove()
    {
        for (var roll = 0; roll < 64; roll++)
        {
            var state = State(roll);
            var cues = new List<StratCue>
            {
                UmadP4KefkaSaysPlaybook.Gather,
                UmadP4KefkaSaysPlaybook.MysteryMagic(state, 0), UmadP4KefkaSaysPlaybook.MysteryMagic(state, 1), UmadP4KefkaSaysPlaybook.MysteryMagic(state, 2),
                UmadP4KefkaSaysPlaybook.FloodOfNaught(state),
                UmadP4KefkaSaysPlaybook.Elements(state, 0), UmadP4KefkaSaysPlaybook.Elements(state, 1),
                UmadP4KefkaSaysPlaybook.AccelerationBombFake,
                UmadP4KefkaSaysPlaybook.FireBait, UmadP4KefkaSaysPlaybook.StrayFlames(state),
                UmadP4KefkaSaysPlaybook.WaterBait, UmadP4KefkaSaysPlaybook.StraySprayAndManaRelease(state),
            };
            foreach (var layout in Enum.GetValues<GazeLayout>())
                foreach (var round in new[] { 0, 1 })
                    foreach (var facing in new[] { false, true })
                        cues.Add(UmadP4KefkaSaysPlaybook.Gaze(state, layout, round, facing));
            foreach (var cue in cues)
                PlaybookAssert.ExplainsEveryRole(cue, $"roll {roll}");
        }
    }

    private static T Strat<T>(string method, params object[] args)
        => (T)typeof(UmadP4KefkaSaysAi).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, args)!;

    // The lane the text calls safe is where the strat's own Thunder solver stands, and the
    // party's Mystery Magic spot is in neither cone the text calls real.
    [Test]
    public void MysteryTextMatchesTheStratsSolver()
    {
        for (var roll = 0; roll < 8; roll++)
        {
            var state = State(roll);
            for (var cast = 0; cast < 3; cast++)
            {
                var spot = Strat<Vector2>("ThunderSafeSpot", state.Mystery[cast]);
                var lane = DeathRecap.Compass(Vector3.Zero, new Vector3(spot.X, 0f, spot.Y));
                var why = UmadP4KefkaSaysPlaybook.MysteryMagic(state, cast).Why(PartyRole.MainTank);
                Assert.That(why, Does.Contain($"just {lane} of the middle"), $"{state.Mystery[cast]}");

                var stack = Strat<Vector2>("SafeSpot", state.Mystery[cast]);
                var stackSide = DeathRecap.Compass(Vector3.Zero, new Vector3(stack.X, 0f, stack.Y));
                var realCones = why[(why.IndexOf("really hit point ") + 17)..why.IndexOf(", and")].Split(" and ");
                Assert.That(realCones, Has.No.Member(stackSide), $"{state.Mystery[cast]} stack at {stack}");
            }
        }
    }

    // The Flood text sends each role to the side the strat's FloodOfNaught does.
    [Test]
    public void FloodTextNamesTheBotsSide()
    {
        for (var roll = 0; roll < 64; roll++)
        {
            var state = State(roll);
            var move = Strat<IAiMove>("FloodOfNaught", state);
            foreach (var role in UmadP1Roles.All)
            {
                var spot = move[(int)role]!.Value;
                var side = DeathRecap.Compass(Vector3.Zero, new Vector3(spot.X, 0f, spot.Y));
                Assert.That(UmadP4KefkaSaysPlaybook.FloodOfNaught(state).Why(role), Does.Contain($"on the {side} side"), $"{role} roll {roll}");
            }
        }
    }
}
