using AnoMech.Core.Game.PartyMit;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

// The P1 mitigation sheet's Graven Image 1 rows on this scenario's clock. Presses land 1.5s before
// their hit, and late in Light of Judgment's cast so they still cover Hyperdrive.
internal static class UmadP1GravenImage1Mitigation
{
    private const float Fire = 14.00f;
    private const float Wave = 18.23f;
    private const float Towers = 21.88f;
    private const float Trap = 25.48f;
    private const float Judgment = 38.51f;
    private const float Hyperdrive1 = 41.65f;
    private const float Hyperdrive2 = 43.74f;
    private const float Hyperdrive3 = 45.83f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan(bool extrasAtWaveCannon) => new(UmadMitigation.Profile, UmadP1Hits.All,
        [
            new(Fire, [UmadP1Hits.FlagrantFire]), new(Wave, [UmadP1Hits.WaveCannon]), new(Towers, [UmadP1Hits.Explosion]),
            new(Trap, [UmadP1Hits.DoubleTroubleTrap]), new(Judgment, [UmadP1Hits.LightOfJudgment]),
            new(Hyperdrive1, [UmadP1Hits.Hyperdrive]), new(Hyperdrive2, [UmadP1Hits.Hyperdrive]), new(Hyperdrive3, [UmadP1Hits.Hyperdrive]),
        ],
        [.. Rows, .. extrasAtWaveCannon ? ExtrasAtWaveCannon : ExtrasAtJudgment],
        UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(OT, PartyMit, 0.60f, "Mystery Magic 1 (at Kefka's re-centre)", [Fire]),
        new(D3, PartyMit, 0.60f, "Mystery Magic 1 (at Kefka's re-centre)", [Fire]),
        new(SCH, DeploymentTactics, 2.10f, "Mystery Magic 1 (early)", [Fire]),
        new(SGE, Zoe, 2.10f, "Mystery Magic 1 (early, so it's back for the second trap)", []),
        new(SGE, Kerachole, 5.50f, "Mystery Magic 1 (after Graven Image)", [Fire, Wave]),
        new(MT, PartyMit, 12.50f, "Mystery Magic 1", [Fire, Wave, Trap], [Gunbreaker, DarkKnight]),
        new(OT, MitActionId.Reprisal, 12.50f, "Mystery Magic 1", [Fire, Trap]),
        new(WHM, Temperance, 12.50f, "Mystery Magic 1", [Fire, Wave, Trap]),
        new(AST, NeutralSect, 12.00f, "Mystery Magic 1", []),
        new(AST, SunSign, 12.50f, "Mystery Magic 1", [Fire, Wave, Trap]),
        new(SCH, Expedient, 12.50f, "Mystery Magic 1", [Fire, Wave, Trap]),
        new(SGE, EukrasianPrognosisII, 12.50f, "Mystery Magic 1 (Zoe shields)", [Fire]),
        new(D1, Feint, 12.50f, "Mystery Magic 1", [Fire, Trap]),
        new(MT, PartyMit, 16.73f, "Wave Cannon", [Wave, Trap], [Warrior, Paladin]),
        new(WHM, DivineCaress, 16.73f, "Wave Cannon", [Wave]),
        new(SGE, Holos, 16.73f, "Wave Cannon", [Wave, Trap]),
        new(SCH, Concitation, 16.73f, "Wave Cannon (shields)", [Wave]),
        new(AST, HeliosConjunction, 16.73f, "Wave Cannon (Neutral Sect shields)", [Wave]),
        new(SCH, SacredSoil, 23.98f, "Double-trouble Trap 1", [Trap, Judgment]),
        new(D4, Addle, 23.98f, "Double-trouble Trap 1", [Trap, Judgment]),
        new(MT, MitActionId.Reprisal, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(WHM, PlenaryIndulgence, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(AST, CollectiveUnconscious, 37.51f, "Light of Judgment 1", [Judgment]),
        new(SGE, Kerachole, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(D2, Feint, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3]),
        new(Extras, Dismantle, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3], [Machinist]),
    ];

    private static readonly MitPlanEntry[] ExtrasAtJudgment =
    [
        new(Extras, MagickBarrier, 37.51f, "Light of Judgment 1", [Judgment, Hyperdrive1, Hyperdrive2, Hyperdrive3], [RedMage]),
        new(Extras, TemperaCoat, 36.81f, "Light of Judgment 1 (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 37.51f, "Light of Judgment 1", [Judgment], [Pictomancer]),
    ];

    private static readonly MitPlanEntry[] ExtrasAtWaveCannon =
    [
        new(Extras, MagickBarrier, 16.73f, "Wave Cannon", [Wave, Towers, Trap], [RedMage]),
        new(Extras, TemperaCoat, 16.03f, "Wave Cannon (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 16.73f, "Wave Cannon", [Wave], [Pictomancer]),
    ];
}
