using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Scenarios.Umad.UmadConstants;
using TetherOrder = AnoMech.Scenarios.Umad.P3BlackHole.UmadP3BlackHoleAi.TetherOrder;

namespace AnoMech.Tests;

public class UmadP3BlackHolePlaybookTests
{
    // Seats 0-2 non-Accretion Supports, 4-6 non-Accretion DPS, 3 and 7 Accretion: either the
    // healer holds the First in Line one, or (after the fight's 3/7 swap) the DPS does.
    private static readonly PartyRole[] HealerFirstAccretion =
    [
        PartyRole.MainTank, PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.RegenHealer,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];

    private static readonly PartyRole[] DpsFirstAccretion =
    [
        PartyRole.OffTank, PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.CasterDps,
        PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.MeleeDpsA, PartyRole.ShieldHealer,
    ];

    private static UmadP3BlackHoleState State(PartyRole[] seats, uint slap, int kefka, uint implosion,
        ThunderIIIAssignment set1, ThunderIIIAssignment set2, bool supportStacksFirst)
    {
        var world = new SimWorld(new EventScheduler());
        PartyRole[] stacks = supportStacksFirst ? [PartyRole.RegenHealer, PartyRole.MeleeDpsA] : [PartyRole.CasterDps, PartyRole.OffTank];
        var other = slap == ActionId.SlapHappy_Left ? ActionId.SlapHappy_Right : ActionId.SlapHappy_Left;
        var kefkaRadians = Enumerable.Range(0, 5).Select(i => (kefka + 3 * i) * MathF.PI / 4f).ToArray();
        return UmadP3BlackHoleState.FromNetworkReplay(world, seats, stacks, [slap, other, slap], kefkaRadians, implosion, set1, set2);
    }

    private static IEnumerable<(UmadP3BlackHoleState State, TetherOrder Order, string Context)> States()
    {
        var plans = Enum.GetValues<ThunderIIIAssignment>();
        var i = 0;
        foreach (var order in Enum.GetValues<TetherOrder>())
            foreach (var seats in new[] { HealerFirstAccretion, DpsFirstAccretion })
                foreach (var slap in new[] { ActionId.SlapHappy_Left, ActionId.SlapHappy_Right })
                    foreach (var implosion in new[] { ActionId.LongitudinalImplosion, ActionId.LatitudinalImplosion })
                    {
                        i++;
                        var state = State(seats, slap, kefka: i % 8, implosion, plans[i % 4], plans[(i + 1) % 4], supportStacksFirst: i % 2 == 0);
                        yield return (state, order, $"{order} seats {(seats == HealerFirstAccretion ? "healer" : "dps")}-Accretion slap {slap:X} implosion {implosion:X} kefka {i % 8}");
                    }
    }

    [Test]
    public void BlackHoleExplainsEveryMove()
    {
        foreach (var (state, order, context) in States())
            foreach (var cue in new[]
            {
                UmadP3BlackHolePlaybook.Earthquake, UmadP3BlackHolePlaybook.AccretionHeals,
                UmadP3BlackHolePlaybook.BlackHoleWave(state, order, 0), UmadP3BlackHolePlaybook.BlackHoleWave(state, order, 3),
                UmadP3BlackHolePlaybook.Grab(state, order, 0), UmadP3BlackHolePlaybook.Grab(state, order, 2),
                UmadP3BlackHolePlaybook.Pull(state, order), UmadP3BlackHolePlaybook.ReturnToMiddle(state, order),
                UmadP3BlackHolePlaybook.LookUponSplit(state, order),
                UmadP3BlackHolePlaybook.GrabBoth(state, order), UmadP3BlackHolePlaybook.HoldBoth(state, order),
                UmadP3BlackHolePlaybook.ExdeathToFirstTether,
                UmadP3BlackHolePlaybook.Slap(state, 0, 0), UmadP3BlackHolePlaybook.Slap(state, 1, 1), UmadP3BlackHolePlaybook.Slap(state, 2, 3),
                UmadP3BlackHolePlaybook.Edict, UmadP3BlackHolePlaybook.EdictAndBodySlam(state),
                UmadP3BlackHolePlaybook.ImplosionAnchor(state),
                UmadP3BlackHolePlaybook.Implosion(state, 0), UmadP3BlackHolePlaybook.Implosion(state, 1),
                UmadP3BlackHolePlaybook.ThunderTank(state.ThunderSet1, UmadP3BlackHolePlaybook.Thunder1),
                UmadP3BlackHolePlaybook.ThunderSwap(state.ThunderSet1, UmadP3BlackHolePlaybook.Thunder1),
                UmadP3BlackHolePlaybook.ThunderStack(state.ThunderSet2, UmadP3BlackHolePlaybook.Thunder2),
                UmadP3BlackHolePlaybook.ThunderSwap(state.ThunderSet2, UmadP3BlackHolePlaybook.Thunder2),
                UmadP3BlackHolePlaybook.ThunderClearance(PartyRole.MainTank, PartyRole.OffTank),
                UmadP3BlackHolePlaybook.ThunderClearance(PartyRole.OffTank, null),
                UmadP3BlackHolePlaybook.StompPreposition(state), UmadP3BlackHolePlaybook.StompCorners(state),
                UmadP3BlackHolePlaybook.StompStackAndTowers(state),
                UmadP3BlackHolePlaybook.StompSwap(state, west: true), UmadP3BlackHolePlaybook.StompSwap(state, west: false),
            })
                PlaybookAssert.ExplainsEveryRole(cue, context);
    }

