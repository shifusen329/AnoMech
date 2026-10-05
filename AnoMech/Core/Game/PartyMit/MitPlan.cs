using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyMit;

// A sheet row bound to the slot and job that press it. [Open, Close] is when a press counts for
// it; Soft covers are hits the row is listed for but no press in the window can reach.
public sealed record ResolvedPress(int Entry, PartyRole Slot, JobId Job, MitActionDef Action, float At, string Label,
    float[] Covers, float[] Soft, float Open, float Close, bool IsExtra, bool IsPreStart);

public static class MitPlan
{
    // A press with no covers counts this far either side of its planned time.
    private const float UncoveredWindow = 3f;
    // How long before its first cover a press must land, and the margin it must outlast the last by.
    private const float CloseLead = 0.2f;
    private const float CoverMargin = 0.3f;
    private const float Epsilon = 0.01f;

    public static IReadOnlyList<ResolvedPress> Resolve(IReadOnlyList<MitPlanEntry> entries, IReadOnlyDictionary<PartyRole, JobId> jobs)
    {
        var resolved = new List<ResolvedPress>();
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            foreach (var (slot, job) in SlotsFor(e, jobs))
            {
                if (e.Jobs is { } only && Array.IndexOf(only, job) < 0) continue;
                if (MitCatalogue.For(e.ActionId, job) is not { } def || !MitCatalogue.CanPress(job, def)) continue;
                var (open, close, hard, soft) = Window(e.At, e.Covers, def.Duration);
                resolved.Add(new(i, slot, job, def, e.At, e.Label, hard, soft, open, close, e.IsExtra, e.IsPreStart));
            }
        }
        return resolved;
    }

    private static IEnumerable<(PartyRole Slot, JobId Job)> SlotsFor(MitPlanEntry e, IReadOnlyDictionary<PartyRole, JobId> jobs)
    {
        var fixedSlot = e.Column switch
        {
            SheetColumn.MT => PartyRole.MainTank,
            SheetColumn.OT => PartyRole.OffTank,
            SheetColumn.D1 => PartyRole.MeleeDpsA,
            SheetColumn.D2 => PartyRole.MeleeDpsB,
            SheetColumn.D3 => PartyRole.PhysRangedDps,
            SheetColumn.D4 => PartyRole.CasterDps,
            _ => (PartyRole?)null,
        };
        if (fixedSlot is { } slot)
            return jobs.TryGetValue(slot, out var job) ? [(slot, job)] : [];
        JobId[] columnJobs = e.Column switch
        {
            SheetColumn.WHM => [JobId.WhiteMage],
            SheetColumn.AST => [JobId.Astrologian],
            SheetColumn.SCH => [JobId.Scholar],
            SheetColumn.SGE => [JobId.Sage],
            _ => e.Jobs ?? [],
        };
        return jobs.Where(kv => Array.IndexOf(columnJobs, kv.Value) >= 0).OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value));
    }

    // The press must land before its first cover; trailing covers it can't outlast from inside
    // that window become soft, and the window opens no later than the planned time.
    internal static (float Open, float Close, float[] Hard, float[] Soft) Window(float at, float[] covers, float duration)
    {
        if (covers.Length == 0) return (at - UncoveredWindow, at + UncoveredWindow, [], []);
        var close = covers[0] - CloseLead;
        var hard = covers.ToList();
        var soft = new List<float>();
        while (hard.Count > 1 && hard[^1] - duration + CoverMargin > close)
        {
            soft.Insert(0, hard[^1]);
            hard.RemoveAt(hard.Count - 1);
        }
        var open = MathF.Min(at, hard[^1] - duration + CoverMargin);
        return (open, close, hard.ToArray(), soft.ToArray());
    }

    // What's wrong with a resolved plan: a press outside its own window, a button pressed again
    // inside its recast, or a press whose required status nothing granted in time.
    public static IReadOnlyList<string> Validate(IReadOnlyList<ResolvedPress> plan)
    {
        var problems = new List<string>();
        foreach (var p in plan)
            if (p.At < p.Open - Epsilon || p.At > p.Close + Epsilon)
                problems.Add($"{p.Slot} {p.Action.Name} at {p.At:F2}s is outside its window {p.Open:F2}–{p.Close:F2}s ({p.Label})");

        foreach (var group in plan.GroupBy(p => (p.Slot, p.Action.ActionId)))
        {
            var presses = group.OrderBy(p => p.At).ToList();
            for (var i = 1; i < presses.Count; i++)
                if (presses[i].At - presses[i - 1].At < presses[i].Action.Recast - Epsilon)
                    problems.Add($"{group.Key.Slot} {presses[i].Action.Name} at {presses[i].At:F2}s is {presses[i].At - presses[i - 1].At:F2}s after the last, inside its {presses[i].Action.Recast:F0}s recast");
        }

        foreach (var p in plan.Where(p => p.Action.Requires != 0))
        {
            var granted = plan.Any(g => g.Slot == p.Slot && g.At <= p.At + Epsilon && !ReferenceEquals(g, p)
                && JobActions.GrantsOf(g.Action.ActionId).Any(s => s.StatusId == p.Action.Requires && p.At <= g.At + s.Duration + Epsilon));
            if (!granted)
                problems.Add($"{p.Slot} {p.Action.Name} at {p.At:F2}s needs status {p.Action.Requires}, which nothing granted in time");
        }
        return problems;
    }
}
