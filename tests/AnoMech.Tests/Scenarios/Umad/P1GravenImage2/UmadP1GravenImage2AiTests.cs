using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1GravenImage2;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Tests;

// The bots' spots against the mechanics' real sizes, for every roll that moves them. A puddle sits
// up to 0.6y off its stack spot (stack offset plus jitter); a bot lands up to 0.3y off its own.
public class UmadP1GravenImage2AiTests
{
    private const float Jitter = 0.3f;
    private const float PuddleSlack = 0.6f;
    private const float Puddle = UmadP1GravenImage2Scenario.PuddleRadius;
    private const float Vitrophyre = UmadP1GravenImage2Scenario.VitrophyreRadius;
    private static readonly Vector2 SouthStack = new(0f, 12f);

    private static UmadP1GravenImage2State State(bool purple1 = false, bool purple2 = false, int ice = 0,
        bool cleave1West = true, bool cleave2West = true, PartyRole trapSupport = PartyRole.MainTank, PartyRole trapDps = PartyRole.MeleeDpsA)
        => UmadP1GravenImage2State.FromNetworkReplay(new UmadP1GravenImage2AiReplayStateMessage(
            [purple1, purple2], ice, false, [cleave1West, cleave2West], trapSupport, trapDps))!;

    [Test]
    public void FirstPuddleStackClearsTheIce()
    {
        foreach (var ice in new[] { 0, 1 })
        {
            var state = State(ice: ice);
            Assert.That(state.Ice.Clearance(UmadP1GravenImage2Ai.FirstPuddleStack(state)), Is.GreaterThan(PuddleSlack + 0.3f), $"ice {ice}");
        }
    }

    // Every Gravitas must land on all eight, whichever role gets the puddles.
    [Test]
    public void PuddleStacksPutEveryoneInEveryGravitas()
    {
        foreach (var ice in new[] { 0, 1 })
        {
            var state = State(ice: ice);
            foreach (var stack in new[] { UmadP1GravenImage2Ai.FirstPuddleStack(state), SouthStack })
            {
                var spots = UmadP1Roles.All.Select(r => UmadP1GravenImage2Ai.PuddleStackSpot(stack, r)).ToList();
                Assert.That(spots.All(a => spots.All(b => Vector2.Distance(a, b) < Puddle - 2 * Jitter)), Is.True, $"stack {stack}, ice {ice}");
            }
        }
    }

