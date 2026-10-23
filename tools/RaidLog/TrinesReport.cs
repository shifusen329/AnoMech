using System.Numerics;
using RaidReplay.Core.GameData;
using RaidReplay.Core.Model;

namespace AnoMech.RaidLog;

// DMU P2 Trines, on a clock that counts from Kefka's Trine cast, in scenario-local coordinates
// (log position minus the arena centre; north is -Z). Triangles are named by bearing: O is the
// centre, then A (11 deg), B (71), C (131), D (191), E (251), F (311).
internal static class TrinesReport
{
    private const uint TrineCast = 0xBADF;
    private const uint TrinePoint = 0xBAE0;
    private const uint WingsLeft = 0xBACD;
    private const uint WingsRight = 0xBACE;
    private const uint WingsBusterCast = 0xC487;
    private const uint WingsBuster = 0xBACF;
    private const uint UltimateEmbrace = 0xC24C;
    private const uint LightOfJudgment = 0xBABD;
    private const uint TriangleEast = 0x1EBFB2;
    private const uint TriangleWest = 0x1EBFB3;
    private const uint KefkaP2 = 19506;
    private static readonly Vector2 Centre = new(100f, 100f);

    private const string Names = "OABCDEF";
    private static readonly Vector2[] Centroids =
    [
        Vector2.Zero,
        new(2.887f, -15f), new(14.434f, -5f), new(11.547f, 10f),
        new(-2.887f, 15f), new(-14.434f, 5f), new(-11.547f, -10f),
    ];

    // A, C and E point east; B, D and F west; the centre either way.
    private static bool PointsEast(int triangle) => triangle % 2 == 1;

    private static Vector2[] Corners(int triangle, bool east)
    {
        var c = Centroids[triangle];
        var s = east ? 1f : -1f;
        return [c + new Vector2(s * 5.7735f, 0f), c + new Vector2(-s * 2.8868f, 5f), c + new Vector2(-s * 2.8868f, -5f)];
    }

