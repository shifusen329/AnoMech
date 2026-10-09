using System.Numerics;
using RaidReplay.Core.GameData;
using RaidReplay.Core.Model;

namespace AnoMech.RaidLog;

// DMU P3 Black Hole, on AnoMech's scenario clock (Exdeath's Black Hole cast starts at 22.18s) and in
// scenario-local coordinates (log position minus the arena centre; north is -Z).
internal static class BlackHoleReport
{
    private const uint BlackHoleCast = 0xBAFB;
    private const uint Nothingness = 0xBAFC;
    private const uint ThunderIIIResolve = 0xBB0C;
    private const uint LookUponMeAndDespairOmen = 0xBAEE;
    private const uint GrabbyTether = 0x54;
    private const uint BlackHoleBase = 19512;
    private const uint KefkaP3Base = 19504;
    private const uint ExdeathBase = 19509;
    private const uint Unbecoming = 0x154C;
    private const uint MeanestExistence = 0x154D;
    private const uint PrimordialCrust = 0x154E;
    private const float ScenarioBlackHoleCastAt = 22.18f;
    private const float BeamLength = 125f;
    private static readonly Vector2 Centre = new(100f, 100f);

    public static bool Print(PullReplay r, TextWriter o)
    {
        var cast = r.Casts.FirstOrDefault(c => c.ActionId == BlackHoleCast);
        if (cast is null) return false;

        float Local(int ms) => (ms - cast.StartMs) / 1000f + ScenarioBlackHoleCastAt;
        Vector2? At(Actor a, int ms) => a.Track.TrySample(ms, out var p, out _) ? p - Centre : null;
        string Who(Actor a) => a.IsPlayer ? Jobs.Abbrev(a.Job) : a.DisplayName;

        var tethers = r.Tethers
            .Where(t => t.TetherId == GrabbyTether && t.Source.BNpcBaseId == BlackHoleBase && t.StartMs >= cast.StartMs)
            .OrderBy(t => t.StartMs)
            .ToList();
        var nothingness = r.Actions.Where(a => a.ActionId == Nothingness && a.T >= cast.StartMs).OrderBy(a => a.T).ToList();

        o.WriteLine($"== {Path.GetFileName(r.Summary.FilePath)} pull #{r.Summary.Ordinal} ({r.Summary.Outcome}): Black Hole cast at {Local(cast.StartMs):0.00}");
        o.WriteLine($"   party: {string.Join(", ", r.Party.Select(p => $"{Jobs.Abbrev(p.Job)} {p.Name}"))}");

        var holes = tethers.Select(t => t.Source).Distinct().OrderBy(h => tethers.First(t => t.Source == h).StartMs).ToList();
        var waves = new List<List<Actor>>();
        foreach (var hole in holes.OrderBy(h => h.SpawnMs))
        {
            if (waves.Count > 0 && Math.Abs(waves[^1][0].SpawnMs - hole.SpawnMs) < 2000) waves[^1].Add(hole);
            else waves.Add([hole]);
        }

        var grabLatencies = new List<float>();
        var holderRadii = new List<float>();
        var bystanderGaps = new List<(float Gap, string Who, float T)>();

        for (var w = 0; w < waves.Count; w++)
        {
            var wave = waves[w].OrderBy(h => tethers.First(t => t.Source == h).StartMs).ToList();
            var waveStart = tethers.First(t => t.Source == wave[0]).StartMs;
            var kefkaFacing = r.Casts
                .Where(c => c.Source.BNpcBaseId == KefkaP3Base && c.StartMs <= waveStart)
                .OrderBy(c => c.StartMs)
                .LastOrDefault();
            var kefkaDeg = kefkaFacing is null ? float.NaN : HeadingBearing(kefkaFacing.Heading);
            o.WriteLine();
            o.WriteLine($"-- wave {w + 1}: Kefka faces (relative north) {(float.IsNaN(kefkaDeg) ? "unknown" : $"{Compass(kefkaDeg)} ({kefkaDeg:0}°)")}");

            var firstTether = wave.ToDictionary(h => h, h => tethers.First(t => t.Source == h).StartMs);
            var firstShot = wave.ToDictionary(h => h, h => nothingness.FirstOrDefault(a => a.Source == h)?.T ?? int.MaxValue);
            foreach (var hole in wave.OrderBy(h => firstShot[h]).ThenBy(h => firstTether[h]))
            {
                var pos = At(hole, firstTether[hole]) ?? Vector2.Zero;
                var deg = Bearing(pos);
                var mates = wave.Where(h => Math.Abs(firstShot[h] - firstShot[hole]) < 300).ToList();
                var set = mates.Count switch { 1 => "single", 2 => "pair", _ => $"set of {mates.Count}" };
                var shots = nothingness.Where(a => a.Source == hole).Select(a => $"{Local(a.T):0.00}");
                var rel = float.IsNaN(kefkaDeg) ? "" : $", {Wrap360(deg - kefkaDeg):0}° CW of Kefka";
                var holder = tethers.Where(t => t.Source == hole && t.StartMs <= firstShot[hole]).OrderBy(t => t.StartMs).LastOrDefault();
                var took = "";
                if (holder != null)
                {
                    var setStart = mates.Min(h => firstTether[h]);
                    var latency = (holder.StartMs - setStart) / 1000f;
                    grabLatencies.Add(latency);
                    took = $"; first shot's holder {Who(holder.Target)} took it at {Local(holder.StartMs):0.00} (+{latency:0.0}s after the set's first tether line)";
                }

                o.WriteLine($"   hole {Compass(deg),-2} ({pos.X:0.0},{pos.Y:0.0}){rel}: {set}, first tether line {Local(firstTether[hole]):0.00}, shots {string.Join(" ", shots)}{took}");
            }

            o.WriteLine("   tethers:");
            foreach (var t in tethers.Where(t => wave.Contains(t.Source)))
            {
                var hole = At(t.Source, t.StartMs) ?? Vector2.Zero;
                var holder = At(t.Target, t.StartMs);
                var where = holder is { } h
                    ? $"({h.X:0.0},{h.Y:0.0}) r={h.Length():0.0}, {Vector2.Distance(h, hole):0.0}y from hole"
                    : "(no position)";
                o.WriteLine($"     {Local(t.StartMs),7:0.00}  {Compass(Bearing(hole)),-2} -> {Who(t.Target),-4} {where}");
            }

            foreach (var shot in nothingness.Where(a => wave.Contains(a.Source)))
            {
                var hole = At(shot.Source, shot.T) ?? Vector2.Zero;
                var holeDeg = Bearing(hole);
                var heading = shot.BestHeading;
                var dir = Angles.Dir(heading);
                var targets = shot.Hits.Select(h => h.Target).Distinct().ToList();
                var parts = new List<string>();
                foreach (var target in targets)
                {
                    var p = At(target, shot.T) ?? Vector2.Zero;
                    holderRadii.Add(p.Length());
                    parts.Add($"{Who(target)} ({p.X:0.0},{p.Y:0.0}) r={p.Length():0.0}, {Wrap180(Bearing(p) - holeDeg):+0;-0}° CW of its hole{Outcome(r, target, shot.T)}");
                }

                var gap = r.Party
                    .Where(p => !targets.Contains(p) && Alive(r, p, shot.T))
                    .Select(p => (Who: p, Pos: At(p, shot.T)))
                    .Where(x => x.Pos is not null)
                    .Select(x =>
                    {
                        var rel = x.Pos!.Value - hole;
                        var forward = Vector2.Dot(rel, dir);
                        var side = MathF.Abs(rel.X * dir.Y - rel.Y * dir.X);
                        return (Gap: forward is >= 0f and <= BeamLength ? side : float.PositiveInfinity, x.Who);
                    })
                    .OrderBy(x => x.Gap)
                    .FirstOrDefault();
                var bystander = gap.Who is null || float.IsPositiveInfinity(gap.Gap) ? "no bystander ahead of the beam" : $"nearest bystander {Who(gap.Who)} {gap.Gap:0.0}y off the centre line";
                if (gap.Who is not null && !float.IsPositiveInfinity(gap.Gap)) bystanderGaps.Add((gap.Gap, Who(gap.Who), Local(shot.T)));
                o.WriteLine($"   Nothingness {Local(shot.T):0.00} from {Compass(holeDeg)}, facing {Compass(HeadingBearing(heading))}: {string.Join("; ", parts)}; {bystander}");
            }
        }

        o.WriteLine();
        o.WriteLine("-- one player holding two black-hole tethers at once:");
        var any = false;
        foreach (var player in tethers.Select(t => t.Target).Distinct())
        {
            var spans = tethers.Where(t => t.Target == player).Select(t => (t.Source, t.StartMs, End: t.EndMs > t.StartMs ? t.EndMs : t.Source.DespawnMs)).ToList();
            foreach (var a in spans)
                foreach (var b in spans)
                {
                    if (a.Source == b.Source || a.StartMs > b.StartMs || (a.StartMs == b.StartMs && a.Source.Index > b.Source.Index)) continue;
                    var from = Math.Max(a.StartMs, b.StartMs);
                    var to = Math.Min(a.End, b.End);
                    if (to <= from) continue;
                    any = true;
                    o.WriteLine($"   {Who(player)} {Local(from):0.00}-{Local(to):0.00} ({(to - from) / 1000f:0.00}s): {Compass(Bearing(At(a.Source, from) ?? Vector2.Zero))} and {Compass(Bearing(At(b.Source, from) ?? Vector2.Zero))}");
                }
        }

        if (!any) o.WriteLine("   none");

        o.WriteLine();
        o.WriteLine("-- Exdeath at Thunder III:");
        var exdeath = r.Actors.FirstOrDefault(a => a.BNpcBaseId == ExdeathBase && a.IsPresent(cast.StartMs));
        foreach (var hit in r.Actions.Where(a => a.ActionId == ThunderIIIResolve && a.T >= cast.StartMs))
        {
            var e = exdeath is null ? null : At(exdeath, hit.T);
            var targets = string.Join(", ", hit.Hits.Select(h => h.Target).Distinct().Select(t => $"{Who(t)} ({At(t, hit.T)?.X:0.0},{At(t, hit.T)?.Y:0.0})"));
            var at = e is { } v ? $"({v.X:0.0},{v.Y:0.0}) r={v.Length():0.0} {Compass(Bearing(v))}" : "(not found)";
            o.WriteLine($"   {Local(hit.T):0.00}: Exdeath {at}; hit {targets}");
        }

        foreach (var look in r.Casts.Where(c => c.ActionId == LookUponMeAndDespairOmen && c.StartMs >= cast.StartMs))
            o.WriteLine($"   Look upon Me and Despair {Local(look.StartMs):0.00}: line axis {Compass(HeadingBearing(look.Heading))}-{Compass(Wrap360(HeadingBearing(look.Heading) + 180f))}");

        o.WriteLine();
        if (grabLatencies.Count > 0)
            o.WriteLine($"-- summary: first-shot holders took their tether {grabLatencies.Min():0.0}-{grabLatencies.Max():0.0}s after the set's first tether line; " +
                        $"holders stood {holderRadii.Min():0.0}-{holderRadii.Max():0.0}y from the middle; " +
                        $"closest bystander to a beam: {(bystanderGaps.Count == 0 ? "none" : $"{bystanderGaps.Min(g => g.Gap):0.0}y")}; " +
                        $"{nothingness.Count} shots, {nothingness.Sum(a => a.Hits.Select(h => h.Target).Distinct().Count())} target hits");
        return true;
    }