    [Test]
    public void TetherExitsKeepSpreadsOffPuddlesAndEachOther()
    {
        foreach (var ice in new[] { 0, 1 })
            foreach (var set in new[] { 0, 1 })
                foreach (var purpleDps in new[] { false, true })
                {
                    var state = State(purple1: purpleDps, purple2: purpleDps, ice: ice);
                    var north = UmadP1GravenImage2Ai.FirstPuddleStack(state);
                    var puddles = set == 0 ? new[] { north } : [north, SouthStack];
                    var exits = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage2Ai.TetherExit(state, set, r));
                    var yellow = state.YellowRoles(set).ToList();
                    var context = $"ice {ice} set {set} purpleDps {purpleDps}";
                    foreach (var role in UmadP1Roles.All)
                    {
                        var spot = exits[role];
                        foreach (var puddle in puddles)
                        {
                            var touch = yellow.Contains(role) ? Vitrophyre + Puddle : Puddle;
                            if (!yellow.Contains(role) && puddle == puddles[^1]) continue;
                            Assert.That(Vector2.Distance(spot, puddle), Is.GreaterThan(touch + PuddleSlack + Jitter), $"{role} at {spot} vs puddle {puddle}, {context}");
                        }
                        foreach (var spread in yellow.Where(y => y != role))
                            Assert.That(Vector2.Distance(spot, exits[spread]), Is.GreaterThan(Vitrophyre + 2 * Jitter), $"{role} inside {spread}'s Vitrophyre, {context}");
                    }
                }
    }

    [Test]
    public void CleaveSpotsStayOnTheSafeHalfAndOutOfPuddles()
    {
        foreach (var ice in new[] { 0, 1 })
            foreach (var west in new[] { true, false })
            {
                var state = State(ice: ice, cleave1West: west, cleave2West: west);
                var north = UmadP1GravenImage2Ai.FirstPuddleStack(state);
                var safeSide = west ? 1f : -1f;
                foreach (var role in UmadP1Roles.All)
                {
                    var first = UmadP1GravenImage2Ai.FirstCleaveSpot(state, role);
                    var second = UmadP1GravenImage2Ai.SecondCleaveSpot(state, role);
                    var context = $"{role} ice {ice} west {west}";
                    Assert.That(first.X * safeSide, Is.GreaterThan(Jitter + 0.5f), context);
                    Assert.That(second.X * safeSide, Is.GreaterThan(Jitter + 0.5f), context);
                    Assert.That(Vector2.Distance(first, north), Is.GreaterThan(Puddle + PuddleSlack + Jitter), context);
                    Assert.That(Vector2.Distance(second, north), Is.GreaterThan(Puddle + PuddleSlack + Jitter), context);
                    Assert.That(Vector2.Distance(second, SouthStack), Is.GreaterThan(Puddle + PuddleSlack + Jitter), context);
                }
            }
    }

    [Test]
    public void TrapThrowsEachStackIntoTheFarPuddles()
    {
        foreach (var ice in new[] { 0, 1 })
            foreach (var support in UmadP1Roles.Supports)
                foreach (var dps in UmadP1Roles.Dps)
                {
                    var state = State(ice: ice, trapSupport: support, trapDps: dps);
                    var north = UmadP1GravenImage2Ai.FirstPuddleStack(state);
                    var spots = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage2Ai.TrapSpot(state, r));
                    foreach (var holder in new[] { support, dps })
                    {
                        var context = $"holder {holder} ice {ice}";
                        Assert.That(UmadP1Roles.All.Count(r => Vector2.Distance(spots[r], spots[holder]) <= 6f - Jitter), Is.EqualTo(4), context);
                        Assert.That(UmadP1Roles.All.Count(r => Vector2.Distance(spots[r], spots[holder]) <= 6f + Jitter), Is.EqualTo(4), context);
                        foreach (var puddle in new[] { north, SouthStack })
                            Assert.That(Vector2.Distance(spots[holder], puddle), Is.GreaterThan(Puddle + PuddleSlack + Jitter), context);
                        var far = UmadP1Roles.IsDps(holder) ? north : SouthStack;
                        foreach (var role in UmadP1Roles.Of(UmadP1Roles.IsDps(holder)).Where(r => r != holder))
                        {
                            var land = spots[role] + Vector2.Normalize(spots[role] - spots[holder]) * 14f;
                            Assert.That(Vector2.Distance(land, far), Is.LessThan(Puddle - PuddleSlack - Jitter), $"{role} lands at {land}, {context}");
                        }
                    }
                }
    }

    [Test]
    public void PuddleSoaksSplitFourAndFour()
    {
        foreach (var ice in new[] { 0, 1 })
            foreach (var support in UmadP1Roles.Supports)
                foreach (var dps in UmadP1Roles.Dps)
                {
                    var state = State(ice: ice, trapSupport: support, trapDps: dps);
                    var north = UmadP1GravenImage2Ai.FirstPuddleStack(state);
                    var spots = UmadP1Roles.All.Select(r => UmadP1GravenImage2Ai.PuddleSoakSpot(state, r)).ToList();
                    var context = $"holders {support}/{dps} ice {ice}";
                    Assert.That(spots.Count(s => Vector2.Distance(s, north) < Puddle - PuddleSlack - Jitter), Is.EqualTo(4), context);
                    Assert.That(spots.Count(s => Vector2.Distance(s, SouthStack) < Puddle - PuddleSlack - Jitter), Is.EqualTo(4), context);
                }
    }
}
