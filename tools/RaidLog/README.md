# raidlog

A developer tool that reads ACT network logs (`Network_*.log`) with raid-replay's parser and reports what a scenario needs, on AnoMech's scenario clock and in scenario-local coordinates. It is not part of `AnoMech.sln`, so the plugin's build and tests don't touch it.

```
dotnet run --project tools/RaidLog -- pulls docs/logs/Network_30301_20261005.log
dotnet run --project tools/RaidLog -- blackhole docs/logs/Network_30301_20261005.log [--pull N]
dotnet run --project tools/RaidLog -- p3bosses docs/logs/Network_30301_20261005.log [--pull N] [--every SECONDS]
```

- `pulls` lists the pulls in a log.
- `blackhole` reports every DMU pull that reaches P3 Black Hole, wave by wave. Times count from the scenario's start, with Exdeath's Black Hole cast at 22.18 s. Positions are the log's minus the arena centre (100, 100), so north is -Z. The report covers:
  - Kefka's side of the arena (relative north), which is opposite the way he faces when he casts.
  - The active black holes, as single, pair or set of three.
  - Every tether pass.
  - Each Nothingness: who it hit, the Unbecoming / Meanest Existence / crust result, and the nearest bystander to the beam.
  - Anyone who held two tethers at once.
  - Exdeath's position at each Thunder III.

The log writes some tether lines late, so a hole can appear to fire before its first tether line.

- `p3bosses` reports where Chaos, Exdeath, Kefka and both tanks stand in every DMU pull that reaches P3, on each P3 scenario's clock:
  - Bowels of Agony counts from its start, with the Bowels of Agony cast at 25.00.
  - Limit Cut counts from its start, with Umbra Smash at 8.00.
  - Black Hole counts from its start, with the Black Hole cast at 22.18.

  There is a row at every boss cast and at the hits that matter (Thunder III, Shockwave, the crystals, Umbra Smash, Vacuum Wave, Damning Edict, Knock Down, Big Bang). `--every 2` adds a row every 2 s. Positions are the log's minus the arena centre. In Bowels of Agony and Limit Cut, `w` is the bearing clockwise from the wind crystal, the corner both scenarios hold the bosses on. In Black Hole, `k` is the bearing clockwise from Kefka's side of the arena.

## `RaidReplay.Core/` is a copy

`RaidReplay.Core/` is raid-replay's `RaidReplay.Core` library at commit `d0c49db` (0.0.9), copied verbatim without its csproj. Don't edit it here. To update it, delete the folder and copy it again from a raid-replay checkout:

```
git -C ../raid-replay archive <commit> RaidReplay.Core | tar -x -C tools/RaidLog && rm tools/RaidLog/RaidReplay.Core/RaidReplay.Core.csproj
```

Then update the commit above. Both projects are AGPL-3.0.
