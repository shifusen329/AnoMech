namespace AnoMech.Scenarios.Umad.P3Full;

// Each slice reads its own settings as it starts. Limit Cut's corner and winds come from Bowels of
// Agony instead of its own.
public sealed class UmadP3FullStateOverrides(object bowelsOfAgony, object limitCut, object blackHole)
{
    public UmadP3SliceSettings BowelsOfAgony { get; } = new(bowelsOfAgony);
    public UmadP3SliceSettings LimitCut { get; } = new(limitCut);
    public UmadP3SliceSettings BlackHole { get; } = new(blackHole);
}

// The settings summary peers see shows a slice's forced rolls under its name.
public sealed record UmadP3SliceSettings(object Overrides)
{
    public override string ToString()
        => ScenarioSettingsSummary.Describe(Overrides) is { Count: > 0 } lines ? string.Join("; ", lines) : "Auto";
}
