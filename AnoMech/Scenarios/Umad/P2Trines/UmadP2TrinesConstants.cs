using System;
using System.Numerics;

namespace AnoMech.Scenarios.Umad.P2Trines;

public static class UmadP2TrinesConstants
{
    // Header.AnimationLock of each real ActionEffect, as Bowels of Agony's.
    public static class AnimationLock
    {
        public const float BossCast = 3.1f;
        public const float Instant = 1.1f;
        public const float AutoAttack = 0.1f;
    }

    public static class Geometry
    {
        public const float ArenaRadius = 20f;
        // Kefka holds the middle facing north until the busters.
        public const float KefkaFacingNorth = MathF.PI;

        // Seven equilateral triangles of side 10, the same every pull: the centre (0) and six
        // 15.3y out, numbered clockwise from north.
        public const int Centre = 0;
        public static readonly Vector3[] Centroids =
        [
            Vector3.Zero,
            new(2.887f, 0f, -15f), new(14.434f, 0f, -5f), new(11.547f, 0f, 10f),
            new(-2.887f, 0f, 15f), new(-14.434f, 0f, 5f), new(-11.547f, 0f, -10f),
        ];
        public static string Name(int triangle) => Names[triangle];
        private static readonly string[] Names = ["O", "A", "B", "C", "D", "E", "F"];

        // The odd ones point east, the even ones west; the centre is rolled.
        public static bool OuterPointsEast(int triangle) => triangle % 2 == 1;

        // The corner circles reach 6y; a triangle's own centroid is 5.8y from its corners.
        public static Vector3[] Corners(int triangle, bool pointsEast)
        {
            var c = Centroids[triangle];
            var s = pointsEast ? 1f : -1f;
            return [c + new Vector3(s * 5.7735f, 0f, 0f), c + new Vector3(-s * 2.8868f, 0f, 5f), c + new Vector3(-s * 2.8868f, 0f, -5f)];
        }

        // Wings of Destruction's rect starts this far to the struck side and behind Kefka and runs
        // 80 forward along his facing, covering exactly that half of the arena.
        public const float WingsSideOffset = 20f;
        public const float WingsBehindOffset = 40f;
    }

    // The P2 track starts on the 0x4F44 music beat as Kefka arrives; Light of Judgment II is cast
    // 136.6s into it, 2s after this scenario starts.
    public const float BgmSecondsAtStart = 134.60f;

    public static class Damage
    {
        public const float MagicVulnerabilityUpSeconds = 0.96f;
        public const float DamageDownSeconds = 180f;
        // UNVERIFIED: a non-tank took 0.66-1.1M from one buster, against a ~325k tank.
        public const float WingsBusterRequiredMitigation = 0.55f;
        // UNVERIFIED: shared, each tank took 330-430k; alone, 1.7M, which only an invuln lives through.
        public const float UltimateEmbraceRequiredMitigation = 0.30f;
        public const float UltimateEmbraceSoloTankMitigation = 0.80f;
    }

    // EObjAnimation beats on each triangle prop.
    public static readonly (uint State, uint Bitmask) TriangleFliesDown = (0x10, 0x20);
    public static readonly (uint State, uint Bitmask) TriangleLands = (0x40, 0x80);
    public static readonly (uint State, uint Bitmask) TriangleExplodes = (0x4, 0x8);

    // ActorControl 0x3E on Kefka lights his wings: the right one, the left, both for the busters,
    // then off. UNVERIFIED what each param draws.
    public const uint WingGlowCategory = 0x3E;
    public const uint WingGlowRight = 1;
    public const uint WingGlowLeft = 2;
    public const uint WingGlowBoth = 3;
    public const uint WingGlowOff = 0;

    public const uint MapEffectFlags = 0x00080004U;
    public const byte MapEffectIndex = 0x13;
}
