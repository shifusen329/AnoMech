using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

public sealed class UmadP1GravenImage2Ai : IScenarioAi<UmadP1GravenImage2State>
{
    public string Name => "NAUR";

    private static readonly Vector2 NorthStack = new(1.2f, -12f);
    private static readonly Vector2 SouthStack = new(0f, 12f);
    private const float GravityMiddle = 3.4f;
    private static readonly Vector2 RockNear = new(7.5f, 0f);
    private static readonly Vector2 RockFar = new(13f, 11f);
    private static readonly Vector2 HoldParty = new(0f, 3.5f);
    private static readonly Vector2 HoldTank = new(0f, -4.5f);
    private static readonly Vector2 Cleave1Party = new(2.5f, 3.5f);
    private static readonly Vector2 Cleave1Tank = new(1.5f, -4f);
    private static readonly Vector2 Cleave2Supports = new(3f, -4f);
    private static readonly Vector2 Cleave2Dps = new(3f, 2.5f);
    private const float TrapStackRadius = 2.8f;
    private const float TrapHolderRadius = 5.6f;
    private const float TrapStackSpacing = 0.2f;

    public void Run(UmadP1GravenImage2State state, SimWorld world)
    {
        var ai = new AiManager(world);
        ai.Move(0.3f, Uptime);
        ai.Move(7.2f, () => StackForPuddles(FirstPuddleStack(state)), arrivalTime: 11.8f, settleFraction: 0.5f);
        ai.Move(12.4f, () => TetherExits(state, 0), jitter: 0.2f, arrivalTime: 15.9f);
        ai.Move(16.5f, Hold, arrivalTime: 20.6f, settleFraction: 0.5f);
        ai.Move(21.0f, () => FirstCleaveSpots(state), jitter: 0.2f, arrivalTime: 25.6f);
        ai.Move(26.2f, () => StackForPuddles(SouthStack), arrivalTime: 30.4f, settleFraction: 0.5f);
        ai.Move(30.9f, () => TetherExits(state, 1), jitter: 0.2f, arrivalTime: 34.4f);
        ai.Move(34.9f, () => SecondCleaveSpots(state), jitter: 0.2f, arrivalTime: 38.9f);
        ai.Move(39.5f, () => TrapStacks(state), jitter: 0.1f, arrivalTime: 42.7f, settleFraction: 0.5f);
        ai.Move(43.8f, () => PuddleSoaks(state), jitter: 0.1f);
        ai.Move(46.5f, Uptime, arrivalTime: 51f, settleFraction: 0.5f);
    }

    private static IAiMove Uptime() => AiMove.Create(UmadP1Spots.Uptime.Select(p => (Vector2?)p).ToArray()).NaturalOrder();

    internal static Vector2 FirstPuddleStack(UmadP1GravenImage2State state)
        => new(state.Ice.HitsNorthEast ? -NorthStack.X : NorthStack.X, NorthStack.Y);

    internal static Vector2 PuddleStackSpot(Vector2 stack, PartyRole role) => stack + UmadP1Spots.Offset(role, 0.3f);

    private static IAiMove StackForPuddles(Vector2 stack)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = PuddleStackSpot(stack, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static Vector2 TetherExit(UmadP1GravenImage2State state, int set, PartyRole role)
    {
        var fromNorth = set == 0;
        if (UmadP1Roles.IsDps(role) == state.PurpleDps[set])
            return new Vector2(0f, fromNorth ? GravityMiddle : -GravityMiddle) + UmadP1Spots.Offset(role, 0.5f);
        var side = (UmadP1Roles.IsGroupOne(role) ? 1f : -1f) * (fromNorth ? 1f : -1f);
        return UmadP1Roles.IsTankOrMelee(role)
            ? new Vector2(side * RockNear.X, RockNear.Y)
            : new Vector2(side * RockFar.X, fromNorth ? -RockFar.Y : RockFar.Y);
    }

    private static IAiMove TetherExits(UmadP1GravenImage2State state, int set)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = TetherExit(state, set, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    private static IAiMove Hold()
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All)
            coords[(int)role] = role == PartyRole.MainTank ? HoldTank : HoldParty + UmadP1Spots.Offset(role, 0.5f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private static float SafeSide(UmadP1GravenImage2State state, int cleave) => state.CleaveWest[cleave] ? 1f : -1f;

    internal static Vector2 FirstCleaveSpot(UmadP1GravenImage2State state, PartyRole role)
    {
        var p = role == PartyRole.MainTank ? Cleave1Tank : Cleave1Party + UmadP1Spots.Offset(role, 0.5f);
        return p with { X = p.X * SafeSide(state, 0) };
    }

    private static IAiMove FirstCleaveSpots(UmadP1GravenImage2State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = FirstCleaveSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static Vector2 SecondCleaveSpot(UmadP1GravenImage2State state, PartyRole role)
    {
        var p = (UmadP1Roles.IsDps(role) ? Cleave2Dps : Cleave2Supports) + UmadP1Spots.Offset(role, 0.4f);
        return p with { X = p.X * SafeSide(state, 1) };
    }

    private static IAiMove SecondCleaveSpots(UmadP1GravenImage2State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = SecondCleaveSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static Vector2 TrapSpot(UmadP1GravenImage2State state, PartyRole role)
    {
        var dps = UmadP1Roles.IsDps(role);
        var direction = new Vector2(0f, dps ? 1f : -1f);
        var holder = dps ? state.TrapDps : state.TrapSupport;
        if (role == holder) return direction * TrapHolderRadius;
        var k = UmadP1Roles.Of(dps).Where(r => r != holder).ToList().IndexOf(role) - 1;
        return UmadP1Spots.TrapStackSpot(direction, TrapStackRadius, direction, TrapStackSpacing, k);
    }

    private static IAiMove TrapStacks(UmadP1GravenImage2State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = TrapSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }

    internal static Vector2 PuddleSoakSpot(UmadP1GravenImage2State state, PartyRole role)
    {
        var dps = UmadP1Roles.IsDps(role);
        var holder = role == (dps ? state.TrapDps : state.TrapSupport);
        var north = !dps == holder;
        var spot = north ? FirstPuddleStack(state) : SouthStack;
        return spot + new Vector2(0.25f * ((int)role % 4) - 0.375f, 0f);
    }

    private static IAiMove PuddleSoaks(UmadP1GravenImage2State state)
    {
        var coords = new Vector2?[8];
        foreach (var role in UmadP1Roles.All) coords[(int)role] = PuddleSoakSpot(state, role);
        return AiMove.Create(coords).NaturalOrder();
    }
}
