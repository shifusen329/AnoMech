using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3BowelsOfAgony;
using AnoMech.Scenarios.Umad.P3LimitCut;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P3Full;

// Dancing Mad P3 from the bosses' arrival until they fall: Bowels of Agony, Limit Cut and Black Hole
// back to back on one clock, with one Kefka, Chaos and Exdeath, and whatever each mechanic leaves
// carrying into the next.
public sealed class UmadP3FullScenario : IMultiplayerReplayable
{
    public string Name => "Full phase";
    public IPhase Phase => UmadZone.P3;
    public float BgmSecondsAtStart => bowels.BgmSecondsAtStart;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public bool SupportsMitigationPractice => true;
    public IReadOnlyList<IScenarioAi> AiStrats { get; }
    public object SettingsOverrides { get; }
    public bool HasPerPlayerSettings => true;
    public IReadOnlyList<string> SettingsConflicts => [.. bowels.SettingsConflicts, .. limitCut.SettingsConflicts, .. blackHole.SettingsConflicts];
    public string? RunProgress => started.Count > 0 ? started[^1].Slice.RunProgress : null;

    // Limit Cut starts 8.0s before Umbra Smash's cast, Black Hole 0.98s before Earthquake's.
    internal const float LimitCutAt = 78.42f;
    internal const float BlackHoleAt = LimitCutAt + 54.82f;

    private readonly UmadP3BowelsOfAgonyScenario bowels = new();
    private readonly UmadP3LimitCutScenario limitCut = new();
    private readonly UmadP3BlackHoleScenario blackHole = new();
    // Each slice begun so far, with where it started on the run's clock.
    private readonly List<(IScenario Slice, float At)> started = [];
    private (UmadP3BowelsOfAgonyState Bowels, UmadP3LimitCutState LimitCut, UmadP3BlackHoleState BlackHole)? rolled;

    public UmadP3FullScenario()
    {
        AiStrats =
        [
            .. from b in Enumerable.Range(0, bowels.AiStrats.Count)
               from h in Enumerable.Range(0, blackHole.AiStrats.Count)
               select new UmadP3FullStrat(b, h, bowels.AiStrats[b].Name, blackHole.AiStrats[h].Name),
        ];
        SettingsOverrides = new UmadP3FullStateOverrides(bowels.SettingsOverrides, limitCut.SettingsOverrides, blackHole.SettingsOverrides);
    }

    public void Run(SimWorld world, int? selectedAi)
    {
        started.Clear();
        var strat = selectedAi is { } idx && idx < AiStrats.Count ? (UmadP3FullStrat)AiStrats[idx] : null;
        var bowelsState = bowels.Roll(world);
        var limitCutState = limitCut.Roll(world, bowelsState.WindSpot, bowelsState.Winds);
        var blackHoleState = blackHole.Roll(world);
        rolled = (bowelsState, limitCutState, blackHoleState);

        var cast = new UmadP3Cast { Chained = true };
        world.MitPractice.Begin(MitigationPlan(bowelsState.ShortElement),
            new Dictionary<MitSource, Func<SimCharacter?>>
            {
                [UmadMitigation.Chaos] = () => cast.Chaos,
                [UmadMitigation.Exdeath] = () => cast.Exdeath,
            });

        Start(bowels, 0f, () => bowels.Play(world, strat?.Bowels, bowelsState, cast));
        world.Events.Add(LimitCutAt, () => Start(limitCut, LimitCutAt, () => limitCut.Play(world, strat is null ? null : 0, limitCutState, cast)));
        world.Events.Add(BlackHoleAt, () => Start(blackHole, BlackHoleAt, () => blackHole.Play(world, strat?.BlackHole, blackHoleState, cast)));
    }

    internal static MitPlanData MitigationPlan(Element shortElement) => MitPlanChain.Of(
        (UmadP3BowelsOfAgonyMitigation.Plan(shortElement), 0f),
        (UmadP3LimitCutMitigation.Plan(), LimitCutAt),
        (UmadP3BlackHoleMitigation.Plan(), BlackHoleAt));

