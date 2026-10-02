namespace AnoMech.Scenarios.Umad.P1Shared;

// Ids for the Graven Image 1 / 2 scenarios. The ones Tele-trouncing also uses are repeated here
// so the two can change independently; SimAssets only needs each declared somewhere.
public static class UmadP1GravenConstants
{
    public static class ActionId
    {
        public const uint GravenImage = 0xBCF2U;
        // Kefka's unnamed instant ~1.5s before each Graven Image cast (BossMod's TeleportP1).
        public const uint TeleportP1 = 0xC3FDU;
        public const uint MysteryMagic = 0xBA94U;

        // Graven Image 1. Pulse Wave: statue -> each tethered player, knockback row 72.
        // Wave Cannon: rect 100 x 6 from the statue toward each target, 3.96s Magic Vulnerability Up.
        // Explosion: 4y tower cast on each Wave Cannon target's spot; unsoaked = Unmitigated
        // Explosion (100y).
        public const uint PulseWave = 0xBAA9U;
        public const uint WaveCannon = 0xBAA8U;
        public const uint Explosion = 0xBAAAU;
        public const uint UnmitigatedExplosion = 0xBAABU;

        // Graven Image 2. Gravitas (5y, from the central statue) leaves a puddle; Vitrophyre (5y,
        // from the right-side statue) knocks other players away and sets off any puddle it
        // touches (Gravitational Explosion, 100y); Gravity III is a popped puddle's soak.
        public const uint Gravitas = 0xBAACU;
        public const uint GravitationalExplosion = 0xBAADU;
        public const uint GravityIII = 0xBAAFU;
        public const uint Vitrophyre = 0xBAB0U;
        // The half-room cleaves: a statue at the centre facing west (Gravitational Wave) or east
        // (Intemperate Will). Cone type 13, range 100, no XAxisModifier: resolved as a half plane.
        public const uint GravitationalWave = 0xBAB1U;
        public const uint IntemperateWill = 0xBAB2U;

        public const uint DoubleTroubleTrap = 0xBAA6U;
        public const uint DoubleTroubleTrapStack = 0xBAA7U;
        public const uint FlagrantFireSpread = 0xBAA2U;
        public const uint FlagrantFireStack = 0xBAA3U;
        public const uint ThrummingThunderReal1 = 0xBA9FU;
        public const uint ThrummingThunderFake = 0xBAA0U;
        public const uint ThrummingThunderReal2 = 0xBAA1U;

        public const uint LightOfJudgment = 0xC622U;
        public const uint Hyperdrive = 0xC24BU;
        public const uint RevoltingRuinIII = 0xC403U;
        public const uint RevoltingRuinIIIFollowUp = 0xC4E1U;
    }

    public static class StatusId
    {
        public const ushort DoubleTroubleTrap = 0x13D6;
    }

    // Kefka's orbs (BossMod's IconID values) and the fire marker on players.
    public static class LockonId
    {
        public const uint FireSpread = 127;
        public const uint FireStack = 128;
        public const uint FireLie = 673;
        public const uint FireTruth = 674;
        public const uint ColdLie = 675;
        public const uint ColdTruth = 676;
        public const uint LightningLie = 677;
        public const uint LightningTruth = 678;
    }

    public static class KnockbackId
    {
        // Distance 13, speed 25.
        public const uint PulseWave = 72;
        // Distance 14, speed 20.
        public const uint DoubleTroubleTrapStack = 240;
    }

    public static class TetherId
    {
        public const ushort GravenImage = 0x2D;
    }

    public static class BNpcBaseId
    {
        public const uint Kefka = 19504;
        public const uint GravenImage = 19505;
    }

    public static class BNpcNameId
    {
        public const uint Kefka = 7131;
        public const uint GravenImage = 7132;
    }

    // The statue props are EObjs whose SharedGroup timelines are the telegraphs. Their layout ids
    // are unknown, so these spawn unbound to a layout instance (Tele-trouncing's own resolve to
    // nothing on the client anyway). UNVERIFIED that they render this way.
    public static class EObjId
    {
        // The colossus at the arena centre (shared with Tele-trouncing).
        public const uint GravenStatue = 0x1EBFB4U;
        public const uint GravenStatueLayoutId = 0xBC7C91U;
        public const uint GravenStatueArg2 = 0x00400003U;

        // Graven Image 1's statue (world (100, 18.5, 56)) and Wave Cannon spot (100, 0, 65).
        public const uint StatueFront = 0x1EBFBBU;
        // Graven Image 2's central (purple) statue and the west cleave telegraph.
        public const uint StatueCentral = 0x1EBFBCU;
        // Graven Image 2's right-side (yellow) statue and the east cleave telegraph.
        public const uint StatueRight = 0x1EBFBDU;

        public const uint PropEventId = 0x800375D2U;

        // The real spawns' Arg2: 3 in the low byte, a per-object serial in the high word,
        // counting down from the gaze props' 0x4A to the colossus's 0x40 by actor id.
        public const uint StatueRightArg2 = 0x00460003U;
        public const uint StatueCentralArg2 = 0x00450003U;
        public const uint EastTelegraphArg2 = 0x00440003U;
        public const uint WestTelegraphArg2 = 0x00430003U;
        public const uint StatueFrontArg2 = 0x00420003U;
        public const uint WaveCannonSpotArg2 = 0x00410003U;

        public const ushort SpawnState = 0x0004;
    }

    public static class VfxPath
    {
        // A stand-in: the real Gravitas puddle's visual is unknown.
        public const string PuddleOmen = "general_1bf";
        public const string DoubleTroubleTrapStackHit = "vfx/monster/gimmick6/eff/z3oy_b0_g02c0c.avfx";
    }

    // Director 0x80000001 switches the music: 20289 at 10.47s into the pull, 20290 at 59.97s,
    // 20291 at 127.75s (the track Tele-trouncing starts in).
    public static class Bgm
    {
        public const ushort Opening = 20289;
        public const ushort Middle = 20290;
        public const ushort Late = 20291;
    }

    // Map effect slot 0's resting state steps 0x1 -> 0x4 (59.97s) -> 0x10 (127.75s); the
    // packets carry (transition << 16) | rest. The pull-start 0x1 is UNVERIFIED, inferred from
    // the sequence.
    public static class ArenaState
    {
        public const uint Gi1Rest = 0x00010001U;
        public const uint Gi2Rest = 0x00040004U;
        public const uint ToMiddle = 0x00080004U;
        public const uint ToLate = 0x00200010U;
    }

    // Director 0x80000027 voice lines (param 1), as Tele-trouncing replays them.
    public static class KefkaLine
    {
        public const uint Category = 0x80000027U;
        public const uint Arg2 = 0x2U;
        public const uint NameId = 0x1BDBU;
        public const uint Actor = 0x400250DCU;
    }
}
