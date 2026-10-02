using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Bot spots shared by the Graven Image strats (kefkasim's NAUR / wtfdig strat, scenario-local XZ:
// north is -Z, the boss at the origin).
public static class UmadP1Spots
{
    // Uptime: the aggro tank north of the boss so Revolting Ruin III points away; everyone else
    // behind him on an arc 6y out, Supports southwest and DPS southeast.
    public static readonly Vector2[] Uptime =
    [
        new(0f, -6.5f), new(-1.25f, 5.87f),
        new(-2.44f, 5.48f), new(-3.53f, 4.85f),
        new(1.25f, 5.87f), new(2.44f, 5.48f),
        new(3.53f, 4.85f), new(4.46f, 4.01f),
    ];

    // A small per-slot spread inside a stack.
    public static Vector2 Offset(PartyRole role, float radius)
    {
        var angle = (int)role * MathF.Tau / 8f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    // Grid points (1y apart, within `radius` of the centre) clearing every hit by `margin`.
    public static List<Vector2> SafePoints(Func<Vector2, float> clearance, float margin, float radius = 17.5f)
    {
        var points = new List<Vector2>();
        for (var x = -18; x <= 18; x++)
            for (var z = -18; z <= 18; z++)
            {
                var p = new Vector2(x, z);
                if (p.Length() <= radius && clearance(p) >= margin) points.Add(p);
            }
        return points;
    }

    public static Vector2 Nearest(IReadOnlyList<Vector2> points, Vector2 from)
    {
        var best = points.Count > 0 ? points[0] : from;
        foreach (var p in points)
            if (Vector2.DistanceSquared(p, from) < Vector2.DistanceSquared(best, from)) best = p;
        return best;
    }

    // Double-trouble Trap stacks: each role on the boss's hitbox ring with the holder at max melee
    // behind it, so the knockback throws the stack through the boss. `direction` is the role's
    // side; `along` spaces the stack across (first trap) or along (second trap) that line.
    public static Vector2 TrapStackSpot(Vector2 direction, float stackRadius, Vector2 along, float spacing, int k)
        => direction * stackRadius + along * spacing * k;
}
