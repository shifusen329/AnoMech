using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

using Constants = UmadP1GravenConstants;
using CastBar = P1TeleTrouncing.UmadP1TeleTrouncingConstants.CastBar;
using AnimationLock = P1TeleTrouncing.UmadP1TeleTrouncingConstants.AnimationLock;

// Dancing Mad P1 from Graven Image 1 to Graven Image 2's cast: knockback tethers + Mystery Magic 1
// (ice + fire), Wave Cannon and its towers, Double-trouble Trap 1, Mystery Magic 2 (ice +
// thunder), Light of Judgment and Hyperdrive. t=0 is 24.50s into the pull. Kefka holds the main
// tank throughout: the real fight's Revolting Ruin III swap isn't modelled.
public sealed class UmadP1GravenImage1Scenario : IMultiplayerReplayable
{
    public string Name => "Graven Image 1";
    public IPhase Phase => UmadZone.P1;
    public ushort Bgm => Constants.Bgm.Opening;
    // The track starts 10.47s into the pull.
    public float BgmSecondsAtStart => 14.03f;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public bool SupportsMitigationPractice => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP1GravenImage1Ai()];
    public object SettingsOverrides => settingsWindow.Overrides;
    public void DrawSettings() => settingsWindow.Draw();

    public UmadP1GravenImage1State? LastState { get; private set; }

    // Local to UmadZone.Origin. The statue hangs 44y north, 18.5y up; Wave Cannon fires from the
    // floor 35y north.
    internal static readonly Vector3 StatuePosition = new(0f, 18.5f, -44f);
    internal static readonly Vector3 WaveCannonPosition = new(0f, 0f, -35f);
    // Explosion's EffectRange.
    internal const float TowerRadius = 4f;
    private const float WaveCannonVulnSeconds = 3.96f;


    private readonly UmadP1GravenImage1SettingsWindow settingsWindow = new();
    private UmadP1Kit? kit;
    private UmadP1GravenImage1State state = null!;
    private SimEnemy?[] statues = [];
    private SimEventObject? statueProp;
    private SimEventObject? waveCannonProp;
    private readonly List<(SimEnemy Helper, PartyRole Owner)> towers = [];
    private List<SimCharacter> trapHitSupport = [];
    private List<SimCharacter> trapHitDps = [];

    public void Run(SimWorld world, int? selectedAi)
    {
        kit?.Teardown();
        var overrides = settingsWindow.Overrides;
        state = new UmadP1GravenImage1State(world.Rng, overrides);
        LastState = state;
        var k = kit = new UmadP1Kit(world, "UmadP1GravenImage1", overrides.PropsBeatMode);
        towers.Clear();
        trapHitSupport = [];
        trapHitDps = [];
        world.EnforceArenaBoundary(UmadP1Kit.ArenaRadius, "Knocked off the arena");
        world.MitPractice.Begin(UmadP1GravenImage1Mitigation.Plan(overrides.ExtrasAtWaveCannon),
            new Dictionary<MitSource, Func<SimCharacter?>> { [UmadMitigation.Kefka] = () => k.Kefka });
        DiagnosticLog.Info($"[UmadP1GravenImage1] Roll: tethers={(state.TetherDps ? "DPS" : "Supports")} ice1={state.Ice1} fire={state.Fire} "
            + $"wave=[{string.Join(",", state.WaveTargets)}] trap={state.TrapSupport}/{state.TrapDps} ice2={state.Ice2} thunder={state.Thunder}.");

        world.Events.Add(0f, () =>
        {
            k.SpawnKefka();
            statues = k.SpawnGravenImages(StatuePosition, StatuePosition, StatuePosition, StatuePosition);
            statueProp = k.SpawnProp(Constants.EObjId.StatueFront, 0x4000EBA0u, Constants.EObjId.StatueFrontArg2, StatuePosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            waveCannonProp = k.SpawnProp(Constants.EObjId.StatueFront, 0x4000EBA1u, Constants.EObjId.WaveCannonSpotArg2, WaveCannonPosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            k.SpawnColossus([StatueBeat.Appear], overrides.PropsBindDirector, overrides.PropsForceActive);
        });

        foreach (var at in new[] { 5.10f, 8.13f, 16.18f, 22.23f, 30.28f, 33.29f, 41.34f, 44.36f, 47.39f, 50.42f })
            world.Events.Add(at, k.AutoAttack);
        world.Events.Add(0.42f, () => k.KefkaCast(Constants.ActionId.TeleportP1, 0f, AnimationLock.Teleport));
        world.Events.Add(2.07f, () => k.KefkaCast(Constants.ActionId.GravenImage, CastBar.Short, AnimationLock.GravenImage));

        // Graven Image 1: the statue tethers one role, then Pulse Wave knocks them 13y away from it.
        world.Events.Add(5.81f, () =>
        {
            k.Beat(statueProp, StatueBeat.Appear);
            var i = 0;
            foreach (var role in state.TetheredRoles)
                if (k.Party.Get(role) is { } member && member.IsAlive() && i < statues.Length)
                    world.Tether(statues[i++], member, Constants.TetherId.GravenImage, duration: 5.07f);
        });
        world.Events.Add(10.80f, () => k.Beat(statueProp, StatueBeat.Vanish));
        world.Events.Add(10.88f, ResolvePulseWave);

        // Mystery Magic 1: ice cones, then Flagrant Fire 0.8s later.
        world.Events.Add(8.13f, () =>
        {
            k.AttachIceOrb(state.Ice1);
            k.AttachFireOrbAndMarkers(state.Fire);
        });
        world.Events.Add(8.22f, () =>
        {
            k.KefkaCast(Constants.ActionId.MysteryMagic, CastBar.Long, AnimationLock.MysteryMagic);
            k.CastIce(state.Ice1);
        });
        world.Events.Add(13.20f, k.ResolveIce);
        world.Events.Add(14.00f, () => k.ResolveFire(state.Fire));

        // Wave Cannon: the statues drop to the floor 35y north and fire at four players; a tower
        // then lands where each target stood.
        world.Events.Add(13.11f, () => k.Beat(waveCannonProp, StatueBeat.Telegraph));
        world.Events.Add(17.38f, () => k.KefkaCast(Constants.ActionId.DoubleTroubleTrap, CastBar.Short, AnimationLock.Helper));
        world.Events.Add(18.14f, AimWaveCannon);
        world.Events.Add(18.23f, ResolveWaveCannon);
        world.Events.Add(18.85f, PlaceTowers);
        world.Events.Add(20.36f, () =>
        {
            k.ApplyTrap(state.TrapSupport, 5f);
            k.ApplyTrap(state.TrapDps, 5f);
        });
        world.Events.Add(21.88f, ResolveTowers);

        // Mystery Magic 2 (ice + thunder) around the first Double-trouble Trap stack.
        world.Events.Add(24.36f, () =>
        {
            k.AttachIceOrb(state.Ice2);
            k.AttachThunderOrb(state.Thunder);
        });
        world.Events.Add(24.45f, () =>
        {
            k.KefkaCast(Constants.ActionId.MysteryMagic, CastBar.Long, AnimationLock.MysteryMagic);
            k.CastIce(state.Ice2);
            k.CastThunder(state.Thunder);
        });
        world.Events.Add(25.48f, () =>
        {
            trapHitSupport = k.ResolveTrapStack(state.TrapSupport, UmadP1Kit.TrapHelperBase, Name);
            trapHitDps = k.ResolveTrapStack(state.TrapDps, UmadP1Kit.TrapHelperBase + 1, Name);
        });
        // The trap jumps to one of each stack's three for the second trap (in Graven Image 2).
        world.Events.Add(26.06f, () =>
        {
            if (UmadP1Kit.TrapJumpTarget(trapHitSupport, state.TrapJumpSupport) is { } support) k.ApplyTrap(support, 68f);
            if (UmadP1Kit.TrapJumpTarget(trapHitDps, state.TrapJumpDps) is { } dps) k.ApplyTrap(dps, 68f);
        });
        world.Events.Add(29.44f, () =>
        {
            k.ResolveIce();
            k.ResolveThunder();
        });

        world.Events.Add(33.52f, () => k.KefkaCast(Constants.ActionId.LightOfJudgment, CastBar.Long, AnimationLock.LightOfJudgment));
        world.Events.Add(35.47f, () => k.BeatColossus(StatueBeat.ColossusStage2));
        world.Events.Add(38.51f, () => world.MitPractice.HitParty(UmadP1Hits.LightOfJudgment));
        world.Events.Add(41.65f, k.Hyperdrive);
        world.Events.Add(43.74f, k.Hyperdrive);
        world.Events.Add(45.83f, k.Hyperdrive);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP1GravenImage1State>)AiStrats[idx]).Run(state, world);
    }

    // Host and peer alike (see IScenario.RunInstanceEvents), so broadcast: false.
    public void RunInstanceEvents(SimWorld world)
    {
        world.Events.Add(0.05f, () => world.Map.AddEffect(Constants.ArenaState.Gi1Rest, 0, broadcast: false));
        world.Events.Add(27.03f, () => UmadP1Kit.DirectorLine(world, 0xAU, broadcast: false));
        world.Events.Add(35.47f, () =>
        {
            world.Map.AddEffect(Constants.ArenaState.ToMiddle, 0, broadcast: false);
            if (!Plugin.Config.SuppressBgm) Natives.Bgm.Play(Constants.Bgm.Middle);
        });
    }

    public void Tick(float delta, float elapsed)
    {
        if (kit == null) return;
        kit.FaceAggroTank();
        kit.TickColossus(elapsed);
        for (var i = 0; i < statues.Length; i++) statues[i] = kit.ReplaceIfDropped(statues[i]);
    }

    private void ResolvePulseWave()
    {
        if (kit == null) return;
        var i = 0;
        foreach (var role in state.TetheredRoles)
        {
            if (kit.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            var statue = i < statues.Length ? statues[i++] : null;
            statue?.Cast(Constants.ActionId.PulseWave, castSeconds: 0f, targetId: member.GameObjectId, animationLock: AnimationLock.Helper);
            kit.KnockbackFrom(member, StatuePosition, Constants.KnockbackId.PulseWave);
        }
    }

    private void AimWaveCannon()
    {
        if (kit == null) return;
        kit.Beat(waveCannonProp, StatueBeat.Fire);
        for (var i = 0; i < statues.Length && i < state.WaveTargets.Count; i++)
        {
            var target = kit.Party.Get(state.WaveTargets[i]);
            var placement = new Placement(WaveCannonPosition, 0f);
            statues[i]?.SetPosition(target != null ? placement.Face(target.Position) : placement);
        }
    }

    // Each line gives its targets 3.96s of Magic Vulnerability Up, so a second line on anyone is
    // lethal (the solver checks the vuln before applying it again).
    private void ResolveWaveCannon()
    {
        if (kit == null) return;
        for (var i = 0; i < statues.Length && i < state.WaveTargets.Count; i++)
        {
            if (statues[i] is not { } statue || kit.Party.Get(state.WaveTargets[i]) is not { } target) continue;
            statue.Cast(Constants.ActionId.WaveCannon, castSeconds: 0f, targetId: target.GameObjectId, animationLock: AnimationLock.Helper);
            var hit = kit.Damage.Resolve(statue, Constants.ActionId.WaveCannon, [DamageType.Magic],
                [(UmadConstants.StatusId.MagicVulnerabilityUp, WaveCannonVulnSeconds)]);
            kit.World.MitPractice.Hit(hit, UmadP1Hits.WaveCannon);
        }
    }

    private void PlaceTowers()
    {
        if (kit == null) return;
        towers.Clear();
        for (var i = 0; i < state.WaveTargets.Count; i++)
        {
            var owner = state.WaveTargets[i];
            if (kit.Party.Get(owner) is not { } target || kit.Helper(i) is not { } helper) continue;
            helper.SetPosition(new Placement(target.Position, 0f));
            helper.Cast(Constants.ActionId.Explosion, castSeconds: CastBar.Short, fireDelay: CastBar.FireDelay, animationLock: AnimationLock.Helper);
            towers.Add((helper, owner));
        }
    }

    // An unsoaked tower wipes the party; a soaker still carrying Wave Cannon's vuln dies, and so
    // does anyone standing in two towers.
    private void ResolveTowers()
    {
        if (kit == null) return;
        var towerCount = new Dictionary<SimCharacter, int>();
        foreach (var (helper, _) in towers)
            foreach (var member in kit.Party.Find.InsideCircle(helper.Position, TowerRadius))
                towerCount[member] = towerCount.GetValueOrDefault(member) + 1;
        foreach (var (helper, owner) in towers)
        {
            if (kit.Party.Find.InsideCircle(helper.Position, TowerRadius).Count == 0)
            {
                kit.KefkaCast(Constants.ActionId.UnmitigatedExplosion, 0f, AnimationLock.Helper);
                kit.Party.WipeAllPlayers($"Unmitigated Explosion: nobody soaked {owner}'s tower during Graven Image 1");
                return;
            }
            kit.World.MitPractice.Hit(kit.Damage.Resolve(helper, Constants.ActionId.Explosion, [DamageType.Magic], []), UmadP1Hits.Explosion);
        }
        foreach (var (member, count) in towerCount)
            if (count >= 2 && member.IsAlive())
                member.Die("Soaked two Explosion towers during Graven Image 1");
        towers.Clear();
    }

    public MpMessage? BuildReplayStateMessage() => LastState?.ToReplayMessage();

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP1GravenImage1AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (UmadP1GravenImage1State.FromNetworkReplay(msg) is not { } shadowState) return null;
        ((IScenarioAi<UmadP1GravenImage1State>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }
}
