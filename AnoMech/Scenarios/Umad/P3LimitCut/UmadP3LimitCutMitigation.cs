using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P3LimitCut;

// The P3 mitigation sheet's Limit Cut rows on this scenario's clock. Temperance and Neutral Sect go
// out for the Stray Flames before the scenario starts and carry into it. Vacuum Wave and The Decisive
// Battle deal no damage: the sheet's Vacuum Wave row covers the blasters after it, and its Decisive
// Battle barriers are for the Accretions in Black Hole. Clones and helpers deal every hit here, so
// the debuffs on Chaos and Exdeath only soften the bosses' autos.
internal static class UmadP3LimitCutMitigation
{
    public const string UltimaBlaster = "ultima_blaster";
    public const string Cyclone = "cyclone";
    public const string UltimaBlasterCharge = "ultima_blaster_charge";
    public const string ThunderIII = "thunder_iii";

    // Raw on a physical DPS. Cyclone is per Cyclone a player stands in, the stack not sharing it;
    // Thunder III is what the tank takes before cooldowns.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(UltimaBlaster, "Ultima Blaster", 43000f, null),
        new(Cyclone, "Cyclone", 198000f, null),
        new(UltimaBlasterCharge, "Ultima Blaster", 124000f, null),
        new(ThunderIII, "Thunder III", 566000f, null, TankBuster: true),
    ];

    private const float Blaster1 = 8.89f;
    private const float Blaster5 = 16.92f;
    private const float CycloneHit = 19.97f;

    private const uint PartyMit = MitPlanEntry.PartyMit;
    private const uint TankLb3 = MitPlanEntry.TankLb3;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(Blaster1, [UltimaBlaster]), new(10.90f, [UltimaBlaster]), new(12.90f, [UltimaBlaster]), new(14.91f, [UltimaBlaster]),
            new(Blaster5, [UltimaBlaster]), new(18.93f, [UltimaBlaster]), new(CycloneHit, [Cyclone]), new(20.93f, [UltimaBlaster]),
            new(22.93f, [UltimaBlaster]),
            new(30.95f, [UltimaBlasterCharge]), new(31.18f, [UltimaBlasterCharge]), new(31.40f, [UltimaBlasterCharge]),
            new(31.62f, [UltimaBlasterCharge]), new(31.85f, [UltimaBlasterCharge]), new(32.07f, [UltimaBlasterCharge]),
            new(32.29f, [UltimaBlasterCharge]), new(32.52f, [UltimaBlasterCharge]),
            new(38.57f, [ThunderIII]), new(41.60f, [ThunderIII]),
        ],
        Rows, UmadMitigation.Chaos);

    private static readonly MitPlanEntry[] Rows =
    [
        new(WHM, Temperance, -1.70f, "Stray Flames/Tsunami (before the scenario)", [Blaster1, Blaster5]),
        new(AST, NeutralSect, -1.70f, "Stray Flames/Tsunami (before the scenario)", []),

        new(MT, MitActionId.Reprisal, 7.39f, "Ultima Blaster (Chaos's autos)", []),
        new(AST, SunSign, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),
        new(SCH, SacredSoil, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),
        new(SCH, FeyIllumination, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),
        new(SCH, Seraphism, 7.39f, "Ultima Blaster", []),
        new(SGE, Kerachole, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),
        new(SGE, Panhaima, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),
        new(D2, Feint, 7.39f, "Ultima Blaster (Chaos's autos)", []),
        new(D3, PartyMit, 7.39f, "Ultima Blaster", [Blaster1, Blaster5, CycloneHit]),

        new(MT, TankLb3, 14.94f, "Vacuum Wave (either tank)", [Blaster5, CycloneHit]),
        new(OT, TankLb3, 14.94f, "Vacuum Wave (either tank)", [Blaster5, CycloneHit]),
        new(WHM, PlenaryIndulgence, 14.60f, "Vacuum Wave", [Blaster5, CycloneHit]),
        new(AST, CollectiveUnconscious, 14.60f, "Vacuum Wave", [Blaster5, CycloneHit]),

        new(WHM, DivineCaress, 18.47f, "Cyclone", [CycloneHit]),

        new(OT, MitActionId.Reprisal, 44.99f, "The Decisive Battle (Exdeath's autos)", [], On: UmadMitigation.Exdeath),
        new(SCH, DeploymentTactics, 44.99f, "The Decisive Battle (barriers for the Accretions)", []),
        new(SGE, Zoe, 44.69f, "The Decisive Battle", []),
        new(SGE, EukrasianPrognosisII, 44.99f, "The Decisive Battle (barriers for the Accretions)", []),
    ];
}
