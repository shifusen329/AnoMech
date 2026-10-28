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
using AnoMech.Scenarios.Umad.P3LimitCut;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

using Constants = UmadP3BowelsOfAgonyConstants;
using Geometry = UmadP3BowelsOfAgonyConstants.Geometry;

// Dancing Mad P3 from the bosses' arrival to Limit Cut. Chaos starts casting Bowels of Agony at
// 25.0; the scenario ends 53.35s after that, where Limit Cut's clock starts.
public sealed class UmadP3BowelsOfAgonyScenario : IMultiplayerReplayable
{
    public string Name => "Bowels of Agony";
    public IPhase Phase => UmadZone.P3;
    public float BgmSecondsAtStart => Constants.BgmSecondsAtStart;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public bool SupportsMitigationPractice => true;
    public IReadOnlyList<IScenarioAi> AiStrats =>
    [
        new UmadP3BowelsOfAgonyAi(UmadP3BowelsOfAgonyAi.Strat.Lb3),
        new UmadP3BowelsOfAgonyAi(UmadP3BowelsOfAgonyAi.Strat.Sg3k),
    ];
    public void DrawSettings() => settingsWindow.Draw();
    public bool HasPerPlayerSettings => true;
    public void DrawPerPlayerSettings() => settingsWindow.DrawPerPlayer();
    public object SettingsOverrides => settingsWindow.Overrides;
    public IReadOnlyList<string> SettingsConflicts => settingsWindow.Overrides.Validate().Problems;
    public void DrawMultiplayerSettings() => settingsWindow.DrawThunderIIIPlan();

