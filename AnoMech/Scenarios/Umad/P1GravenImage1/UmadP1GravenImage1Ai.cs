using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

public sealed class UmadP1GravenImage1Ai : IScenarioAi<UmadP1GravenImage1State>
{
    public string Name => "NAUR";

    internal const float PulseWaveDistance = 13f;
    internal static readonly Vector2 StatueXZ = new(UmadP1GravenImage1Scenario.StatuePosition.X, UmadP1GravenImage1Scenario.StatuePosition.Z);

    private static readonly Vector2[] QuadrantSpread = [new(1f, 7f), new(7f, 1f), new(12.5f, 6.5f), new(18f, 1f)];
    private static readonly Vector2 QuadrantStack = new(4f, 4f);

    internal static readonly Vector2[] WaveCannonLineupSpots =
    [
        new(-3.1f, 4.3f), new(-7.4f, 0.4f),
        new(-13.8f, -1.8f), new(-18.3f, -4.2f),
        new(3.1f, 4.3f), new(7.4f, 0.4f),
        new(13.8f, -1.8f), new(18.3f, -4.2f),
    ];

    private const float TowerClearance = UmadP1GravenImage1Scenario.TowerRadius + 1.5f;
    private const float TrapStackRadius = 5f;
    private const float TrapHolderRadius = 8f;
    private const float TrapStackSpacing = 0.4f;
    private const float DodgeMargin = 1.5f;

    public void Run(UmadP1GravenImage1State state, SimWorld world)
    {
        var ai = new AiManager(world);
        ai.Move(0.3f, Uptime, cue: UmadP1GravenImage1Playbook.Uptime);
        ai.Move(8.25f, () => KnockbackPrepositions(state), arrivalTime: 10.7f, cue: UmadP1GravenImage1Playbook.KnockbackTether(state));
        ai.Move(8.25f, () => UntetheredQuadrantSpots(state), arrivalTime: 13.0f, settleFraction: 0.5f, cue: UmadP1GravenImage1Playbook.MysteryMagic1(state));
        ai.Move(11.5f, () => TetheredQuadrantSpots(state), jitter: 0.2f, arrivalTime: 13.0f, cue: UmadP1GravenImage1Playbook.MysteryMagic1(state));
        ai.Move(14.1f, WaveCannonLineup, jitter: 0.2f, arrivalTime: 17.9f, settleFraction: 0.5f, cue: UmadP1GravenImage1Playbook.WaveCannonLineup);
        ai.Move(18.87f, () => TowerSoaks(state, world), jitter: 0.2f, arrivalTime: 21.5f, settleFraction: 0.5f, cue: UmadP1GravenImage1Playbook.Towers(state));
        ai.Move(21.95f, () => TrapStacks(state), jitter: 0.1f, arrivalTime: 25.2f, settleFraction: 0.5f, cue: UmadP1GravenImage1Playbook.Confetti(state));
        ai.Move(26.25f, () => MysteryMagic2Dodges(state, world), jitter: 0.2f, arrivalTime: 29.2f, cue: UmadP1GravenImage1Playbook.MysteryMagic2(state));
        ai.Move(29.6f, Uptime, arrivalTime: 33f, settleFraction: 0.5f, cue: UmadP1GravenImage1Playbook.LightOfJudgment);
    }

    private static IAiMove Uptime() => AiMove.Create(UmadP1Spots.Uptime.Select(p => (Vector2?)p).ToArray()).NaturalOrder();

    internal static Vector2 QuadrantSpot(UmadP1GravenImage1State state, PartyRole role)
    {
        var q = state.Ice1.SafeQuadrant(west: !UmadP1Roles.IsDps(role));
        if (state.Fire.IsStack)
            return new Vector2(QuadrantStack.X * q.X, QuadrantStack.Y * q.Y) + UmadP1Spots.Offset(role, 0.5f);
        var u = QuadrantSpread[(int)role % 4];
        return new Vector2(u.X * q.X, u.Y * q.Y);
    }

    private static IAiMove KnockbackPrepositions(UmadP1GravenImage1State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in state.TetheredRoles)
            coords[(int)role] = PrepositionForKnockback(QuadrantSpot(state, role), state.Ice1.SafeQuadrant(west: !UmadP1Roles.IsDps(role)));
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove UntetheredQuadrantSpots(UmadP1GravenImage1State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.Of(!state.TetherDps)) coords[(int)role] = QuadrantSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove TetheredQuadrantSpots(UmadP1GravenImage1State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in state.TetheredRoles) coords[(int)role] = QuadrantSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static Vector2 KnockbackLanding(Vector2 from)
        => from + Vector2.Normalize(from - StatueXZ) * PulseWaveDistance;

