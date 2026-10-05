using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P5Flood;

// The P5 mitigation sheet's Chaotic Flood row on this scenario's clock; Holos goes out for the
// Ultima Repeater before the scenario starts and carries into it. No damage is known yet, so HP
// doesn't move.
internal static class UmadP5FloodMitigation
{
    public const string ChaoticFlood = "chaotic_flood";

    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(ChaoticFlood, "Chaotic Flood", null, UmadMitigation.Kefka),
    ];

    private const float Flood1 = 6.33f;
    private const float Flood2 = 7.35f;
    private const float Flood3 = 8.37f;
    private const float Flood4 = 9.39f;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [new(Flood1, [ChaoticFlood]), new(Flood2, [ChaoticFlood]), new(Flood3, [ChaoticFlood]), new(Flood4, [ChaoticFlood])],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(SGE, Holos, -11.00f, "Ultima Repeater (before the scenario)", [Flood1]),
        new(WHM, Temperance, 4.83f, "Chaotic Flood", [Flood1, Flood4]),
        new(AST, NeutralSect, 4.83f, "Chaotic Flood", []),
        new(SCH, Expedient, 4.83f, "Chaotic Flood", [Flood1, Flood4]),
        new(SGE, Panhaima, 4.83f, "Chaotic Flood", [Flood1, Flood4]),
    ];
}
