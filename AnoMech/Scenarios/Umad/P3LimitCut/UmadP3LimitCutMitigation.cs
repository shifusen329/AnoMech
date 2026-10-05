using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P3LimitCut;

// The P3 mitigation sheet's Limit Cut rows on this scenario's clock, from the Ultima Blaster
// raidwides to The Decisive Battle. Temperance and Neutral Sect go out for the Stray Flames before
// the scenario starts and carry into it. No damage is known yet, so HP doesn't move.
internal static class UmadP3LimitCutMitigation
{
    public const string UltimaBlaster = "ultima_blaster";
    public const string VacuumWave = "vacuum_wave";
    public const string Cyclone = "cyclone";
    public const string DecisiveBattle = "decisive_battle";

    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(UltimaBlaster, "Ultima Blaster", null, UmadMitigation.Kefka),
        new(VacuumWave, "Vacuum Wave", null, UmadMitigation.Exdeath),
        new(Cyclone, "Cyclone", null, UmadMitigation.Chaos),
        new(DecisiveBattle, "The Decisive Battle", null, UmadMitigation.Exdeath),
    ];

    private const float Blaster1 = 8.89f;
    private const float Blaster2 = 10.90f;
    private const float Blaster3 = 12.90f;
    private const float Blaster4 = 14.91f;
    private const float VacuumHit = 16.10f;
    private const float Blaster5 = 16.92f;
    private const float Blaster6 = 18.93f;
    private const float CycloneHit = 19.97f;
    private const float Blaster7 = 20.93f;
    private const float Blaster8 = 22.93f;
    private const float DecisiveHit = 46.49f;

    private const uint PartyMit = MitPlanEntry.PartyMit;
    private const uint TankLb3 = MitPlanEntry.TankLb3;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(Blaster1, [UltimaBlaster]), new(Blaster2, [UltimaBlaster]), new(Blaster3, [UltimaBlaster]), new(Blaster4, [UltimaBlaster]),
            new(VacuumHit, [VacuumWave]), new(Blaster5, [UltimaBlaster]), new(Blaster6, [UltimaBlaster]), new(CycloneHit, [Cyclone]),
            new(Blaster7, [UltimaBlaster]), new(Blaster8, [UltimaBlaster]), new(DecisiveHit, [DecisiveBattle]),
        ],
        Rows, UmadMitigation.Chaos);

    private static readonly MitPlanEntry[] Rows =
    [
        new(WHM, Temperance, -1.70f, "Stray Flames/Tsunami (before the scenario)", [Blaster1, VacuumHit]),
        new(AST, NeutralSect, -1.70f, "Stray Flames/Tsunami (before the scenario)", []),

        new(MT, MitActionId.Reprisal, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(AST, SunSign, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(SCH, SacredSoil, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(SCH, FeyIllumination, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(SCH, Seraphism, 7.39f, "Ultima Blaster", []),
        new(SGE, Kerachole, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(SGE, Panhaima, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(D2, Feint, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),
        new(D3, PartyMit, 7.39f, "Ultima Blaster", [Blaster1, VacuumHit, CycloneHit]),

        new(MT, TankLb3, 14.94f, "Vacuum Wave (either tank)", [VacuumHit, CycloneHit]),
        new(OT, TankLb3, 14.94f, "Vacuum Wave (either tank)", [VacuumHit, CycloneHit]),
        new(WHM, PlenaryIndulgence, 14.60f, "Vacuum Wave", [VacuumHit, CycloneHit]),
        new(AST, CollectiveUnconscious, 14.60f, "Vacuum Wave", [VacuumHit, CycloneHit]),

        new(WHM, DivineCaress, 18.47f, "Cyclone", [CycloneHit]),

        new(OT, MitActionId.Reprisal, 44.99f, "The Decisive Battle", [DecisiveHit], On: UmadMitigation.Exdeath),
        new(SCH, DeploymentTactics, 44.99f, "The Decisive Battle", [DecisiveHit]),
        new(SGE, Zoe, 44.69f, "The Decisive Battle", []),
        new(SGE, EukrasianPrognosisII, 44.99f, "The Decisive Battle (Zoe shields)", [DecisiveHit]),
    ];
}
