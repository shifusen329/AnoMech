using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1GravenImage1;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Tests;

// The bots' spots against the mechanics' real sizes, for every roll that moves them. AiManager's
// jitter (up to 0.3y) is the margin each check keeps.
public class UmadP1GravenImage1AiTests
{
    private const float Jitter = 0.3f;
    private static readonly Vector2 WaveCannonOrigin = new(0f, -35f);

    private static UmadP1GravenImage1State State(bool tetherDps = false, int ice1 = 0, bool fireStack = false,
        PartyRole trapSupport = PartyRole.MainTank, PartyRole trapDps = PartyRole.MeleeDpsA,
        int ice2 = 0, int thunderOffset = 0, bool thunderFlipped = false)
        => UmadP1GravenImage1State.FromNetworkReplay(new UmadP1GravenImage1AiReplayStateMessage(
            tetherDps, ice1, false, fireStack, false, PartyRole.MainTank, PartyRole.MeleeDpsA,
            [PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB],
            trapSupport, trapDps, 0, 0, ice2, false, thunderOffset, thunderFlipped, false))!;

    private static IEnumerable<(bool TetherDps, int Ice, bool FireStack)> FirstSetRolls()
    {
        foreach (var tetherDps in new[] { false, true })
            foreach (var ice in new[] { 0, 1 })
                foreach (var fireStack in new[] { false, true })
                    yield return (tetherDps, ice, fireStack);
    }

    [Test]
    public void PulseWaveLandsEveryTetheredRoleInItsSafeQuadrant()
    {
        foreach (var (tetherDps, ice, fireStack) in FirstSetRolls())
        {
            var state = State(tetherDps, ice, fireStack);
            foreach (var role in state.TetheredRoles)
            {
                var quadrant = state.Ice1.SafeQuadrant(west: !UmadP1Roles.IsDps(role));
                var start = UmadP1GravenImage1Ai.PrepositionForKnockback(UmadP1GravenImage1Ai.QuadrantSpot(state, role), quadrant);
                var land = UmadP1GravenImage1Ai.KnockbackLanding(start);
                var context = $"{role} tetherDps={tetherDps} ice={ice} stack={fireStack}: start {start}, lands {land}";
                Assert.That(start.Length(), Is.LessThanOrEqualTo(17.5f), context);
                Assert.That(land.Length(), Is.LessThanOrEqualTo(18f), context);
                Assert.That(state.Ice1.Clearance(land), Is.GreaterThanOrEqualTo(1f), context);
            }
        }
    }

