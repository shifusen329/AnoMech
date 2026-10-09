using System;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P3BlackHole;
using static AnoMech.Scenarios.Umad.P3LimitCut.UmadP3LimitCutConstants;

namespace AnoMech.Scenarios.Umad.P3LimitCut;

public sealed class UmadP3LimitCutAi : IScenarioAi<UmadP3LimitCutState>
{
    public string Name => "Standard";

    private UmadP3LimitCutState state = null!;
    private SimWorld world = null!;

    public void Run(UmadP3LimitCutState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, StackAtBosses, cue: UmadP3LimitCutPlaybook.Stack(state));
        ai.Move(4.5f, BaitOut, arrivalTime: 7.5f, cue: UmadP3LimitCutPlaybook.UmbraBait(state));
        ai.Move(8.3f, TanksIntoStack, arrivalTime: 12.0f, cue: UmadP3LimitCutPlaybook.TanksIntoStack);
        ai.Move(8.4f, BaitBack, arrivalTime: 12.6f, cue: UmadP3LimitCutPlaybook.BaitBack(state));
        world.Events.Add(15.3f, FaceForVacuumWave);
        ai.Move(17.5f, StackForCyclones, arrivalTime: 19.6f, cue: UmadP3LimitCutPlaybook.Cyclones);
        ai.Move(20.6f, ChargeSpots, arrivalTime: 30.2f, cue: UmadP3LimitCutPlaybook.Charges(state));
        ai.Move(32.7f, ClearForThunder, arrivalTime: 37.5f, cue: UmadP3LimitCutPlaybook.ClearForThunder(state));
        world.Events.Add(33.6f, FirstTankOntoExdeath);
        world.Events.Add(37.5f, InvulnPlannedTank);
        ai.Move(38.9f, SwapThunderTanks, arrivalTime: 41.2f, cue: UmadP3LimitCutPlaybook.ThunderSwap(state));
        ScheduleThunderClearance(36.5f, 38.9f, 42.2f);
        ai.Move(43.6f, SplitForDecisiveBattle, arrivalTime: 46.3f, cue: UmadP3LimitCutPlaybook.DecisiveBattleSplit);
        ai.Move(47.0f, StackAtBosses, cue: UmadP3LimitCutPlaybook.Uptime(state));
    }

    private (PartyRole First, PartyRole? Second) ThunderRoles => ThunderIIIPlanning.Roles(state.ThunderPlan);
    private PartyRole ThunderBystanderTank => ThunderRoles.First == PartyRole.MainTank ? PartyRole.OffTank : PartyRole.MainTank;

    private IAiMove ClearForThunder()
    {
        var coords = new Vector2?[8];
        coords[(int)ThunderBystanderTank] = Flat(Geometry.OnCircle(Hold, Geometry.ExdeathHoldRadius));
        for (var slot = 2; slot < 8; slot++)
            coords[slot] = Flat(Geometry.OnCircle(Hold + MathF.PI, 4f) + Geometry.OnCircle(Hold + MathF.PI / 2f, (slot % 3 - 1) * 1.5f) + Geometry.OnCircle(Hold + MathF.PI, slot / 3 * 1.5f));
        return AiMove.Create(coords).NaturalOrder();
    }

    private void FirstTankOntoExdeath()
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        if (world.Party.Get(ThunderRoles.First) is { } first && first.IsAlive())
        {
            world.Strat.Note(ThunderRoles.First, Flat(exdeath.Position), UmadP3LimitCutPlaybook.ThunderTank(state), deadline: null);
            first.Follow(exdeath);
        }
    }

    private void InvulnPlannedTank()
    {
        if (ThunderIIIPlanning.InvulnRole(state.ThunderPlan) is not { } role) return;
        if (world.Party.Get(role) is ISimPartyMember tank && ((SimCharacter)tank).IsAlive())
            tank.UseInvuln();
    }

    private IAiMove SwapThunderTanks()
    {
        var (first, second) = ThunderRoles;
        if (second is not { } secondRole) return AiMove.Create().NaturalOrder();
        var a = world.Party.Get(first);
        var b = world.Party.Get(secondRole);
        if (a is null || b is null) return AiMove.Create().NaturalOrder();
        var coords = new Vector2?[8];
        coords[(int)secondRole] = Flat(a.Position);
        coords[(int)first] = Flat(b.Position);
        return AiMove.Create(coords).NaturalOrder();
    }

    private const float ThunderClearDistance = 10f;
    private const float ThunderClearanceInterval = 0.4f;

    private void ScheduleThunderClearance(float fromTime, float swapTime, float toTime)
    {
        var (first, second) = ThunderRoles;
        for (var t = fromTime; t < swapTime; t += ThunderClearanceInterval)
            world.Events.Add(t, () => EnforceThunderClearanceOnce(first, second));
        var postSwapExcluded = second ?? first;
        for (var t = swapTime; t <= toTime; t += ThunderClearanceInterval)
            world.Events.Add(t, () => EnforceThunderClearanceOnce(postSwapExcluded, null));
    }

    private void EnforceThunderClearanceOnce(PartyRole first, PartyRole? second)
    {
        if (second is { } secondRole
            && world.Party.Get(first) is { } firstMember && firstMember.IsAlive()
            && world.Party.Get(secondRole) is { } secondMember && secondMember.IsAlive())
        {
            var firstPos = Flat(firstMember.Position);
            var secondPos = Flat(secondMember.Position);
            if ((secondPos - firstPos).LengthSquared() < ThunderClearDistance * ThunderClearDistance)
            {
                var away = firstPos.LengthSquared() > 1e-4f ? Vector2.Normalize(-firstPos) : new Vector2(0f, -1f);
                var seat = firstPos + RotateVec(away, MathF.PI / 2f) * ThunderClearDistance;
                var seatClearRadius = Geometry.ArenaRadius - 1f;
                if (seat.LengthSquared() > seatClearRadius * seatClearRadius)
                    seat = seat.LengthSquared() > 1e-4f ? Vector2.Normalize(seat) * seatClearRadius : seat;
                world.Strat.Note(secondRole, seat, UmadP3LimitCutPlaybook.ThunderClearance(first, secondRole), deadline: null);
                secondMember.MoveTo(new Vector3(seat.X, 0f, seat.Y));
            }
        }

        if (state.Objects.Exdeath is not { } exdeath || !exdeath.IsAlive()) return;
        var bossPos = Flat(exdeath.Position);
        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            if (role == first || (second is { } sec && role == sec)) continue;
            if (world.Party.Get(i) is not { } member || !member.IsAlive()) continue;
            var offset = Flat(member.Position) - bossPos;
            if (offset.LengthSquared() >= ThunderClearDistance * ThunderClearDistance) continue;
            var dir = offset.LengthSquared() > 1e-4f ? Vector2.Normalize(offset) : new Vector2(1f, 0f);
            var target = bossPos + dir * ThunderClearDistance;
            var clearRadius = Geometry.ArenaRadius - 1f;
            if (target.LengthSquared() > clearRadius * clearRadius)
                target = target.LengthSquared() > 1e-4f ? Vector2.Normalize(target) * clearRadius : target;
            world.Strat.Note(role, target, UmadP3LimitCutPlaybook.ThunderClearance(first, second), deadline: null);
            member.MoveTo(new Vector3(target.X, 0f, target.Y));
        }
    }

    private static Vector2 RotateVec(Vector2 v, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    private float Hold => Geometry.SpotHeading(state.BossSpot);

    private Vector2 Flat(Vector3 v) => new(v.X, v.Z);

    private Vector2 StackSpot(int slot)
    {
        var spread = Geometry.OnCircle(Hold + MathF.PI / 2f, (slot % 4 - 1.5f) * 1.2f) + Geometry.OnCircle(Hold, slot / 4 * 1.4f);
        return Flat(Geometry.OnCircle(Hold, Geometry.StackRadius) + spread);
    }

    private IAiMove StackAtBosses()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = Flat(Geometry.OnCircle(Hold, Geometry.ChaosHoldRadius - 1.5f));
        coords[(int)PartyRole.OffTank] = Flat(Geometry.OnCircle(Hold, Geometry.ExdeathHoldRadius - 1.5f) + Geometry.OnCircle(Hold + MathF.PI / 2f, 2.5f));
        for (var slot = 2; slot < 8; slot++) coords[slot] = StackSpot(slot);
        return AiMove.Create(coords).NaturalOrder();
    }

    private static readonly PartyRole[] EpicHeroes = [PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];

    private IAiMove SplitForDecisiveBattle()
    {
        if (state.Objects.Chaos is not { } chaos || state.Objects.Exdeath is not { } exdeath) return StackAtBosses();
        var apart = Flat(chaos.Position - exdeath.Position);
        var away = apart.LengthSquared() > 0.01f ? Vector2.Normalize(apart) : Flat(Geometry.OnCircle(Hold + MathF.PI / 2f, 1f));
        var across = new Vector2(-away.Y, away.X);
        var coords = new Vector2?[8];
        var atChaos = 0;
        var atExdeath = 0;
        for (var slot = 0; slot < 8; slot++)
        {
            var epic = Array.IndexOf(EpicHeroes, (PartyRole)slot) >= 0;
            var n = epic ? atChaos++ : atExdeath++;
            var beside = epic ? Flat(chaos.Position) + away * 3f : Flat(exdeath.Position) - away * 3f;
            coords[slot] = InsideArena(beside + across * (n - 1.5f));
        }
        return AiMove.Create(coords).NaturalOrder();
    }

    private static Vector2 InsideArena(Vector2 spot)
    {
        var limit = Geometry.ArenaRadius - 1f;
        return spot.LengthSquared() > limit * limit ? Vector2.Normalize(spot) * limit : spot;
    }

    private IAiMove TanksIntoStack()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = StackSpot((int)PartyRole.MainTank);
        coords[(int)PartyRole.OffTank] = StackSpot((int)PartyRole.OffTank);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove StackForCyclones()
    {
        var away = state.Objects.Exdeath is { } exdeath && exdeath.Position.LengthSquared() > 1f
            ? MathF.Atan2(-exdeath.Position.X, -exdeath.Position.Z)
            : Hold + MathF.PI;
        var centre = Geometry.OnCircle(away, Geometry.CycloneStackRadius);
        var coords = new Vector2?[8];
        for (var slot = 0; slot < 8; slot++)
            coords[slot] = Flat(centre + Geometry.OnCircle(away + MathF.PI / 2f, slot % 3 - 1f) + Geometry.OnCircle(away, slot / 3));
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove BaitOut() => AiMove.Single(state.BaitRole, Flat(Geometry.OnCircle(Hold + MathF.PI, Geometry.BaitRadius)));

    private IAiMove BaitBack() => AiMove.Single(state.BaitRole, StackSpot((int)state.BaitRole));

    private IAiMove ChargeSpots()
    {
        var coords = new Vector2?[8];
        for (var k = 0; k < 8; k++)
            coords[(int)state.Numbers[k]] = Flat(Geometry.OnCircle(state.SafeHeading(k), Geometry.ChargeStandRadius));
        return AiMove.Create(coords).NaturalOrder();
    }

    private void FaceForVacuumWave()
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        var cue = UmadP3LimitCutPlaybook.VacuumWave(state);
        for (var slot = 0; slot < 8; slot++)
        {
            var member = world.Party.Get(slot);
            if (member is null || !member.IsAlive() || member is not ISimPartyMember pm) continue;
            world.Strat.Note(pm.Role, StackSpot(slot), cue, deadline: null);
            if (member is SimPlayer && !DebugBotControl.Enabled) continue;
            var away = member.Position - exdeath.Position;
            member.Face(state.Winds[pm.Role] == Wind.Headwind ? member.Position + away : exdeath.Position);
        }
    }
}