    public static bool Print(PullReplay r, TextWriter o)
    {
        var trine = r.Casts.FirstOrDefault(c => c.ActionId == TrineCast);
        if (trine is null) return false;
        var t0 = trine.StartMs;
        float Local(int ms) => (ms - t0) / 1000f;
        Vector2? At(Actor a, int ms) => a.Track.TrySample(ms, out var p, out _) ? p - Centre : null;
        string Who(Actor? a) => a is null ? "?" : a.IsPlayer ? Jobs.Abbrev(a.Job) : a.DisplayName;
        bool IsTank(Actor a) => Jobs.Get(a.Job)?.Role == 1;
        string Times(IEnumerable<int> ms) => string.Join(" ", ms.Select(m => $"{Local(m):0.00}"));

        var kefka = r.Actors.Where(a => a.BNpcBaseId == KefkaP2 && a.IsPresent(t0)).MaxBy(a => a.SpawnMs);
        string Facing(int ms) => kefka is not null && kefka.Track.TrySample(ms, out _, out var h) ? Compass(HeadingBearing(h)) : "?";

        o.WriteLine($"== {Path.GetFileName(r.Summary.FilePath)} pull #{r.Summary.Ordinal} ({r.Summary.Outcome}, {r.Summary.FurthestPhase})");

        var loj = r.Casts.Where(c => c.ActionId == LightOfJudgment && c.StartMs < t0 && c.StartMs > t0 - 30000).OrderBy(c => c.StartMs).LastOrDefault();
        var lojHit = loj is null ? null : r.Actions.FirstOrDefault(a => a.ActionId == LightOfJudgment && a.T >= loj.StartMs);
        var wings = r.Casts.FirstOrDefault(c => c.ActionId is WingsLeft or WingsRight && c.StartMs >= t0);
        var wingsHit = wings is null ? null : r.Actions.FirstOrDefault(a => a.ActionId == wings.ActionId && a.T >= wings.StartMs);
        var busterCast = r.Casts.FirstOrDefault(c => c.ActionId == WingsBusterCast && c.StartMs >= t0);
        var busters = r.Actions.Where(a => a.ActionId == WingsBuster && a.T >= t0 && a.T < t0 + 30000).OrderBy(a => a.T).ThenBy(a => a.Index).ToList();
        var embrace = r.Casts.FirstOrDefault(c => c.ActionId == UltimateEmbrace && c.StartMs >= t0);
        var embraceHit = embrace is null ? null : r.Actions.FirstOrDefault(a => a.ActionId == UltimateEmbrace && a.T >= embrace.StartMs);
        var points = r.Actions.Where(a => a.ActionId == TrinePoint && a.T >= t0 && a.T < t0 + 30000).OrderBy(a => a.T).ToList();
        var drops = new List<List<ActionEvent>>();
        foreach (var p in points)
        {
            if (drops.Count > 0 && p.T - drops[^1][0].T < 1000) drops[^1].Add(p);
            else drops.Add([p]);
        }

        o.WriteLine($"   Light of Judgment II cast {(loj is null ? "-" : $"{Local(loj.StartMs):0.00}")} hit {(lojHit is null ? "-" : $"{Local(lojHit.T):0.00}")}; Trine resolves {Times(r.Actions.Where(a => a.ActionId == TrineCast && a.T >= t0).Take(1).Select(a => a.T))}");
        o.WriteLine($"   Wings {(wings is null ? "-" : wings.ActionId == WingsRight ? "right (0xBACE)" : "left (0xBACD)")} cast {(wings is null ? "-" : $"{Local(wings.StartMs):0.00}")} hit {(wingsHit is null ? "-" : $"{Local(wingsHit.T):0.00}")}; Kefka faces {Facing(t0)} at Trine, {(wings is null ? "?" : Facing(wings.StartMs))} at Wings");
        o.WriteLine($"   drops {string.Join(" / ", drops.Select(d => $"{Local(d[0].T):0.00} x{d.Count}"))}; buster cast {(busterCast is null ? "-" : $"{Local(busterCast.StartMs):0.00} (Kefka faces {Facing(busterCast.StartMs + 500)})")} hits {Times(busters.Select(b => b.T))}; Ultimate Embrace cast {(embrace is null ? "-" : $"{Local(embrace.StartMs):0.00} on {Who(embrace.Target)}")} hit {(embraceHit is null ? "-" : $"{Local(embraceHit.T):0.00}")}");

        var markers = r.Actors.Where(a => a.BNpcBaseId is TriangleEast or TriangleWest && a.SpawnMs >= t0 && a.SpawnMs <= t0 + 12000).OrderBy(a => a.SpawnMs).ToList();
        var sets = new List<List<(int Triangle, bool East, int Spawn)>>();
        foreach (var m in markers)
        {
            var pos = At(m, m.SpawnMs + 50) ?? Vector2.Zero;
            var tri = Enumerable.Range(0, 7).MinBy(i => Vector2.Distance(Centroids[i], pos));
            if (sets.Count > 0 && m.SpawnMs - sets[^1][0].Spawn < 1000) sets[^1].Add((tri, m.BNpcBaseId == TriangleEast, m.SpawnMs));
            else sets.Add([(tri, m.BNpcBaseId == TriangleEast, m.SpawnMs)]);
        }

        var fromDrops = drops.Select(d => d
            .Select(a => (a.Source.Track.TrySample(a.T, out var p, out _) ? p : a.SourcePos) - Centre)
            .Select(NearestCorner)
            .Distinct()
            .OrderBy(x => x.Triangle)
            .ToList()).ToList();

        string SetText(IEnumerable<(int Triangle, bool East)> s) =>
            string.Concat(s.OrderBy(x => x.Triangle).Select(x => x.Triangle == 0 ? $"O{(x.East ? "e" : "w")}" : Names[x.Triangle].ToString()));
        o.WriteLine($"   sets from the props: {(sets.Count == 0 ? "none logged" : string.Join("  ", sets.Select((s, i) => $"{i + 1}={SetText(s.Select(x => (x.Triangle, x.East)))} @{Local(s[0].Spawn):0.00}")))}");
        o.WriteLine($"   sets from the drops: {string.Join("  ", fromDrops.Select((s, i) => $"{i + 1}={SetText(s)}"))}");

        var party = r.Party.Where(p => p.IsPlayer).ToList();
        var drop3 = drops.Count >= 3 ? drops[2][0].T : t0 + 19900;
        foreach (var (label, ms) in new[] { ("at drop 3", drop3), ("at the busters", busters.Count > 0 ? busters[0].T : t0 + 20390), ("at Ultimate Embrace", embraceHit?.T ?? t0 + 27440) })
        {
            var parts = party.Select(p => (p, At(p, ms))).Where(x => x.Item2 is not null)
                .Select(x => $"{Who(x.p)}{(IsTank(x.p) ? "*" : "")} {Fmt(x.Item2!.Value)}{Inside(x.Item2!.Value)}");
            o.WriteLine($"   {label}: {string.Join("; ", parts)}");
        }

        for (var i = 0; i < busters.Count; i++)
        {
            var b = busters[i];
            var targets = b.Hits.Select(h => $"{Who(h.Target)} {Fmt(At(h.Target, b.T) ?? Vector2.Zero)} {h.Damage / 1000}k").ToList();
            o.WriteLine($"   buster {i + 1} ({(i == 0 ? "first helper" : "second helper")}): {string.Join(", ", targets)}");
        }
        var tanks = party.Where(IsTank).ToList();
        if (tanks.Count == 2 && busters.Count > 0 && At(tanks[0], busters[0].T) is { } t1 && At(tanks[1], busters[0].T) is { } t2)
            o.WriteLine($"   tanks {Vector2.Distance(t1, t2):0.0}y apart at the busters");

        foreach (var (d, k) in drops.Select((d, k) => (d, k)))
            foreach (var hit in d.SelectMany(a => a.Hits.Where(h => h.Target.IsPlayer).Select(h => (a, h))))
            {
                var corner = (hit.a.Source.Track.TrySample(hit.a.T, out var cp, out _) ? cp : hit.a.SourcePos) - Centre;
                o.WriteLine($"   drop {k + 1} hits {Who(hit.h.Target)} at {Fmt(At(hit.h.Target, hit.a.T) ?? Vector2.Zero)}, {Vector2.Distance(corner, At(hit.h.Target, hit.a.T) ?? corner):0.0}y from its corner, {hit.h.Damage / 1000}k");
            }
        if (wingsHit is not null)
            foreach (var h in wingsHit.Hits.Where(h => h.Target.IsPlayer))
                o.WriteLine($"   Wings hits {Who(h.Target)} at {Fmt(At(h.Target, wingsHit.T) ?? Vector2.Zero)}, {h.Damage / 1000}k");
        foreach (var death in r.Deaths.Where(d => d.T >= t0 - 14000 && d.T <= t0 + 30000))
            o.WriteLine($"   {Who(death.Victim)} dies at {Local(death.T):0.00} ({death.KillingBlow?.Action.Name ?? death.Cause})");

        var propsSets = sets.Select(s => s.Select(x => (x.Triangle, x.East)).ToList()).ToList();
        var best = propsSets.Count == 3 ? propsSets : fromDrops;
        var centre = best.SelectMany(s => s).Where(x => x.Triangle == 0).Select(x => x.East ? "E" : "W").FirstOrDefault() ?? "?";
        var partyTri = TriangleUnder(party.Where(p => !IsTank(p)).Select(p => At(p, drop3)).OfType<Vector2>().ToList());
        o.WriteLine($"SUMMARY set1={(best.Count > 0 ? SetText(best[0].Where(x => x.Triangle != 0)) : "?")} set2={(best.Count > 1 ? SetText(best[1].Where(x => x.Triangle != 0)) : "?")} set3={(best.Count > 2 ? SetText(best[2].Where(x => x.Triangle != 0)) : "?")} O={centre} wings={(wings is null ? "?" : wings.ActionId == WingsRight ? "R" : "L")} party={partyTri} "
                    + $"tanks={string.Join(",", tanks.Select(t => $"{Who(t)}{Inside(At(t, drop3) ?? Vector2.Zero)}{Fmt(At(t, drop3) ?? Vector2.Zero)}"))} deaths={r.Deaths.Count(d => d.T >= t0 - 14000 && d.T <= t0 + 30000)} outcome={r.Summary.Outcome}");
        o.WriteLine();
        return true;
    }

