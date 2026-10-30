using AnoMech.Core.Game.Ai;

namespace AnoMech.Scenarios.Umad.P3Full;

// A Bowels of Agony strat, then a Black Hole one; Limit Cut has the one.
public sealed record UmadP3FullStrat(int Bowels, int BlackHole, string BowelsName, string BlackHoleName) : IScenarioAi
{
    public string Name => $"{BowelsName} · {BlackHoleName}";
    public string? Group => BowelsName;
}
