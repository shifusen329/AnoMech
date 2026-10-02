using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

using Constants = UmadP1GravenConstants;
using CastBar = P1TeleTrouncing.UmadP1TeleTrouncingConstants.CastBar;
using AnimationLock = P1TeleTrouncing.UmadP1TeleTrouncingConstants.AnimationLock;

// Dancing Mad P1 from Graven Image 2 to Tele-trouncing: two sets of statue tethers (Gravitas
// puddles from the central statue, Vitrophyre knock-off spreads from the right-side one), the ice
// cones, Revolting Ruin III, two half-room cleaves, Double-trouble Trap 2 into the puddle soaks,
// Light of Judgment and Hyperdrive. t=0 is 75.60s into the pull. Kefka holds the main tank
// throughout.
public sealed class UmadP1GravenImage2Scenario : IMultiplayerReplayable
{
    public string Name => "Graven Image 2";
    public IPhase Phase => UmadZone.P1;
    public ushort Bgm => Constants.Bgm.Middle;
    // The track starts 59.97s into the pull.
    public float BgmSecondsAtStart => 15.63f;
    public bool SupportsSolo => true;
    public bool SupportsMultiplayer => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UmadP1GravenImage2Ai()];
    public object SettingsOverrides => settingsWindow.Overrides;
    public void DrawSettings() => settingsWindow.Draw();

    public UmadP1GravenImage2State? LastState { get; private set; }

    // Local to UmadZone.Origin. The central (purple) statue and the right-side (yellow) one hang
    // north of the arena; each cleave is telegraphed by a statue piece on its own side.
    internal static readonly Vector3 CentralStatuePosition = new(2.5f, 22.5f, -73f);
    internal static readonly Vector3 RightStatuePosition = new(26f, 7f, -58.5f);
    private static readonly Vector3 WestTelegraphPosition = new(-8f, 15f, -73f);
    private static readonly Vector3 EastTelegraphPosition = new(16f, 6.5f, -57f);

    // Gravitas, Vitrophyre and Gravity III are all 5y.
    internal const float PuddleRadius = 5f;
    internal const float VitrophyreRadius = 5f;
    // UNVERIFIED: nobody was clipped in the captures.
    private const float VitrophyreKnockback = 20f;
    private const float PuddlePopDelay = 2f;
    private const float CleaveHalfAngle = MathF.PI / 2f;

    private readonly UmadP1GravenImage2SettingsWindow settingsWindow = new();
    private UmadP1Kit? kit;
    private UmadP1GravenImage2State state = null!;
    // 0-3 tether the central statue's role, 4-7 the right-side's, 8 casts the cleaves.
    private SimEnemy?[] statues = [];
    private SimEventObject? centralProp;
    private SimEventObject? rightProp;
    private SimEventObject? westTelegraph;
    private SimEventObject? eastTelegraph;
    private readonly List<Puddle> puddles = [];
    private bool puddlesSoakable;
    private bool puzzleFailed;
    private string? puzzleFailure;

    private sealed class Puddle(Vector3 position, int set)
    {
        public Vector3 Position { get; } = position;
        public int Set { get; } = set;
        public HashSet<SimCharacter> Exempt { get; } = [];
        public bool Triggered { get; set; }
        public bool Popped { get; set; }
    }

    public void Run(SimWorld world, int? selectedAi)
    {
        kit?.Teardown();
        var overrides = settingsWindow.Overrides;
        state = new UmadP1GravenImage2State(world.Rng, overrides);
        LastState = state;
        var k = kit = new UmadP1Kit(world, "UmadP1GravenImage2", overrides.PropsBeatMode);
        puddles.Clear();
        puddlesSoakable = false;
        puzzleFailed = false;
        puzzleFailure = null;
        world.EnforceArenaBoundary(UmadP1Kit.ArenaRadius, "Knocked off the arena");
        DiagnosticLog.Info($"[UmadP1GravenImage2] Roll: purpleDps=[{string.Join(",", state.PurpleDps)}] ice={state.Ice} "
            + $"cleaveWest=[{string.Join(",", state.CleaveWest)}] trap={state.TrapSupport}/{state.TrapDps}.");

        world.Events.Add(0f, () =>
        {
            k.SpawnKefka();
            statues = k.SpawnGravenImages(
                CentralStatuePosition, CentralStatuePosition, CentralStatuePosition, CentralStatuePosition,
                RightStatuePosition, RightStatuePosition, RightStatuePosition, RightStatuePosition, Vector3.Zero);
            centralProp = k.SpawnProp(Constants.EObjId.StatueCentral, 0x4000EBA2u, Constants.EObjId.StatueCentralArg2, CentralStatuePosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            rightProp = k.SpawnProp(Constants.EObjId.StatueRight, 0x4000EBA3u, Constants.EObjId.StatueRightArg2, RightStatuePosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            westTelegraph = k.SpawnProp(Constants.EObjId.StatueCentral, 0x4000EBA4u, Constants.EObjId.WestTelegraphArg2, WestTelegraphPosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            eastTelegraph = k.SpawnProp(Constants.EObjId.StatueRight, 0x4000EBA5u, Constants.EObjId.EastTelegraphArg2, EastTelegraphPosition,
                overrides.PropsBindDirector, overrides.PropsForceActive);
            k.SpawnColossus([StatueBeat.Appear, StatueBeat.ColossusStage2], overrides.PropsBindDirector, overrides.PropsForceActive);
            k.ApplyTrap(state.TrapSupport, 42.96f);
            k.ApplyTrap(state.TrapDps, 42.96f);
        });

        foreach (var at in new[] { 5.38f, 13.45f, 16.49f, 24.55f, 27.59f, 32.67f, 35.70f, 38.72f, 41.75f, 46.84f, 49.87f, 57.94f, 60.96f, 63.99f })
            world.Events.Add(at, k.AutoAttack);
        world.Events.Add(0.53f, () => k.KefkaCast(Constants.ActionId.TeleportP1, 0f, AnimationLock.Teleport));
        world.Events.Add(1.95f, () => k.KefkaCast(Constants.ActionId.GravenImage, CastBar.Short, AnimationLock.GravenImage));

        // First tethers, with the ice cones.
        world.Events.Add(5.25f, PlaceStatues);
        world.Events.Add(5.69f, () =>
        {
            k.Beat(centralProp, StatueBeat.Appear);
            k.Beat(rightProp, StatueBeat.Appear);
            TetherStatues(0, 6.51f, 10.53f);
        });
        world.Events.Add(6.98f, () => k.AttachIceOrb(state.Ice));
        world.Events.Add(7.07f, () =>
        {
            k.KefkaCast(UmadConstants.ActionId.BlizzardIIIBlowout_Cast, CastBar.Long, AnimationLock.MysteryMagic);
            k.CastIce(state.Ice);
        });
        world.Events.Add(12.07f, k.ResolveIce);
        world.Events.Add(12.20f, () => ResolveGravitas(0));
        world.Events.Add(16.22f, () => ResolveVitrophyre(0));

        // Revolting Ruin III, then the first cleave as the second tethers land.
        world.Events.Add(16.98f, () => k.KefkaCast(Constants.ActionId.RevoltingRuinIII, CastBar.Long, AnimationLock.Helper, k.AggroTank));
        world.Events.Add(20.90f, () => k.Beat(CleaveTelegraph(0), StatueBeat.Telegraph));
        world.Events.Add(21.80f, PlaceStatues);
        world.Events.Add(22.10f, () => k.ResolveRevoltingRuin(Constants.ActionId.RevoltingRuinIII, instantCast: false));
        world.Events.Add(22.15f, () => TetherStatues(1, 8.51f, 12.52f));
        world.Events.Add(25.27f, () => k.ResolveRevoltingRuin(Constants.ActionId.RevoltingRuinIIIFollowUp, instantCast: true));
        world.Events.Add(25.94f, () => AimCleave(0));
        world.Events.Add(26.03f, () => ResolveCleave(0));

        // Second puddles and spreads, then the second cleave.
        world.Events.Add(30.66f, () => ResolveGravitas(1));
        world.Events.Add(34.14f, () => k.Beat(CleaveTelegraph(1), StatueBeat.Telegraph));
        world.Events.Add(34.67f, () => ResolveVitrophyre(1));
        world.Events.Add(38.59f, () =>
        {
            k.Beat(centralProp, StatueBeat.Vanish);
            k.Beat(rightProp, StatueBeat.Vanish);
        });
        world.Events.Add(39.17f, () => AimCleave(1));
        world.Events.Add(39.26f, () => ResolveCleave(1));

        // Double-trouble Trap 2 throws each role into the far puddles; from here they can be
        // popped, each 2s after someone first steps in.
        world.Events.Add(43.00f, () =>
        {
            k.ResolveTrapStack(state.TrapSupport, UmadP1Kit.TrapHelperBase, Name);
            k.ResolveTrapStack(state.TrapDps, UmadP1Kit.TrapHelperBase + 1, Name);
            puddlesSoakable = true;
        });
        world.Events.Add(46.00f, RefreshUnpoppedPuddleOmens);
        world.Events.Add(49.40f, ResolveLeftoverPuddles);
        world.Events.Add(49.50f, PunishFailedPuzzle);

        world.Events.Add(52.15f, () => k.BeatColossus(StatueBeat.ColossusStage3));
        world.Events.Add(52.24f, () => k.KefkaCast(Constants.ActionId.LightOfJudgment, CastBar.Long, AnimationLock.LightOfJudgment));
        world.Events.Add(60.39f, k.Hyperdrive);
        world.Events.Add(62.48f, k.Hyperdrive);
        world.Events.Add(64.57f, k.Hyperdrive);

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP1GravenImage2State>)AiStrats[idx]).Run(state, world);
    }

    // Host and peer alike (see IScenario.RunInstanceEvents), so broadcast: false.
    public void RunInstanceEvents(SimWorld world)
    {
        world.Events.Add(0.05f, () => world.Map.AddEffect(Constants.ArenaState.Gi2Rest, 0, broadcast: false));
        world.Events.Add(1.86f, () => UmadP1Kit.DirectorLine(world, 0x2U, broadcast: false));
        world.Events.Add(52.15f, () =>
        {
            world.Map.AddEffect(Constants.ArenaState.ToLate, 0, broadcast: false);
            if (!Plugin.Config.SuppressBgm) Natives.Bgm.Play(Constants.Bgm.Late);
        });
        world.Events.Add(66.58f, () => UmadP1Kit.DirectorLine(world, 0xBU, broadcast: false));
    }

    public void Tick(float delta, float elapsed)
    {
        if (kit == null) return;
        kit.FaceAggroTank();
        kit.TickColossus(elapsed);
        for (var i = 0; i < statues.Length; i++) statues[i] = kit.ReplaceIfDropped(statues[i]);
        TickPuddles();
    }

    private SimEventObject? CleaveTelegraph(int i) => state.CleaveWest[i] ? westTelegraph : eastTelegraph;

    // Each set's statues are re-split between the two platforms; the actors are interchangeable,
    // so only their places are reasserted.
    private void PlaceStatues()
    {
        for (var i = 0; i < 8 && i < statues.Length; i++)
            statues[i]?.SetPosition(new Placement(i < 4 ? CentralStatuePosition : RightStatuePosition, 0f));
    }

    private void TetherStatues(int set, float purpleSeconds, float yellowSeconds)
    {
        if (kit == null) return;
        var i = 0;
        foreach (var role in state.PurpleRoles(set))
        {
            if (kit.Party.Get(role) is { } member && member.IsAlive())
                kit.World.Tether(statues[i], member, Constants.TetherId.GravenImage, duration: purpleSeconds);
            i++;
        }
        foreach (var role in state.YellowRoles(set))
        {
            if (kit.Party.Get(role) is { } member && member.IsAlive())
                kit.World.Tether(statues[i], member, Constants.TetherId.GravenImage, duration: yellowSeconds);
            i++;
        }
    }

    // Each puddle drops under its target. Whoever stands in it as it lands may walk out; walking
    // back in before the soak wipes the party. Every Gravitas has to land on the whole party:
    // anyone left out of any of them wipes everyone.
    private void ResolveGravitas(int set)
    {
        if (kit == null) return;
        var i = 0;
        var landed = new List<Vector3>();
        foreach (var role in state.PurpleRoles(set))
        {
            var statue = statues[i++];
            if (kit.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            statue?.Cast(Constants.ActionId.Gravitas, castSeconds: 0f, targetId: target.GameObjectId, animationLock: AnimationLock.Helper);
            var puddle = new Puddle(target.Position, set);
            foreach (var member in kit.Party.Find.InsideCircle(puddle.Position, PuddleRadius)) puddle.Exempt.Add(member);
            puddles.Add(puddle);
            landed.Add(puddle.Position);
            kit.World.SpawnOmen(Constants.VfxPath.PuddleOmen, new Placement(puddle.Position, 0f), new Vector3(PuddleRadius, 1f, PuddleRadius), 46.00f - (set == 0 ? 12.20f : 30.66f));
        }

        var outside = UmadP1Roles.All
            .Where(r => kit.Party.Get(r) is { } m && m.IsAlive() && landed.Any(p => DistanceXZ(p, m.Position) > PuddleRadius))
            .ToList();
        if (outside.Count > 0)
            kit.Party.WipeAllPlayers($"Gravitas: {string.Join(", ", outside)} not in the stack during Graven Image 2");
    }

    // Anyone else in a spread is thrown away from its target; a spread touching a puddle sets it
    // off.
    private void ResolveVitrophyre(int set)
    {
        if (kit == null) return;
        var i = 4;
        foreach (var role in state.YellowRoles(set))
        {
            var statue = statues[i++];
            if (kit.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            statue?.Cast(Constants.ActionId.Vitrophyre, castSeconds: 0f, targetId: target.GameObjectId, animationLock: AnimationLock.Helper);
            if (puddles.Any(p => !p.Popped && DistanceXZ(p.Position, target.Position) < VitrophyreRadius + PuddleRadius))
            {
                ExplodePuddle($"{role}'s Vitrophyre touched a Gravitas puddle");
                return;
            }
            foreach (var other in kit.Party.Find.InsideCircle(target.Position, VitrophyreRadius))
                if (!ReferenceEquals(other, target))
                    (other as ISimPartyMember)?.Knockback(target.Position, VitrophyreKnockback);
        }
    }

    private void ExplodePuddle(string cause)
    {
        if (kit == null) return;
        puddles.Clear();
        kit.KefkaCast(Constants.ActionId.GravitationalExplosion, 0f, AnimationLock.Helper);
        kit.Party.WipeAllPlayers($"Gravitational Explosion: {cause} during Graven Image 2");
    }

    private void AimCleave(int i)
    {
        if (kit == null) return;
        kit.Beat(CleaveTelegraph(i), StatueBeat.Fire);
        statues[8]?.SetPosition(new Placement(Vector3.Zero, state.CleaveWest[i] ? -MathF.PI / 2f : MathF.PI / 2f));
    }

    private void ResolveCleave(int i)
    {
        if (kit == null || statues[8] is not { } caster) return;
        var actionId = state.CleaveWest[i] ? Constants.ActionId.GravitationalWave : Constants.ActionId.IntemperateWill;
        caster.Cast(actionId, castSeconds: 0f, animationLock: AnimationLock.Helper);
        kit.Damage.Resolve(caster, actionId, [DamageType.Lethal], [], size: CleaveHalfAngle);
    }

    private void TickPuddles()
    {
        if (kit == null || puddles.Count == 0) return;
        foreach (var puddle in puddles)
        {
            if (puddle.Popped) continue;
            var inside = kit.Party.Find.InsideCircle(puddle.Position, PuddleRadius);
            if (!puddlesSoakable)
            {
                puddle.Exempt.IntersectWith(inside);
                if (inside.FirstOrDefault(m => !puddle.Exempt.Contains(m)) is { } intruder)
                {
                    ExplodePuddle($"{(intruder as ISimPartyMember)?.Role.ToString() ?? "someone"} stepped into a Gravitas puddle");
                    return;
                }
                continue;
            }
            if (puddle.Triggered || inside.Count == 0) continue;
            puddle.Triggered = true;
            var target = puddle;
            kit.World.Events.Add(PuddlePopDelay, () => PopPuddle(target));
        }
    }

    // A full soak takes 4; under 3 kills whoever soaked it, and anything short of 4 fails the
    // puzzle (UNVERIFIED thresholds).
    private void PopPuddle(Puddle puddle)
    {
        if (kit == null || puddle.Popped) return;
        puddle.Popped = true;
        var soakers = kit.Party.Find.InsideCircle(puddle.Position, PuddleRadius);
        if (puddles.IndexOf(puddle) is >= 0 and var index && kit.Helper(index % UmadP1Kit.TrapHelperBase) is { } helper)
        {
            helper.SetPosition(new Placement(puddle.Position, 0f));
            foreach (var soaker in soakers)
                helper.Cast(Constants.ActionId.GravityIII, castSeconds: 0f, targetId: soaker.GameObjectId, animationLock: AnimationLock.Helper);
        }
        if (soakers.Count < 3)
            foreach (var soaker in soakers.ToList())
                soaker.Die($"Gravity III soaked by only {soakers.Count} during Graven Image 2");
        if (soakers.Count < 4) FailPuzzle($"a set-{puddle.Set + 1} puddle was popped by {soakers.Count} player(s), not 4");
    }

    private void RefreshUnpoppedPuddleOmens()
    {
        if (kit == null) return;
        foreach (var puddle in puddles.Where(p => !p.Popped))
            kit.World.SpawnOmen(Constants.VfxPath.PuddleOmen, new Placement(puddle.Position, 0f), new Vector3(PuddleRadius, 1f, PuddleRadius), 3.4f);
    }

    private void ResolveLeftoverPuddles()
    {
        foreach (var puddle in puddles.Where(p => !p.Popped).ToList())
        {
            PopPuddle(puddle);
            FailPuzzle("a puddle was never popped");
        }
    }

    private void FailPuzzle(string reason)
    {
        if (puzzleFailed) return;
        puzzleFailed = true;
        puzzleFailure = reason;
        DiagnosticLog.Info($"[UmadP1GravenImage2] Puddle puzzle failed: {reason}.");
    }

    // Unpopped or under-soaked puddles fail the phase's puzzle, which ends the real fight at the
    // P1 enrage.
    private void PunishFailedPuzzle()
    {
        if (kit == null || !puzzleFailed) return;
        kit.World.Announce($"Not every Gravitas puddle was popped by 4 ({puzzleFailure}).");
        kit.Party.WipeAllPlayers($"Failed the Gravitas puddle puzzle ({puzzleFailure})");
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
        => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

    public MpMessage? BuildReplayStateMessage() => LastState?.ToReplayMessage();

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP1GravenImage2AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (UmadP1GravenImage2State.FromNetworkReplay(msg) is not { } shadowState) return null;
        ((IScenarioAi<UmadP1GravenImage2State>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }
}