    internal static Vector2 PrepositionForKnockback(Vector2 goal, Vector2 quadrant)
    {
        var best = goal;
        var bestDistance = float.PositiveInfinity;
        for (var ix = -36; ix <= 36; ix++)
            for (var iz = -36; iz <= 36; iz++)
            {
                var p = new Vector2(ix * 0.5f, iz * 0.5f);
                if (p.Length() > 17.5f) continue;
                var land = KnockbackLanding(p);
                if (land.Length() > 18f || land.X * quadrant.X < 1f || land.Y * quadrant.Y < 1f) continue;
                var distance = Vector2.Distance(land, goal);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = p;
            }
        return best;
    }

    private static IAiMove WaveCannonLineup() => AiMove.Create(WaveCannonLineupSpots.Select(p => (Vector2?)p).ToArray()).NaturalOrder();

    private static IAiMove TowerSoaks(UmadP1GravenImage1State state, SimWorld world)
    {
        var positions = new Dictionary<PartyRole, Vector2>();
        var vulnerable = new HashSet<PartyRole>();
        foreach (var role in UmadP1Roles.All)
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            positions[role] = XZ(member.Position);
            if (member.HasStatus(UmadConstants.StatusId.MagicVulnerabilityUp)) vulnerable.Add(role);
        }
        return AiMove.Create(TowerSoakSpots(state.WaveTargets, positions, vulnerable)).NaturalOrder();
    }

    internal static Vector2?[] TowerSoakSpots(IReadOnlyList<PartyRole> waveTargets, IReadOnlyDictionary<PartyRole, Vector2> positions, IReadOnlySet<PartyRole> vulnerable)
    {
        var coords = new Vector2?[8];
        var towers = waveTargets.Where(positions.ContainsKey).Select(t => (Position: positions[t], Dps: UmadP1Roles.IsDps(t))).ToList();
        var towerSpots = towers.Select(t => t.Position).ToList();
        foreach (var dps in new[] { false, true })
        {
            var groupTowers = towers.Where(t => t.Dps == dps).Select(t => t.Position).OrderBy(p => p.X).ToList();
            var soakers = new List<(PartyRole Role, Vector2 Position)>();
            foreach (var role in UmadP1Roles.Of(dps))
            {
                if (!positions.TryGetValue(role, out var position)) continue;
                if (vulnerable.Contains(role)) coords[(int)role] = TowerExit(position, towerSpots);
                else soakers.Add((role, position));
            }
            soakers.Sort((a, b) => a.Position.X.CompareTo(b.Position.X));
            for (var i = 0; i < Math.Min(groupTowers.Count, soakers.Count); i++)
                coords[(int)soakers[i].Role] = groupTowers[i];
        }
        return coords;
    }

    internal static Vector2 TowerExit(Vector2 from, IReadOnlyList<Vector2> towers)
    {
        var best = from;
        var bestDistance = float.PositiveInfinity;
        for (var ix = -16; ix <= 16; ix++)
            for (var iz = -16; iz <= 16; iz++)
            {
                var p = from + new Vector2(ix, iz) * 0.5f;
                if (p.Length() > 18f || towers.Any(t => Vector2.Distance(p, t) < TowerClearance)) continue;
                var distance = Vector2.Distance(p, from);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = p;
            }
        return best;
    }

    internal static Vector2 TrapSpot(UmadP1GravenImage1State state, PartyRole role)
    {
        var dps = UmadP1Roles.IsDps(role);
        var direction = new Vector2(dps ? 1f : -1f, 0f);
        if (role == (dps ? state.TrapDps : state.TrapSupport)) return direction * TrapHolderRadius;
        var across = new Vector2(direction.Y, -direction.X);
        var k = UmadP1Roles.Of(dps).Where(r => r != (dps ? state.TrapDps : state.TrapSupport)).ToList().IndexOf(role) - 1;
        return UmadP1Spots.TrapStackSpot(direction, TrapStackRadius, across, TrapStackSpacing, k);
    }

    private static IAiMove TrapStacks(UmadP1GravenImage1State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = TrapSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static float MysteryMagic2Clearance(UmadP1GravenImage1State state, Vector2 p)
        => MathF.Min(state.Ice2.Clearance(p), state.Thunder.Clearance(p));

    private static IAiMove MysteryMagic2Dodges(UmadP1GravenImage1State state, SimWorld world)
    {
        var safe = MysteryMagic2SafePoints(state);
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All)
            if (world.Party.Get(role) is { } member && member.IsAlive())
                coords[(int)role] = MysteryMagic2Dodge(safe, XZ(member.Position), role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static List<Vector2> MysteryMagic2SafePoints(UmadP1GravenImage1State state)
        => UmadP1Spots.SafePoints(p => MysteryMagic2Clearance(state, p), DodgeMargin);

    internal static Vector2 MysteryMagic2Dodge(IReadOnlyList<Vector2> safe, Vector2 from, PartyRole role)
        => UmadP1Spots.Nearest(safe, from) + UmadP1Spots.Offset(role, 0.6f);

    private static Vector2 XZ(Vector3 v) => new(v.X, v.Z);
}
