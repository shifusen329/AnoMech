using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad.P2Trines;

using Geometry = UmadP2TrinesConstants.Geometry;

public sealed class UmadP2TrinesScenarioObjects
{
    public SimEnemy? Kefka { get; set; }
}

// Per-run randomization: any three of the six outer triangles drop first, one of the other three
// second, and the centre with the last two third. The centre points east two pulls in three, and
// the first Wings of Destruction takes either half.
public sealed class UmadP2TrinesState
{
    public static readonly IReadOnlyList<int[]> FirstSets = BuildFirstSets();

    public UmadP2TrinesScenarioObjects Objects { get; } = new();
    public IReadOnlyList<int> FirstSet { get; }
    public int SecondSet { get; }
    public bool CentrePointsEast { get; }
    public bool WingsRight { get; }

    public IReadOnlyList<int> ThirdSet => [Geometry.Centre, .. Enumerable.Range(1, 6).Where(t => !FirstSet.Contains(t) && t != SecondSet)];
    public IReadOnlyList<int> Set(int drop) => drop switch { 0 => FirstSet, 1 => [SecondSet], _ => ThirdSet };
    public int DropOf(int triangle) => FirstSet.Contains(triangle) ? 0 : triangle == SecondSet ? 1 : 2;
    public bool PointsEast(int triangle) => triangle == Geometry.Centre ? CentrePointsEast : Geometry.OuterPointsEast(triangle);
    public IEnumerable<System.Numerics.Vector3> CornersOf(int triangle) => Geometry.Corners(triangle, PointsEast(triangle));
    public IEnumerable<System.Numerics.Vector3> CornersDroppingFrom(int drop)
        => Enumerable.Range(0, 7).Where(t => DropOf(t) >= drop).SelectMany(CornersOf);

    // NAUR's: the party takes the first first-set triangle clockwise from north, the far tank the
    // first counter-clockwise from the north-west waymark. The outer six are numbered clockwise
    // from north, so these are the lowest and the highest.
    public int PartyTriangle => FirstSet.Min();
    public int TankTriangle => FirstSet.Max();

    public UmadP2TrinesState(Rng rng, UmadP2TrinesStateOverrides overrides)
    {
        FirstSet = FirstSets[overrides.FirstSet ?? rng.NextInt(FirstSets.Count)];
        var left = Enumerable.Range(1, 6).Where(t => !FirstSet.Contains(t)).ToArray();
        if (overrides.SecondSet is { } second && !left.Contains(second))
            DiagnosticLog.Warn($"[UmadP2Trines] Second triangle {Geometry.Name(second)} is in the first set -- leaving it to the roll.");
        SecondSet = overrides.SecondSet is { } s && left.Contains(s) ? s : rng.NextObj(left);
        CentrePointsEast = overrides.CentrePointsEast ?? rng.NextInt(3) < 2;
        WingsRight = overrides.WingsRight ?? rng.NextBool();
    }

    private UmadP2TrinesState(int[] firstSet, int secondSet, bool centrePointsEast, bool wingsRight)
    {
        FirstSet = firstSet;
        SecondSet = secondSet;
        CentrePointsEast = centrePointsEast;
        WingsRight = wingsRight;
    }

    public static UmadP2TrinesState? FromNetworkReplay(int[] firstSet, int secondSet, bool centrePointsEast, bool wingsRight)
    {
        var sorted = firstSet.Order().ToArray();
        if (!FirstSets.Any(s => s.SequenceEqual(sorted))) return null;
        if (secondSet is < 1 or > 6 || sorted.Contains(secondSet)) return null;
        return new(sorted, secondSet, centrePointsEast, wingsRight);
    }

    public static string Describe(IEnumerable<int> triangles) => string.Concat(triangles.Select(Geometry.Name));

    private static List<int[]> BuildFirstSets()
    {
        var sets = new List<int[]>();
        for (var a = 1; a <= 6; a++)
            for (var b = a + 1; b <= 6; b++)
                for (var c = b + 1; c <= 6; c++)
                    sets.Add([a, b, c]);
        return sets;
    }
}
