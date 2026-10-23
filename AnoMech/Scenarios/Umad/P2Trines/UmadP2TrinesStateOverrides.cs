using System.Linq;

namespace AnoMech.Scenarios.Umad.P2Trines;

public sealed class UmadP2TrinesStateOverrides
{
    // --- Fight-wide: one roll the whole sim shares -------------------------------------
    // Index into UmadP2TrinesState.FirstSets; null = random.
    public int? FirstSet { get; set; } = null;
    // The lone second-set triangle, 1-6 (A-F); null = random among the three left.
    public int? SecondSet { get; set; } = null;
    // Which way the centre triangle points; null = the fight's 2 in 3 east.
    public bool? CentrePointsEast { get; set; } = null;
    // Wings of Destruction's first cast takes Kefka's right half; null = random.
    public bool? WingsRight { get; set; } = null;

    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (FirstSet is { } first && SecondSet is { } second && UmadP2TrinesState.FirstSets[first].Contains(second))
            conflicts.Add($"The second triangle ({UmadP2TrinesConstants.Geometry.Name(second)}) is already in the first set.");
        return conflicts;
    }
}
