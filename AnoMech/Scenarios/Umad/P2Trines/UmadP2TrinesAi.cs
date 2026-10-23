using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Umad.P2Trines.UmadP2TrinesConstants;

namespace AnoMech.Scenarios.Umad.P2Trines;

public sealed class UmadP2TrinesAi : IScenarioAi<UmadP2TrinesState>
{
    public string Name => "NAUR";

    private UmadP2TrinesState state = null!;
    private SimWorld world = null!;

    public void Run(UmadP2TrinesState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        var ai = new AiManager(world);

        ai.Move(0.5f, StackForLightOfJudgment, cue: UmadP2TrinesPlaybook.Opening);
        ai.Move(21.6f, HugKefkaOnTheSafeSide, arrivalTime: 25.0f, cue: UmadP2TrinesPlaybook.WingsDodge(state));
        ai.Move(31.2f, LineUpOnTheWayOut, arrivalTime: 32.6f, cue: UmadP2TrinesPlaybook.Spots(state));
        ai.Move(33.25f, TakeTheTriangleSpots, arrivalTime: 34.8f, cue: UmadP2TrinesPlaybook.Spots(state));
        ai.Move(35.0f, TakeTheTriangleSpots, cue: UmadP2TrinesPlaybook.BusterSpots);
        ai.Move(36.0f, TanksStackForUltimateEmbrace, arrivalTime: 41.5f, cue: UmadP2TrinesPlaybook.UltimateEmbrace);
    }

    private static readonly PartyRole[] NonTanks =
        [PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private const float MainTankLightOfJudgmentDistance = 6f;
    private const float StackSouthDistance = 3f;
    private const float HugDistance = 2f;
    private const float PartyRadius = 13.5f;
    private const float FarTankRadius = 18.5f;
    private const float PartyStagingRadius = 5f;
    private const float FarTankStagingRadius = 9f;
    private const float EmbraceRadius = 5.7f;
    private const float CornerClearance = 6.6f;
    private const float NearToFarClearance = 9.5f;
    private const float NearToPartyClearance = 8f;
    private const float NearestAllowedRadius = 11.5f;
    private const float Deg = MathF.PI / 180f;

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
    private static Vector2 FromBearing(float degrees, float radius) => new(MathF.Sin(degrees * Deg) * radius, -MathF.Cos(degrees * Deg) * radius);
    private static Vector2 Toward(Vector3 centroid, float radius) => Vector2.Normalize(Flat(centroid)) * radius;

    private IAiMove StackForLightOfJudgment()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = new(0f, -MainTankLightOfJudgmentDistance);
        for (var slot = 1; slot < 8; slot++)
            coords[slot] = new((slot % 3 - 1) * 1.2f, StackSouthDistance + (slot / 3) * 1.2f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove HugKefkaOnTheSafeSide()
    {
        var side = state.WingsRight ? -1f : 1f;
        var coords = new Vector2?[8];
        for (var slot = 0; slot < 8; slot++)
            coords[slot] = new(side * (HugDistance + (slot % 2) * 0.6f), (slot / 2 - 1.5f) * 0.9f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private Vector2 PartySpot(int index)
    {
        var centre = Toward(Geometry.Centroids[state.PartyTriangle], PartyRadius);
        var outward = Vector2.Normalize(centre);
        var across = new Vector2(-outward.Y, outward.X);
        return centre + across * ((index % 3 - 1) * 0.7f) + outward * ((index / 3 - 0.5f) * 0.8f);
    }

    private Vector2 FarTankSpot() => Toward(Geometry.Centroids[state.TankTriangle], FarTankRadius);

    private Vector2 NearTankSpot()
    {
        var baseBearing = state.CentrePointsEast ? 267f : 208f;
        var baseRadius = state.CentrePointsEast ? 9.5f : 8.8f;
        var corners = state.CornersDroppingFrom(2).Select(Flat).ToList();
        var far = FarTankSpot();
        var party = Enumerable.Range(0, NonTanks.Length).Select(PartySpot).ToList();
        bool Clear(Vector2 spot) =>
            spot.Length() <= NearestAllowedRadius
            && corners.All(c => Vector2.Distance(c, spot) >= CornerClearance)
            && Vector2.Distance(far, spot) >= NearToFarClearance
            && party.All(p => Vector2.Distance(p, spot) >= NearToPartyClearance);
        foreach (var turn in Enumerable.Range(0, 73).Select(i => (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 5f))
            foreach (var radius in new[] { baseRadius, baseRadius + 1f, baseRadius - 0.7f, baseRadius + 2f })
                if (FromBearing(baseBearing + turn, radius) is var spot && Clear(spot))
                    return spot;
        return FromBearing(baseBearing, baseRadius);
    }

    private bool NearSpotWaitsForTheSecondSet()
    {
        var spot = NearTankSpot();
        return state.Set(1).SelectMany(state.CornersOf).Any(c => Vector2.Distance(Flat(c), spot) < CornerClearance);
    }

    private IAiMove LineUpOnTheWayOut()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = NearSpotWaitsForTheSecondSet() ? null : NearTankSpot();
        coords[(int)PartyRole.OffTank] = Toward(Geometry.Centroids[state.TankTriangle], FarTankStagingRadius);
        var party = Toward(Geometry.Centroids[state.PartyTriangle], PartyStagingRadius);
        var across = new Vector2(-party.Y, party.X) / PartyStagingRadius;
        for (var i = 0; i < NonTanks.Length; i++) coords[(int)NonTanks[i]] = party + across * ((i - 2.5f) * 0.5f);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove TakeTheTriangleSpots()
    {
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = NearTankSpot();
        coords[(int)PartyRole.OffTank] = FarTankSpot();
        for (var i = 0; i < NonTanks.Length; i++) coords[(int)NonTanks[i]] = PartySpot(i);
        return AiMove.Create(coords).NaturalOrder();
    }

    private IAiMove TanksStackForUltimateEmbrace()
    {
        var at = Vector2.Normalize(NearTankSpot()) * EmbraceRadius;
        var across = new Vector2(-at.Y, at.X) / EmbraceRadius;
        var coords = new Vector2?[8];
        coords[(int)PartyRole.MainTank] = at;
        coords[(int)PartyRole.OffTank] = at + across * 0.8f;
        return AiMove.Create(coords).NaturalOrder();
    }
}
