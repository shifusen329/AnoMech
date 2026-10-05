using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Core.Game.Ai;

// Every spot a strat gave each role this run, the human's included (the player's own MoveTo is
// inert, so this is the only place it survives). AiManager.Move records on its own; strats that
// drive MoveTo themselves call Note alongside it.
public sealed class StratTrail(EventScheduler events)
{
    private const int MaxEntries = 1024;

    private readonly List<StratTarget> targets = [];

    public void Record(StratTarget target)
    {
        targets.Add(target);
        if (targets.Count > MaxEntries) targets.RemoveRange(0, targets.Count - MaxEntries);
    }

    public void Note(PartyRole role, Vector2 spot, StratCue? cue = null, float? deadline = null)
        => Record(new StratTarget(role, new Vector3(spot.X, 0f, spot.Y), events.Elapsed, deadline,
            cue?.Mechanic, cue?.Why(role), cue?.Source));

    // The spot `role` was due at by `time`: the last one whose deadline had passed (one without a
    // deadline is due as it's issued). Strats hand out the next spot before the current mechanic
    // resolves, so the latest one issued is often for what comes next; it's returned only when
    // nothing is due yet. A move due later in the same frame isn't recorded yet when a death
    // resolves, so it can't be picked up by mistake.
    public StratTarget? DueFor(PartyRole role, float time)
    {
        StratTarget? upcoming = null;
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            if (target.Role != role || target.IssuedAt > time) continue;
            if ((target.Deadline ?? target.IssuedAt) <= time) return target;
            upcoming ??= target;
        }
        return upcoming;
    }

    public void Clear() => targets.Clear();
}
