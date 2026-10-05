using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P5Celestriad;

// The P5 mitigation sheet's Celestriad row on this scenario's clock. The Feint and Addle it says to
// use after the third towers are for the Ultima Repeater after the scenario ends, and are left out.
// No damage is known yet, so HP doesn't move.
internal static class UmadP5CelestriadMitigation
{
    public const string Celestriad = "celestriad";
    public const string Tower = "tower";

    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Celestriad, "Celestriad", null, UmadMitigation.Kefka),
        new(Tower, "Tower", null, UmadMitigation.Kefka),
    ];

    private const float CelestriadHit = 6.10f;
    private const float Towers1 = 14.18f;
    private const float Towers2 = 20.50f;
    private const float Towers3 = 26.34f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [new(CelestriadHit, [Celestriad]), new(Towers1, [Tower]), new(Towers2, [Tower]), new(Towers3, [Tower])],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(SCH, SummonSeraph, 3.00f, "Celestriad (during the cast bar)", [CelestriadHit]),
        new(MT, PartyMit, 4.60f, "Celestriad", [CelestriadHit]),
    ];
}
