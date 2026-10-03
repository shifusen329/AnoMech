using System.Numerics;
using System.Reflection;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P2Forsaken;
using AnoMech.Scenarios.Umad.P2Forsaken.Ai;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Tests;

// Runs each strat's helper tower by tower (its private steps, by reflection, since Run needs a
// SimWorld) and checks the recap text against the spots the bots really take.
public class UmadP2ForsakenPlaybookTests
{
    private const uint Stack = LockonId.ForsakenStack;
    private const uint Cone = LockonId.ForsakenCone;
    private const uint Chariot = LockonId.ForsakenChariot;

    private sealed record Strat(string Name, UmadP2ForsakenPlaybook Playbook, Func<object> Helper);

    private static readonly Strat KroxyRinon = new("Kroxy-Rinon", UmadP2ForsakenPlaybook.KroxyRinon,
        () => new UmadP2ForsakenRinonAiHelper(UmadP2ForsakenRinonAiHelper.StandardOdd, UmadP2ForsakenRinonAiHelper.DiamonMarkersEven,
            UmadP2ForsakenRinonAiHelper.KroxyReorder, UmadP2ForsakenPlaybook.KroxyRinon));

    private static readonly Strat KroxyRinonOld = new("Kroxy-Rinon old", UmadP2ForsakenPlaybook.KroxyRinonOld,
        () => new UmadP2ForsakenRinonAiHelper(UmadP2ForsakenRinonAiHelper.StandardOdd, UmadP2ForsakenRinonAiHelper.OldEven,
            UmadP2ForsakenRinonAiHelper.KroxyReorder, UmadP2ForsakenPlaybook.KroxyRinonOld));

    private static readonly Strat SouthFlex = new("South Flex", UmadP2ForsakenPlaybook.SouthFlex,
        () => new UmadP2ForsakenRinonAiHelper(UmadP2ForsakenRinonAiHelper.StandardOdd, UmadP2ForsakenRinonAiHelper.DiamonMarkersEven,
            UmadP2ForsakenRinonAiHelper.SouthFlexReorder, UmadP2ForsakenPlaybook.SouthFlex));

    private static readonly Strat SouthFlexOld = new("South Flex old", UmadP2ForsakenPlaybook.SouthFlexOld,
        () => new UmadP2ForsakenRinonAiHelper(UmadP2ForsakenRinonAiHelper.StandardOdd, UmadP2ForsakenRinonAiHelper.OldEven,
            UmadP2ForsakenRinonAiHelper.SouthFlexReorder, UmadP2ForsakenPlaybook.SouthFlexOld));

    private static readonly Strat LpduBuddies = new("LPDU Buddies", UmadP2ForsakenPlaybook.LpduBuddies,
        () => new UmadP2ForsakenLpduBuddiesAiHelper(UmadP2ForsakenLpduBuddiesAiHelper.StandardOdd, UmadP2ForsakenLpduBuddiesAiHelper.DiamonMarkersEven,
            UmadP2ForsakenLpduBuddiesAiHelper.KroxyReorder, UmadP2ForsakenPlaybook.LpduBuddies));

    private static readonly Strat P3ZBuddyMeow = new("p3Z Buddy Meow", UmadP2ForsakenPlaybook.P3ZBuddyMeow,
        () => new UmadP2ForsakenP3ZBuddyMeowAiHelper(UmadP2ForsakenP3ZBuddyMeowAiHelper.StandardOdd, UmadP2ForsakenP3ZBuddyMeowAiHelper.DiamonMarkersEven,
            UmadP2ForsakenP3ZBuddyMeowAiHelper.KroxyReorder, UmadP2ForsakenPlaybook.P3ZBuddyMeow));

    private static readonly Strat ZP6SouthAdjust = new("zP6 South adjust", UmadP2ForsakenPlaybook.ZP6SouthAdjust,
        () => new UmadP2ForsakenZP6SouthAdjustAiHelper(UmadP2ForsakenZP6SouthAdjustAiHelper.StandardOdd, UmadP2ForsakenZP6SouthAdjustAiHelper.DiamonMarkersEven,
            UmadP2ForsakenZP6SouthAdjustAiHelper.SouthFlexReorder, UmadP2ForsakenPlaybook.ZP6SouthAdjust));

    private static readonly Strat[] All = [KroxyRinon, KroxyRinonOld, SouthFlex, SouthFlexOld, LpduBuddies, P3ZBuddyMeow, ZP6SouthAdjust];

    private static readonly EndAttack[][] EndRolls =
    [
        [EndAttack.FuturesEnd, EndAttack.PastsEnd, EndAttack.FuturesEnd, EndAttack.PastsEnd],
        [EndAttack.PastsEnd, EndAttack.FuturesEnd, EndAttack.PastsEnd, EndAttack.FuturesEnd],
    ];

