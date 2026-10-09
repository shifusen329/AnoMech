using AnoMech.RaidLog;
using RaidReplay.Core.Encounters;
using RaidReplay.Core.Indexing;
using RaidReplay.Core.Loading;

const string Usage = """
    usage: raidlog pulls <log>
           raidlog blackhole <log> [--pull N]
    """;

if (args.Length < 2 || args[0] is not ("pulls" or "blackhole"))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var path = Path.GetFullPath(args[1]);
int? onlyPull = null;
for (var i = 2; i < args.Length - 1; i++)
    if (args[i] == "--pull")
        onlyPull = int.Parse(args[i + 1]);

var registry = EncounterRegistry.Load();
var index = IndexStore.IndexFile(path, null, new EncounterObserverFactory(registry), registry.HashFor);

if (args[0] == "pulls")
{
    Console.WriteLine($"{index.FileName}: {index.Pulls.Count} pulls");
    foreach (var p in index.Pulls)
        Console.WriteLine($"#{p.Ordinal,-3} {p.StartLocal:MM-dd HH:mm:ss} {p.DurationMs / 1000f,7:0.0}s {p.Outcome,-10} {p.ZoneName,-32} deaths={p.Deaths,-2} {p.FurthestPhase}");
    return 0;
}

var found = false;
foreach (var p in index.Pulls)
{
    if (onlyPull is { } n && p.Ordinal != n) continue;
    if (p.DurationMs < 60_000 && onlyPull is null) continue;
    var replay = PullLoader.Load(p, null, registry);
    found |= BlackHoleReport.Print(replay, Console.Out);
}

if (!found)
    Console.WriteLine($"{index.FileName}: no pull reaches Black Hole");
return 0;
