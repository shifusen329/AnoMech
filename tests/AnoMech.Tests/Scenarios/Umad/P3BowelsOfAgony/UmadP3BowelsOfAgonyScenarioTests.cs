using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3BowelsOfAgony;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Tests;

// The bots run the whole scenario headless, both strats, on every crystal layout: nobody dies,
// nobody nears the edge, and each AoE lands only where the strat puts it.
public class UmadP3BowelsOfAgonyScenarioTests
{
    private const int Lb3 = 0;
    private const int Sg3k = 1;

    private static IEnumerable<TestCaseData> Layouts()
    {
        var plans = Enum.GetValues<ThunderIIIAssignment>();
        var i = 0;
        foreach (var strat in new[] { Lb3, Sg3k })
            foreach (var wind in new[] { 1, 3, 5, 7 })
                foreach (var waterClockwise in new[] { true, false })
                    foreach (var shortElement in new[] { Element.Water, Element.Fire })
                    {
                        i++;
                        yield return new TestCaseData(strat, wind, waterClockwise, shortElement, i % 2 == 0, plans[i % 4], i)
                            .SetName($"{(strat == Lb3 ? "LB3" : "SG3K")} wind {wind} water {(waterClockwise ? "cw" : "ccw")} short {shortElement} {(i % 2 == 0 ? "Lat" : "Lon")} {plans[i % 4]}");
                    }
    }

    [TestCaseSource(nameof(Layouts))]
    public void TheBotsClearIt(int strat, int wind, bool waterClockwise, Element shortElement, bool latitudinal, ThunderIIIAssignment plan, int seed)
    {
        var farthest = 0f;
        var run = ScenarioRun.Execute(typeof(UmadP3BowelsOfAgonyScenario), strat, seed, new ScenarioRunOptions
        {
            Overrides = o =>
            {
                var overrides = (UmadP3BowelsOfAgonyStateOverrides)o;
                overrides.WindSpot = wind;
                overrides.WaterClockwise = waterClockwise;
                overrides.ShortElement = shortElement;
                overrides.Latitudinal = latitudinal;
                overrides.ThunderPlan = plan;
            },
            Probe = p =>
            {
                for (var i = 0; i < 8; i++)
                    if (p.World.Party.Get(i) is { } m) farthest = MathF.Max(farthest, new Vector2(m.Position.X, m.Position.Z).Length());
            },
        });
        var log = DiagnosticLog.Snapshot();

        Assert.That(run.Passed, run.ToString);
        Assert.That(farthest, Is.LessThan(19.6f));
        Assert.That(Hits(log, ActionId.Shockwave), Is.EqualTo(new[] { 0, 0, 0, 0 }), "Shockwave");
        Assert.That(Hits(log, ActionId.StraySpray_P3), Is.EqualTo(new[] { 0, 0 }), "Stray Spray");
        Assert.That(Hits(log, ActionId.StrayFlames_P3), Is.EqualTo(new[] { 1, 1 }), "Stray Flames");
        if (strat == Lb3)
        {
            Assert.That(Hits(log, ActionId.Tsunami_P3), Is.EqualTo(new[] { 1, 1 }), "Tsunami");
            Assert.That(Hits(log, ActionId.Inferno_P3), Is.EqualTo(new[] { 0, 0 }), "Inferno");
            Assert.That(Hits(log, ActionId.Cyclone), Is.Empty, "Cyclone");
        }
        else
        {
            Assert.That(Hits(log, ActionId.Tsunami_P3).Order(), Is.EqualTo(new[] { 1, 3 }), "Tsunami");
            Assert.That(Hits(log, ActionId.Inferno_P3).Order(), Is.EqualTo(new[] { 0, 2 }), "Inferno");
            Assert.That(Hits(log, ActionId.Cyclone), Is.EqualTo(new[] { 2, 2, 2, 2 }), "Cyclone");
        }
    }

    private static List<int> Hits(IReadOnlyList<string> log, uint actionId)
    {
        var marker = $"Resolve: {Natives.Data.ActionName(actionId)} at";
        return log.Where(l => l.Contains(marker))
            .Select(l => int.Parse(l[(l.LastIndexOf("-- ") + 3)..].Split(' ')[0]))
            .ToList();
    }
}
