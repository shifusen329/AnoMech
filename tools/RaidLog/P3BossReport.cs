using System.Numerics;
using RaidReplay.Core.GameData;
using RaidReplay.Core.Model;

namespace AnoMech.RaidLog;

// DMU P3's bosses on each P3 scenario's clock, in scenario-local coordinates (log position minus
// the arena centre; north is -Z). "w" bearings are clockwise from the wind crystal's bearing,
// the corner Bowels of Agony and Limit Cut hold the bosses on; in Black Hole, "k" bearings are
// clockwise from Kefka's side of the arena (relative north), opposite the way he faces.
internal static class P3BossReport
{
    private const uint ChaosBase = 19508;
    private const uint ExdeathBase = 19509;
    private const uint KefkaBase = 19504;
    private const uint WindCrystal = 2015292;
    private const uint FireCrystal = 2015290;
    private const uint WaterCrystal = 2015291;
    private const uint AutoChaos = 0xC252;
    private const uint AutoExdeath = 0xC250;
    private static readonly Vector2 Centre = new(100f, 100f);

    private sealed record Window(string Name, uint Anchor, float AnchorAt, float From, float To, uint[] Resolves, bool KefkaFrame = false);

    private static readonly Window[] Windows =
    [
        new("Bowels of Agony", 0xBAF2, 25.0f, 0f, 78.35f, [0xBB0C, 0xBAFF, 0xBB12, 0xBAF4, 0xBAF5]),
        new("Limit Cut", 0xBB00, 8.0f, 0f, 52.7f, [0xBB00, 0xBB0C, 0xBB13]),
        new("Black Hole", 0xBAFB, 22.18f, 0f, 165f, [0xBB0C, 0xBAFF, 0xBB01, 0xBB03, 0xBB06], KefkaFrame: true),
    ];