    private static string Outcome(PullReplay r, Actor target, int t)
    {
        var parts = new List<string>();
        foreach (var s in r.Statuses.Where(s => s.Target == target))
        {
            if (s.StatusId is Unbecoming or MeanestExistence && s.StartMs >= t - 100 && s.StartMs <= t + 1000)
                parts.Add($"gains {s.Name} ({(s.Duration >= 9000 ? "no timer" : $"{s.Duration:0}s")})");
            if (s.StatusId == PrimordialCrust && s.Removed && s.EndMs >= t - 100 && s.EndMs <= t + 1000)
                parts.Add("crust cleansed");
        }

        if (r.Deaths.Any(d => d.Victim == target && d.T >= t && d.T <= t + 2000))
            parts.Add("dies");
        return parts.Count == 0 ? "" : $" [{string.Join(", ", parts)}]";
    }

    private static bool Alive(PullReplay r, Actor a, int t) =>
        !r.Deaths.Any(d => d.Victim == a && d.T <= t && (d.RaisedMs < 0 || d.RaisedMs > t));

    // Compass degrees: 0 = north (-Z), 90 = east (+X).
    private static float Bearing(Vector2 p) => Wrap360(MathF.Atan2(p.X, -p.Y) * 180f / MathF.PI);

    // A game heading (0 faces south, pi/2 east) as compass degrees.
    private static float HeadingBearing(float heading) => Wrap360(180f - heading * 180f / MathF.PI);

    private static float Wrap360(float deg) => ((deg % 360f) + 360f) % 360f;

    private static float Wrap180(float deg)
    {
        var d = Wrap360(deg);
        return d > 180f ? d - 360f : d;
    }

    private static string Compass(float deg) => new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[(int)MathF.Round(Wrap360(deg) / 45f) % 8];
}