    [Test]
    public void QuadrantSpotsClearTheIceAndTheFire()
    {
        foreach (var (tetherDps, ice, fireStack) in FirstSetRolls())
        {
            var state = State(tetherDps, ice, fireStack);
            var spots = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage1Ai.QuadrantSpot(state, r));
            foreach (var (role, spot) in spots)
                Assert.That(state.Ice1.Clearance(spot), Is.GreaterThan(Jitter + 0.4f), $"{role} at {spot} in ice {ice}");
            if (fireStack)
            {
                foreach (var dps in new[] { false, true })
                {
                    var group = UmadP1Roles.Of(dps).Select(r => spots[r]).ToList();
                    Assert.That(group.All(a => group.All(b => Vector2.Distance(a, b) <= 2f)), Is.True, "a stack spreads out");
                }
                foreach (var support in UmadP1Roles.Supports)
                    foreach (var dps in UmadP1Roles.Dps)
                        Assert.That(Vector2.Distance(spots[support], spots[dps]), Is.GreaterThan(6f + 2 * Jitter), $"{support} and {dps} share a stack");
            }
            else
            {
                foreach (var a in UmadP1Roles.All)
                    foreach (var b in UmadP1Roles.All.Where(b => b > a))
                        Assert.That(Vector2.Distance(spots[a], spots[b]), Is.GreaterThan(5f + 2 * Jitter), $"{a} and {b} share a spread");
            }
        }
    }

    [Test]
    public void EachWaveCannonLineHitsOnlyItsTarget()
    {
        var spots = UmadP1GravenImage1Ai.WaveCannonLineupSpots;
        foreach (var target in spots)
        {
            var forward = Vector2.Normalize(target - WaveCannonOrigin);
            foreach (var other in spots.Where(s => s != target))
            {
                var rel = other - WaveCannonOrigin;
                var across = MathF.Abs(rel.X * forward.Y - rel.Y * forward.X);
                Assert.That(across, Is.GreaterThan(3f + 2 * Jitter), $"the line at {target} clips {other}");
            }
        }
    }

    [Test]
    public void TowersEachGetOneSoakerAndTheVulnerableStayOut()
    {
        var lineup = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage1Ai.WaveCannonLineupSpots[(int)r]);
        foreach (var supports in Pairs(UmadP1Roles.Supports))
            foreach (var dps in Pairs(UmadP1Roles.Dps))
            {
                PartyRole[] targets = [.. supports, .. dps];
                var spots = UmadP1GravenImage1Ai.TowerSoakSpots(targets, lineup, targets.ToHashSet());
                var towers = targets.Select(t => lineup[t]).ToList();
                var context = $"targets {string.Join(",", targets)}";
                foreach (var tower in towers)
                    Assert.That(UmadP1Roles.All.Count(r => !targets.Contains(r) && Vector2.Distance(spots[(int)r]!.Value, tower) <= 4f - Jitter), Is.EqualTo(1), $"tower at {tower}, {context}");
                foreach (var role in UmadP1Roles.All)
                {
                    var spot = spots[(int)role]!.Value;
                    var inside = towers.Count(t => Vector2.Distance(spot, t) <= 4f + Jitter);
                    Assert.That(inside, Is.EqualTo(targets.Contains(role) ? 0 : 1), $"{role} at {spot}, {context}");
                }
            }
    }

    [Test]
    public void TrapStacksHoldFourAndLandInsideTheArena()
    {
        foreach (var support in UmadP1Roles.Supports)
            foreach (var dps in UmadP1Roles.Dps)
            {
                var state = State(trapSupport: support, trapDps: dps);
                var spots = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage1Ai.TrapSpot(state, r));
                foreach (var holder in new[] { support, dps })
                {
                    var inStack = UmadP1Roles.All.Where(r => Vector2.Distance(spots[r], spots[holder]) <= 6f - Jitter).ToList();
                    Assert.That(inStack, Is.EquivalentTo(UmadP1Roles.Of(UmadP1Roles.IsDps(holder))), $"holder {holder}");
                    Assert.That(UmadP1Roles.All.Count(r => Vector2.Distance(spots[r], spots[holder]) <= 6f + Jitter), Is.EqualTo(4), $"holder {holder}");
                    foreach (var role in inStack.Where(r => r != holder))
                    {
                        var land = spots[role] + Vector2.Normalize(spots[role] - spots[holder]) * 14f;
                        Assert.That(land.Length(), Is.LessThan(19f), $"{role} thrown off by {holder}'s trap");
                    }
                }
            }
    }

    [Test]
    public void MysteryMagic2HasAClearSpotFromEveryLanding()
    {
        foreach (var ice in new[] { 0, 1 })
            foreach (var offset in new[] { 0, 1 })
                foreach (var flipped in new[] { false, true })
                {
                    var state = State(ice2: ice, thunderOffset: offset, thunderFlipped: flipped);
                    var safe = UmadP1GravenImage1Ai.MysteryMagic2SafePoints(state);
                    Assert.That(safe, Is.Not.Empty, $"ice {ice} thunder {offset}/{flipped}");
                    var trap = UmadP1Roles.All.ToDictionary(r => r, r => UmadP1GravenImage1Ai.TrapSpot(state, r));
                    foreach (var role in UmadP1Roles.All)
                    {
                        var holder = UmadP1Roles.IsDps(role) ? state.TrapDps : state.TrapSupport;
                        var from = role == holder ? trap[role] : trap[role] + Vector2.Normalize(trap[role] - trap[holder]) * 14f;
                        var spot = UmadP1GravenImage1Ai.MysteryMagic2Dodge(safe, from, role);
                        Assert.That(UmadP1GravenImage1Ai.MysteryMagic2Clearance(state, spot), Is.GreaterThan(Jitter + 0.2f),
                            $"{role} from {from} to {spot}, ice {ice} thunder {offset}/{flipped}");
                    }
                }
    }

    private static IEnumerable<PartyRole[]> Pairs(PartyRole[] roles)
    {
        for (var i = 0; i < roles.Length; i++)
            for (var j = i + 1; j < roles.Length; j++)
                yield return [roles[i], roles[j]];
    }
}
