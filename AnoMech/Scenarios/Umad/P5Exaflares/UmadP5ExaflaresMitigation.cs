using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P5Exaflares;

// The P5 mitigation sheet's Stray Entropy row on this scenario's clock: the off tank's Reprisal two
// GCDs after Stray Apocalypse, so it's back for Forsaken. No damage is known yet, so HP doesn't move.
internal static class UmadP5ExaflaresMitigation
{
    public const string StrayEntropy = "stray_entropy";

    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(StrayEntropy, "Stray Entropy", null, UmadMitigation.Kefka),
    ];

    private const float StrayEntropyHit = 25.09f;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits, [new(StrayEntropyHit, [StrayEntropy])], Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(OT, MitActionId.Reprisal, 12.00f, "Stray Entropy (two GCDs after Stray Apocalypse)", [StrayEntropyHit]),
    ];
}
