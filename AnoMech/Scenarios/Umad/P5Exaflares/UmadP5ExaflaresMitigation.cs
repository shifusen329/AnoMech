using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P5Exaflares;

// The P5 mitigation sheet's Stray Entropy row on this scenario's clock: the off tank's Reprisal two
// GCDs after Stray Apocalypse, so it's back for Forsaken. Helpers deal Stray Entropy, so the Reprisal
// only softens Kefka's autos here.
internal static class UmadP5ExaflaresMitigation
{
    public const string StrayEntropy = "stray_entropy";

    // Raw on a physical DPS, per spread a player stands in.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(StrayEntropy, "Stray Entropy", 155000f, null),
    ];

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits, [new(25.09f, [StrayEntropy])], Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(OT, MitActionId.Reprisal, 12.50f, "Stray Entropy (two GCDs after Stray Apocalypse)", []),
    ];
}
