using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;
using static AnoMech.Core.Game.PartyMit.MitActionId;
using static AnoMech.Core.Game.PartyMit.SheetColumn;

namespace AnoMech.Scenarios.Umad.P5Celestriad;

// The P5 mitigation sheet's Celestriad row on this scenario's clock. Celestriad itself deals no
// damage; the row's party mitigation and Seraph go to the first towers. The Feint and Addle the sheet
// says to use after the third towers are for the Ultima Repeater after the scenario ends, and are
// left out.
internal static class UmadP5CelestriadMitigation
{
    public const string Tower = "tower";

    // Raw on a physical DPS, per soaker.
    public static readonly IReadOnlyList<MitHitDef> Hits =
    [
        new(Tower, "Tower", 165000f, null),
    ];

    private const float Towers1 = 15.13f;

    private const uint PartyMit = MitPlanEntry.PartyMit;

    public static MitPlanData Plan() => new(UmadMitigation.Profile, Hits,
        [new(Towers1, [Tower]), new(21.15f, [Tower]), new(27.18f, [Tower])],
        Rows, UmadMitigation.Kefka);

    private static readonly MitPlanEntry[] Rows =
    [
        new(SCH, SummonSeraph, 3.00f, "Celestriad (during the cast bar)", [Towers1]),
        new(MT, PartyMit, 4.60f, "Celestriad", [Towers1]),
    ];
}
