using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P2Forsaken.Ai;

// Where the party baits Future's/Past's End, the same in every strat: at max melee on the bisector
// of the next tower pair, between the towers for Past's End and opposite them for Future's End.
// Each role keeps its own spot rather than one shared point, so the markers the last towers just
// handed out stay readable on every head.
public static class UmadP2ForsakenEndBait
{
    // Kefka's hitbox (ModelChara radius 1.72 x BNpcBase scale 3.5 = 6.0y) + 3.
    public const float MaxMelee = 9f;

    // Wide enough that no two markers overlap, narrow enough that the front corners stay outside
    // the next towers' 4y circles on a Past's End bait.
    public const float Spacing = 1.4f;

    // Left to right facing Kefka: one column per buddy pair, Supports then DPS, the tank or melee
    // in front.
    private static readonly (PartyRole Front, PartyRole Back)[] Columns =
    [
        (PartyRole.MainTank, PartyRole.RegenHealer),
        (PartyRole.OffTank, PartyRole.ShieldHealer),
        (PartyRole.MeleeDpsA, PartyRole.PhysRangedDps),
        (PartyRole.MeleeDpsB, PartyRole.CasterDps),
    ];

    // `north` turns the authoring frame, where the next towers sit to the north (-Z), onto the set.
    public static IAiMove Move(bool betweenTowers, Direction north)
    {
        var outward = betweenTowers ? -1f : 1f;
        var spots = new Vector2?[8];
        for (var c = 0; c < Columns.Length; c++)
        {
            // Facing Kefka from the north your left is +X, from the south -X.
            var x = (1.5f - c) * Spacing * -outward;
            spots[(int)Columns[c].Front] = new Vector2(x, outward * (MaxMelee - Spacing));
            spots[(int)Columns[c].Back] = new Vector2(x, outward * MaxMelee);
        }
        return AiMove.Create(spots).NaturalOrder().ApplyPositions(north.Apply);
    }
}
