using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

// null leaves the field randomized at scenario start.
public sealed class UmadP1GravenImage2StateOverrides
{
    // Per tether set: true = the DPS get the central (Gravitas) tethers; false = the Supports.
    public bool? PurpleDps1 { get; set; }
    public bool? PurpleDps2 { get; set; }

    // 1 = the real cones are NE + SW, 0 = NW + SE.
    public int? IceRealOffset { get; set; }
    public bool? IceIsLie { get; set; }

    // Per cleave: true = the west half.
    public bool? CleaveWest1 { get; set; }
    public bool? CleaveWest2 { get; set; }

    public bool PropsBindDirector { get; set; } = true;
    public bool PropsForceActive { get; set; } = false;
    public PropBeatMode PropsBeatMode { get; set; } = PropBeatMode.ActorControl;
}