    private void Start(IScenario slice, float at, Action play)
    {
        started.Add((slice, at));
        play();
    }

    public void RunInstanceEvents(SimWorld world)
    {
        bowels.RunInstanceEvents(world);
        world.Events.Add(LimitCutAt, () => limitCut.RunInstanceEvents(world));
        world.Events.Add(BlackHoleAt, () => blackHole.RunInstanceEvents(world));
    }

    public void Tick(float delta, float elapsed)
    {
        foreach (var (slice, at) in started) slice.Tick(delta, elapsed - at);
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        foreach (var (slice, _) in started) slice.OnActionResolved(resolved);
    }

    public void OnStatusAdded(StatusAdded added)
    {
        foreach (var (slice, _) in started) slice.OnStatusAdded(added);
    }

    public void OnStatusLost(StatusLost lost)
    {
        foreach (var (slice, _) in started) slice.OnStatusLost(lost);
    }

    public void OnTetherEnded(TetherEnded ended)
    {
        foreach (var (slice, _) in started) slice.OnTetherEnded(ended);
    }

    public void DrawSettings()
    {
        DrawSlice("Bowels of Agony", bowels.DrawSettings);
        DrawSlice("Limit Cut (its corner and winds come from Bowels of Agony)", limitCut.DrawSettings);
        DrawSlice("Black Hole", blackHole.DrawSettings);
    }

    public void DrawPerPlayerSettings()
    {
        DrawSlice("Bowels of Agony##perplayer", bowels.DrawPerPlayerSettings);
        DrawSlice("Limit Cut (its winds come from Bowels of Agony)##perplayer", limitCut.DrawPerPlayerSettings);
        DrawSlice("Black Hole##perplayer", blackHole.DrawPerPlayerSettings);
    }

    public void DrawMultiplayerSettings()
    {
        bowels.DrawMultiplayerSettings();
        limitCut.DrawMultiplayerSettings();
        blackHole.DrawMultiplayerSettings();
    }

    private static void DrawSlice(string label, Action draw)
    {
        if (ImGui.CollapsingHeader(label)) draw();
    }

    public MpMessage? BuildReplayStateMessage()
        => rolled is var (b, l, h)
            ? new UmadP3FullAiReplayStateMessage(UmadP3BowelsOfAgonyScenario.ReplayMessage(b), UmadP3LimitCutScenario.ReplayMessage(l),
                UmadP3BlackHoleScenario.ReplayMessage(h))
            : null;

    // Each slice's bots start with it, as on the host.
    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP3FullAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        var strat = (UmadP3FullStrat)AiStrats[aiIndex];
        var shadow = new Shadow { Bowels = bowels.StartReplay(msg.Bowels, strat.Bowels, myRole, replayWorld) };
        replayWorld.Events.Add(LimitCutAt, () => shadow.LimitCut = limitCut.StartReplay(msg.LimitCut, 0, myRole, replayWorld));
        replayWorld.Events.Add(BlackHoleAt, () => shadow.BlackHole = blackHole.StartReplay(msg.BlackHole, strat.BlackHole, myRole, replayWorld));
        return shadow;
    }

    public void RefreshLiveHandles(object shadowState, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowState is not Shadow shadow) return;
        if (shadow.Bowels is { } b) bowels.RefreshLiveHandles(b, peerEnemies);
        if (shadow.LimitCut is { } l) limitCut.RefreshLiveHandles(l, peerEnemies);
        if (shadow.BlackHole is { } h) blackHole.RefreshLiveHandles(h, peerEnemies);
    }

    public void RebuildPeerObstacles(ObstacleField obstacles, IReadOnlyDictionary<int, SimEnemy> peerEnemies,
        IReadOnlyDictionary<int, SimEventObject> peerEventObjects, SimCharacter? localPlayer)
        => blackHole.RebuildPeerObstacles(obstacles, peerEnemies, peerEventObjects, localPlayer);

    private sealed class Shadow
    {
        public object? Bowels { get; set; }
        public object? LimitCut { get; set; }
        public object? BlackHole { get; set; }
    }
}
