using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// The P3 mitigation sheet's rows from Earthquake to Stomp-a-Mole on this scenario's clock. Shocking
// Impact and the next set are under 30s apart, so Sacred Soil and Kerachole go out early for the
// first and late for the second. No damage is known yet, so HP doesn't move.
internal static class UmadP3BlackHoleMitigation
{
    public const string Earthquake = "earthquake";
    public const string ShockingImpact = "shocking_impact";
    public const string Nothingness = "nothingness";
    public const string ThunderIII = "thunder_iii";
    public const string KnockDown = "knock_down";
    public const string StompAMole = "stomp_a_mole";

    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Earthquake, "Earthquake", null, UmadMitigation.Chaos),
        new(ShockingImpact, "Shocking Impact", null, UmadMitigation.Kefka),
        new(Nothingness, "Nothingness", null, null),
        new(ThunderIII, "Thunder III", null, UmadMitigation.Exdeath, TankBuster: true),
        new(KnockDown, "Knock Down", null, UmadMitigation.Chaos),
        new(StompAMole, "Stomp-a-Mole", null, UmadMitigation.Kefka),
    ];

    private const float EarthquakeHit = 5.98f;
    private const float Shocking1 = 24.81f;
    private const float Tether1 = 32.27f;
    private const float Tether2 = 39.33f;
    private const float Thunder1 = 42.63f;
    private const float Thunder2 = 45.63f;
    private const float Shocking2 = 55.26f;
    private const float Tether3 = 62.83f;
    private const float Tether4 = 67.93f;
    private const float Tether5 = 73.03f;
    private const float Thunder3 = 83.94f;
    private const float Thunder4 = 86.94f;
    private const float Tether6 = 97.12f;
    private const float Tether7 = 102.22f;
    private const float Tether8 = 107.32f;
    private const float Shocking3 = 123.25f;
    private const float Tether9 = 130.60f;
    private const float Tether10 = 137.67f;
    private const float KnockDown1 = 152.13f;
    private const float Stomp1 = 152.21f;
    private const float Stomp2 = 153.50f;
    private const float Stomp3 = 154.79f;
    private const float Stomp4 = 156.13f;
    private const float KnockDown2 = 157.67f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(EarthquakeHit, [Earthquake]), new(Shocking1, [ShockingImpact]), new(Tether1, [Nothingness]), new(Tether2, [Nothingness]),
            new(Thunder1, [ThunderIII]), new(Thunder2, [ThunderIII]), new(Shocking2, [ShockingImpact]),
            new(Tether3, [Nothingness]), new(Tether4, [Nothingness]), new(Tether5, [Nothingness]),
            new(Thunder3, [ThunderIII]), new(Thunder4, [ThunderIII]),
            new(Tether6, [Nothingness]), new(Tether7, [Nothingness]), new(Tether8, [Nothingness]),
            new(Shocking3, [ShockingImpact]), new(Tether9, [Nothingness]), new(Tether10, [Nothingness]),
            new(KnockDown1, [KnockDown]), new(Stomp1, [StompAMole]), new(Stomp2, [StompAMole]), new(Stomp3, [StompAMole]),
            new(Stomp4, [StompAMole]), new(KnockDown2, [KnockDown]),
        ],
        Rows, UmadMitigation.Chaos);

    private static readonly MitPlanEntry[] Rows =
    [
        new(MT, PartyMit, 4.48f, "Earthquake", [EarthquakeHit, Shocking1], [Gunbreaker, DarkKnight]),
        new(AST, Macrocosmos, 4.48f, "Earthquake", []),
        new(SCH, SacredSoil, 4.48f, "Earthquake", [EarthquakeHit, Shocking1]),
        new(SGE, Kerachole, 4.48f, "Earthquake", [EarthquakeHit, Shocking1]),
        new(D1, Feint, 4.48f, "Earthquake", [EarthquakeHit]),
        new(SGE, Philosophia, 16.60f, "Earthquake (once both Accretions have gone off)", []),

        new(MT, MitActionId.Reprisal, 22.70f, "Shocking Impact 1 (early, so it's back for Thunder III)", [Shocking1]),
        new(MT, PartyMit, 23.31f, "Shocking Impact 1", [Shocking1], [Warrior, Paladin]),
        new(WHM, PlenaryIndulgence, 23.31f, "Shocking Impact 1", [Shocking1]),
        new(AST, CollectiveUnconscious, 23.31f, "Shocking Impact 1", [Shocking1]),

        new(OT, PartyMit, 53.76f, "Shocking Impact 2", [Shocking2, Tether3, Tether4], [Gunbreaker, DarkKnight]),
        new(WHM, LiturgyOfTheBell, 53.76f, "Shocking Impact 2", []),
        new(SCH, Expedient, 53.76f, "Shocking Impact 2", [Shocking2, Tether3, Tether4, Tether5]),
        new(SCH, SummonSeraph, 53.76f, "Shocking Impact 2", [Shocking2]),
        new(SCH, SacredSoil, 53.26f, "Shocking Impact 2 (early, so it's back for Thunder III)", [Shocking2, Tether3, Tether4]),
        new(SGE, Holos, 53.76f, "Shocking Impact 2", [Shocking2, Tether3, Tether4, Tether5]),
        new(SGE, Kerachole, 53.26f, "Shocking Impact 2 (early, so it's back for Thunder III)", [Shocking2, Tether3]),
        new(Extras, Dismantle, 53.76f, "Shocking Impact 2", [Shocking2], [Machinist]),
        new(Extras, MagickBarrier, 53.76f, "Shocking Impact 2", [Shocking2], [RedMage]),
        new(Extras, TemperaCoat, 53.06f, "Shocking Impact 2 (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 53.76f, "Shocking Impact 2", [Shocking2], [Pictomancer]),

        new(OT, PartyMit, 61.33f, "Black Holes II (3rd tether set)", [Tether3, Tether4], [Warrior, Paladin]),
        new(SCH, Consolation, 61.33f, "Black Holes II (3rd tether set)", [Tether3]),
        new(SCH, FeyIllumination, 71.53f, "Black Holes II (5th tether set)", [Tether5, Thunder3]),

        new(MT, MitActionId.Reprisal, 82.74f, "Thunder III (5th set)", [Thunder3]),
        new(WHM, Temperance, 82.44f, "Thunder III (5th set)", [Thunder3, Tether6]),
        new(AST, NeutralSect, 82.44f, "Thunder III (5th set)", []),
        new(SCH, DeploymentTactics, 82.44f, "Thunder III (5th set)", [Thunder3]),
        new(SCH, SacredSoil, 83.30f, "Thunder III (5th set)", [Thunder3]),
        new(SGE, Zoe, 82.14f, "Thunder III (5th set)", []),
        new(SGE, EukrasianPrognosisII, 82.44f, "Thunder III (5th set, Zoe shields)", [Thunder3]),
        new(SGE, Panhaima, 82.44f, "Thunder III (5th set)", [Thunder3, Tether6]),
        new(SGE, Kerachole, 83.30f, "Thunder III (5th set)", [Thunder3]),
        new(D2, Feint, 82.44f, "Thunder III (5th set)", [Thunder3]),
        new(D4, Addle, 82.44f, "Thunder III (5th set)", [Thunder3], On: UmadMitigation.Exdeath),

        new(WHM, DivineCaress, 95.62f, "Black Holes III (6th tether set)", [Tether6]),
        new(AST, SunSign, 95.62f, "Black Holes III (6th tether set)", [Tether6, Tether7, Tether8]),
        new(D3, PartyMit, 95.62f, "Black Holes III (6th tether set)", [Tether6, Tether7, Tether8]),

        new(WHM, PlenaryIndulgence, 121.75f, "Shocking Impact 3", [Shocking3]),
        new(AST, CollectiveUnconscious, 121.75f, "Shocking Impact 3", [Shocking3]),
        new(SCH, SacredSoil, 121.25f, "Shocking Impact 3 (early, so it's back for Stomp-a-Mole)", [Shocking3]),
        new(SGE, Kerachole, 121.25f, "Shocking Impact 3 (early, so it's back for Stomp-a-Mole)", [Shocking3]),

        new(MT, MitActionId.Reprisal, 150.63f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(MT, PartyMit, 150.63f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(OT, MitActionId.Reprisal, 150.63f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(SCH, Seraphism, 150.63f, "Stomp-a-Mole + Knock Down", []),
        new(SCH, SacredSoil, 151.30f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(SGE, Kerachole, 151.30f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
        new(D1, Feint, 150.63f, "Stomp-a-Mole + Knock Down", [KnockDown1, KnockDown2]),
    ];
}
