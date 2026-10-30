using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad;

// The Phase 3 actors a run keeps from Bowels of Agony to Black Hole. A slice spawns any that are
// missing and adopts the rest. Chained, it also leaves out its own mitigation plan and the
// stand-ins that fake what the slice before would have left.
public sealed class UmadP3Cast
{
    public bool Chained { get; init; }
    public SimEnemy? Kefka { get; set; }
    public SimEnemy? Chaos { get; set; }
    public SimEnemy? Exdeath { get; set; }
    public SimEventObject? WindCrystal { get; set; }
}
