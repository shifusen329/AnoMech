using System.Linq;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

public enum Element { Water, Fire }

public enum ElementDebuff { None, DynamicFluid, Entropy }

public sealed class UmadP3BowelsOfAgonyStateOverrides
{
    // --- Fight-wide: one roll the whole sim shares -------------------------------------
    // The wind crystal's intercardinal (1 = SE, 3 = NE, 5 = NW, 7 = SW); null = random.
    public int? WindSpot { get; set; } = null;
    // Whether water sits clockwise of wind on the map (fire across from it); null = random.
    public bool? WaterClockwise { get; set; } = null;
    // Which element's debuffs run out first; null = random.
    public Element? ShortElement { get; set; } = null;
    // Latitudinal (sides first) or Longitudinal (front and back first); null = random.
    public bool? Latitudinal { get; set; } = null;
    // The fight's own Thunder III buster: the logs swap, main tank first.
    public ThunderIIIAssignment ThunderPlan { get; set; } = ThunderIIIAssignment.ShareMtFirst;

    // --- Per player: everyone has their own ---------------------------------------------
    public PerRoleSetting<ElementDebuff> Debuff { get; set; } = new();
    public PerRoleSetting<Wind> Wind { get; set; } = new();

    // Each element goes to one support and one DPS, so each half has one of each and two without.
    // The wind split is always 2 to 5 Headwinds, as in Limit Cut.
    public SettingsConflicts Validate()
    {
        var conflicts = new SettingsConflicts();
        if (!PerRole.SeatsActive) return conflicts;

        foreach (var (half, name) in new[] { (true, "supports"), (false, "DPS") })
        {
            var seats = PerRole.All.Where(r => UmadP3BowelsOfAgonyState.IsSupport(r) == half).ToList();
            conflicts.AtMost(1, seats.Where(r => Debuff[r] == ElementDebuff.DynamicFluid).ToList(), $"Dynamic Fluid among the {name}");
            conflicts.AtMost(1, seats.Where(r => Debuff[r] == ElementDebuff.Entropy).ToList(), $"Entropy among the {name}");
            conflicts.AtMost(2, seats.Where(r => Debuff[r] == ElementDebuff.None).ToList(), $"no element among the {name}");
        }
        conflicts.AtMost(5, PerRole.All.Where(r => Wind[r] == P3LimitCut.Wind.Headwind).ToList(), "Headwind");
        conflicts.AtMost(6, PerRole.All.Where(r => Wind[r] == P3LimitCut.Wind.Tailwind).ToList(), "Tailwind");
        return conflicts;
    }
}
