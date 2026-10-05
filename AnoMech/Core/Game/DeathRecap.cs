using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Core.Game;

// The AoE that killed someone, in world coordinates, shaped from the Action sheet the way
// CharacterFind.InsideActionAoe reads it.
public sealed record DeathAoe(uint ActionId, string Name, byte CastType, float Range, float HalfWidth, float? Size, Vector3 Origin, float Rotation)
{
    public static DeathAoe? From(AoeQuery query, Coordinates coordinates)
    {
        if (Natives.Data.Action(query.ActionId) is not { } row) return null;
        var range = (float)row.EffectRange;
        var halfWidth = row.XAxisModifier > 0 ? row.XAxisModifier * 0.5f : range;
        return new DeathAoe(query.ActionId, row.Name, row.CastType, range, halfWidth, query.Size,
            coordinates.ToGlobal(query.Source.Position), query.Source.Rotation + query.OmenRotate);
    }
}

// What the death recap shows. Positions are world coordinates, taken at the death, since the
// scenario origin resets with the run.
public sealed record DeathRecap(PartyRole Role, string Cause, Vector3 DiedAt, float DiedAtTime, StratTarget? Strat, Vector3? Spot, DeathAoe? Aoe, string? Progress = null)
{
    // Closer than this counts as on the spot: the bots themselves land up to 0.3y off theirs.
    public const float OnSpotTolerance = 1f;

    public static DeathRecap Build(PartyRole role, string cause, Vector3 localDiedAt, float diedAtTime, StratTarget? strat, AoeQuery? aoe, Coordinates coordinates, string? progress = null)
        => new(role, cause, coordinates.ToGlobal(localDiedAt), diedAtTime, strat,
            strat is { } s ? coordinates.ToGlobal(s.Spot) : null,
            aoe is { } q ? DeathAoe.From(q, coordinates) : null, progress);

    public float? MissedBy => Spot is { } spot ? DistanceXZ(spot, DiedAt) : null;

    public string WhereText => Spot is not { } spot
        ? "The strat had no spot for you at this point."
        : MissedBy <= OnSpotTolerance
            ? "You were on your spot."
            : $"You were {MissedBy:F1}y {Compass(spot, DiedAt)} of your spot.";

    public string? TimingText => Strat?.Deadline is { } deadline
        ? $"Be there by {deadline:F1}s; you died at {DiedAtTime:F1}s."
        : null;

    // Where `to` lies as seen from `from`, on the 8-point compass (north is -Z).
    public static string Compass(Vector3 from, Vector3 to)
    {
        var degrees = MathF.Atan2(to.X - from.X, from.Z - to.Z) * 180f / MathF.PI;
        var index = (int)MathF.Round(((degrees % 360f) + 360f) % 360f / 45f) % 8;
        return Points[index];
    }

    private static readonly string[] Points = ["north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west"];

    private static float DistanceXZ(Vector3 a, Vector3 b)
        => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
}