    private readonly UmadP3BowelsOfAgonySettingsWindow settingsWindow = new();
    private SimWorld world = null!;
    private SimParty party = null!;
    private UmadP3BowelsOfAgonyState state = null!;
    private readonly SimEnemy?[] elementHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] crystalHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] shockwaveHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] cycloneHelpers = new SimEnemy?[4];
    private SimEnemy? thunderHelper;
    private SimEnemy? gravenImage;
    private SimEventObject? waterCrystal;
    private SimEventObject? fireCrystal;
    // Both of a crystal's shots land on one frame; the Cyclones that follow answer every wind they took.
    private int windsTaken;

    public UmadP3BowelsOfAgonyState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP3BowelsOfAgonyState(world.Rng, party, settingsWindow.Overrides);
        LastState = state;
        windsTaken = 0;
        DiagnosticLog.Info(
            $"[UmadP3BowelsOfAgony] Roll: wind {Geometry.SpotName(state.WindSpot)}, water {Geometry.SpotName(state.WaterSpot)}, fire {Geometry.SpotName(state.FireSpot)}, "
            + $"short {state.ShortElement}, Dynamic Fluid {string.Join("+", state.WaterHolders)}, Entropy {string.Join("+", state.FireHolders)}, "
            + $"{(state.Latitudinal ? "Latitudinal" : "Longitudinal")}, Thunder III {state.ThunderPlan}, winds "
            + string.Join(" ", state.Winds.Select(kv => $"{kv.Key}={kv.Value}")) + ".");

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP3BowelsOfAgonyState>)AiStrats[idx]).Run(state, world);

        world.EnforceArenaBoundary(Geometry.ArenaRadius, "Fell off the arena");
        SpawnActors();
        world.MitPractice.Begin(UmadP3BowelsOfAgonyMitigation.Plan(state.ShortElement), new Dictionary<MitSource, Func<SimCharacter?>>
        {
            [UmadMitigation.Chaos] = () => state.Objects.Chaos,
            [UmadMitigation.Exdeath] = () => state.Objects.Exdeath,
        });

        var objects = state.Objects;
        world.Events.Add(0.40f, () => objects.Kefka?.Cast(ActionId.DefinitionOfInsanity, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(5.40f, () =>
        {
            objects.Chaos?.PlayActionTimeline(ActionTimelineId.WarpEnd);
            objects.Exdeath?.PlayActionTimeline(ActionTimelineId.WarpEnd);
        });
        world.Events.Add(7.45f, () =>
        {
            objects.Chaos?.SetTargetable(true);
            objects.Exdeath?.SetTargetable(true);
        });
        world.Events.Add(7.54f, () => objects.Kefka?.PlayActionTimeline(ActionTimelineId.WarpStart));
        world.Events.Add(7.58f, () => objects.Chaos?.Cast(ActionId.DecisiveBattle_Chaos, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(7.63f, () => objects.Exdeath?.Cast(ActionId.DecisiveBattle_Exdeath, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(9.58f, () => objects.Kefka?.SetPosition(new Placement(Geometry.KefkaPerch, 0f)));
        world.Events.Add(9.67f, () => objects.Kefka?.PlayActionTimeline(ActionTimelineId.WarpEnd));
        world.Events.Add(10.56f, ResolveDecisiveBattle);
        world.Events.Add(10.80f, () =>
        {
            objects.Chaos?.Follow(party.Get(PartyRole.MainTank));
            objects.Exdeath?.Follow(party.Get(PartyRole.OffTank));
        });
        world.Events.Add(11.81f, () => objects.Kefka?.Cast(ActionId.KefkaRest, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(15.60f, () => objects.Kefka?.SetModelState(0x04));

        foreach (var at in new[] { 14.71f, 17.74f, 20.77f, 23.80f, 31.87f, 34.90f, 37.93f, 40.96f, 43.99f, 47.02f, 50.05f, 53.08f, 56.11f, 59.14f, 62.17f })
            world.Events.Add(at, () => objects.Chaos?.Cast(UmadActions.AutoAttack, party.Get(PartyRole.MainTank)));
        foreach (var at in new[] { 13.73f, 16.76f, 19.79f, 22.82f, 25.85f, 28.88f, 31.91f, 34.94f, 37.97f, 41.00f, 64.45f, 67.47f, 70.50f, 73.54f, 76.56f })
            world.Events.Add(at, () => objects.Exdeath?.Cast(UmadActions.ExdeathAutoAttack, party.Get(PartyRole.OffTank)));

        world.Events.Add(24.80f, () => objects.Chaos?.Follow());
        world.Events.Add(25.00f, () => objects.Chaos?.Cast(UmadActions.BowelsOfAgony));
        world.Events.Add(29.98f, ApplyWindsAndElements);
        world.Events.Add(30.20f, () => objects.Chaos?.Follow(party.Get(PartyRole.MainTank)));
        world.Events.Add(30.37f, SpawnCrystals);

        world.Events.Add(42.00f, () =>
        {
            objects.Chaos?.Follow();
            objects.Exdeath?.Follow();
        });
        world.Events.Add(42.07f, RemoveHeroes);
        world.Events.Add(42.21f, () =>
        {
            objects.Exdeath?.Cast(UmadActions.ThunderIIIAoe);
            if (gravenImage != null) world.Tether(objects.Exdeath, gravenImage, TetherId.ExdeathToGravenImage, duration: 6.99f);
        });
        world.Events.Add(49.03f, () => PlaceElements(state.ShortElement));
        world.Events.Add(49.11f, () => CastElements(state.ShortElement));
        world.Events.Add(50.14f, () => ShootCrystal(state.ShortElement));
        world.Events.Add(50.83f, () => CrystalOf(state.ShortElement)?.FadeOut());
        world.Events.Add(50.14f + Constants.CycloneDelay, FireCyclones);

        world.Events.Add(53.35f, () => objects.Exdeath?.Cast(ActionId.ThunderIII_Cast, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(58.43f, ResolveThunder);
        world.Events.Add(61.46f, ResolveThunder);
        world.Events.Add(62.00f, () => objects.Exdeath?.Follow(party.Get(PartyRole.OffTank)));
        world.Events.Add(63.50f, () => objects.Chaos?.Follow(party.Get(PartyRole.MainTank)));
        world.Events.Add(64.05f, () => objects.Kefka?.Cast(ActionId.KefkaUnrest, animationLock: Constants.AnimationLock.Instant));
        world.Events.Add(64.85f, () => objects.Kefka?.SetModelState(0x00));
        // Neither boss moves again before Limit Cut: the off-tank's Implosion dodge stays in
        // Exdeath's reach.
        world.Events.Add(66.03f, () =>
        {
            objects.Chaos?.Follow();
            objects.Exdeath?.Follow();
        });
        world.Events.Add(66.14f, () => objects.Kefka?.Cast(ActionId.Trance, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(66.23f, () => objects.Chaos?.Cast(state.Latitudinal ? ActionId.LatitudinalImplosion : ActionId.LongitudinalImplosion,
            animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(70.19f, () => objects.Kefka?.AddStatusParam(StatusId.KefkaTrance, 0x1FF));
        world.Events.Add(71.84f, () => AimShockwaves(sides: state.Latitudinal));
        world.Events.Add(71.93f, FireShockwaves);
        world.Events.Add(73.85f, () => AimShockwaves(sides: !state.Latitudinal));
        world.Events.Add(73.94f, FireShockwaves);

        world.Events.Add(76.03f, () => PlaceElements(state.LongElement));
        world.Events.Add(76.12f, () => CastElements(state.LongElement));
        world.Events.Add(77.14f, () => ShootCrystal(state.LongElement));
        world.Events.Add(77.99f, () => CrystalOf(state.LongElement)?.FadeOut());
        world.Events.Add(77.14f + Constants.CycloneDelay, FireCyclones);
        world.Events.Add(78.35f, () => DiagnosticLog.Info("[UmadP3BowelsOfAgony] End: Limit Cut's clock starts here."));
    }

    public void RunInstanceEvents(SimWorld instanceWorld)
    {
        foreach (var (at, arg) in new[] { (0.32f, 0x14U), (7.45f, 0x15U), (9.58f, 0x16U), (71.40f, 0x17U) })
            instanceWorld.Events.Add(at, () => instanceWorld.Map.DirectorUpdate(Constants.DirectorCategory, arg, 0x2U, Constants.DirectorArg3, Constants.DirectorKefkaId));
        instanceWorld.Events.Add(41.94f, () => instanceWorld.Map.AddEffect(packetFlags: 0x00020001U, index: 0x22));
        instanceWorld.Events.Add(53.26f, () => instanceWorld.Map.AddEffect(packetFlags: 0x00080004U, index: 0x22));
    }

    public void OnActionResolved(ActionResolved resolved)
    {
        if (resolved.Action.ActionId is not (ActionId.Tsunami_P3 or ActionId.Inferno_P3)) return;
        windsTaken += resolved.Hits.Except(resolved.Killed)
            .Count(m => m != resolved.Target && (m.HasStatus(StatusId.Headwind) || m.HasStatus(StatusId.Tailwind)));
    }

    public MpMessage? BuildReplayStateMessage()
        => LastState is { } s
            ? new UmadP3BowelsOfAgonyAiReplayStateMessage(s.WindSpot, s.WaterSpot, s.ShortElement, s.WaterHolders.ToArray(), s.FireHolders.ToArray(),
                s.Winds.Where(kv => kv.Value == Wind.Headwind).Select(kv => kv.Key).ToArray(), s.Latitudinal, s.ThunderPlan)
            : null;

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP3BowelsOfAgonyAiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (UmadP3BowelsOfAgonyState.FromNetworkReplay(msg.WindSpot, msg.WaterSpot, msg.ShortElement, msg.WaterHolders, msg.FireHolders,
                msg.Headwinds, msg.Latitudinal, msg.ThunderPlan) is not { } shadowState) return null;
        ((IScenarioAi<UmadP3BowelsOfAgonyState>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }

    public void RefreshLiveHandles(object shadowStateObj, IReadOnlyDictionary<int, SimEnemy> peerEnemies)
    {
        if (shadowStateObj is not UmadP3BowelsOfAgonyState shadow) return;
        shadow.Objects.Chaos ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.ChaosP3);
        shadow.Objects.Exdeath ??= peerEnemies.Values.FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.Exdeath);
    }

    private void SpawnActors()
    {
        var objects = state.Objects;
        objects.Kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.KefkaP3, NameId: BNpcNameId.Kefka, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            LayoutId: BNpcLayoutId.KefkaP3, Placement: new Placement(Vector3.Zero, 0f)));
        objects.Chaos = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.ChaosP3, NameId: BNpcNameId.Chaos, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.HiddenUntilShown,
            LayoutId: BNpcLayoutId.ChaosP3, Placement: new Placement(Geometry.ChaosArrival, 0f)));
        objects.Exdeath = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Exdeath, NameId: BNpcNameId.Exdeath, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.HiddenUntilShown,
            LayoutId: BNpcLayoutId.Exdeath, Placement: new Placement(Geometry.ExdeathArrival, 0f)));
        gravenImage = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            LayoutId: BNpcLayoutId.GravenImage, Placement: new Placement(Geometry.GravenImage, 0f)));
        var wind = Geometry.Crystal(state.WindSpot);
        for (var i = 0; i < 2; i++)
        {
            elementHelpers[i] = Helper(BNpcNameId.Chaos);
            crystalHelpers[i] = Helper(BNpcNameId.Chaos);
            shockwaveHelpers[i] = Helper(BNpcNameId.Chaos);
        }
        // Cyclone's caster-side VFX needs a real skeleton, as in Limit Cut.
        for (var i = 0; i < cycloneHelpers.Length; i++)
            cycloneHelpers[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Chaos, NameId: BNpcNameId.Chaos, Level: 1,
                Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.HiddenUntilShown,
                Placement: new Placement(wind, 0f)));
        thunderHelper = Helper(BNpcNameId.Exdeath);
    }

    private SimEnemy? Helper(uint nameId) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.KefkaHelper, NameId: nameId, Level: 1,
        Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
        Placement: new Placement(Vector3.Zero, 0f)));

    // The wind crystal stays until Limit Cut fires its Cyclones from it.
    private void SpawnCrystals()
    {
        waterCrystal = SpawnCrystal(EObjId.WaterCrystal, state.WaterSpot);
        fireCrystal = SpawnCrystal(EObjId.FireCrystal, state.FireSpot);
        SpawnCrystal(EObjId.WindCrystal, state.WindSpot);
    }

    private SimEventObject? SpawnCrystal(uint eobjId, int spot) => world.SpawnEventObject(new EventObjectSpawnConfig
    {
        EObjId = eobjId,
        TargetableStatus = 5,
        Placement = new Placement(Geometry.Crystal(spot), Geometry.CrystalRotation(spot)),
    });

    private SimEventObject? CrystalOf(Element element) => element == Element.Water ? waterCrystal : fireCrystal;

    // The four nearest Chaos side with him; the rest with Exdeath.
    private void ResolveDecisiveBattle()
    {
        if (state.Objects.Chaos is not { } chaos || state.Objects.Exdeath is not { } exdeath) return;
        chaos.AddStatus(StatusId.EpicVillain);
        exdeath.AddStatus(StatusId.FatedVillain);
        var members = party.ActiveMembers()
            .OrderBy(m => Vector3.Distance(m.Position, chaos.Position) - Vector3.Distance(m.Position, exdeath.Position))
            .ToList();
        for (var i = 0; i < members.Count; i++)
            members[i].AddStatus(i < 4 ? StatusId.EpicHero : StatusId.FatedHero);
    }

    private void RemoveHeroes()
    {
        foreach (var member in party.AllMembers())
        {
            member.RemoveStatus(StatusId.EpicHero);
            member.RemoveStatus(StatusId.FatedHero);
        }
    }

    private void ApplyWindsAndElements()
    {
        foreach (var role in UmadP3BowelsOfAgonyState.AllRoles)
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            member.AddStatus(state.Winds[role] == Wind.Headwind ? StatusId.Headwind : StatusId.Tailwind, Constants.WindSeconds);
            if (state.ElementOf(role) is not { } element) continue;
            member.AddStatus(StatusOf(element), element == state.ShortElement ? Constants.ShortElementSeconds : Constants.LongElementSeconds);
        }
    }

    private static ushort StatusOf(Element element) => element == Element.Water ? StatusId.DynamicFluidP3 : StatusId.EntropyP3;

    // Where each holder stood when their debuff ran out is where its spread lands. The status
    // itself runs out a beat earlier, so it can't be what says who holds the element.
    private void PlaceElements(Element element)
    {
        var holders = state.HoldersOf(element);
        for (var i = 0; i < 2; i++)
        {
            if (party.Get(holders[i]) is not { } member || !member.IsAlive()) continue;
            elementHelpers[i]?.SetPosition(new Placement(member.Position, 0f));
            member.RemoveStatus(StatusOf(element));
        }
    }

    private void CastElements(Element element)
    {
        var holders = state.HoldersOf(element);
        for (var i = 0; i < 2; i++)
            if (party.Get(holders[i]) is { } member && member.IsAlive())
                elementHelpers[i]?.Cast(element == Element.Water ? UmadActions.StraySprayP3 : UmadActions.StrayFlamesP3);
    }

    // Each crystal fires at the two players nearest it.
    private void ShootCrystal(Element element)
    {
        var crystal = Geometry.Crystal(state.SpotOf(element));
        var targets = party.Find.ClosestN(crystal, 2);
        for (var i = 0; i < targets.Count; i++)
        {
            crystalHelpers[i]?.SetPosition(new Placement(crystal, 0f));
            crystalHelpers[i]?.Cast(element == Element.Water ? UmadActions.TsunamiP3 : UmadActions.InfernoP3, targets[i]);
        }
    }

    // One Cyclone per wind a crystal took, each a two-player stack on whoever stands nearest the
    // wind crystal.
    private void FireCyclones()
    {
        var targets = party.Find.ClosestN(Geometry.Crystal(state.WindSpot), windsTaken);
        windsTaken = 0;
        for (var i = 0; i < targets.Count; i++)
            cycloneHelpers[i % cycloneHelpers.Length]?.Cast(UmadActions.Cyclone, targets[i]);
    }

    // Black Hole's buster, on whoever is closest to Exdeath.
    private void ResolveThunder()
    {
        if (state.Objects.Exdeath is { } exdeath) thunderHelper?.Cast(UmadActions.ThunderIII, party.Find.Closest(exdeath.Position));
    }

    // Two opposite 90 deg cones from Chaos: sides then front and back for Latitudinal, the
    // reverse for Longitudinal.
    private void AimShockwaves(bool sides)
    {
        if (state.Objects.Chaos is not { } chaos) return;
        for (var i = 0; i < 2; i++)
            shockwaveHelpers[i]?.SetPosition(chaos.Placement().AddToRotation((sides ? MathF.PI / 2f : 0f) + i * MathF.PI));
    }

    private void FireShockwaves()
    {
        foreach (var helper in shockwaveHelpers) helper?.Cast(UmadActions.ImplosionShockwave);
    }
}
