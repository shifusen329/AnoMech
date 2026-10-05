using System.Numerics;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;
using AnoMech.Scenarios.Umad.P1Shared;
using AnoMech.Scenarios.Umad.P2Forsaken.Ai;

namespace AnoMech.Tests;

public class UmadP2ForsakenEndBaitTests
{
    private const float KefkaHitbox = 6f;
    private const float TowerRadius = 4f;
    // Melee reach runs hitbox edge to hitbox edge, so a player's centre may sit this much past MaxMelee.
    private const float PlayerHitbox = 0.5f;

    private static Dictionary<PartyRole, Vector2> Spots(bool betweenTowers, Direction north)
    {
        var move = UmadP2ForsakenEndBait.Move(betweenTowers, north);
        return UmadP1Roles.All.ToDictionary(r => r, r => move[(int)r]!.Value);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void EveryRoleHasItsOwnSpotAtMaxMeleeOutsideTheHitbox(bool betweenTowers)
    {
        var spots = Spots(betweenTowers, Direction.N);
        foreach (var (role, spot) in spots)
        {
            Assert.That(spot.Length(), Is.LessThanOrEqualTo(UmadP2ForsakenEndBait.MaxMelee + PlayerHitbox), $"{role}");
            Assert.That(spot.Length(), Is.GreaterThan(KefkaHitbox), $"{role}");
            Assert.That(spot.Y > 0f, Is.EqualTo(!betweenTowers), $"{role} on the wrong side");
            foreach (var (other, otherSpot) in spots)
                if (other != role)
                    Assert.That(Vector2.Distance(spot, otherSpot), Is.GreaterThanOrEqualTo(UmadP2ForsakenEndBait.Spacing - 0.01f), $"{role} and {other}");
        }
    }

    [Test]
    public void BuddiesStandSideBySide()
    {
        var spots = Spots(true, Direction.N);
        foreach (var (a, b) in new[] { (PartyRole.MainTank, PartyRole.RegenHealer), (PartyRole.OffTank, PartyRole.ShieldHealer),
                     (PartyRole.MeleeDpsA, PartyRole.PhysRangedDps), (PartyRole.MeleeDpsB, PartyRole.CasterDps) })
            Assert.That(Vector2.Distance(spots[a], spots[b]), Is.EqualTo(UmadP2ForsakenEndBait.Spacing).Within(0.01f), $"{a} and {b}");
    }

    // The scenario puts each tower 8y out, 45 degrees either side of the set's north.
    [TestCase(0)]
    [TestCase(3)]
    public void PastsEndStaysOutOfTheNextTowers(int octant)
    {
        var north = Direction.N.Rotate(octant);
        var towers = new[] { north.Rotate(-1), north.Rotate(1) }
            .Select(d => d.Apply(new Vector3(0f, 0f, -8f)))
            .Select(p => new Vector2(p.X, p.Z));
        foreach (var (role, spot) in Spots(true, north))
            foreach (var tower in towers)
                Assert.That(Vector2.Distance(spot, tower), Is.GreaterThan(TowerRadius), $"{role}");
    }
}
