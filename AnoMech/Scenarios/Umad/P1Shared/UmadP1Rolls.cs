using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1TeleTrouncing;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Blizzard III Blowout: four 90-degree cones from the centre, one per quadrant. Slot i faces
// pi/4 + i*pi/2 (0 SE, 1 NE, 2 NW, 3 SW); (i + RealOffset) even is real. A truth telegraphs only
// the reals; a lie casts the reals untelegraphed and telegraphs the two safe quadrants instead.
public readonly record struct IceRoll(int RealOffset, bool IsLie)
{
    public static float Heading(int slot) => MathF.PI / 4f + slot * MathF.PI / 2f;

    public bool IsReal(int slot) => (slot + RealOffset) % 2 == 0;

    public static IceRoll Roll(Rng rng, int? realOffset, bool? isLie)
        => new(realOffset ?? rng.NextInt(2), isLie ?? rng.NextBool());

    // The quadrant a slot covers, as X/Z signs.
    public static Vector2 QuadrantSigns(int slot)
    {
        var heading = Heading(slot);
        return new Vector2(MathF.Sign(MathF.Round(MathF.Sin(heading), 3)), MathF.Sign(MathF.Round(MathF.Cos(heading), 3)));
    }

    // The safe quadrant on the west (x < 0) or east side, as X/Z signs.
    public Vector2 SafeQuadrant(bool west)
    {
        for (var i = 0; i < 4; i++)
        {
            if (IsReal(i)) continue;
            var q = QuadrantSigns(i);
            if (q.X < 0f == west) return q;
        }
        return new Vector2(west ? -1f : 1f, 1f);
    }

    public bool HitsNorthEast => IsReal(1);

    // Signed distance from p to the nearest real cone (< 0 = inside). The cones are exactly the
    // quadrants, so the distance is to the two half-axes bounding each one.
    public float Clearance(Vector2 p)
    {
        var min = float.PositiveInfinity;
        for (var i = 0; i < 4; i++)
        {
            if (!IsReal(i)) continue;
            var q = QuadrantSigns(i);
            var x = p.X * q.X;
            var z = p.Y * q.Y;
            float d;
            if (x >= 0f && z >= 0f) d = -MathF.Min(x, z);
            else if (x < 0f && z < 0f) d = MathF.Sqrt(x * x + z * z);
            else d = x < 0f ? -x : -z;
            min = MathF.Min(min, d);
        }
        return min;
    }
}

// Thrumming Thunder III on Tele-trouncing's lattice (UmadP1TeleTrouncingScenario.ThunderAnchors):
// 4 parallel rects 40 x 10, (i + RealOffset) even real, mirrored by Flipped.
public readonly record struct ThunderRoll(int RealOffset, bool Flipped, bool IsLie)
{
    public const float HalfWidth = 5f;
    public const float Length = 40f;

    public float Orientation => Flipped ? -1f : 1f;

    public bool IsReal(int slot) => (slot + RealOffset) % 2 == 0;

    public Placement Anchor(int slot)
        => UmadP1TeleTrouncingScenario.ThunderAnchors[slot].MulX(Orientation).MulRot(Orientation);

    public static ThunderRoll Roll(Rng rng, int? realOffset, bool? flipped, bool? isLie)
        => new(realOffset ?? rng.NextInt(2), flipped ?? rng.NextBool(), isLie ?? rng.NextBool());

    // Signed distance to the nearest real line edge (< 0 = inside).
    public float Clearance(Vector2 p)
    {
        var min = float.PositiveInfinity;
        for (var i = 0; i < 4; i++)
        {
            if (!IsReal(i)) continue;
            var anchor = Anchor(i);
            var rot = anchor.Rotation;
            var fwd = new Vector2(MathF.Sin(rot), MathF.Cos(rot));
            var right = new Vector2(MathF.Cos(rot), -MathF.Sin(rot));
            var rel = p - new Vector2(anchor.Position.X, anchor.Position.Z);
            var along = Vector2.Dot(rel, fwd);
            if (along < 0f || along > Length) continue;
            min = MathF.Min(min, MathF.Abs(Vector2.Dot(rel, right)) - HalfWidth);
        }
        return min;
    }
}

// Flagrant Fire III: spread (5y on everyone) or stack (6y on one Support and one DPS, 4 each).
// A lie shows the other icon.
public readonly record struct FireRoll(bool IsStack, bool IsLie, PartyRole StackSupport, PartyRole StackDps)
{
    public bool ShowsStackIcon => IsStack ^ IsLie;

    public static FireRoll Roll(Rng rng, bool? isStack, bool? isLie)
        => new(isStack ?? rng.NextBool(), isLie ?? rng.NextBool(), rng.NextSupportRole(), rng.NextDpsRole());
}

public static class UmadP1Roles
{
    public static readonly PartyRole[] Supports =
        [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer];
    public static readonly PartyRole[] Dps =
        [PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];
    public static readonly PartyRole[] All =
    [
        PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];

    public static bool IsDps(PartyRole role) => (int)role >= 4;

    public static PartyRole[] Of(bool dps) => dps ? Dps : Supports;

    // Light-party 1 within each role (T1/H1/M1/R1): the even slots.
    public static bool IsGroupOne(PartyRole role) => (int)role % 2 == 0;

    public static bool IsTankOrMelee(PartyRole role)
        => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;
}
