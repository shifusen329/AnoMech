using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

// The P3 mitigation sheet's Bowels of Agony, Stray Flames/Tsunami and first Thunder III rows on this
// scenario's clock. Bowels of Agony and the first elements are under 30s apart, so Sacred Soil and
// Kerachole go out early for the first. Only Bowels of Agony is Chaos's own hit; helpers deal the
// rest, so Reprisal, Feint and Addle there only soften the bosses' autos.
internal static class UmadP3BowelsOfAgonyMitigation
{
    public const string BowelsOfAgony = "bowels_of_agony";
    public const string StrayFlames = "stray_flames";
    public const string StraySpray = "stray_spray";
    public const string Tsunami = "tsunami";
    public const string Inferno = "inferno";
    public const string ThunderIII = "thunder_iii";
    public const string Shockwave = "shockwave";
    public const string Cyclone = "cyclone";

    // Raw on a physical DPS, per player hit. UNVERIFIED past Bowels of Agony and Thunder III: the
    // element and crystal hits land on few players and their logged damage varies widely.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(BowelsOfAgony, "Bowels of Agony", 115000f, UmadMitigation.Chaos, ActionIds: [UmadConstants.ActionId.BowelsOfAgony]),
        new(StrayFlames, "Stray Flames", 200000f, null, ActionIds: [UmadConstants.ActionId.StrayFlames_P3]),
        new(StraySpray, "Stray Spray", 200000f, null, ActionIds: [UmadConstants.ActionId.StraySpray_P3]),
        new(Tsunami, "Tsunami", 250000f, null, ActionIds: [UmadConstants.ActionId.Tsunami_P3]),
        new(Inferno, "Inferno", 200000f, null, ActionIds: [UmadConstants.ActionId.Inferno_P3]),
        new(ThunderIII, "Thunder III", 566000f, null, TankBuster: true, ActionIds: [UmadConstants.ActionId.ThunderIII_Resolve]),
        new(Shockwave, "Shockwave", 220000f, null, ActionIds: [UmadConstants.ActionId.Shockwave]),
        new(Cyclone, "Cyclone", 198000f, null, ActionIds: [UmadConstants.ActionId.Cyclone]),
    ];

    private const float Bowels = 29.98f;
    private const float ShortElement = 49.11f;
    private const float ShortCrystal = 50.14f;
    private const float Thunder1 = 58.43f;
    private const float Thunder2 = 61.46f;
    private const float LongElement = 76.12f;
    private const float LongCrystal = 77.14f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    private static string ElementKey(Element element) => element == Element.Water ? StraySpray : StrayFlames;
    private static string CrystalKey(Element element) => element == Element.Water ? Tsunami : Inferno;

    public static MitPlanData Plan(Element shortElement)
    {
        var longElement = shortElement == Element.Water ? Element.Fire : Element.Water;
        return new(UmadMitigation.Profile, Hits,
            [
                new(Bowels, [BowelsOfAgony]),
                new(ShortElement, [ElementKey(shortElement)]), new(ShortCrystal, [CrystalKey(shortElement)]),
                new(Thunder1, [ThunderIII]), new(Thunder2, [ThunderIII]),
                new(LongElement, [ElementKey(longElement)]), new(LongCrystal, [CrystalKey(longElement)]),
            ],
            Rows, UmadMitigation.Chaos);
    }

    private static readonly MitPlanEntry[] Rows =
    [
        new(SCH, SacredSoil, 18.40f, "Bowels of Agony (early, so it's back for the short elements)", [Bowels]),
        new(SGE, Kerachole, 18.40f, "Bowels of Agony (early, so it's back for the short elements)", [Bowels]),
        new(MT, MitActionId.Reprisal, 28.48f, "Bowels of Agony", [Bowels]),
        new(WHM, PlenaryIndulgence, 28.48f, "Bowels of Agony", [Bowels]),
        new(AST, CollectiveUnconscious, 28.48f, "Bowels of Agony", [Bowels]),
        new(D1, Feint, 28.48f, "Bowels of Agony", [Bowels]),

        new(MT, PartyMit, 47.61f, "Stray Flames/Tsunami", [ShortElement, ShortCrystal]),
        new(SCH, DeploymentTactics, 47.61f, "Stray Flames/Tsunami", [ShortElement, ShortCrystal]),
        new(SCH, SacredSoil, 48.50f, "Stray Flames/Tsunami", [ShortElement, ShortCrystal]),
        new(SGE, Zoe, 47.31f, "Stray Flames/Tsunami", []),
        new(SGE, EukrasianPrognosisII, 47.61f, "Stray Flames/Tsunami (Zoe shields)", [ShortElement, ShortCrystal]),
        new(SGE, Kerachole, 48.50f, "Stray Flames/Tsunami", [ShortElement, ShortCrystal]),

        new(OT, MitActionId.Reprisal, 57.65f, "Thunder III (Exdeath's autos)", [], On: UmadMitigation.Exdeath),
        new(SCH, Expedient, 57.65f, "Thunder III", [Thunder1, Thunder2, LongElement, LongCrystal]),
        new(SGE, Holos, 57.65f, "Thunder III", [Thunder1, Thunder2, LongElement, LongCrystal]),
        new(D4, Addle, 57.65f, "Thunder III (Exdeath's autos)", [], On: UmadMitigation.Exdeath),

        new(OT, PartyMit, 74.62f, "Stray Flames/Tsunami", [LongElement, LongCrystal]),
        new(WHM, Temperance, 74.62f, "Stray Flames/Tsunami", [LongElement, LongCrystal]),
        new(AST, NeutralSect, 74.62f, "Stray Flames/Tsunami", []),
        new(SCH, SummonSeraph, 74.62f, "Stray Flames/Tsunami", [LongElement, LongCrystal]),
    ];
}
