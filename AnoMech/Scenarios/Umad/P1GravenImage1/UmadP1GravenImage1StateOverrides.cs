using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

// null leaves the field randomized at scenario start.
public sealed class UmadP1GravenImage1StateOverrides
{
    // true = the DPS get the knockback tethers; false = the Supports.
    public bool? TetherDps { get; set; }

    // Mystery Magic 1: 1 = the real cones are NE + SW, 0 = NW + SE.
    public int? Ice1RealOffset { get; set; }
    public bool? Ice1IsLie { get; set; }
    public bool? FireIsStack { get; set; }
    public bool? FireIsLie { get; set; }

    // Mystery Magic 2.
    public int? Ice2RealOffset { get; set; }
    public bool? Ice2IsLie { get; set; }
    public int? ThunderRealOffset { get; set; }
    public bool? ThunderFlipped { get; set; }
    public bool? ThunderIsLie { get; set; }

    // Statue prop knobs, as Tele-trouncing's (debug builds only).
    public bool PropsBindDirector { get; set; } = true;
    public bool PropsForceActive { get; set; } = false;
    public PropBeatMode PropsBeatMode { get; set; } = PropBeatMode.ActorControl;
}
