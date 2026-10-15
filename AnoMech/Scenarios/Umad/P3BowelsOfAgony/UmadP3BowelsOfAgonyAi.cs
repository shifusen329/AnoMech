using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;
using static AnoMech.Scenarios.Umad.P3BowelsOfAgony.UmadP3BowelsOfAgonyConstants;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

public sealed class UmadP3BowelsOfAgonyAi(UmadP3BowelsOfAgonyAi.Strat strat) : IScenarioAi<UmadP3BowelsOfAgonyState>
{
    public enum Strat { Lb3, Sg3k }

    public string Name => strat == Strat.Lb3 ? "LB3" : "SG3K";

    private UmadP3BowelsOfAgonyState state = null!;
    private SimWorld world = null!;

    public void Run(UmadP3BowelsOfAgonyState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, SplitForDecisiveBattle, arrivalTime: 9.5f, cue: UmadP3BowelsOfAgonyPlaybook.DecisiveBattleSplit);
        ai.Move(11.0f, PullBossesToTheMiddle, arrivalTime: 16.0f, cue: UmadP3BowelsOfAgonyPlaybook.BossesToTheMiddle);
        if (strat == Strat.Lb3) RunLb3(ai);
        else RunSg3k(ai);
        world.Events.Add(50.5f, () => ThunderTanksHeadIn(windPairsToo: false));
        world.Events.Add(54.1f, () => ThunderTanksHeadIn(windPairsToo: true));
        world.Events.Add(57.6f, InvulnPlannedTank);
        ai.Move(58.6f, SwapThunderTanks, arrivalTime: 61.1f, cue: UmadP3BowelsOfAgonyPlaybook.ThunderSwap(state));
        ScheduleThunderClearance(54.4f, 58.6f, 62.0f);
    }

    private void RunLb3(AiManager ai)
    {
        ai.Move(30.6f, Lb3TakeBossesToTheWindAndRangedToTheShortCrystal, arrivalTime: 40.0f, settleFraction: 0.5f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3Setup(state));
        ai.Move(42.3f, Lb3OffTankLeavesExdeathForTheWall, arrivalTime: 47.5f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3ThunderAoe(state));
        ai.Move(43.5f, () => Lb3ResolveSpots(state.ShortElement), arrivalTime: 48.6f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3Resolve(state, state.ShortElement));
        ai.Move(50.4f, Lb3RegroupAtTheWall, arrivalTime: 52.5f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3Regroup);
        ai.Move(61.8f, Lb3TanksToTheWindAndRangedToTheLongCrystal, arrivalTime: 65.8f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3PreImplosion(state));
        ai.Move(66.5f, () => Lb3ImplosionSpots(firstHit: true), arrivalTime: 71.5f, cue: UmadP3BowelsOfAgonyPlaybook.Implosion(state, strat, firstHit: true));
        ai.Move(72.1f, () => Lb3ImplosionSpots(firstHit: false), arrivalTime: 73.7f, cue: UmadP3BowelsOfAgonyPlaybook.Implosion(state, strat, firstHit: false));
        ai.Move(74.1f, () => Lb3ResolveSpots(state.LongElement), arrivalTime: 75.7f, cue: UmadP3BowelsOfAgonyPlaybook.Lb3Resolve(state, state.LongElement));
    }

    private void RunSg3k(AiManager ai)
    {
        ai.Move(30.6f, Sg3kParkBossesAndTakeShortSpots, arrivalTime: 40.0f, settleFraction: 0.5f, cue: UmadP3BowelsOfAgonyPlaybook.Sg3kSetup(state));
        ai.Move(42.3f, () => Sg3kResolveSpots(state.ShortElement), arrivalTime: 48.6f, cue: UmadP3BowelsOfAgonyPlaybook.Sg3kResolve(state, state.ShortElement));
        world.Events.Add(49.7f, () => FaceForCrystal(state.ShortElement));
        ai.Move(61.8f, Sg3kChaosToTheMiddleFacingNorth, arrivalTime: 64.6f, cue: UmadP3BowelsOfAgonyPlaybook.Sg3kPreImplosion(state));
        ai.Move(64.8f, Sg3kMainTankNorthOfChaos, arrivalTime: 65.8f, cue: UmadP3BowelsOfAgonyPlaybook.Sg3kPreImplosion(state));
        ai.Move(66.5f, () => Sg3kImplosionSpots(firstHit: true), arrivalTime: 71.5f, cue: UmadP3BowelsOfAgonyPlaybook.Implosion(state, strat, firstHit: true));
        ai.Move(72.1f, () => Sg3kImplosionSpots(firstHit: false), arrivalTime: 73.7f, cue: UmadP3BowelsOfAgonyPlaybook.Implosion(state, strat, firstHit: false));
        ai.Move(74.1f, () => Sg3kResolveSpots(state.LongElement), arrivalTime: 75.7f, cue: UmadP3BowelsOfAgonyPlaybook.Sg3kResolve(state, state.LongElement));
        world.Events.Add(76.7f, () => FaceForCrystal(state.LongElement));
    }

    private static readonly PartyRole[] WallSix =
        [PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB];
    private static readonly PartyRole[] Ranged = [PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private const float StackRadius = 17.9f;
    private const float StackInnerRadius = 16.8f;
    private const float GroupSplitAngle = 11.5f * MathF.PI / 180f;
    private const float FireStepOutAngle = 32f * MathF.PI / 180f;
    private const float RangedCrystalAngle = 20f * MathF.PI / 180f;
    private const float RangedCrystalRadius = 17.5f;
    private const float ChaosHoldRadius = 10.4f;
    private const float MainTankHoldRadius = 16.8f;
    private const float ExdeathParkRadius = 15.5f;
    private const float ExdeathParkAngle = 12f * MathF.PI / 180f;
    private const float ImplosionMeleeDistance = 6.8f;
    private const float ImplosionFarRangedDistance = 22.5f;
    private const float ImplosionNearRangedDistance = 11f;
    private const float Deg = MathF.PI / 180f;
    private const float SafeLimit = Geometry.ArenaRadius - 0.8f;

    private float Hw => Geometry.SpotHeading(state.WindSpot);
    private int ShortSide => state.SideOf(state.ShortSpot);
    private int LongSide => -ShortSide;
    private int SideOf(Element element) => element == state.ShortElement ? ShortSide : LongSide;

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
    private static Vector2 Polar(float heading, float radius) => Flat(Geometry.OnCircle(heading, radius));
    private Vector2 Frame(float along, float across, int side) => Polar(Hw, along) + Polar(Hw + side * MathF.PI / 2f, across);

    private static Vector2 InsideArena(Vector2 spot)
        => spot.LengthSquared() > SafeLimit * SafeLimit ? Vector2.Normalize(spot) * SafeLimit : spot;

    private static bool IsSupport(PartyRole role) => UmadP3BowelsOfAgonyState.IsSupport(role);

    private IAiMove SplitForDecisiveBattle()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = new(-3.35f, 0f);
        coords[(int)PartyRole.RegenHealer] = new(-4.26f, -2.48f);
        coords[(int)PartyRole.MeleeDpsA] = new(-6.96f, 2.17f);
        coords[(int)PartyRole.MeleeDpsB] = new(-4.91f, 1.48f);
        coords[(int)PartyRole.OffTank] = new(3.48f, 0f);
        coords[(int)PartyRole.ShieldHealer] = new(3.91f, -2.48f);
        coords[(int)PartyRole.PhysRangedDps] = new(4.91f, 1.48f);
        coords[(int)PartyRole.CasterDps] = new(6.96f, 2.17f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove PullBossesToTheMiddle()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = new(6.0f, 0f);
        coords[(int)PartyRole.OffTank] = new(-4.0f, 0f);
        coords[(int)PartyRole.RegenHealer] = new(-3.0f, -5.0f);
        coords[(int)PartyRole.ShieldHealer] = new(3.0f, -5.0f);
        coords[(int)PartyRole.MeleeDpsA] = new(-2.5f, 4.5f);
        coords[(int)PartyRole.MeleeDpsB] = new(2.5f, 4.5f);
        coords[(int)PartyRole.PhysRangedDps] = new(-7.0f, -7.0f);
        coords[(int)PartyRole.CasterDps] = new(7.0f, -7.0f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private Vector2 WallStackSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => Frame(StackInnerRadius, 0f, 1),
        PartyRole.OffTank => Frame(StackInnerRadius, 1.2f, 1),
        PartyRole.MeleeDpsA => Frame(StackInnerRadius, -1.2f, 1),
        PartyRole.RegenHealer => Frame(StackRadius, -1.2f, 1),
        PartyRole.MeleeDpsB => Frame(StackRadius, 0f, 1),
        _ => Frame(StackRadius, 1.2f, 1),
    };

    private Vector2 RangedAtCrystal(PartyRole role, Element element)
    {
        var crystal = Geometry.SpotHeading(state.SpotOf(element));
        var awayFromWind = role == PartyRole.PhysRangedDps ? 1f : -1f;
        return Polar(crystal + SideOf(element) * awayFromWind * RangedCrystalAngle, RangedCrystalRadius);
    }

    private IAiMove Lb3TakeBossesToTheWindAndRangedToTheShortCrystal()
    {
        var coords = new Vector2?[8];
        foreach (var role in WallSix) coords[(int)role] = WallStackSpot(role);
        coords[(int)PartyRole.MainTank] = Polar(Hw, MainTankHoldRadius);
        coords[(int)PartyRole.OffTank] = Polar(Hw + MathF.PI, 3.5f);
        foreach (var role in Ranged) coords[(int)role] = RangedAtCrystal(role, state.ShortElement);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove Lb3OffTankLeavesExdeathForTheWall() => AiMove.Single(PartyRole.OffTank, WallStackSpot(PartyRole.OffTank));

    private Vector2 FireGroupSpot(PartyRole role)
    {
        var support = IsSupport(role);
        var side = support ? -1f : 1f;
        if (state.FireHolders.Contains(role)) return Polar(Hw + side * FireStepOutAngle, StackRadius);
        var centre = Hw + side * GroupSplitAngle;
        var group = WallSix.Where(r => IsSupport(r) == support && !state.FireHolders.Contains(r)).ToList();
        var i = group.IndexOf(role);
        return Polar(centre, i < 2 ? StackInnerRadius : StackRadius) + Polar(centre + MathF.PI / 2f, i % 2 == 0 ? -0.6f : 0.6f);
    }

    private Vector2 Lb3LongRangedSpot(PartyRole role)
    {
        var distance = role == PartyRole.PhysRangedDps ? ImplosionFarRangedDistance : ImplosionNearRangedDistance;
        return InsideArena(Polar(Hw, ChaosHoldRadius) + Polar(Hw + LongSide * 135f * Deg, distance));
    }

    private IAiMove Lb3ResolveSpots(Element element)
    {
        var coords = new Vector2?[8];
        foreach (var role in WallSix)
            coords[(int)role] = element == Element.Fire ? FireGroupSpot(role) : WallStackSpot(role);
        foreach (var role in Ranged)
            coords[(int)role] = element == state.ShortElement ? RangedAtCrystal(role, element) : Lb3LongRangedSpot(role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove Lb3RegroupAtTheWall()
    {
        var coords = new Vector2?[8];
        foreach (var role in WallSix) coords[(int)role] = WallStackSpot(role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove Lb3TanksToTheWindAndRangedToTheLongCrystal()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = Polar(Hw, MainTankHoldRadius);
        coords[(int)PartyRole.OffTank] = Polar(Hw + ShortSide * ExdeathParkAngle, ExdeathParkRadius);
        foreach (var role in Ranged) coords[(int)role] = Lb3LongRangedSpot(role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private (Vector2 Position, float Facing) ChaosPlacementOr(Vector2 position, float facing)
        => state.Objects.Chaos is { } chaos && chaos.IsAlive() ? (Flat(chaos.Position), chaos.Rotation) : (position, facing);

    private bool SidesHit(bool firstHit) => firstHit == state.Latitudinal;

    private IAiMove Lb3ImplosionSpots(bool firstHit)
    {
        var (chaos, facing) = ChaosPlacementOr(Polar(Hw, ChaosHoldRadius), Hw);
        var sidesHit = SidesHit(firstHit);
        var coords = new Vector2?[8];
        for (var i = 0; i < WallSix.Length; i++)
        {
            var near = chaos + Polar(facing + LongSide * 45f * Deg, ImplosionMeleeDistance + i * 0.6f);
            coords[(int)WallSix[i]] = DodgeAround(chaos, facing, near, sidesHit);
        }
        foreach (var role in Ranged)
        {
            var distance = role == PartyRole.PhysRangedDps ? ImplosionFarRangedDistance : ImplosionNearRangedDistance;
            coords[(int)role] = DodgeAround(chaos, facing, chaos + Polar(facing + LongSide * 135f * Deg, distance), sidesHit);
        }
        return AiMove.Create(coords).NaturalOrder();
    }

    private static Vector2 DodgeAround(Vector2 chaos, float facing, Vector2 wanted, bool sidesHit)
    {
        var offset = wanted - chaos;
        var distance = MathF.Max(offset.Length(), 1f);
        var rel = MathF.Atan2(offset.X, offset.Y) - facing;
        var margin = Math.Clamp(MathF.Asin(MathF.Min(1f, 3.2f / distance)), 12f * Deg, 22f * Deg);
        var heading = facing + SafeBearing(rel, sidesHit, margin);
        return Along(chaos, Polar(heading, 1f), distance);
    }

    private static Vector2 Along(Vector2 from, Vector2 direction, float distance)
    {
        var end = from + direction * distance;
        if (end.LengthSquared() <= SafeLimit * SafeLimit) return end;
        var b = Vector2.Dot(from, direction);
        var reach = -b + MathF.Sqrt(MathF.Max(0f, b * b - from.LengthSquared() + SafeLimit * SafeLimit));
        return from + direction * MathF.Max(0f, reach);
    }

    private (PartyRole Sup, PartyRole Dps) PairOf(Element element)
    {
        var holders = state.HoldersOf(element);
        return (holders[0], holders[1]);
    }

    private (PartyRole SupNear, PartyRole SupFar, PartyRole DpsNear, PartyRole DpsFar) WindGroup()
    {
        var free = state.Unmarked.OrderBy(r => IsSupport(r) ? 0 : 1).ThenBy(r => Array.IndexOf(UmadP3BowelsOfAgonyState.AllRoles, r)).ToList();
        return (free[0], free[1], free[2], free[3]);
    }

    private Vector2 Sg3kWindSpot(PartyRole role)
    {
        var (supNear, supFar, dpsNear, _) = WindGroup();
        return role == supNear ? Frame(11.9f, 3.42f, ShortSide)
            : role == supFar ? Frame(14.3f, -4.21f, ShortSide)
            : role == dpsNear ? Frame(12.42f, 7.32f, ShortSide)
            : Frame(16.02f, -7.41f, ShortSide);
    }

    private IAiMove Sg3kResolveSpots(Element resolving) => AiMove.Create(Sg3kResolveCoords(resolving)).NaturalOrder();

    private Vector2?[] Sg3kResolveCoords(Element resolving)
    {
        var side = SideOf(resolving);
        var other = resolving == Element.Water ? Element.Fire : Element.Water;
        var (resolverSup, resolverDps) = PairOf(resolving);
        var (cleanseSup, cleanseDps) = PairOf(other);
        var cleanseAcross = resolving == Element.Fire ? 0.80f : 4.49f;
        var cleanseAlong = resolving == Element.Fire ? 0.49f : 0.30f;
        var coords = new Vector2?[8];
        foreach (var role in state.Unmarked) coords[(int)role] = Sg3kWindSpot(role);
        coords[(int)resolverSup] = Frame(0f, 19.2f, side);
        coords[(int)resolverDps] = Frame(0f, 7.13f, side);
        coords[(int)cleanseSup] = Frame(cleanseAlong, cleanseAcross, side);
        coords[(int)cleanseDps] = Frame(-cleanseAlong, cleanseAcross, side);
        return coords;
    }

    private IAiMove Sg3kParkBossesAndTakeShortSpots()
    {
        var coords = Sg3kResolveCoords(state.ShortElement);
        coords[(int)PartyRole.MainTank] = Frame(12.0f, 4.8f, ShortSide);
        coords[(int)PartyRole.OffTank] = Frame(3.0f, -18.9f, ShortSide);
        return AiMove.Create(coords).NaturalOrder();
    }

    private void FaceForCrystal(Element resolving)
    {
        var other = resolving == Element.Water ? Element.Fire : Element.Water;
        var (_, resolverDps) = PairOf(resolving);
        if (world.Party.Get(resolverDps) is not { } centre || !centre.IsAlive()) return;
        var cue = UmadP3BowelsOfAgonyPlaybook.Sg3kCleanseFacing(state, resolving);
        foreach (var role in state.HoldersOf(other))
        {
            if (world.Party.Get(role) is not { } member || !member.IsAlive()) continue;
            world.Strat.Note(role, Flat(member.Position), cue, deadline: null);
            if (member is SimPlayer && !DebugBotControl.Enabled) continue;
            var away = member.Position - centre.Position;
            member.Face(state.Winds[role] == Wind.Headwind ? member.Position + away : centre.Position);
        }
    }

    private IAiMove Sg3kChaosToTheMiddleFacingNorth()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = new(0f, -6.5f);
        coords[(int)PartyRole.OffTank] = Polar(Hw, ExdeathParkRadius);
        var spots = Sg3kImplosionTargets(Vector2.Zero, MathF.PI, SidesHit(firstHit: true));
        foreach (var (role, spot) in spots)
            if (role is not (PartyRole.MainTank or PartyRole.OffTank)) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove Sg3kMainTankNorthOfChaos()
    {
        var (chaos, _) = ChaosPlacementOr(Vector2.Zero, MathF.PI);
        return AiMove.Single(PartyRole.MainTank, chaos + new Vector2(0f, -6f));
    }

    private IAiMove Sg3kImplosionSpots(bool firstHit)
    {
        var (chaos, facing) = ChaosPlacementOr(Vector2.Zero, MathF.PI);
        var coords = new Vector2?[8];
        foreach (var (role, spot) in Sg3kImplosionTargets(chaos, facing, SidesHit(firstHit))) coords[(int)role] = spot;
        return AiMove.Create(coords).NaturalOrder();
    }

    private IEnumerable<(PartyRole Role, Vector2 Spot)> Sg3kImplosionTargets(Vector2 chaos, float facing, bool sidesHit)
    {
        var (supNear, supFar, dpsNear, dpsFar) = WindGroup();
        var (shortSup, shortDps) = PairOf(state.ShortElement);
        var (longSup, longDps) = PairOf(state.LongElement);
        var longCrystal = Geometry.SpotHeading(state.LongSpot);
        foreach (var (role, bearing, distance) in new[]
        {
            (supNear, Hw, 15.98f), (supFar, Hw, 15.37f), (dpsNear, Hw, 11.07f), (dpsFar, Hw, 11.68f),
            (shortDps, longCrystal, 4.3f), (shortSup, longCrystal, 4.92f), (longDps, longCrystal, 7.13f), (longSup, longCrystal, 19.0f),
        })
        {
            yield return (role, DodgeAround(chaos, facing, chaos + Polar(bearing, distance), sidesHit));
        }
    }

    private static float SafeBearing(float rel, bool sidesHit, float margin)
    {
        var halfWidth = MathF.PI / 4f - margin;
        var centres = sidesHit ? new[] { 0f, MathF.PI } : new[] { MathF.PI / 2f, -MathF.PI / 2f };
        var best = 0f;
        var bestOff = float.MaxValue;
        foreach (var centre in centres)
        {
            var off = MathF.IEEERemainder(rel - centre, 2f * MathF.PI);
            if (MathF.Abs(off) < MathF.Abs(bestOff)) { bestOff = off; best = centre; }
        }
        return best + Math.Clamp(bestOff, -halfWidth, halfWidth);
    }

    private (PartyRole First, PartyRole? Second) ThunderRoles => ThunderIIIPlanning.Roles(state.ThunderPlan);

    private void ThunderTanksHeadIn(bool windPairsToo)
    {
        if (state.Objects.Exdeath is not { } exdeath) return;
        var (firstRole, secondRole) = ThunderRoles;
        bool Goes(PartyRole role) => windPairsToo || strat == Strat.Lb3 || !state.Unmarked.Contains(role);
        if (world.Party.Get(firstRole) is not { } first || !first.IsAlive()) return;
        if (Goes(firstRole))
        {
            world.Strat.Note(firstRole, Flat(exdeath.Position), UmadP3BowelsOfAgonyPlaybook.ThunderTank(state), deadline: null);
            first.Follow(exdeath);
        }
        if (secondRole is not { } role || !Goes(role) || world.Party.Get(role) is not { } second || !second.IsAlive()) return;
        var boss = Flat(exdeath.Position);
        var approach = Flat(second.Position) - boss;
        var u = approach.LengthSquared() > 1e-4f ? Vector2.Normalize(approach) : new Vector2(0f, -1f);
        var spot = Along(boss, u, SecondTankStandbyDistance);
        world.Strat.Note(role, spot, UmadP3BowelsOfAgonyPlaybook.ThunderClearance(firstRole, role), deadline: null);
        second.MoveTo(new Vector3(spot.X, 0f, spot.Y));
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
    private const float SecondTankClearDistance = 7f;
    private const float SecondTankStandbyDistance = 12f;
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
        if (state.Objects.Exdeath is not { } exdeath || !exdeath.IsAlive()) return;
        var bossPos = Flat(exdeath.Position);
        if (second is { } secondRole
            && world.Party.Get(first) is { } firstMember && firstMember.IsAlive()
            && world.Party.Get(secondRole) is { } secondMember && secondMember.IsAlive())
        {
            var firstPos = Flat(firstMember.Position);
            var secondPos = Flat(secondMember.Position);
            if ((secondPos - firstPos).LengthSquared() < SecondTankClearDistance * SecondTankClearDistance)
            {
                var away = secondPos - bossPos;
                var seat = Along(bossPos, away.LengthSquared() > 1e-4f ? Vector2.Normalize(away) : new Vector2(0f, -1f), SecondTankStandbyDistance);
                if ((seat - firstPos).LengthSquared() < SecondTankClearDistance * SecondTankClearDistance)
                    seat = Along(bossPos, Vector2.Normalize(bossPos - firstPos + new Vector2(1e-3f, 0f)), SecondTankStandbyDistance);
                world.Strat.Note(secondRole, seat, UmadP3BowelsOfAgonyPlaybook.ThunderClearance(first, secondRole), deadline: null);
                secondMember.MoveTo(new Vector3(seat.X, 0f, seat.Y));
            }
        }

        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            if (role == first || (second is { } sec && role == sec)) continue;
            if (world.Party.Get(i) is not { } member || !member.IsAlive()) continue;
            var offset = Flat(member.Position) - bossPos;
            if (offset.LengthSquared() >= ThunderClearDistance * ThunderClearDistance) continue;
            var dir = offset.LengthSquared() > 1e-4f ? Vector2.Normalize(offset) : new Vector2(1f, 0f);
            var target = InsideArena(bossPos + dir * (ThunderClearDistance + 1f));
            if ((target - bossPos).LengthSquared() < ThunderClearDistance * ThunderClearDistance)
                target = InsideArena(bossPos + RotateVec(dir, MathF.PI / 2f) * (ThunderClearDistance + 1f));
            world.Strat.Note(role, target, UmadP3BowelsOfAgonyPlaybook.ThunderClearance(first, second), deadline: null);
            member.MoveTo(new Vector3(target.X, 0f, target.Y));
        }
    }

    private static Vector2 RotateVec(Vector2 v, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
