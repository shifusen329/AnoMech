using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P2Trines;

using Constants = UmadP2TrinesConstants;
using Geometry = UmadP2TrinesConstants.Geometry;

// Dancing Mad P2 from Light of Judgment II, where Forsaken ends, through Trine and both Wings of
// Destruction to Ultimate Embrace II. Light of Judgment II is cast at 2.00 and Trine at 15.21.
public sealed class UmadP2TrinesScenario : IMultiplayerReplayable
{
    public string Name => "Trines";
    public IPhase Phase => UmadZone.P2;
    public float BgmSecondsAtStart => Constants.BgmSecondsAtStart;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public bool SupportsMitigationPractice => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP2TrinesAi()];
    public void DrawSettings() => settingsWindow.Draw();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;

    private readonly UmadP2TrinesSettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private SimParty party = null!;
    private UmadP2TrinesState state = null!;
    private DamageSolver damage = null!;
    private readonly SimEnemy?[] pointHelpers = new SimEnemy?[9];
    private readonly SimEnemy?[] busterHelpers = new SimEnemy?[2];
    private SimEnemy? wingsHelper;
    private readonly SimEventObject?[] triangles = new SimEventObject?[7];
    private Vector3 wingsOrigin;

    public UmadP2TrinesState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP2TrinesState(world.Rng, settingsWindow.Overrides);
        LastState = state;
        damage = new DamageSolver(party);
        damage.SetStatuses(DamageType.Magic, StatusId.MagicVulnerabilityUp);
        Array.Clear(triangles);
        DiagnosticLog.Info(
            $"[UmadP2Trines] Roll: first {UmadP2TrinesState.Describe(state.FirstSet)}, second {Geometry.Name(state.SecondSet)}, third {UmadP2TrinesState.Describe(state.ThirdSet)}, "
            + $"centre points {(state.CentrePointsEast ? "east" : "west")}, Wings of Destruction {(state.WingsRight ? "right" : "left")}.");

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP2TrinesState>)AiStrats[idx]).Run(state, world);

        world.EnforceArenaBoundary(Geometry.ArenaRadius, "Fell off the arena");
        SpawnActors();
        world.MitPractice.Begin(UmadP2TrinesMitigation.Plan(), new Dictionary<MitSource, Func<SimCharacter?>>
        {
            [UmadMitigation.Kefka] = () => state.Objects.Kefka,
        });

        var kefka = state.Objects;
        foreach (var at in new[] { 0.87f, 8.93f, 11.96f, 14.99f, 21.05f, 28.10f, 31.13f, 43.21f })
            world.Events.Add(at, () => kefka.Kefka?.Cast(ActionId.AutoAttack1, castSeconds: 0f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId, animationLock: Constants.AnimationLock.AutoAttack));

        world.Events.Add(2.00f, () => kefka.Kefka?.Cast(ActionId.LightOfJudgment, castSeconds: 4.7f, fireDelay: 0.27f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(6.97f, () => world.MitPractice.HitParty(UmadP2TrinesMitigation.LightOfJudgment));
        world.Events.Add(15.21f, () => kefka.Kefka?.Cast(ActionId.Trine, castSeconds: 2.7f, fireDelay: 0.29f, animationLock: Constants.AnimationLock.BossCast));

        world.Events.Add(18.77f, () => SpawnTriangles(drop: 0));
        world.Events.Add(20.47f, () => Animate(drop: 0, Constants.TriangleFliesDown));
        world.Events.Add(20.78f, () => SpawnTriangles(drop: 1));
        world.Events.Add(21.36f, StartWings);
        world.Events.Add(21.99f, () => Animate(drop: 0, Constants.TriangleLands));
        world.Events.Add(22.48f, () => Animate(drop: 1, Constants.TriangleFliesDown));
        world.Events.Add(22.78f, () => SpawnTriangles(drop: 2));
        world.Events.Add(24.00f, () => Animate(drop: 1, Constants.TriangleLands));
        world.Events.Add(24.49f, () => Animate(drop: 2, Constants.TriangleFliesDown));
        world.Events.Add(25.33f, ResolveWings);
        world.Events.Add(26.00f, () => Animate(drop: 2, Constants.TriangleLands));
        world.Events.Add(28.41f, () => kefka.Kefka?.ActorControl(Constants.WingGlowCategory, 0, Constants.WingGlowOff));

        world.Events.Add(30.99f, () => PlaceHelpersOnCorners(drop: 0));
        world.Events.Add(31.00f, () => Animate(drop: 0, Constants.TriangleExplodes));
        world.Events.Add(31.09f, () => DropTriangles(drop: 0));
        world.Events.Add(31.54f, StartBusters);
        world.Events.Add(31.84f, () => DespawnTriangles(drop: 0));
        world.Events.Add(33.02f, () => PlaceHelpersOnCorners(drop: 1));
        world.Events.Add(33.03f, () => Animate(drop: 1, Constants.TriangleExplodes));
        world.Events.Add(33.12f, () => DropTriangles(drop: 1));
        world.Events.Add(33.87f, () => DespawnTriangles(drop: 1));
        world.Events.Add(35.01f, () => PlaceHelpersOnCorners(drop: 2));
        world.Events.Add(35.02f, () => Animate(drop: 2, Constants.TriangleExplodes));
        world.Events.Add(35.11f, () => DropTriangles(drop: 2));
        world.Events.Add(35.60f, ResolveBusters);
        world.Events.Add(35.86f, () => DespawnTriangles(drop: 2));

        world.Events.Add(37.55f, () => kefka.Kefka?.ActorControl(Constants.WingGlowCategory, 0, Constants.WingGlowOff));
        world.Events.Add(37.67f, StartUltimateEmbrace);
        world.Events.Add(42.65f, ResolveUltimateEmbrace);
        world.Events.Add(44.00f, () => DiagnosticLog.Info("[UmadP2Trines] End: Ultimate Embrace II has landed."));
    }

    // Scheduled by host and peer alike, so broadcast: false.
    public void RunInstanceEvents(SimWorld instanceWorld)
        => instanceWorld.Events.Add(8.71f, () => instanceWorld.Map.AddEffect(packetFlags: Constants.MapEffectFlags, index: Constants.MapEffectIndex, broadcast: false));

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new UmadP2TrinesAiReplayStateMessage(s.FirstSet.ToArray(), s.SecondSet, s.CentrePointsEast, s.WingsRight)
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP2TrinesAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (UmadP2TrinesState.FromNetworkReplay(msg.FirstSet, msg.SecondSet, msg.CentrePointsEast, msg.WingsRight) is not { } shadowState) return null;
        ((IScenarioAi<UmadP2TrinesState>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }

    public void RefreshLiveHandles(object shadowStateObj, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowStateObj is not UmadP2TrinesState shadow) return;
        shadow.Objects.Kefka ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.GodKefka);
    }

    private void SpawnActors()
    {
        state.Objects.Kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GodKefka, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, IsVisible: true,
            Placement: new Placement(Vector3.Zero, Geometry.KefkaFacingNorth)));
        for (var i = 0; i < pointHelpers.Length; i++) pointHelpers[i] = Helper();
        for (var i = 0; i < busterHelpers.Length; i++) busterHelpers[i] = Helper();
        wingsHelper = Helper();
    }

    private SimEnemy? Helper() => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.KefkaHelper, NameId: BNpcNameId.Kefka, Level: 1,
        Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
        Placement: new Placement(Vector3.Zero, 0f)));

    // Spawned like the real ones and like the P1 statues, which share their Invisibility=7 rows
    // and do render.
    private void SpawnTriangles(int drop)
    {
        foreach (var t in state.Set(drop))
            triangles[t] = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = state.PointsEast(t) ? EObjId.TriangleEast : EObjId.TriangleWest,
                Placement = new Placement(Geometry.Centroids[t], 0f),
                EventId = EObjId.PropEventId,
                TargetableStatus = 5,
                Arg2 = ((Constants.TriangleArg2Serial + (uint)t) << 16) | 0x3U,
                TimelineState = Constants.TriangleSpawnState,
            });
    }

    private void Animate(int drop, (uint State, uint Bitmask) beat)
    {
        foreach (var t in state.Set(drop))
            triangles[t]?.PlayBeat(beat.State, beat.Bitmask, PropBeatMode.ActorControl);
    }

    private void DespawnTriangles(int drop)
    {
        foreach (var t in state.Set(drop))
        {
            triangles[t]?.Despawn();
            triangles[t] = null;
        }
    }

    // Kefka faces north, so the struck half is the one the cast names.
    private void StartWings()
    {
        if (state.Objects.Kefka is not { } kefka) return;
        var heading = kefka.Rotation;
        var forward = new Vector3(MathF.Sin(heading), 0f, MathF.Cos(heading));
        var right = new Vector3(-MathF.Cos(heading), 0f, MathF.Sin(heading));
        wingsOrigin = kefka.Position + right * ((state.WingsRight ? 1f : -1f) * Geometry.WingsSideOffset) - forward * Geometry.WingsBehindOffset;
        wingsHelper?.SetPosition(new Placement(wingsOrigin, heading));
        kefka.ActorControl(Constants.WingGlowCategory, 0, state.WingsRight ? Constants.WingGlowRight : Constants.WingGlowLeft);
        kefka.Cast(state.WingsRight ? ActionId.WingsOfDestructionRight : ActionId.WingsOfDestructionLeft, targetLocation: wingsOrigin,
            castSeconds: 3.7f, fireDelay: 0.27f, animationLock: Constants.AnimationLock.BossCast);
    }

    private void ResolveWings()
        => damage.Resolve(wingsHelper, state.WingsRight ? ActionId.WingsOfDestructionRight : ActionId.WingsOfDestructionLeft, [DamageType.Lethal], []);

    private void PlaceHelpersOnCorners(int drop)
    {
        var corners = state.Set(drop).SelectMany(state.CornersOf).ToList();
        for (var i = 0; i < corners.Count && i < pointHelpers.Length; i++)
            pointHelpers[i]?.SetPosition(new Placement(corners[i], 0f));
    }

    // A corner circle leaves Damage Down; two at once kill.
    private void DropTriangles(int drop)
    {
        var corners = state.Set(drop).SelectMany(state.CornersOf).Count();
        var struck = new Dictionary<SimCharacter, int>();
        for (var i = 0; i < corners && i < pointHelpers.Length; i++)
        {
            if (pointHelpers[i] is not { } helper) continue;
            helper.Cast(ActionId.TrinePoint, castSeconds: 0f, animationLock: Constants.AnimationLock.Instant);
            var hit = damage.Resolve(helper, ActionId.TrinePoint, [DamageType.Magic], [(StatusId.DamageDown, Constants.Damage.DamageDownSeconds)]);
            world.MitPractice.Hit(hit, UmadP2TrinesMitigation.TrinePoint);
            foreach (var member in hit) struck[member] = struck.GetValueOrDefault(member) + 1;
        }
        foreach (var (member, count) in struck)
            if (count > 1 && member.IsAlive())
                member.Die($"Died to Trine ({count} corners at once)");
    }

    private void StartBusters()
    {
        if (state.Objects.Kefka is not { } kefka) return;
        kefka.Face(party.Get(PartyRole.MainTank));
        kefka.ActorControl(Constants.WingGlowCategory, 0, Constants.WingGlowBoth);
        kefka.Cast(ActionId.WingsOfDestruction, castSeconds: 3.7f, fireDelay: 0.27f, animationLock: Constants.AnimationLock.BossCast);
    }

    // The farthest player first, then the nearest. A buster on a non-tank kills, and so does the
    // second on a tank still carrying the first's vulnerability.
    private void ResolveBusters()
    {
        if (state.Objects.Kefka is not { } kefka) return;
        var targets = new[] { party.Find.Farest(kefka.Position), party.Find.Closest(kefka.Position) };
        DiagnosticLog.Info($"[UmadP2Trines] Wings of Destruction busters: far {(targets[0] as ISimPartyMember)?.Role}, near {(targets[1] as ISimPartyMember)?.Role}.");
        for (var i = 0; i < targets.Length; i++)
        {
            if (targets[i] is not { } target) continue;
            busterHelpers[i]?.Cast(ActionId.WingsOfDestructionBuster, castSeconds: 0f, targetId: target.GameObjectId, animationLock: Constants.AnimationLock.Instant);
            var hit = damage.Resolve(target, ActionId.WingsOfDestructionBuster, [DamageType.TankBuster, DamageType.Magic],
                [(StatusId.MagicVulnerabilityUp, Constants.Damage.MagicVulnerabilityUpSeconds)],
                requiredMitigation: Constants.Damage.WingsBusterRequiredMitigation);
            world.MitPractice.Hit(hit, UmadP2TrinesMitigation.WingsBuster);
        }
    }

    private void StartUltimateEmbrace()
    {
        if (state.Objects.Kefka is not { } kefka || party.Get(PartyRole.MainTank) is not { } mainTank) return;
        mainTank.AttachLockonVfx(LockonId.UltimateEmbrace, duration: 4.98f, persistent: false);
        kefka.Cast(ActionId.UltimateEmbrace, castSeconds: 4.7f, fireDelay: 0.28f, targetId: mainTank.GameObjectId, animationLock: Constants.AnimationLock.BossCast);
    }

    // A 5y stack on the main tank for both tanks; one tank alone needs an invuln.
    private void ResolveUltimateEmbrace()
    {
        if (party.Get(PartyRole.MainTank) is not { } mainTank || !mainTank.IsAlive()) return;
        var hit = damage.Resolve(mainTank, ActionId.UltimateEmbrace, [DamageType.TankBuster, DamageType.Magic], [],
            stackMinTargets: 2, requiredMitigation: Constants.Damage.UltimateEmbraceRequiredMitigation,
            understackedTankMitigation: Constants.Damage.UltimateEmbraceSoloTankMitigation);
        world.MitPractice.Hit(hit, UmadP2TrinesMitigation.UltimateEmbrace);
    }
}
