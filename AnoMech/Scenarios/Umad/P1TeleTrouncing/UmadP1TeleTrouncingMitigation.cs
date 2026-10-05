using AnoMech.Core.Game.PartyMit;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P1TeleTrouncing;

// The P1 mitigation sheet's Graven Image 3 and Mystery Magic 3 rows on this scenario's clock.
internal static class UmadP1TeleTrouncingMitigation
{
    private const float Trap = 22.78f;
    private const float Wills = 28.46f;
    private const float Fire = 42.19f;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, UmadP1Hits.All,
        [
            new(Trap, [UmadP1Hits.DoubleTroubleTrap]),
            new(Wills, [UmadP1Hits.IdyllicWill, UmadP1Hits.IndulgentWill]),
            new(Fire, [UmadP1Hits.FlagrantFire]),
        ],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(WHM, Temperance, 21.28f, "Double-trouble Trap 3", [Trap, Wills, Fire]),
        new(AST, NeutralSect, 21.28f, "Double-trouble Trap 3", []),
        new(SCH, Expedient, 21.28f, "Double-trouble Trap 3", [Trap, Wills]),
        new(WHM, DivineCaress, 26.96f, "Idyllic / Indulgent Will", [Wills]),
        new(AST, SunSign, 26.96f, "Idyllic / Indulgent Will", [Wills, Fire]),
        new(SCH, SacredSoil, 26.96f, "Idyllic / Indulgent Will", [Wills, Fire]),
        new(SGE, Kerachole, 26.96f, "Idyllic / Indulgent Will", [Wills, Fire]),
        new(OT, MitActionId.Reprisal, 40.69f, "Mystery Magic 3", [Fire]),
    ];
}
