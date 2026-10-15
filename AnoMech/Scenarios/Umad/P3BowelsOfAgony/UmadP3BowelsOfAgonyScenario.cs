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
using LimitCut = UmadP3LimitCutConstants;

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
    private DamageSolver damage = null!;
    private readonly SimEnemy?[] elementHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] crystalHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] shockwaveHelpers = new SimEnemy?[2];
    private readonly SimEnemy?[] cycloneHelpers = new SimEnemy?[4];
    private SimEnemy? thunderHelper;
    private SimEnemy? gravenImage;
    private SimEventObject? waterCrystal;
    private SimEventObject? fireCrystal;
    private SimEventObject? windCrystal;
    private readonly Vector3?[] elementSnapshots = new Vector3?[2];

    public UmadP3BowelsOfAgonyState? LastState { get; private set; }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new UmadP3BowelsOfAgonyState(world.Rng, party, settingsWindow.Overrides);
        LastState = state;
        damage = new DamageSolver(party);
        damage.SetStatuses(DamageType.Lightning, StatusId.LightningResistanceDownII);
        damage.SetStatuses(DamageType.Magic, StatusId.MagicVulnerabilityUp);
        damage.SetMitigableStatuses(DamageType.Wind, Constants.Damage.CycloneVulnMitigation, LimitCut.StatusId.WindResistanceDownII);
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
        world.Events.Add(0.40f, () => objects.Kefka?.Cast(ActionId.DefinitionOfInsanity, castSeconds: 3.7f, fireDelay: 0.3f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(5.05f, () =>
        {
            objects.Chaos?.SetVisible(true);
            objects.Exdeath?.SetVisible(true);
        });
        world.Events.Add(5.40f, () =>
        {
            objects.Chaos?.PlayAnimationTimeline(TimelineId.Spawn);
            objects.Exdeath?.PlayAnimationTimeline(TimelineId.Spawn);
        });
        world.Events.Add(7.45f, () =>
        {
            objects.Chaos?.SetTargetable(true);
            objects.Exdeath?.SetTargetable(true);
        });
        world.Events.Add(7.54f, () => objects.Kefka?.PlayAnimationTimeline(TimelineId.WarpOut));
        world.Events.Add(7.58f, () => objects.Chaos?.Cast(LimitCut.ActionId.DecisiveBattleChaos, castSeconds: 2.7f, fireDelay: 0.28f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(7.63f, () => objects.Exdeath?.Cast(LimitCut.ActionId.DecisiveBattleExdeath, castSeconds: 2.7f, fireDelay: 0.23f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(8.42f, () => objects.Kefka?.SetVisible(false));
        world.Events.Add(9.58f, () => objects.Kefka?.SetPosition(new Placement(Geometry.KefkaPerch, 0f)));
        world.Events.Add(9.59f, () => objects.Kefka?.SetVisible(true));
        world.Events.Add(9.67f, () => objects.Kefka?.PlayAnimationTimeline(TimelineId.Spawn));
        world.Events.Add(10.56f, ResolveDecisiveBattle);
        world.Events.Add(10.80f, () =>
        {
            objects.Chaos?.Follow(party.Get(PartyRole.MainTank));
            objects.Exdeath?.Follow(party.Get(PartyRole.OffTank));
        });
        world.Events.Add(11.81f, () => objects.Kefka?.Cast(ActionId.KefkaRest, castSeconds: 2.7f, fireDelay: 0.3f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(15.60f, () => objects.Kefka?.SetModelState(0x04));

        foreach (var at in new[] { 14.71f, 17.74f, 20.77f, 23.80f, 31.87f, 34.90f, 37.93f, 40.96f, 43.99f, 47.02f, 50.05f, 53.08f, 56.11f, 59.14f, 62.17f })
            world.Events.Add(at, () => objects.Chaos?.Cast(ActionId.AutoAttack1, castSeconds: 0f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId, animationLock: Constants.AnimationLock.AutoAttack));
        foreach (var at in new[] { 13.73f, 16.76f, 19.79f, 22.82f, 25.85f, 28.88f, 31.91f, 34.94f, 37.97f, 41.00f, 64.45f, 67.47f, 70.50f, 73.54f, 76.56f })
            world.Events.Add(at, () => objects.Exdeath?.Cast(ActionId.AutoAttack2, castSeconds: 0f, targetId: party.Get(PartyRole.OffTank)?.GameObjectId, animationLock: Constants.AnimationLock.AutoAttack));

        world.Events.Add(24.80f, () => objects.Chaos?.Follow());
        world.Events.Add(25.00f, () => objects.Chaos?.Cast(ActionId.BowelsOfAgony, castSeconds: 4.7f, fireDelay: 0.28f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(29.83f, SpawnCrystals);
        world.Events.Add(29.98f, ResolveBowelsOfAgony);
        world.Events.Add(30.20f, () => objects.Chaos?.Follow(party.Get(PartyRole.MainTank)));
        world.Events.Add(30.37f, () =>
        {
            waterCrystal?.SetVisible(true);
            fireCrystal?.SetVisible(true);
            windCrystal?.SetVisible(true);
        });

        world.Events.Add(42.00f, () =>
        {
            objects.Chaos?.Follow();
            objects.Exdeath?.Follow();
        });
        world.Events.Add(42.07f, RemoveHeroes);
        world.Events.Add(42.21f, () =>
        {
            objects.Exdeath?.Cast(ActionId.ThunderIII_Aoe, castSeconds: 6.7f, fireDelay: 0.29f, animationLock: Constants.AnimationLock.BossCast);
            if (gravenImage != null) world.Tether(objects.Exdeath, gravenImage, TetherId.ExdeathToGravenImage, duration: 6.99f);
        });
        world.Events.Add(49.03f, () => SnapshotElement(state.ShortElement));
        world.Events.Add(49.11f, () => ResolveElement(state.ShortElement));
        world.Events.Add(49.20f, ResolveThunderAoe);
        world.Events.Add(50.14f, () => ResolveCrystal(state.ShortElement));
        world.Events.Add(50.83f, () => CrystalOf(state.ShortElement)?.FadeOut());

        world.Events.Add(53.35f, () => objects.Exdeath?.Cast(ActionId.ThunderIII_Cast, castSeconds: 4.7f, fireDelay: 0.38f, animationLock: LimitCut.AnimationLock.ThunderCast));
        world.Events.Add(58.43f, ResolveThunder);
        world.Events.Add(61.46f, ResolveThunder);
        world.Events.Add(62.00f, () => objects.Exdeath?.Follow(party.Get(PartyRole.OffTank)));
        world.Events.Add(63.50f, () => objects.Chaos?.Follow(party.Get(PartyRole.MainTank)));
        world.Events.Add(64.05f, () => objects.Kefka?.Cast(ActionId.KefkaUnrest, castSeconds: 0f, animationLock: Constants.AnimationLock.Instant));
        world.Events.Add(64.85f, () => objects.Kefka?.SetModelState(0x00));
        world.Events.Add(66.03f, () => objects.Chaos?.Follow());
        world.Events.Add(66.14f, () => objects.Kefka?.Cast(ActionId.Trance, castSeconds: 3.6f, fireDelay: 0.3f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(66.23f, () => objects.Chaos?.Cast(state.Latitudinal ? ActionId.LatitudinalImplosion : ActionId.LongitudinalImplosion,
            castSeconds: 4.7f, fireDelay: 0.29f, animationLock: Constants.AnimationLock.BossCast));
        world.Events.Add(70.19f, () => objects.Kefka?.AddStatusParam(LimitCut.StatusId.KefkaTrance, 0x1FF));
        world.Events.Add(71.84f, () => AimShockwaves(sides: state.Latitudinal));
        world.Events.Add(71.93f, FireShockwaves);
        world.Events.Add(73.85f, () => AimShockwaves(sides: !state.Latitudinal));
        world.Events.Add(73.94f, FireShockwaves);

        world.Events.Add(76.03f, () => SnapshotElement(state.LongElement));
        world.Events.Add(76.12f, () => ResolveElement(state.LongElement));
        world.Events.Add(77.14f, () => ResolveCrystal(state.LongElement));
        world.Events.Add(77.99f, () => CrystalOf(state.LongElement)?.FadeOut());
        world.Events.Add(78.35f, () => DiagnosticLog.Info("[UmadP3BowelsOfAgony] End: Limit Cut's clock starts here."));
    }

    // Scheduled by host and peer alike, so broadcast: false.
    public void RunInstanceEvents(SimWorld instanceWorld)
    {
        foreach (var (at, arg) in new[] { (0.32f, 0x14U), (7.45f, 0x15U), (9.58f, 0x16U), (71.40f, 0x17U) })
            instanceWorld.Events.Add(at, () => instanceWorld.Map.DirectorUpdate(Constants.DirectorCategory, arg, 0x2U, Constants.DirectorArg3, Constants.DirectorKefkaId, broadcast: false));
        instanceWorld.Events.Add(41.94f, () => instanceWorld.Map.AddEffect(packetFlags: 0x00020001U, index: 0x22, broadcast: false));
        instanceWorld.Events.Add(53.26f, () => instanceWorld.Map.AddEffect(packetFlags: 0x00080004U, index: 0x22, broadcast: false));
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
            Targetable: false, EnemyList: EnemyListMode.Always, IsVisible: true,
            Placement: new Placement(Vector3.Zero, 0f)));
        objects.Chaos = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.ChaosP3, NameId: BNpcNameId.Chaos, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, IsVisible: false,
            Placement: new Placement(Geometry.ChaosArrival, MathF.PI / 2f)));
        objects.Exdeath = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Exdeath, NameId: BNpcNameId.Exdeath, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Always, IsVisible: false,
            Placement: new Placement(Geometry.ExdeathArrival, -MathF.PI / 2f)));
        gravenImage = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never,
            Placement: new Placement(Geometry.GravenImage, 0f), NpcSpawnTemplate: UmadRealPackets.GravenImageNpcSpawn));
        var wind = Geometry.Crystal(state.WindSpot);
        for (var i = 0; i < 2; i++)
        {
            elementHelpers[i] = Helper(NameIdOf: BNpcNameId.Chaos);
            crystalHelpers[i] = Helper(NameIdOf: BNpcNameId.Chaos);
            shockwaveHelpers[i] = Helper(NameIdOf: BNpcNameId.Chaos);
        }
        // Cyclone's caster-side VFX needs a real skeleton, as in Limit Cut.
        for (var i = 0; i < cycloneHelpers.Length; i++)
            cycloneHelpers[i] = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Chaos, NameId: BNpcNameId.Chaos, Level: 1,
                Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
                Placement: new Placement(wind, 0f)));
        thunderHelper = Helper(NameIdOf: BNpcNameId.Exdeath);
    }

    private SimEnemy? Helper(uint NameIdOf) => world.SpawnEnemy(new EnemySpawnConfig(
        BNpcBaseId: BNpcBaseId.KefkaHelper, NameId: NameIdOf, Level: 1,
        Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
        Placement: new Placement(Vector3.Zero, 0f)));

    private void SpawnCrystals()
    {
        waterCrystal = SpawnCrystal(LimitCut.EObjId.WaterCrystal, state.WaterSpot);
        fireCrystal = SpawnCrystal(LimitCut.EObjId.FireCrystal, state.FireSpot);
        windCrystal = SpawnCrystal(LimitCut.EObjId.WindCrystal, state.WindSpot);
    }

    private SimEventObject? SpawnCrystal(uint eobjId, int spot) => world.SpawnEventObject(new EventObjectSpawnConfig
    {
        EObjId = eobjId,
        Placement = new Placement(Geometry.Crystal(spot), Geometry.CrystalRotation(spot)),
        SpawnVisible = false,
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

    private void ResolveBowelsOfAgony()
    {
        world.MitPractice.HitParty(UmadP3BowelsOfAgonyMitigation.BowelsOfAgony);
        foreach (var role in UmadP3BowelsOfAgonyState.AllRoles)
        {
            if (party.Get(role) is not { } member || !member.IsAlive()) continue;
            member.AddStatus(state.Winds[role] == Wind.Headwind ? LimitCut.StatusId.Headwind : LimitCut.StatusId.Tailwind, Constants.WindSeconds);
            if (state.ElementOf(role) is not { } element) continue;
            member.AddStatus(StatusOf(element), element == state.ShortElement ? Constants.ShortElementSeconds : Constants.LongElementSeconds);
        }
    }

    private static ushort StatusOf(Element element) => element == Element.Water ? StatusId.DynamicFluidP3 : StatusId.EntropyP3;

    // Where each holder stood when their debuff ran out is where its spread lands.
    private void SnapshotElement(Element element)
    {
        var holders = state.HoldersOf(element);
        for (var i = 0; i < 2; i++)
        {
            elementSnapshots[i] = null;
            if (party.Get(holders[i]) is not { } member || !member.IsAlive() || !member.HasStatus(StatusOf(element))) continue;
            elementSnapshots[i] = member.Position;
            member.RemoveStatus(StatusOf(element));
        }
    }

    // Entropy: a 5y circle on its holder. Dynamic Fluid: a donut whose hole is safe.
    private void ResolveElement(Element element)
    {
        var actionId = element == Element.Water ? ActionId.StraySpray_P3 : ActionId.StrayFlames_P3;
        for (var i = 0; i < 2; i++)
        {
            if (elementSnapshots[i] is not { } at || elementHelpers[i] is not { } helper) continue;
            helper.SetPosition(new Placement(at, 0f));
            helper.Cast(actionId, castSeconds: 0f, animationLock: Constants.AnimationLock.Instant);
            var hit = damage.Resolve(IPositioned.From(at), actionId, [DamageType.Magic],
                [(StatusId.MagicVulnerabilityUp, Constants.Damage.MagicVulnerabilityUpSeconds)],
                size: element == Element.Water ? Geometry.StraySprayInnerRadius : null);
            world.MitPractice.Hit(hit, element == Element.Water ? UmadP3BowelsOfAgonyMitigation.StraySpray : UmadP3BowelsOfAgonyMitigation.StrayFlames);
        }
    }

    // Each crystal fires at the two players nearest it: Tsunami a 5y circle, Inferno a donut. Anyone
    // else caught is pushed away from that player, and the push takes their wind.
    private void ResolveCrystal(Element element)
    {
        var crystal = Geometry.Crystal(state.SpotOf(element));
        var actionId = element == Element.Water ? ActionId.Tsunami_P3 : ActionId.Inferno_P3;
        var targets = party.Find.ClosestN(crystal, 2);
        var pushes = new Dictionary<SimCharacter, Vector3>();
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            crystalHelpers[i]?.SetPosition(new Placement(crystal, 0f));
            crystalHelpers[i]?.Cast(actionId, castSeconds: 0f, targetId: target.GameObjectId, animationLock: Constants.AnimationLock.Instant);
            var hit = damage.Resolve(target, actionId, [DamageType.Magic],
                [(StatusId.MagicVulnerabilityUp, Constants.Damage.MagicVulnerabilityUpSeconds)],
                size: element == Element.Fire ? Geometry.InfernoInnerRadius : null);
            world.MitPractice.Hit(hit, element == Element.Water ? UmadP3BowelsOfAgonyMitigation.Tsunami : UmadP3BowelsOfAgonyMitigation.Inferno);
            foreach (var member in hit)
                if (!targets.Contains(member)) pushes.TryAdd(member, target.Position);
        }
        var cleansed = pushes.Count(kv => PushFromCrystal(kv.Key, kv.Value, element));
        if (cleansed > 0) world.Events.Add(Constants.CycloneDelay, () => ResolveCyclones(cleansed));
    }

    // Vacuum Wave's facing rule: Headwind faces away from the push, Tailwind toward it, within 45
    // deg. Returns whether it took a wind.
    private bool PushFromCrystal(SimCharacter member, Vector3 source, Element element)
    {
        if (member is not ISimPartyMember pm || !member.IsAlive()) return false;
        var hasHeadwind = member.HasStatus(LimitCut.StatusId.Headwind);
        var hasTailwind = member.HasStatus(LimitCut.StatusId.Tailwind);
        var distance = Constants.PlainPush;
        var offDegrees = 0f;
        if (hasHeadwind || hasTailwind)
        {
            member.RemoveStatus(hasHeadwind ? LimitCut.StatusId.Headwind : LimitCut.StatusId.Tailwind);
            var away = MathF.Atan2(member.Position.X - source.X, member.Position.Z - source.Z);
            var facingAway = MathF.Cos(member.Rotation - away);
            var towardSafe = hasHeadwind ? facingAway : -facingAway;
            offDegrees = MathF.Acos(Math.Clamp(towardSafe, -1f, 1f)) * (180f / MathF.PI);
            distance = towardSafe > Constants.CorrectFacingCos ? Constants.CleansePush : Constants.WrongFacingPush;
        }
        var crystalName = element == Element.Water ? "Tsunami" : "Inferno";
        DiagnosticLog.Info($"[UmadP3BowelsOfAgony] {crystalName} push: {pm.Role} {(hasHeadwind ? "Headwind" : hasTailwind ? "Tailwind" : "no wind")} facing {offDegrees:F0} deg off -> {distance:F0}y.");
        pm.Knockback(source, distance, Constants.KnockbackSpeed);
        if (distance >= Constants.WrongFacingPush)
        {
            var p = new Vector2(member.Position.X, member.Position.Z);
            var delta = p - new Vector2(source.X, source.Z);
            var along = delta.LengthSquared() > 1e-4f ? Vector2.Normalize(delta) : new Vector2(0f, 1f);
            var pu = Vector2.Dot(p, along);
            var toEdge = -pu + MathF.Sqrt(MathF.Max(0f, pu * pu - p.LengthSquared() + Geometry.ArenaRadius * Geometry.ArenaRadius));
            var wind = hasHeadwind ? "Headwind faces away from the push" : "Tailwind faces into the push";
            world.Events.Add(MathF.Max(0f, toEdge / Constants.KnockbackSpeed - 0.05f),
                () => member.Die($"Died to {crystalName}'s knockback (faced {offDegrees:F0} deg off, {wind}: pushed off the arena)"));
        }
        return hasHeadwind || hasTailwind;
    }

    // One Cyclone per wind a crystal took, each a two-player stack on whoever stands nearest the
    // wind crystal.
    private void ResolveCyclones(int count)
    {
        var wind = Geometry.Crystal(state.WindSpot);
        var centres = party.Find.ClosestN(wind, count);
        for (var i = 0; i < centres.Count; i++)
        {
            cycloneHelpers[i % cycloneHelpers.Length]?.Cast(ActionId.Cyclone, castSeconds: 0f, targetId: centres[i].GameObjectId, animationLock: LimitCut.AnimationLock.Cyclone);
            world.MitPractice.Hit(damage.Resolve(centres[i], ActionId.Cyclone, [DamageType.Wind],
                [(LimitCut.StatusId.WindResistanceDownII, Constants.Damage.WindResistanceDownSeconds)],
                stackMinTargets: 2, understackedTankMitigation: LimitCut.Damage.CycloneSoloTankMitigation), UmadP3BowelsOfAgonyMitigation.Cyclone);
        }
    }

    private void ResolveThunderAoe()
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        foreach (var member in party.ActiveMembers().ToList())
        {
            var distance = Vector2.Distance(new Vector2(member.Position.X, member.Position.Z), new Vector2(exdeath.Position.X, exdeath.Position.Z));
            if (distance < Geometry.ThunderAoeRadius)
                member.Die($"Died to Thunder III ({distance:F0}y from Exdeath, lethal inside {Geometry.ThunderAoeRadius:F1}y)");
        }
    }

    // Black Hole's rules: whoever is closest to Exdeath, a tank buster, the second hit lethal
    // short of an invuln while Lightning Resistance Down II is still up.
    private void ResolveThunder()
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        var target = party.Find.Closest(exdeath.Position);
        thunderHelper?.Cast(ActionId.ThunderIII_Resolve, castSeconds: 0f, targetId: target?.GameObjectId, animationLock: LimitCut.AnimationLock.ThunderHit);
        damage.Resolve(target, ActionId.ThunderIII_Resolve, [DamageType.TankBuster, DamageType.Magic, DamageType.Lightning],
            [(StatusId.LightningResistanceDownII, Constants.Damage.LightningResistanceDownSeconds)],
            requiredMitigation: Constants.Damage.ThunderIIIRequiredMitigation);
        world.MitPractice.Hit(target, UmadP3BowelsOfAgonyMitigation.ThunderIII);
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
        foreach (var helper in shockwaveHelpers)
        {
            if (helper == null) continue;
            helper.Cast(ActionId.Shockwave, castSeconds: 0f, animationLock: Constants.AnimationLock.Instant);
            var hit = damage.Resolve(helper, ActionId.Shockwave, [], [(StatusId.DamageDown, Constants.Damage.DamageDownSeconds)],
                size: Geometry.ShockwaveHalfAngle);
            world.MitPractice.Hit(hit, UmadP3BowelsOfAgonyMitigation.Shockwave);
        }
    }
}
