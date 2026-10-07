using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad.P4KefkaSays;

// The P4 mitigation sheet on this scenario's clock, up to the second Death Bolt/Wave; the second
// Ultima Upsurge comes after the enrage. Kefka himself deals only Ultima Upsurge, so it's the one hit
// the party's debuffs reduce. Flood of Naught deals no damage: its row carries Sacred Soil and
// Kerachole into Death Bolt/Wave, out early so they're back for Ultima Upsurge.
internal static class UmadP4KefkaSaysMitigation
{
    public const string GrandCross = "grand_cross";
    public const string InfernoTsunami = "inferno_tsunami";
    public const string DeathBolt = "death_bolt";
    public const string DeathWave = "death_wave";
    public const string UltimaUpsurge = "ultima_upsurge";

    // Raw on a physical DPS, per player hit: Death Bolt on a three, Death Wave on one.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(GrandCross, "Grand Cross", 296000f, null),
        new(InfernoTsunami, "Inferno/Tsunami", 230000f, null),
        new(DeathBolt, "Death Bolt", 172000f, null),
        new(DeathWave, "Death Wave", 225000f, null),
        new(UltimaUpsurge, "Ultima Upsurge", 215000f, UmadMitigation.Kefka),
    ];

    private const float GrandCross1 = 20.37f;
    private const float Inferno1 = 25.51f;
    private const float GrandCross2 = 35.30f;
    private const float Inferno2 = 40.43f;
    private const float GrandCross3 = 50.26f;
    private const float DeathBolt1 = 71.37f;
    private const float Upsurge = 89.60f;
    private const float DeathBolt2 = 96.48f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(GrandCross1, [GrandCross]), new(Inferno1, [InfernoTsunami]), new(GrandCross2, [GrandCross]),
            new(Inferno2, [InfernoTsunami]), new(GrandCross3, [GrandCross]),
            new(DeathBolt1, [DeathBolt, DeathWave]), new(Upsurge, [UltimaUpsurge]), new(DeathBolt2, [DeathBolt, DeathWave]),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(D2, Feint, 1.50f, "Kefka's autos (start of the phase)", []),

        new(WHM, PlenaryIndulgence, 18.87f, "Grand Cross 1", [GrandCross1, Inferno1]),
        new(AST, CollectiveUnconscious, 18.87f, "Grand Cross 1", [GrandCross1, Inferno1]),
        new(SCH, DeploymentTactics, 18.87f, "Grand Cross 1", [GrandCross1]),
        new(SCH, SacredSoil, 18.87f, "Grand Cross 1", [GrandCross1, Inferno1, GrandCross2]),
        new(SGE, Kerachole, 18.87f, "Grand Cross 1", [GrandCross1, Inferno1]),
        new(SGE, Philosophia, 18.87f, "Grand Cross 1", []),
        new(SGE, Holos, 18.87f, "Grand Cross 1", [GrandCross1, Inferno1, GrandCross2]),
        new(Extras, Dismantle, 18.87f, "Grand Cross 1 (Kefka's autos)", [], [Machinist]),
        new(Extras, MagickBarrier, 18.87f, "Grand Cross 1", [GrandCross1], [RedMage]),
        new(Extras, TemperaCoat, 18.17f, "Grand Cross 1 (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 18.87f, "Grand Cross 1", [GrandCross1], [Pictomancer]),

        new(D3, PartyMit, 24.01f, "Inferno/Tsunami 1", [Inferno1, GrandCross2]),

        new(WHM, Temperance, 33.80f, "Grand Cross 2", [GrandCross2, Inferno2, GrandCross3]),
        new(AST, NeutralSect, 33.80f, "Grand Cross 2", []),
        new(SCH, Expedient, 33.80f, "Grand Cross 2", [GrandCross2, Inferno2, GrandCross3]),
        new(SCH, FeyIllumination, 33.80f, "Grand Cross 2", [GrandCross2, Inferno2, GrandCross3]),
        new(SGE, Panhaima, 33.80f, "Grand Cross 2", [GrandCross2, Inferno2]),

        new(OT, PartyMit, 38.93f, "Inferno/Tsunami 2", [Inferno2, GrandCross3], [Gunbreaker, DarkKnight]),
        new(AST, SunSign, 38.93f, "Inferno/Tsunami 2", [Inferno2, GrandCross3]),
        new(SCH, SummonSeraph, 38.93f, "Inferno/Tsunami 2", [Inferno2]),

        new(OT, PartyMit, 48.76f, "Grand Cross 3", [GrandCross3], [Warrior, Paladin]),
        new(WHM, DivineCaress, 48.76f, "Grand Cross 3", [GrandCross3]),
        new(SCH, Consolation, 48.76f, "Grand Cross 3", [GrandCross3]),
        new(SGE, Zoe, 48.46f, "Grand Cross 3", []),
        new(SGE, EukrasianPrognosisII, 48.76f, "Grand Cross 3 (Zoe shields)", [GrandCross3]),

        new(WHM, LiturgyOfTheBell, 60.89f, "Flood of Naught", []),
        new(AST, Macrocosmos, 60.89f, "Flood of Naught", []),
        new(SCH, SacredSoil, 58.50f, "Flood of Naught (early, so it's back for Ultima Upsurge)", [DeathBolt1]),
        new(SGE, Kerachole, 58.50f, "Flood of Naught (early, so it's back for Ultima Upsurge)", [DeathBolt1]),

        new(MT, PartyMit, 69.87f, "Death Bolt/Wave 1", [DeathBolt1]),

        new(MT, MitActionId.Reprisal, 88.10f, "Ultima Upsurge", [Upsurge]),
        new(WHM, PlenaryIndulgence, 88.10f, "Ultima Upsurge", [Upsurge, DeathBolt2]),
        new(AST, CollectiveUnconscious, 88.10f, "Ultima Upsurge", [Upsurge, DeathBolt2]),
        new(SCH, SacredSoil, 88.55f, "Ultima Upsurge", [Upsurge, DeathBolt2]),
        new(SGE, Kerachole, 88.55f, "Ultima Upsurge", [Upsurge, DeathBolt2]),
        new(D1, Feint, 88.10f, "Ultima Upsurge", [Upsurge]),
        new(D4, Addle, 88.10f, "Ultima Upsurge", [Upsurge]),
    ];
}
