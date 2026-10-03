using System;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Core.Game.Ai;

// Why a strat sends each role where it does, for the death recap. Why is called once per role as
// the move is worked out, so it may read the run's state.
public sealed record StratCue(string Mechanic, Func<PartyRole, string> Why, string Source)
{
    public static StratCue ForAll(string mechanic, string why, string source) => new(mechanic, _ => why, source);
}

// One role's spot from a strat move: scenario-local and unjittered, timed on the world.Events
// clock (the scaled clock a peer shares with the host).
public sealed record StratTarget(
    PartyRole Role, Vector3 Spot, float IssuedAt, float? Deadline, string? Mechanic, string? Why, string? Source);
