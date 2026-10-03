using AnoMech.Core.Game.Ai;

namespace AnoMech.Scenarios.Umad.P5Flood;

// What UmadP5FloodAi's moves are for, for the death recap. The whole party moves as one stack.
internal static class UmadP5FloodPlaybook
{
    private const string Flood = "NAUR §5.3 (1:03:06)";

    private const string Stack = "Stay stacked: every hit also drops Chaotic Flood, a 6y stack on a random non-tank.";

    // Quadrant indices as UmadP5FloodAi numbers them.
    public static string Cardinal(int quadrant) => quadrant switch
    {
        0 => "north",
        1 => "east",
        2 => "south",
        _ => "west",
    };

    public static StratCue Start(int quadrant) => StratCue.ForAll(
        "Flood: start spot",
        $"The first two line pairs both leave Kefka's {Cardinal(quadrant)} side clear, so the whole party stacks close in on that side. {Stack}",
        Flood);

    public static StratCue Rotate(UmadP5FloodState state, int quadrant, int tick) => StratCue.ForAll(
        $"Flood: rotation for hit {tick + 1} of 4",
        $"The lines turn {(state.RotationClockwise ? "clockwise" : "counterclockwise")}, so right after each hit the party steps 90° with them, "
        + $"here to Kefka's {Cardinal(quadrant)} side. {Stack}",
        Flood);
}