    private sealed class HelperRun
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly object helper;

        public HelperRun(object helper, UmadP2ForsakenState state)
        {
            this.helper = helper;
            helper.GetType().GetField("state", Private)!.SetValue(helper, state);
            helper.GetType().GetMethod("Init", Private)!.Invoke(helper, null);
        }

        public IReadOnlyList<PartyRole> Soakers(int index)
            => (IReadOnlyList<PartyRole>)helper.GetType().GetField(index is < 3 or 7 ? "alpha" : "beta", Private)!.GetValue(helper)!;

        public IAiMove Tower(int index)
            => ((Func<IAiMove>)helper.GetType().GetMethod("TowerPositions", Private)!.Invoke(helper, [index])!)();
    }

    private sealed record TowerSet(int Index, UmadP2ForsakenState State, IReadOnlyList<PartyRole> Soakers, Dictionary<PartyRole, Vector2> Spots,
        Dictionary<PartyRole, uint> Markers);

    // Every roll of the opening markers and End attacks, each played through all eight sets with
    // the scenario's own reassignment after each one. Spots come back in the authoring frame, the
    // tower pair to the north and the left tower (facing Kefka) at +X. The state moves on to the
    // next set's markers once the caller asks for it, so read each set before taking the next.
    private static IEnumerable<(string Context, TowerSet Set)> Play(Strat strat)
    {
        var seed = 0;
        foreach (var supportMarker in new[] { Chariot, Cone })
            foreach (var supportStack in UmadP1Roles.Supports)
                foreach (var dpsStack in UmadP1Roles.Dps)
                    foreach (var ends in EndRolls)
                    {
                        var rng = new Random(++seed);
                        var dpsMarker = supportMarker == Cone ? Chariot : Cone;
                        var lockons = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1Roles.IsDps(r) ? dpsMarker : supportMarker);
                        lockons[supportStack] = Stack;
                        lockons[dpsStack] = Stack;
                        var state = UmadP2ForsakenState.FromNetworkReplay(ends, MathF.PI / 4 * (seed % 8), seed % 2 == 0 ? 1 : -1, lockons);
                        var run = new HelperRun(strat.Helper(), state);
                        for (var index = 0; index < 8; index++)
                        {
                            var move = run.Tower(index);
                            var turn = -state.NewNorthAt(index).RadiansFromNorth;
                            var spots = UmadP1Roles.All.ToDictionary(r => r, r => Rotate(move[(int)r]!.Value, turn));
                            var soakers = run.Soakers(index);
                            yield return ($"{strat.Name} set {index + 1}, {supportMarker} supports, stacks {supportStack}/{dpsStack}, seed {seed}",
                                new TowerSet(index, state, soakers, spots, new Dictionary<PartyRole, uint>(state.Lockons)));
                            Reassign(state, soakers.ToList(), index, rng);
                        }
                    }
    }

    private static Vector2 Rotate(Vector2 v, float radians)
        => new(v.X * MathF.Cos(radians) - v.Y * MathF.Sin(radians), v.X * MathF.Sin(radians) + v.Y * MathF.Cos(radians));

    private static void Reassign(UmadP2ForsakenState state, List<PartyRole> soakers, int index, Random rng)
    {
        uint[] next = index is 6 or 7 ? [0, 0, 0, 0] : index % 2 == 0 ? [Cone, Cone, Chariot, Chariot] : [Stack, Stack, Cone, Chariot];
        var shuffled = soakers.OrderBy(_ => rng.Next()).ToList();
        for (var i = 0; i < 4; i++) state.Lockons[shuffled[i]] = next[i];
    }

    private static bool OnLeft(TowerSet set, PartyRole role) => set.Spots[role].X > 0f;

    [Test]
    public void EveryStratExplainsEveryMove()
    {
        foreach (var strat in All)
        {
            foreach (var ends in EndRolls)
            {
                var state = UmadP2ForsakenState.FromNetworkReplay(ends, 0f, 1, UmadP1Roles.All.ToDictionary(r => r, _ => Cone));
                var cues = new[] { strat.Playbook.Lineup, strat.Playbook.LastClonesGather(state) }
                    .Concat(Enumerable.Range(0, 4).Select(i => strat.Playbook.EndBait(state, i)));
                foreach (var cue in cues)
                    PlaybookAssert.ExplainsEveryRole(cue, $"{strat.Name}, {ends[0]}");
            }
            foreach (var (context, set) in Play(strat))
                PlaybookAssert.ExplainsEveryRole(strat.Playbook.Towers(set.State, set.Index, set.Soakers), context);
        }
    }

    // The side the text names is the side the bot runs to, soakers and outside players alike.
    [Test]
    public void TowerTextNamesTheBotsTower()
    {
        foreach (var strat in All)
            foreach (var (context, set) in Play(strat))
            {
                var cue = strat.Playbook.Towers(set.State, set.Index, set.Soakers);
                foreach (var role in UmadP1Roles.All)
                {
                    var why = cue.Why(role);
                    var side = OnLeft(set, role) ? "left" : "right";
                    var other = OnLeft(set, role) ? "right" : "left";
                    if (set.Soakers.Contains(role))
                    {
                        Assert.That(strat.Playbook.SoaksLeft(set.State, set.Index, set.Soakers, role), Is.EqualTo(OnLeft(set, role)), $"{role}, {context}");
                        Assert.That(why, Does.Contain($"{side} tower").And.Not.Contain($"{other} tower"), $"{role}, {context}");
                    }
                    else if (why.Contains("left tower") != why.Contains("right tower"))
                        Assert.That(why, Does.Contain($"{side} tower"), $"{role}, {context}");
                }
            }
    }

    // NAUR: Support stack left and DPS stack right, cone left and spread right; in even sets
    // Supports left and DPS right. The healer or ranged keeps the side, and a tank or melee flexes
    // when their marker matches their partner's (or, in odd sets, when both stacks share a role).
    private static bool NaurLeft(TowerSet set, PartyRole role)
    {
        var marker = set.Markers[role];
        var support = !UmadP1Roles.IsDps(role);
        if (set.Index % 2 == 0 && marker != Stack) return marker == Cone;
        var flexes = UmadP1Roles.IsTankOrMelee(role) && set.Soakers.Any(r => r != role && set.Markers[r] == marker
            && (set.Index % 2 == 0 ? UmadP1Roles.IsDps(r) == UmadP1Roles.IsDps(role) : r == Partner(role)));
        return support != flexes;
    }

    private static PartyRole Partner(PartyRole role)
        => role switch
        {
            PartyRole.MainTank => PartyRole.RegenHealer,
            PartyRole.RegenHealer => PartyRole.MainTank,
            PartyRole.OffTank => PartyRole.ShieldHealer,
            PartyRole.ShieldHealer => PartyRole.OffTank,
            PartyRole.MeleeDpsA => PartyRole.PhysRangedDps,
            PartyRole.PhysRangedDps => PartyRole.MeleeDpsA,
            PartyRole.MeleeDpsB => PartyRole.CasterDps,
            _ => PartyRole.MeleeDpsB,
        };

    [Test]
    public void KroxyRinonAndTheBuddyStratsFlexLikeNaur()
    {
        foreach (var strat in new[] { KroxyRinon, KroxyRinonOld, LpduBuddies, P3ZBuddyMeow })
            foreach (var (context, set) in Play(strat))
            {
                if (strat == P3ZBuddyMeow && set.Index == 0) continue;
                foreach (var role in set.Soakers)
                    Assert.That(OnLeft(set, role), Is.EqualTo(NaurLeft(set, role)), $"{role}, {context}");
            }
    }

    [Test]
    public void BuddyMeowPutsTheConeRolesStackLeftOnSetOne()
    {
        foreach (var (context, set) in Play(P3ZBuddyMeow).Where(p => p.Set.Index == 0))
        {
            var supportsHoldCones = set.Markers.Any(m => !UmadP1Roles.IsDps(m.Key) && m.Value == Cone);
            foreach (var role in set.Soakers.Where(r => set.Markers[r] == Stack))
                Assert.That(OnLeft(set, role), Is.EqualTo(UmadP1Roles.IsDps(role) != supportsHoldCones), $"{role}, {context}");
        }
    }

    // South Flex: keep the last tower; when both players of a tower share a marker, the one further
    // south (toward the towers, -Z here) flexes. Odd sets still pin the cone left and the spread right.
    [Test]
    public void SouthStratsKeepTheTowerAndFlexTheSouthPlayer()
    {
        foreach (var strat in new[] { SouthFlex, SouthFlexOld, ZP6SouthAdjust })
        {
            var last = new Dictionary<PartyRole, Vector2>();
            foreach (var (context, set) in Play(strat))
            {
                if (set.Index == 0) last.Clear();
                if (set.Index is not (0 or 3))
                    foreach (var role in set.Soakers)
                    {
                        var marker = set.Markers[role];
                        if (set.Index % 2 == 0 && marker != Stack) continue;
                        var wasLeft = last[role].X > 0f;
                        var mate = set.Soakers.Single(r => r != role && last[r].X > 0f == wasLeft);
                        var flexes = set.Markers[mate] == marker && last[role].Y < last[mate].Y;
                        Assert.That(OnLeft(set, role), Is.EqualTo(wasLeft != flexes), $"{role}, {context}");
                    }
                foreach (var role in set.Soakers) last[role] = set.Spots[role];
            }
        }
    }
}