    private static (int Triangle, bool East) NearestCorner(Vector2 p)
    {
        var best = (Triangle: 0, East: true);
        var bestD = float.MaxValue;
        for (var t = 0; t < 7; t++)
            foreach (var east in t == 0 ? new[] { true, false } : [PointsEast(t)])
                foreach (var c in Corners(t, east))
                {
                    var d = Vector2.Distance(c, p);
                    if (d < bestD) { bestD = d; best = (t, east); }
                }
        return best;
    }

    private static string Inside(Vector2 p)
    {
        var t = Enumerable.Range(1, 6).MinBy(i => Vector2.Distance(Centroids[i], p));
        return Vector2.Distance(Centroids[t], p) < 5f ? $"[{Names[t]}]" : "";
    }

    private static string TriangleUnder(List<Vector2> spots)
    {
        if (spots.Count == 0) return "?";
        var mean = spots.Aggregate(Vector2.Zero, (a, b) => a + b) / spots.Count;
        var t = Enumerable.Range(1, 6).MinBy(i => Vector2.Distance(Centroids[i], mean));
        return Vector2.Distance(Centroids[t], mean) < 6f ? Names[t].ToString() : $"none({Fmt(mean)})";
    }

    private static string Fmt(Vector2 p) => $"({p.X:0.0},{p.Y:0.0}) r{p.Length():0.0} b{Bearing(p):0}";

    private static float Bearing(Vector2 p) => Wrap360(MathF.Atan2(p.X, -p.Y) * 180f / MathF.PI);
    private static float HeadingBearing(float heading) => Wrap360(180f - heading * 180f / MathF.PI);
    private static float Wrap360(float deg) => ((deg % 360f) + 360f) % 360f;
    private static string Compass(float deg) => new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[(int)MathF.Round(Wrap360(deg) / 45f) % 8];
}
