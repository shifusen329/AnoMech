using System.Collections.Generic;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyMit;

// A mitigation sheet's columns. The healer columns are each one job, wherever it sits; Extras is
// whichever slot holds one of the entry's Jobs.
public enum SheetColumn { MT, OT, WHM, AST, SCH, SGE, D1, D2, D3, D4, Extras }

// One sheet row: who presses what, when (scenario seconds; negative is before the scenario starts,
// applied at its start and never graded), and the hits it must still be up for. Jobs narrows the
// row to those jobs; On is the boss a debuff goes on, when not the plan's DebuffTarget.
public sealed record MitPlanEntry(SheetColumn Column, uint ActionId, float At, string Label, float[] Covers, JobId[]? Jobs = null,
    MitSource? On = null)
{
    // Placeholder buttons, resolved per job by MitCatalogue.For.
    public const uint PartyMit = 0;
    public const uint TankLb3 = 1;

    public bool IsExtra => Column == SheetColumn.Extras;
    public bool IsPreStart => At < 0f;
}

// Who a hit comes from, which decides whether the party's debuffs on the boss reduce it.
public sealed record MitSource(string Name);

// Raw is the unmitigated damage on a physical DPS, or null while unknown (the hit moves no HP).
// A tank buster ignores the class factor; Fresh lands on full HP whatever came before.
public sealed record MitHitDef(string Key, string Name, float? Raw, MitSource? Source, DamageKind Kind = DamageKind.Magic,
    bool TankBuster = false, bool Fresh = false);

public readonly record struct MitScheduledHit(float At, string[] Keys);

// DebuffTarget is the boss the party's Reprisal/Feint/Addle go on.
public sealed record MitPlanData(MitProfile Profile, IReadOnlyList<MitHitDef> Hits, IReadOnlyList<MitScheduledHit> Schedule,
    IReadOnlyList<MitPlanEntry> Entries, MitSource? DebuffTarget = null);
