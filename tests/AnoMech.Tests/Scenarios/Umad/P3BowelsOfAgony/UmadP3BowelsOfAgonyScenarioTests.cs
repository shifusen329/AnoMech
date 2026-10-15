using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
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

    [TearDown]
    public void ReleaseBots() => DebugBotControl.Enabled = false;

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
        var fake = FakeGame.Install();
        fake.BattleCharas.Player.ClassJob = (byte)JobId.Sage;
        var game = new Game();
        DalamudServices.Install(nameof(Plugin.GameInstance), game);
        DalamudServices.Install(nameof(Plugin.Config), new Configuration { ShowDeathRecap = false, EnableMechanicResultMarks = false });
        game.GodMode = true;
        var world = game.World;
        world.CreateParty((uint)JobId.Sage);
        DebugBotControl.Enabled = true;
        DiagnosticLog.Clear();
        var scenario = new UmadP3BowelsOfAgonyScenario();
        var overrides = (UmadP3BowelsOfAgonyStateOverrides)scenario.SettingsOverrides;
        overrides.WindSpot = wind;
        overrides.WaterClockwise = waterClockwise;
        overrides.ShortElement = shortElement;
        overrides.Latitudinal = latitudinal;
        overrides.ThunderPlan = plan;
        world.Reseed(seed);
        scenario.Run(world, strat);

        var farthest = 0f;
        for (var t = 0f; t < 84f; t += 0.05f)
        {
            fake.Frame(0.05f, d => { world.Events.Tick(d); world.Tick(d); });
            for (var i = 0; i < 8; i++)
                if (world.Party.Get(i) is { } m) farthest = MathF.Max(farthest, new Vector2(m.Position.X, m.Position.Z).Length());
        }
        var log = DiagnosticLog.Snapshot();
        world.Despawn();

        Assert.That(log.Where(l => l.Contains("[Game] Kill:") && l.Contains(" died ")), Is.Empty);
        Assert.That(farthest, Is.LessThan(19.6f));
        Assert.That(Hits(log, ActionId.Shockwave), Is.All.EqualTo(0), "Shockwave");
        Assert.That(Hits(log, ActionId.StraySpray_P3), Is.All.EqualTo(0), "Stray Spray");
        Assert.That(Hits(log, ActionId.StrayFlames_P3), Is.All.EqualTo(1), "Stray Flames");
        if (strat == Lb3)
        {
            Assert.That(Hits(log, ActionId.Tsunami_P3), Is.All.EqualTo(1), "Tsunami");
            Assert.That(Hits(log, ActionId.Inferno_P3), Is.All.EqualTo(0), "Inferno");
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
        var marker = $"Resolve: {ActionLookup.Name(actionId)} from";
        return log.Where(l => l.Contains(marker))
            .Select(l => int.Parse(l[(l.LastIndexOf("-- ") + 3)..].Split(' ')[0]))
            .ToList();
    }
}
