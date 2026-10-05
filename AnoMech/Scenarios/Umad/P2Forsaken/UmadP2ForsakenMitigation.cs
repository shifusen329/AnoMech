using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Scenarios.Umad.P2Forsaken;

// The P2 mitigation sheet's Forsaken rows on this scenario's clock. Light of Judgment and Wings of
// Destruction come after the scenario ends and are left out.
internal static class UmadP2ForsakenMitigation
{
    public const string Forsaken = "forsaken";
    public const string Spelldriver = "spelldriver";
    public const string Spellscatter = "spellscatter";
    public const string Spellwave = "spellwave";
    public const string PastsEnd = "pasts_end";
    public const string FuturesEnd = "futures_end";

    // Raw on a physical DPS, preliminary: logged hits with the party's mitigation divided out and
    // barriers added back. Spelldriver is per sharer of 3. The towers and clones count the party's
    // debuffs on Kefka.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Forsaken, "Forsaken", 548000f, UmadMitigation.Kefka),
        new(Spelldriver, "Spelldriver", 189000f, UmadMitigation.Kefka),
        new(Spellscatter, "Spellscatter", 196000f, UmadMitigation.Kefka),
        new(Spellwave, "Spellwave", 189000f, UmadMitigation.Kefka),
        new(PastsEnd, "Past's End", 168000f, UmadMitigation.Kefka),
        new(FuturesEnd, "Future's End", 168000f, UmadMitigation.Kefka),
    ];

    private const float ForsakenHit = 9.46f;
    private const float Towers1 = 23.16f;
    private const float End1 = 32.31f;
    private const float Towers2 = 33.17f;
    private const float Towers3 = 44.21f;
    private const float End2 = 53.22f;
    private const float Towers4 = 54.22f;
    private const float Towers5 = 65.26f;
    private const float End3 = 74.09f;
    private const float Towers6 = 75.27f;
    private const float Towers7 = 86.31f;
    private const float End4 = 95.06f;
    private const float Towers8 = 96.32f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    private static readonly string[] Spells = [Spelldriver, Spellscatter, Spellwave];
    private static readonly string[] Ends = [PastsEnd, FuturesEnd];

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [
            new(ForsakenHit, [Forsaken]),
            new(Towers1, Spells), new(End1, Ends), new(Towers2, Spells), new(Towers3, Spells), new(End2, Ends), new(Towers4, Spells),
            new(Towers5, Spells), new(End3, Ends), new(Towers6, Spells), new(Towers7, Spells), new(End4, Ends), new(Towers8, Spells),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(SCH, DeploymentTactics, -6.50f, "Forsaken (during Ultimate Embrace)", [ForsakenHit]),
        new(SGE, Holos, -6.50f, "Forsaken (during Ultimate Embrace)", [ForsakenHit]),
        new(SGE, Zoe, 8.20f, "Forsaken", []),
        new(MT, MitActionId.Reprisal, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(OT, PartyMit, 8.70f, "Forsaken", [ForsakenHit], [Warrior, Paladin]),
        new(WHM, PlenaryIndulgence, 8.70f, "Forsaken", [ForsakenHit]),
        new(AST, CollectiveUnconscious, 8.70f, "Forsaken", [ForsakenHit]),
        new(SCH, SacredSoil, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(SGE, Kerachole, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(SGE, EukrasianPrognosisII, 8.70f, "Forsaken (Zoe shields)", [ForsakenHit]),
        new(D1, Feint, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(D3, PartyMit, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(D4, Addle, 8.70f, "Forsaken", [ForsakenHit, Towers1]),
        new(Extras, Dismantle, 8.70f, "Forsaken", [ForsakenHit], [Machinist]),
        new(Extras, MagickBarrier, 8.70f, "Forsaken", [ForsakenHit], [RedMage]),
        new(Extras, TemperaCoat, 8.00f, "Forsaken (Tempera Grassa next)", [], [Pictomancer]),
        new(Extras, TemperaGrassa, 8.70f, "Forsaken", [ForsakenHit], [Pictomancer]),

        new(OT, PartyMit, 21.66f, "Towers I", [Towers1], [Gunbreaker, DarkKnight]),
        new(SCH, SummonSeraph, 21.66f, "Towers I", [Towers1]),
        new(SCH, FeyIllumination, 21.66f, "Towers I", [Towers1, End1, Towers2]),
        new(SGE, Panhaima, 21.66f, "Towers I", [Towers1, End1, Towers2]),
        new(SCH, Consolation, 30.81f, "Towers II", [End1, Towers2]),

        new(MT, PartyMit, 42.71f, "Towers III", [Towers3, End2, Towers4]),
        new(OT, MitActionId.Reprisal, 42.71f, "Towers III", [Towers3]),
        new(AST, Macrocosmos, 42.71f, "Towers III", []),
        new(SCH, SacredSoil, 42.71f, "Towers III", [Towers3, End2, Towers4]),
        new(SGE, Kerachole, 42.71f, "Towers III", [Towers3, End2, Towers4]),

        new(WHM, Temperance, 63.76f, "Towers V", [Towers5, End3, Towers6, Towers7]),
        new(WHM, DivineCaress, 64.00f, "Towers V", [Towers5]),
        new(AST, NeutralSect, 63.76f, "Towers V", []),
        new(SCH, Expedient, 63.76f, "Towers V", [Towers5, End3, Towers6]),

        new(MT, MitActionId.Reprisal, 72.59f, "Towers VI", [End3, Towers6]),
        new(WHM, PlenaryIndulgence, 72.59f, "Towers VI", [End3, Towers6]),
        new(AST, CollectiveUnconscious, 72.59f, "Towers VI", [End3, Towers6]),
        new(SCH, Seraphism, 72.59f, "Towers VI", []),
        new(SGE, Philosophia, 72.59f, "Towers VI", []),

        new(WHM, LiturgyOfTheBell, 84.81f, "Towers VII", []),
        new(AST, SunSign, 84.81f, "Towers VII", [Towers7, End4, Towers8]),
        new(SCH, SacredSoil, 84.81f, "Towers VII", [Towers7, End4, Towers8]),
        new(SGE, Kerachole, 84.81f, "Towers VII", [Towers7, End4, Towers8]),
    ];
}
