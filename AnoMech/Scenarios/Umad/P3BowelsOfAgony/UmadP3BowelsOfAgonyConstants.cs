using System;
using System.Numerics;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

public static class UmadP3BowelsOfAgonyConstants
{
    // Header.AnimationLock of each real ActionEffect, as Limit Cut's.
    public static class AnimationLock
    {
        public const float BossCast = 3.1f;
        public const float Instant = 1.1f;
        public const float AutoAttack = 0.1f;
    }

    public static class Geometry
    {
        public const float ArenaRadius = 20f;
        // 11 from the Action sheet plus Exdeath's 3.8 hitbox. UNVERIFIED: nobody stood inside it in a log.
        public const float ThunderAoeRadius = 14.8f;
        // The hole is somewhere from 3.2y (unhit) to 4.4y (hit). UNVERIFIED in between.
        public const float StraySprayInnerRadius = 4f;
        // UNVERIFIED: no logged Inferno hit anyone.
        public const float InfernoInnerRadius = 4f;
        public const float ShockwaveHalfAngle = MathF.PI / 4f;

        public static float SpotHeading(int spot) => spot * (MathF.PI / 4f);
        public static Vector3 OnCircle(float heading, float radius) => new(MathF.Sin(heading) * radius, 0f, MathF.Cos(heading) * radius);
        public const float CrystalRadius = 14f;
        public static Vector3 Crystal(int spot) => OnCircle(SpotHeading(spot), CrystalRadius);
        public static float CrystalRotation(int spot) => SpotHeading(spot) + MathF.PI;
        public static string SpotName(int spot) => SpotNames[((spot % 8) + 8) % 8];
        private static readonly string[] SpotNames = ["S", "SE", "E", "NE", "N", "NW", "W", "SW"];

        // The bosses arrive either side of the centre; the tanks cross over to pull both onto it.
        public static readonly Vector3 ChaosArrival = new(-8f, 0f, 0f);
        public static readonly Vector3 ExdeathArrival = new(8f, 0f, 0f);
        public static readonly Vector3 KefkaPerch = new(0f, 0f, -10f);
        public static readonly Vector3 GravenImage = new(0f, 10.7f, -47.5f);
    }

    public static class Damage
    {
        public const float MagicVulnerabilityUpSeconds = 3.96f;
        public const float DamageDownSeconds = 180f;
        public const float LightningResistanceDownSeconds = 3.96f;
        public const float ThunderIIIRequiredMitigation = 0.60f;
        public const float WindResistanceDownSeconds = 0.96f;
        public const float CycloneVulnMitigation = 0.80f;
    }

    // Limit Cut's track offset (116.67s before Umbra Smash), and Umbra Smash is 61.52s after the
    // Bowels of Agony cast, which starts at 25.0 here.
    public const float BgmSecondsAtStart = 116.67f - 61.52f - 25.0f;

    // Each debuff lasts from Bowels of Agony's hit at 29.98.
    public const float WindSeconds = 68f;
    public const float ShortElementSeconds = 19f;
    public const float LongElementSeconds = 46f;

    // A crystal's knockback, by Vacuum Wave's logged rules: the facing that cleanses is a short
    // push, the wrong one throws you off the platform. UNVERIFIED for the crystals, whose pushes no
    // log caught.
    public const float CorrectFacingCos = 0.7071f;
    public const float CleansePush = 10f;
    public const float WrongFacingPush = 40f;
    public const float PlainPush = 20f;
    public const float KnockbackSpeed = 40f;
    // UNVERIFIED: Limit Cut's gap from Vacuum Wave's hit to its Cyclones.
    public const float CycloneDelay = 3.87f;

    public const uint DirectorCategory = 0x80000027U;
    public const uint DirectorArg3 = 0x1BDBU;
    public const uint DirectorKefkaId = 0x4000EFB9U;
}
