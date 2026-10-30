using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyMit;

// Back-to-back slices of a fight as one plan on the run's clock. A later slice's pre-start rows
// stand for presses the slice before already plans, so they are dropped. Each slice's hit keys
// stay apart, since two slices can give one key different hits.
public static class MitPlanChain
{
    public static MitPlanData Of(params (MitPlanData Plan, float At)[] slices)
    {
        var hits = new List<MitHitDef>();
        var schedule = new List<MitScheduledHit>();
        var entries = new List<MitPlanEntry>();
        for (var i = 0; i < slices.Length; i++)
        {
            var (plan, at) = slices[i];
            var prefix = $"{i}:";
            var sliceEnd = plan.Schedule.Count > 0 ? plan.Schedule.Max(s => s.At) + at : at;
            hits.AddRange(plan.Hits.Select(h => h with { Key = prefix + h.Key }));
            schedule.AddRange(plan.Schedule.Select(s => new MitScheduledHit(s.At + at, s.Keys.Select(k => prefix + k).ToArray())));
            foreach (var e in plan.Entries.Where(e => i == 0 || !e.IsPreStart))
                if (WhenBackUp(e with { At = e.At + at, Covers = e.Covers.Select(c => c + at).ToArray() }, entries, sliceEnd) is { } placed)
                    entries.Add(placed);
        }
        return new(slices[0].Plan.Profile, hits, schedule, entries, slices[0].Plan.DebuffTarget);
    }

    // Each slice's plan counts on its buttons being up at its own start. One a slice before
    // pressed too recently goes out as soon as it's back, still in time for its hits, or not at all.
    private static MitPlanEntry? WhenBackUp(MitPlanEntry entry, IReadOnlyList<MitPlanEntry> earlier, float sliceEnd)
    {
        var last = earlier.LastOrDefault(e => e.Column == entry.Column && e.ActionId == entry.ActionId);
        if (last == null) return entry;
        var backUpAt = last.At + Recast(entry.ActionId);
        if (entry.At >= backUpAt) return entry;
        if (backUpAt > sliceEnd) return null;
        var covers = entry.Covers.Where(c => c > backUpAt).ToArray();
        if (entry.Covers.Length > 0 && covers.Length == 0) return null;
        return entry with { At = backUpAt, Covers = covers };
    }

    // The longest any job's version of the button takes, for a placeholder.
    private static float Recast(uint actionId)
        => Enum.GetValues<JobId>().Select(job => MitCatalogue.For(actionId, job)?.Recast ?? 0f).Max();
}
