using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// The P3 mitigation sheet's rows from Earthquake to Stomp-a-Mole on this scenario's clock. The
// raidwide in Earthquake and in every tether set is the Earthquake a popped Accretion or broken crust
// sets off; the Spreadlo and Zoe shields from The Decisive Battle, before the scenario, take the first
// two. Shocking Impact and the next set are under 30s apart, so Sacred Soil and Kerachole go out early
// for the first and late for the second. Helpers deal every hit here, so the debuffs on Chaos and
// Exdeath only soften the bosses' autos.
internal static class UmadP3BlackHoleMitigation
{
    public const string Earthquake = "earthquake";
    public const string ShockingImpact = "shocking_impact";
    public const string Shockwave = "shockwave";
    public const string ThunderIII = "thunder_iii";
    public const string KnockDown = "knock_down";
    public const string StompAMole = "stomp_a_mole";

    // Raw on a physical DPS, per player hit: Shocking Impact on the full stack, Shockwave on each
    // half, Knock Down on a four, Stomp-a-Mole on a pair. Thunder III is what the tank takes before
    // cooldowns.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Earthquake, "Earthquake", 80000f, null),
        new(ShockingImpact, "Shocking Impact", 188000f, null),
        new(Shockwave, "Shockwave", 220000f, null),
        new(ThunderIII, "Thunder III", 566000f, null, TankBuster: true),
        new(KnockDown, "Knock Down", 190000f, null),
        new(StompAMole, "Stomp-a-Mole", 205000f, null),
    ];

    private const float Quake1 = 12.38f;
    private const float Quake2 = 16.30f;
    private const float Shocking = 24.81f;
    private const float Shockwave1 = 55.26f;
    private const float Quake3 = 64.39f;
    private const float Quake4 = 69.49f;
    private const float Quake5 = 74.59f;
    private const float Thunder3 = 83.94f;
    private const float Quake6 = 98.68f;
    private const float Quake7 = 103.78f;
    private const float Quake8 = 108.88f;
    private const float Shockwave2 = 123.25f;
    private const float KnockDown1 = 152.13f;
    private const float KnockDown2 = 157.67f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(Quake1, [Earthquake]), new(Quake2, [Earthquake]), new(Shocking, [ShockingImpact]),
            new(42.63f, [ThunderIII]), new(45.63f, [ThunderIII]), new(Shockwave1, [Shockwave]),
            new(Quake3, [Earthquake]), new(Quake4, [Earthquake]), new(Quake5, [Earthquake]),
            new(Thunder3, [ThunderIII]), new(86.94f, [ThunderIII]),
            new(Quake6, [Earthquake]), new(Quake7, [Earthquake]), new(Quake8, [Earthquake]),
            new(Shockwave2, [Shockwave]), new(132.16f, [Earthquake]), new(139.23f, [Earthquake]),
            new(KnockDown1, [KnockDown]), new(152.21f, [StompAMole]), new(153.50f, [StompAMole]), new(154.79f, [StompAMole]),
            new(156.13f, [StompAMole]), new(KnockDown2, [KnockDown]),
        ],
        Rows, UmadMitigation.Chaos);

    private static readonly MitPlanEntry[] Rows =
    [
        new(SCH, DeploymentTactics, -9.73f, "The Decisive Battle (before the scenario: barriers for the Accretions)", [Quake1]),
        new(SGE, Zoe, -10.03f, "The Decisive Battle (before the scenario)", []),
        new(SGE, EukrasianPrognosisII, -9.73f, "The Decisive Battle (before the scenario: barriers for the Accretions)", [Quake1]),

        new(MT, PartyMit, 4.48f, "Earthquake", [Quake1, Quake2, Shocking], [Gunbreaker, DarkKnight]),
        new(AST, Macrocosmos, 4.48f, "Earthquake", []),
        new(SCH, SacredSoil, 4.48f, "Earthquake", [Quake1, Quake2, Shocking]),
        new(SGE, Kerachole, 4.48f, "Earthquake", [Quake1, Quake2, Shocking]),
        new(D1, Feint, 4.48f, "Earthquake (Chaos's autos)", []),
        new(SGE, Philosophia, 16.60f, "Earthquake (once both Accretions have gone off)", []),

        new(MT, MitActionId.Reprisal, 22.70f, "Shocking Impact 1 (Chaos's autos)", []),
        new(MT, PartyMit, 23.31f, "Shocking Impact 1", [Shocking], [Warrior, Paladin]),
        new(WHM, PlenaryIndulgence, 23.31f, "Shocking Impact 1", [Shocking]),
        new(AST, CollectiveUnconscious, 23.31f, "Shocking Impact 1", [Shocking]),

        new(OT, PartyMit, 53.76f, "Shocking Impact 2", [Shockwave1, Quake3, Quake4], [Gunbreaker, DarkKnight]),
        new(WHM, LiturgyOfTheBell, 53.76f, "Shocking Impact 2", []),
        new(SCH, Expedient, 53.76f, "Shocking Impact 2", [Shockwave1, Quake3, Quake4, Quake5]),
        new(SCH, SummonSeraph, 53.76f, "Shocking Impact 2", [Shockwave1]),
        new(SCH, SacredSoil, 53.26f, "Shocking Impact 2 (early, so it's back for Thunder III)", [Shockwave1, Quake3, Quake4]),
        new(SGE, Holos, 53.76f, "Shocking Impact 2", [Shockwave1, Quake3, Quake4, Quake5]),
        new(SGE, Kerachole, 53.26f, "Shocking Impact 2 (early, so it's back for Thunder III)", [Shockwave1, Quake3]),
        new(Extras, Dismantle, 53.76f, "Shocking Impact 2 (Chaos's autos)", [], [Machinist]),
        new(Extras, MagickBarrier, 53.76f, "Shocking Impact 2", [Shockwave1], [RedMage]),
        new(Extras, TemperaCoat, 53.06f, "Shocking Impact 2 (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 53.76f, "Shocking Impact 2", [Shockwave1], [Pictomancer]),

        new(OT, PartyMit, 61.33f, "Black Holes II (3rd tether set)", [Quake3, Quake4], [Warrior, Paladin]),
        new(SCH, Consolation, 61.33f, "Black Holes II (3rd tether set)", [Quake3]),
        new(SCH, FeyIllumination, 71.53f, "Black Holes II (5th tether set)", [Quake5, Thunder3]),

        new(MT, MitActionId.Reprisal, 82.74f, "Thunder III (5th set; Chaos's autos)", []),
        new(WHM, Temperance, 82.44f, "Thunder III (5th set)", [Thunder3, Quake6]),
        new(AST, NeutralSect, 82.44f, "Thunder III (5th set)", []),
        new(SCH, DeploymentTactics, 82.44f, "Thunder III (5th set)", [Thunder3]),
        new(SCH, SacredSoil, 83.30f, "Thunder III (5th set)", [Thunder3]),
        new(SGE, Zoe, 82.14f, "Thunder III (5th set)", []),
        new(SGE, EukrasianPrognosisII, 82.44f, "Thunder III (5th set, Zoe shields)", [Thunder3]),
        new(SGE, Panhaima, 82.44f, "Thunder III (5th set)", [Thunder3, Quake6]),
        new(SGE, Kerachole, 83.30f, "Thunder III (5th set)", [Thunder3]),
        new(D2, Feint, 82.44f, "Thunder III (5th set; Chaos's autos)", []),
        new(D4, Addle, 82.44f, "Thunder III (5th set; Exdeath's autos)", [], On: UmadMitigation.Exdeath),

        new(WHM, DivineCaress, 95.62f, "Black Holes III (6th tether set)", [Quake6]),
        new(AST, SunSign, 95.62f, "Black Holes III (6th tether set)", [Quake6, Quake7, Quake8]),
        new(D3, PartyMit, 95.62f, "Black Holes III (6th tether set)", [Quake6, Quake7, Quake8]),

        new(WHM, PlenaryIndulgence, 121.75f, "Shocking Impact 3", [Shockwave2]),
        new(AST, CollectiveUnconscious, 121.75f, "Shocking Impact 3", [Shockwave2]),
        new(SCH, SacredSoil, 121.25f, "Shocking Impact 3 (early, so it's back for Stomp-a-Mole)", [Shockwave2]),
        new(SGE, Kerachole, 121.25f, "Shocking Impact 3 (early, so it's back for Stomp-a-Mole)", [Shockwave2]),

        new(MT, MitActionId.Reprisal, 150.63f, "Stomp-a-Mole + Knock Down (Chaos's autos)", []),
        new(MT, PartyMit, 150.63f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(OT, MitActionId.Reprisal, 150.63f, "Stomp-a-Mole + Knock Down (the bosses' autos)", []),
        new(SCH, Seraphism, 150.63f, "Stomp-a-Mole + Knock Down", []),
        new(SCH, SacredSoil, 151.30f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(SGE, Kerachole, 151.30f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(D1, Feint, 150.63f, "Stomp-a-Mole + Knock Down (Chaos's autos)", []),
    ];
}
