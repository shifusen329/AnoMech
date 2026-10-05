using AnoMech.Core.Game.PartyMit;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

// The P1 mitigation sheet's Graven Image 2 rows on this scenario's clock. Gravity III goes off 2s
// after the first soaker steps into a puddle; the plan assumes the bots' timing.
internal static class UmadP1GravenImage2Mitigation
{
    private const float Gravitas1 = 12.20f;
    private const float Vitrophyre1 = 16.22f;
    private const float Gravitas2 = 30.66f;
    private const float Vitrophyre2 = 34.67f;
    private const float Trap = 43.00f;
    private const float GravityIII = 45.40f;
    private const float Judgment = 57.23f;
    private const float Hyperdrive1 = 60.39f;
    private const float Hyperdrive2 = 62.48f;
    private const float Hyperdrive3 = 64.57f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, UmadP1Hits.All,
        [
            new(Gravitas1, [UmadP1Hits.Gravitas]), new(Vitrophyre1, [UmadP1Hits.Vitrophyre]),
            new(Gravitas2, [UmadP1Hits.Gravitas]), new(Vitrophyre2, [UmadP1Hits.Vitrophyre]),
            new(Trap, [UmadP1Hits.DoubleTroubleTrap]), new(GravityIII, [UmadP1Hits.GravityIII]),
            new(Judgment, [UmadP1Hits.LightOfJudgment]),
            new(Hyperdrive1, [UmadP1Hits.Hyperdrive]), new(Hyperdrive2, [UmadP1Hits.Hyperdrive]), new(Hyperdrive3, [UmadP1Hits.Hyperdrive]),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(AST, Macrocosmos, 10.70f, "Gravitas (first set)", []),
        new(SCH, SacredSoil, 10.70f, "Gravitas (first set)", [Gravitas1, Vitrophyre1]),
        new(SCH, Seraphism, 11.00f, "Gravitas (first set)", []),
        new(SGE, Kerachole, 26.16f, "Gravitas (second set; early, so it's back for Light of Judgment 2)", [Gravitas2, Vitrophyre2]),
        new(WHM, LiturgyOfTheBell, 29.16f, "Gravitas (second set)", []),
        new(SCH, SummonSeraph, 29.16f, "Gravitas (second set)", [Gravitas2]),
        new(SCH, FeyIllumination, 29.16f, "Gravitas (second set)", [Gravitas2, Vitrophyre2, Trap]),
        new(SGE, Philosophia, 29.16f, "Gravitas (second set)", []),
        new(OT, MitActionId.Reprisal, 41.50f, "Double-trouble Trap 2", [Trap, GravityIII]),
        new(OT, PartyMit, 41.50f, "Double-trouble Trap 2", [Trap, GravityIII]),
        new(SCH, DeploymentTactics, 41.50f, "Double-trouble Trap 2", [Trap]),
        new(SCH, SacredSoil, 41.50f, "Double-trouble Trap 2", [Trap, GravityIII, Judgment]),
        new(SGE, Zoe, 41.20f, "Double-trouble Trap 2", []),
        new(SGE, Panhaima, 41.50f, "Double-trouble Trap 2", [Trap, GravityIII]),
        new(SGE, EukrasianPrognosisII, 41.50f, "Double-trouble Trap 2 (Zoe shields)", [Trap]),
        new(D3, PartyMit, 41.50f, "Double-trouble Trap 2", [Trap, GravityIII]),
        new(MT, MitActionId.Reprisal, 56.23f, "Light of Judgment 2", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(MT, PartyMit, 56.23f, "Light of Judgment 2", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(WHM, PlenaryIndulgence, 56.23f, "Light of Judgment 2", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(AST, CollectiveUnconscious, 56.23f, "Light of Judgment 2", [Judgment]),
        new(SGE, Kerachole, 56.23f, "Light of Judgment 2", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(D1, Feint, 56.23f, "Light of Judgment 2", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
    ];
}
