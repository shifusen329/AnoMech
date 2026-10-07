using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad;

// The P5 mitigation sheet's Forsaken rows on this scenario's clock: everything as late as possible,
// a second round for the 5th hit as the first wears off. The 8th hit's Sacred Soil and Kerachole wait
// out the 30s recast from the 1st. Kefka himself deals Forsaken, so the party's debuffs reduce it; a
// helper deals Forsaken Bonds.
internal static class UmadP5ForsakenNullMitigation
{
    public const string Forsaken = "forsaken";
    public const string ForsakenRepeat = "forsaken_repeat";
    public const string ForsakenBonds = "forsaken_bonds";

    // Raw on a physical DPS. The cast Forsaken opens; the three after it are a lighter instant one.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Forsaken, "Forsaken", 424000f, UmadMitigation.Kefka),
        new(ForsakenRepeat, "Forsaken", 309000f, UmadMitigation.Kefka),
        new(ForsakenBonds, "Forsaken Bonds", 240000f, null),
    ];

    private const float Hit1 = 13.21f;
    private const float Hit2 = 18.32f;
    private const float Hit3 = 21.39f;
    private const float Hit4 = 26.47f;
    private const float Hit5 = 29.55f;
    private const float Hit6 = 34.62f;
    private const float Hit7 = 37.69f;
    private const float Hit8 = 42.78f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(Hit1, [Forsaken]), new(Hit2, [ForsakenBonds]), new(Hit3, [ForsakenRepeat]), new(Hit4, [ForsakenBonds]),
            new(Hit5, [ForsakenRepeat]), new(Hit6, [ForsakenBonds]), new(Hit7, [ForsakenRepeat]), new(Hit8, [ForsakenBonds]),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(MT, MitActionId.Reprisal, 12.21f, "Forsaken (1st hit)", [Hit1, Hit3]),
        new(MT, PartyMit, 12.21f, "Forsaken (1st hit)", [Hit1], [Warrior, Paladin]),
        new(MT, PartyMit, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3, Hit4], [Gunbreaker, DarkKnight]),
        new(WHM, Temperance, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3, Hit4, Hit5]),
        new(WHM, LiturgyOfTheBell, 12.21f, "Forsaken (1st hit)", []),
        new(AST, NeutralSect, 12.21f, "Forsaken (1st hit)", []),
        new(AST, CollectiveUnconscious, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3]),
        new(SCH, DeploymentTactics, 12.21f, "Forsaken (1st hit)", [Hit1]),
        new(SCH, FeyIllumination, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3, Hit4]),
        new(SCH, SacredSoil, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3]),
        new(SGE, Zoe, 11.91f, "Forsaken (1st hit)", []),
        new(SGE, EukrasianPrognosisII, 12.21f, "Forsaken (1st hit, Zoe shields)", [Hit1]),
        new(SGE, Holos, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3, Hit4, Hit5]),
        new(SGE, Kerachole, 12.21f, "Forsaken (1st hit)", [Hit1, Hit2, Hit3]),
        new(D2, Feint, 12.21f, "Forsaken (1st hit)", [Hit1, Hit3]),
        new(Extras, Dismantle, 12.21f, "Forsaken (1st hit)", [Hit1], [Machinist]),
        new(Extras, MagickBarrier, 12.21f, "Forsaken (1st hit)", [Hit1], [RedMage]),
        new(Extras, TemperaCoat, 11.51f, "Forsaken (1st hit, Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 12.21f, "Forsaken (1st hit)", [Hit1], [Pictomancer]),

        new(SCH, Seraphism, 17.32f, "Forsaken Bonds (2nd hit)", []),
        new(SGE, Philosophia, 17.32f, "Forsaken Bonds (2nd hit)", []),

        new(AST, Macrocosmos, 20.39f, "Forsaken (3rd hit)", []),
        new(SCH, Expedient, 20.39f, "Forsaken (3rd hit)", [Hit3, Hit4, Hit5, Hit6]),
        new(SGE, Panhaima, 20.39f, "Forsaken (3rd hit)", [Hit3, Hit4, Hit5, Hit6, Hit7]),

        new(OT, MitActionId.Reprisal, 28.55f, "Forsaken (5th hit)", [Hit5, Hit7]),
        new(OT, PartyMit, 28.55f, "Forsaken (5th hit)", [Hit5], [Warrior, Paladin]),
        new(OT, PartyMit, 28.55f, "Forsaken (5th hit)", [Hit5, Hit6, Hit7, Hit8], [Gunbreaker, DarkKnight]),
        new(WHM, DivineCaress, 28.55f, "Forsaken (5th hit)", [Hit5]),
        new(AST, SunSign, 28.55f, "Forsaken (5th hit)", [Hit5, Hit6, Hit7, Hit8]),
        new(D1, Feint, 28.55f, "Forsaken (5th hit)", [Hit5, Hit7]),
        new(D3, PartyMit, 28.55f, "Forsaken (5th hit)", [Hit5, Hit6, Hit7, Hit8]),
        new(D4, Addle, 28.55f, "Forsaken (5th hit)", [Hit5, Hit7]),

        new(WHM, PlenaryIndulgence, 33.62f, "Forsaken Bonds (6th hit)", [Hit6, Hit7, Hit8]),

        new(SCH, SummonSeraph, 36.69f, "Forsaken (7th hit)", [Hit7]),

        new(SCH, Consolation, 41.78f, "Forsaken Bonds (8th hit)", [Hit8]),
        new(SCH, SacredSoil, 42.25f, "Forsaken Bonds (8th hit)", [Hit8]),
        new(SGE, Kerachole, 42.25f, "Forsaken Bonds (8th hit)", [Hit8]),
    ];
}
