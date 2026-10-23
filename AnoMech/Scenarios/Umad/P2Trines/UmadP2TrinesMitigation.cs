using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P2Trines;

// The P2 mitigation sheet's Light of Judgment, Wings of Destruction and Ultimate Embrace rows on
// this scenario's clock. Wings of Destruction deals no party damage: its presses are for the tanks'
// busters and carry into Ultimate Embrace II. Helpers deal the busters, so Reprisal and Feint only
// soften Kefka's own Ultimate Embrace.
internal static class UmadP2TrinesMitigation
{
    public const string LightOfJudgment = "light_of_judgment";
    public const string TrinePoint = "trine";
    public const string WingsBuster = "wings_of_destruction";
    public const string UltimateEmbrace = "ultimate_embrace";

    // Raw on a physical DPS, preliminary: logged hits with the party's mitigation divided out.
    // Ultimate Embrace is per sharer of 2. UNVERIFIED for the Trine points and the busters, which
    // land on few players.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(LightOfJudgment, "Light of Judgment", 360000f, UmadMitigation.Kefka),
        new(TrinePoint, "Trine", 200000f, null),
        new(WingsBuster, "Wings of Destruction", 900000f, null, TankBuster: true),
        new(UltimateEmbrace, "Ultimate Embrace", 850000f, UmadMitigation.Kefka, TankBuster: true),
    ];

    private const float LightOfJudgmentHit = 6.97f;
    private const float BusterHits = 35.60f;
    private const float UltimateEmbraceHit = 42.65f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(LightOfJudgmentHit, [LightOfJudgment]),
            new(BusterHits, [WingsBuster]),
            new(UltimateEmbraceHit, [UltimateEmbrace]),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(OT, MitActionId.Reprisal, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(OT, PartyMit, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(SCH, DeploymentTactics, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(SCH, SacredSoil, 4.70f, "Light of Judgment II (early, so it's back for Wings of Destruction)", [LightOfJudgmentHit]),
        new(SGE, Zoe, 5.70f, "Light of Judgment II", []),
        new(SGE, EukrasianPrognosisII, 6.20f, "Light of Judgment II (Zoe shields)", [LightOfJudgmentHit]),
        new(SGE, Kerachole, 4.70f, "Light of Judgment II (early, so it's back for Wings of Destruction)", [LightOfJudgmentHit]),
        new(SGE, Holos, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(D1, Feint, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(D3, PartyMit, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),
        new(D4, Addle, 6.20f, "Light of Judgment II", [LightOfJudgmentHit]),

        new(MT, MitActionId.Reprisal, 34.80f, "Wings of Destruction (Kefka's Ultimate Embrace)", [UltimateEmbraceHit]),
        new(MT, PartyMit, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(WHM, PlenaryIndulgence, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(AST, CollectiveUnconscious, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(SCH, SacredSoil, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(SCH, FeyIllumination, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(SCH, SummonSeraph, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(SGE, Kerachole, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(SGE, Panhaima, 34.80f, "Wings of Destruction", [BusterHits, UltimateEmbraceHit]),
        new(D2, Feint, 34.80f, "Wings of Destruction (Kefka's Ultimate Embrace)", [UltimateEmbraceHit]),
    ];
}
