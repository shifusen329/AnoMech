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
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

using Constants = UmadP1GravenConstants;

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
    public bool SupportsMitigationPractice => true;
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
    private const float PuddlePopDelay = 2f;

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
    private string? puzzleFailure;

    private sealed class Puddle(Vector3 position, SimEventObject? visual)
    {
        public Vector3 Position { get; } = position;
        public SimEventObject? Visual { get; } = visual;
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
        var k = kit = new UmadP1Kit(world, "UmadP1GravenImage2", overrides.PropsBindDirector);
        puddles.Clear();
        puddlesSoakable = false;
        puzzleFailure = null;
        world.EnforceArenaBoundary(UmadP1Kit.ArenaRadius, "Knocked off the arena");
        world.MitPractice.Begin(UmadP1GravenImage2Mitigation.Plan(),
            new Dictionary<MitSource, Func<SimCharacter?>> { [UmadMitigation.Kefka] = () => k.Kefka });
        DiagnosticLog.Info($"[UmadP1GravenImage2] Roll: purpleDps=[{string.Join(",", state.PurpleDps)}] ice={state.Ice} "
            + $"cleaveWest=[{string.Join(",", state.CleaveWest)}] trap={state.TrapSupport}/{state.TrapDps}.");

        world.Events.Add(0f, () =>
        {
            k.SpawnKefka();
            statues = k.SpawnGravenImages(
                CentralStatuePosition, CentralStatuePosition, CentralStatuePosition, CentralStatuePosition,
                RightStatuePosition, RightStatuePosition, RightStatuePosition, RightStatuePosition, Vector3.Zero);
            centralProp = k.SpawnProp(Constants.EObjId.StatueCentral, 0x4000EBA2u, Constants.EObjId.StatueCentralArg2, CentralStatuePosition);
            rightProp = k.SpawnProp(Constants.EObjId.StatueRight, 0x4000EBA3u, Constants.EObjId.StatueRightArg2, RightStatuePosition);
            westTelegraph = k.SpawnProp(Constants.EObjId.StatueCentral, 0x4000EBA4u, Constants.EObjId.WestTelegraphArg2, WestTelegraphPosition);
            eastTelegraph = k.SpawnProp(Constants.EObjId.StatueRight, 0x4000EBA5u, Constants.EObjId.EastTelegraphArg2, EastTelegraphPosition);
            k.SpawnColossus((ushort)StatueBeat.ColossusStage2.State);
            k.ApplyTrap(state.TrapSupport, 42.96f);
            k.ApplyTrap(state.TrapDps, 42.96f);
        });

        foreach (var at in new[] { 5.38f, 13.45f, 16.49f, 24.55f, 27.59f, 32.67f, 35.70f, 38.72f, 41.75f, 46.84f, 49.87f, 57.94f, 60.96f, 63.99f })
            world.Events.Add(at, k.AutoAttack);
        world.Events.Add(0.53f, () => k.KefkaCast(Constants.ActionId.TeleportP1, 1.1f));
        world.Events.Add(1.86f, () => k.KefkaLine(0x2U));
        world.Events.Add(1.95f, () => k.KefkaCast(Constants.ActionId.GravenImage, 2.1f));

        // First tethers, with the ice cones.
        world.Events.Add(5.25f, PlaceStatues);
        world.Events.Add(5.69f, () =>
        {
            UmadP1Kit.Beat(centralProp, StatueBeat.Appear);
            UmadP1Kit.Beat(rightProp, StatueBeat.Appear);
            TetherStatues(0, 6.51f, 10.53f);
        });
        world.Events.Add(6.98f, () => k.AttachIceOrb(state.Ice));
        world.Events.Add(7.07f, () =>
        {
            k.KefkaCast(UmadConstants.ActionId.BlizzardIIIBlowout_Cast, 3.1f);
            k.CastIce(state.Ice);
        });
        world.Events.Add(12.20f, () => CastGravitas(0));
        world.Events.Add(16.22f, () => CastVitrophyre(0));

        // Revolting Ruin III, then the first cleave as the second tethers land.
        world.Events.Add(16.98f, () => k.RevoltingRuin(followUp: false));
        world.Events.Add(20.90f, () => UmadP1Kit.Beat(CleaveTelegraph(0), StatueBeat.Telegraph));
        world.Events.Add(21.80f, PlaceStatues);
        world.Events.Add(22.15f, () => TetherStatues(1, 8.51f, 12.52f));
        world.Events.Add(25.27f, () => k.RevoltingRuin(followUp: true));
        world.Events.Add(25.94f, () => AimCleave(0));
        world.Events.Add(26.03f, () => CastCleave(0));

        // Second puddles and spreads, then the second cleave.
        world.Events.Add(30.66f, () => CastGravitas(1));
        world.Events.Add(34.14f, () => UmadP1Kit.Beat(CleaveTelegraph(1), StatueBeat.Telegraph));
        world.Events.Add(34.67f, () => CastVitrophyre(1));
        world.Events.Add(38.59f, () =>
        {
            UmadP1Kit.Beat(centralProp, StatueBeat.Vanish);
            UmadP1Kit.Beat(rightProp, StatueBeat.Vanish);
        });
        world.Events.Add(39.17f, () => AimCleave(1));
        world.Events.Add(39.26f, () => CastCleave(1));

        // Double-trouble Trap 2 throws each role into the far puddles; from here they can be
        // popped, each 2s after someone first steps in.
        world.Events.Add(43.00f, () =>
        {
            k.ResolveTrapStack(state.TrapSupport, UmadP1Kit.TrapHelperBase);
            k.ResolveTrapStack(state.TrapDps, UmadP1Kit.TrapHelperBase + 1);
            puddlesSoakable = true;
            foreach (var puddle in puddles) UmadP1Kit.Beat(puddle.Visual, Constants.Puddle.SoakReady);
        });
        world.Events.Add(49.40f, PopLeftoverPuddles);
        world.Events.Add(49.50f, PunishFailedPuzzle);

        world.Events.Add(52.15f, () =>
        {
            k.BeatColossus(StatueBeat.ColossusStage3);
            if (!Plugin.Config.SuppressBgm) world.PlayBgm(Constants.Bgm.Late);
        });
        world.Events.Add(52.24f, k.LightOfJudgment);
        world.Events.Add(60.39f, k.Hyperdrive);
        world.Events.Add(62.48f, k.Hyperdrive);
        world.Events.Add(64.57f, k.Hyperdrive);
        world.Events.Add(66.58f, () => k.KefkaLine(0xBU));

        if (selectedAi is { } idx && idx < AiStrats.Count)
            ((IScenarioAi<UmadP1GravenImage2State>)AiStrats[idx]).Run(state, world);
    }

    public void RunInstanceEvents(SimWorld world)
    {
        world.Events.Add(0.05f, () => world.Map.AddEffect(Constants.ArenaState.Gi2Rest, 0));
        world.Events.Add(52.15f, () => world.Map.AddEffect(Constants.ArenaState.ToLate, 0));
    }

    public void Tick(float delta, float elapsed) => TickPuddles();

    public void OnActionResolved(ActionResolved resolved)
    {
        switch (resolved.Action.ActionId)
        {
            case Constants.ActionId.Gravitas: DropPuddle(resolved); break;
            case Constants.ActionId.GravityIII: JudgePuddleSoak(resolved); break;
        }
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

    private void CastGravitas(int set)
    {
        if (kit == null) return;
        var i = 0;
        foreach (var role in state.PurpleRoles(set))
        {
            var statue = statues[i++];
            if (kit.Party.Get(role) is { } target && target.IsAlive()) statue?.Cast(UmadActions.Gravitas, target);
        }
    }

    // Each puddle drops under its target. Whoever stands in it as it lands may walk out; walking
    // back in before the soak wipes the party. Every Gravitas has to land on the whole party:
    // anyone left out of any of them wipes everyone.
    private void DropPuddle(ActionResolved resolved)
    {
        if (kit == null) return;
        var outside = UmadP1Roles.All
            .Where(r => kit.Party.Get(r) is { } m && m.IsAlive() && !resolved.Hits.Contains(m))
            .ToList();
        if (outside.Count > 0)
        {
            kit.Party.WipeAllPlayers(Constants.ActionId.Gravitas, $"{string.Join(", ", outside)} not in the Gravitas stack");
            return;
        }
        var position = resolved.Origin.Position;
        var puddle = new Puddle(position, kit.SpawnPuddle(position, puddles.Count));
        puddle.Exempt.UnionWith(kit.Party.Find.InsideCircle(position, PuddleRadius));
        puddles.Add(puddle);
    }

    // A spread touching a puddle sets it off.
    private void CastVitrophyre(int set)
    {
        if (kit == null) return;
        var i = 4;
        foreach (var role in state.YellowRoles(set))
        {
            var statue = statues[i++];
            if (kit.Party.Get(role) is not { } target || !target.IsAlive()) continue;
            if (puddles.Any(p => !p.Popped && DistanceXZ(p.Position, target.Position) < VitrophyreRadius + PuddleRadius))
            {
                ExplodePuddles($"{role}'s Vitrophyre touched a Gravitas puddle");
                return;
            }
            statue?.Cast(UmadActions.Vitrophyre, target);
        }
    }

    private void ExplodePuddles(string cause)
    {
        if (kit == null) return;
        foreach (var puddle in puddles) UmadP1Kit.Beat(puddle.Visual, Constants.Puddle.Explode);
        puddles.Clear();
        kit.KefkaCast(Constants.ActionId.GravitationalExplosion, 1.1f);
        kit.Party.WipeAllPlayers(Constants.ActionId.GravitationalExplosion, cause);
    }

    private void AimCleave(int i)
    {
        UmadP1Kit.Beat(CleaveTelegraph(i), StatueBeat.Fire);
        if (statues.Length > 8)
            statues[8]?.SetPosition(new Placement(Vector3.Zero, state.CleaveWest[i] ? -MathF.PI / 2f : MathF.PI / 2f));
    }

    private void CastCleave(int i)
    {
        if (statues.Length > 8)
            statues[8]?.Cast(state.CleaveWest[i] ? UmadActions.GravitationalWave : UmadActions.IntemperateWill);
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
                    ExplodePuddles($"{(intruder as ISimPartyMember)?.Role.ToString() ?? "someone"} stepped into a Gravitas puddle");
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

    private void PopPuddle(Puddle puddle)
    {
        if (kit == null || puddle.Popped) return;
        puddle.Popped = true;
        UmadP1Kit.Beat(puddle.Visual, Constants.Puddle.Explode);
        if (kit.Helper(puddles.IndexOf(puddle) % UmadP1Kit.TrapHelperBase) is not { } helper) return;
        helper.SetPosition(new Placement(puddle.Position, 0f));
        helper.Cast(UmadActions.GravityIII);
    }

    // A full soak takes 4; anything short fails the puzzle (UNVERIFIED threshold).
    private void JudgePuddleSoak(ActionResolved resolved)
    {
        if (resolved.Hits.Count < 4) FailPuzzle($"a puddle was popped by {resolved.Hits.Count} player(s), not 4");
    }

    private void PopLeftoverPuddles()
    {
        foreach (var puddle in puddles.Where(p => !p.Popped).ToList())
        {
            PopPuddle(puddle);
            FailPuzzle("a puddle was never popped");
        }
    }

    private void FailPuzzle(string reason)
    {
        if (puzzleFailure != null) return;
        puzzleFailure = reason;
        DiagnosticLog.Info($"[UmadP1GravenImage2] Puddle puzzle failed: {reason}.");
    }

    // Unpopped or under-soaked puddles fail the phase's puzzle, which ends the real fight at the
    // P1 enrage.
    private void PunishFailedPuzzle()
    {
        if (kit == null || puzzleFailure == null) return;
        kit.World.Announce($"Not every Gravitas puddle was popped by 4 ({puzzleFailure}).");
        kit.Party.WipeAllPlayers(Constants.ActionId.GravityIII, $"failed the Gravitas puddle puzzle ({puzzleFailure})");
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
        => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));

    public MpMessage? BuildReplayStateMessage() => LastState?.ToReplayMessage();

    public object? StartReplay(MpMessage message, int aiIndex, PartyRole myRole, SimWorld replayWorld)
    {
        if (message is not UmadP1GravenImage2AiReplayStateMessage msg || aiIndex < 0 || aiIndex >= AiStrats.Count) return null;
        if (UmadP1GravenImage2State.FromNetworkReplay(msg) is not { } shadowState) return null;
        ((IScenarioAi<UmadP1GravenImage2State>)AiStrats[aiIndex]).Run(shadowState, replayWorld);
        return shadowState;
    }
}
