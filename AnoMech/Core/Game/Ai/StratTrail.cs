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

    // The strat's latest word for `role` as of `time`. A move due later in the same frame isn't
    // recorded yet when a death resolves, so it can't be picked up by mistake.
    public StratTarget? LatestFor(PartyRole role, float time)
    {
        for (var i = targets.Count - 1; i >= 0; i--)
            if (targets[i].Role == role && targets[i].IssuedAt <= time)
                return targets[i];
        return null;
    }

    public void Clear() => targets.Clear();
}