    // NAUR's rotation: sets 1-2 one then two tethers, sets 3-10 the First, Second then Third in
    // Line taking over one tether at a time; each player's third laser cleanses.
    [TestCase(TetherOrder.DpsSupportAccretion, 4, 1)]
    [TestCase(TetherOrder.DpsSupportAccretion, 0, 2)]
    [TestCase(TetherOrder.DpsSupportAccretion, 3, 3)]
    [TestCase(TetherOrder.DpsSupportAccretion, 5, 4)]
    [TestCase(TetherOrder.DpsSupportAccretion, 1, 5)]
    [TestCase(TetherOrder.DpsSupportAccretion, 7, 6)]
    [TestCase(TetherOrder.DpsSupportAccretion, 6, 7)]
    [TestCase(TetherOrder.DpsSupportAccretion, 2, 8)]
    [TestCase(TetherOrder.SupportDpsAccretion, 0, 1)]
    [TestCase(TetherOrder.SupportDpsAccretion, 4, 2)]
    [TestCase(TetherOrder.SupportDpsAccretion, 3, 3)]
    [TestCase(TetherOrder.SupportDpsAccretion, 6, 8)]
    public void TetherTurnNamesTheSeatsLasers(TetherOrder order, int seat, int firstSet)
    {
        var state = State(HealerFirstAccretion, ActionId.SlapHappy_Left, 0, ActionId.LongitudinalImplosion,
            ThunderIIIAssignment.MtInvulnsBoth, ThunderIIIAssignment.ShareMtFirst, supportStacksFirst: true);
        var why = UmadP3BlackHolePlaybook.Pull(state, order).Why(HealerFirstAccretion[seat]);
        Assert.That(why, Does.Contain($"sets {firstSet}, {firstSet + 1} and {firstSet + 2}"));
    }

    // Modified DSA: Support 1st takes set 1 alone, DPS 1st both of set 2, Support 3rd both of set 9,
    // DPS 3rd set 10; the other seats keep D>S>A's lasers.
    [TestCase(0, "sets 1, 3 and 4")]
    [TestCase(4, "lasers are both of set 2, then set 3")]
    [TestCase(2, "lasers are set 8, then both of set 9")]
    [TestCase(6, "sets 7, 8 and 10")]
    [TestCase(5, "sets 4, 5 and 6")]
    [TestCase(3, "sets 3, 4 and 5")]
    public void ModifiedDsaTurnNamesTheSeatsLasers(int seat, string sets)
    {
        var state = State(HealerFirstAccretion, ActionId.SlapHappy_Left, 0, ActionId.LongitudinalImplosion,
            ThunderIIIAssignment.MtInvulnsBoth, ThunderIIIAssignment.ShareMtFirst, supportStacksFirst: true);
        var why = UmadP3BlackHolePlaybook.Pull(state, TetherOrder.ModifiedDsa).Why(HealerFirstAccretion[seat]);
        Assert.That(why, Does.Contain(sets));
    }

    [Test]
    public void ModifiedDsaLastTetherIsTheDpsThirdInLines()
    {
        var state = State(HealerFirstAccretion, ActionId.SlapHappy_Left, 0, ActionId.LongitudinalImplosion,
            ThunderIIIAssignment.MtInvulnsBoth, ThunderIIIAssignment.ShareMtFirst, supportStacksFirst: true);
        var cue = UmadP3BlackHolePlaybook.LookUponSplit(state, TetherOrder.ModifiedDsa);
        Assert.That(cue.Why(HealerFirstAccretion[6]), Does.Contain("You hold the last tether"));
        Assert.That(cue.Why(HealerFirstAccretion[2]), Does.Not.Contain("You hold the last tether"));
    }

    // The slap text names the side DodgeSlap sends each role to.
    [Test]
    public void SlapTextNamesTheBotsSide()
    {
        foreach (var slap in new[] { ActionId.SlapHappy_Left, ActionId.SlapHappy_Right })
            for (var kefka = 0; kefka < 8; kefka++)
            {
                var state = State(HealerFirstAccretion, slap, kefka, ActionId.LongitudinalImplosion,
                    ThunderIIIAssignment.MtInvulnsBoth, ThunderIIIAssignment.ShareMtFirst, supportStacksFirst: true);
                var direction = state.KefkaPosition[0];
                foreach (var role in UmadP1Roles.All)
                {
                    var spot = slap == ActionId.SlapHappy_Left
                        ? direction.Apply(new Vector3(9f, 0f, 0f))
                        : direction.Flip().Apply(role.IsTank() ? new Vector3(7f, 0f, 7f) : role.IsDps() ? new Vector3(7f, 0f, -7f) : new Vector3(9f, 0f, 0f));
                    var side = DeathRecap.Compass(Vector3.Zero, spot);
                    var why = UmadP3BlackHolePlaybook.Slap(state, 0, 0).Why(role);
                    Assert.That(why, Does.Contain(slap == ActionId.SlapHappy_Left ? $"on the {side} side" : $"to the {side}."), $"{role} slap {slap:X} kefka {kefka}");
                }
            }
    }
}