    public static bool Print(PullReplay r, TextWriter o, float every)
    {
        var chaos = r.Actors.Where(a => a.BNpcBaseId == ChaosBase).ToList();
        var exdeath = r.Actors.Where(a => a.BNpcBaseId == ExdeathBase).ToList();
        var kefka = r.Actors.Where(a => a.BNpcBaseId == KefkaBase).ToList();
        if (chaos.Count == 0) return false;

        string Who(Actor? a) => a is null ? "?" : a.IsPlayer ? Jobs.Abbrev(a.Job) : a.DisplayName;
        var mt = r.Actions.Where(a => a.ActionId == AutoChaos).SelectMany(a => a.Hits.Select(h => h.Target)).GroupBy(t => t).MaxBy(g => g.Count())?.Key;
        var ot = r.Actions.Where(a => a.ActionId == AutoExdeath).SelectMany(a => a.Hits.Select(h => h.Target)).GroupBy(t => t).MaxBy(g => g.Count())?.Key;
        var wind = r.Actors.Where(a => a.BNpcBaseId == WindCrystal).Select(a => a.Track.TrySample(a.SpawnMs + 50, out var p, out _) ? p - Centre : (Vector2?)null).FirstOrDefault(p => p is not null);
        var fire = r.Actors.Where(a => a.BNpcBaseId == FireCrystal).Select(a => a.Track.TrySample(a.SpawnMs + 50, out var p, out _) ? p - Centre : (Vector2?)null).FirstOrDefault(p => p is not null);
        var water = r.Actors.Where(a => a.BNpcBaseId == WaterCrystal).Select(a => a.Track.TrySample(a.SpawnMs + 50, out var p, out _) ? p - Centre : (Vector2?)null).FirstOrDefault(p => p is not null);
        var windDeg = wind is { } w ? Bearing(w) : float.NaN;

        o.WriteLine($"== {Path.GetFileName(r.Summary.FilePath)} pull #{r.Summary.Ordinal} ({r.Summary.Outcome})");
        o.WriteLine($"   Chaos tank {Who(mt)}, Exdeath tank {Who(ot)}; crystals wind {Spot(wind)} fire {Spot(fire)} water {Spot(water)}");

        foreach (var win in Windows)
        {
            var anchor = r.Casts.FirstOrDefault(c => c.ActionId == win.Anchor);
            if (anchor is null) continue;
            float Local(int ms) => (ms - anchor.StartMs) / 1000f + win.AnchorAt;
            int Ms(float local) => anchor.StartMs + (int)MathF.Round((local - win.AnchorAt) * 1000f);
            var from = Ms(win.From);
            var to = Ms(win.To);

            var rows = new List<(int T, string Label)>();
            foreach (var c in r.Casts.Where(c => c.StartMs >= from && c.StartMs <= to && c.Source.BNpcBaseId is ChaosBase or ExdeathBase or KefkaBase))
                rows.Add((c.StartMs, $"{Who(c.Source)} casts {c.Name}"));
            foreach (var g in r.Actions.Where(a => a.T >= from && a.T <= to && win.Resolves.Contains(a.ActionId)).GroupBy(a => (a.ActionId, a.T / 150)))
            {
                var a = g.First();
                var hit = string.Join(",", g.SelectMany(x => x.Hits.Select(h => h.Target)).Distinct().Select(Who));
                rows.Add((a.T, $"{a.Name} hits {(hit.Length == 0 ? "nobody" : hit)}"));
            }
            foreach (var a in chaos.Concat(exdeath).Where(a => a.SpawnMs >= from && a.SpawnMs <= to))
                rows.Add((a.SpawnMs + 50, $"{Who(a)} spawns"));
            if (every > 0)
                for (var t = win.From; t <= win.To; t += every)
                    rows.Add((Ms(t), "."));

            // Kefka faces his first Slap Happy direction from 14.5s on; before that he is still turning in.
            var firstFacing = Ms(14.5f);
            (float Deg, char Tag) Frame(int t)
            {
                if (!win.KefkaFrame) return (windDeg, 'w');
                var k = Latest(kefka, Math.Max(t, firstFacing));
                return k is not null && k.Track.TrySample(Math.Max(t, firstFacing), out _, out var h) ? (Wrap360(HeadingBearing(h) + 180f), 'k') : (float.NaN, 'k');
            }

            o.WriteLine();
            o.WriteLine($"-- {win.Name} (anchor {anchor.Name} at {win.AnchorAt:0.00})");
            o.WriteLine($"   {"t",7}  {"",-40} {"Chaos",-34} {"Exdeath",-34} {"Kefka",-34} {$"Chaos tank {Who(mt)}",-34} Exdeath tank {Who(ot)}");
            foreach (var (t, label) in rows.OrderBy(x => x.T))
            {
                var frame = Frame(t);
                o.WriteLine($"   {Local(t),7:0.00}  {Trim(label, 40),-40} {Pos(chaos, t, frame),-34} {Pos(exdeath, t, frame),-34} {Pos(kefka, t, frame),-34} {Pos(mt, t, frame),-34} {Pos(ot, t, frame)}");
            }
        }

        o.WriteLine();
        return true;
    }

    private static Actor? Latest(List<Actor> actors, int t) =>
        actors.Where(x => x.IsPresent(t)).MaxBy(x => x.SpawnMs) ?? actors.LastOrDefault(x => x.SpawnMs <= t);

    private static string Pos(List<Actor> actors, int t, (float Deg, char Tag) frame) => Pos(Latest(actors, t), t, frame);

    private static string Pos(Actor? a, int t, (float Deg, char Tag) frame)
    {
        if (a is null || !a.Track.TrySample(t, out var p, out var h)) return "-";
        p -= Centre;
        var rel = float.IsNaN(frame.Deg) ? "" : $" {frame.Tag}{Wrap180(Bearing(p) - frame.Deg):+0;-0}";
        return $"({p.X,5:0.0},{p.Y,5:0.0}) r{p.Length(),4:0.0}{rel} f{Compass(HeadingBearing(h))}{(a.IsHidden(t) ? " hid" : "")}";
    }

    private static string Spot(Vector2? p) => p is { } v ? $"{Compass(Bearing(v))} ({v.X:0.0},{v.Y:0.0})" : "?";

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..n];

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
