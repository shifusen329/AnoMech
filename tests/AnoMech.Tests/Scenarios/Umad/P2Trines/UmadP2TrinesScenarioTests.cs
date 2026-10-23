using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad.P2Trines;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Tests;

// The bots run the whole scenario headless on every triangle layout: nobody dies, nobody nears the
// edge, no corner or wing touches anyone, and the busters land on the two tanks.
public class UmadP2TrinesScenarioTests
{
    [TearDown]
    public void ReleaseBots() => DebugBotControl.Enabled = false;

    private static IEnumerable<TestCaseData> Layouts()
    {
        var i = 0;
        for (var first = 0; first < UmadP2TrinesState.FirstSets.Count; first++)
            foreach (var second in Enumerable.Range(1, 6).Where(t => !UmadP2TrinesState.FirstSets[first].Contains(t)))
            {
                i++;
                var centreEast = i % 3 != 0;
                var wingsRight = i % 2 == 0;
                yield return new TestCaseData(first, second, centreEast, wingsRight, i)
                    .SetName($"first {UmadP2TrinesState.Describe(UmadP2TrinesState.FirstSets[first])} second {UmadP2TrinesState.Describe([second])} centre {(centreEast ? "E" : "W")} wings {(wingsRight ? "R" : "L")}");
            }
    }

    [TestCaseSource(nameof(Layouts))]
    public void TheBotsClearIt(int first, int second, bool centreEast, bool wingsRight, int seed)
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
        var scenario = new UmadP2TrinesScenario();
        var overrides = (UmadP2TrinesStateOverrides)scenario.SettingsOverrides;
        overrides.FirstSet = first;
        overrides.SecondSet = second;
        overrides.CentrePointsEast = centreEast;
        overrides.WingsRight = wingsRight;
        world.Reseed(seed);
        scenario.Run(world, 0);

        var farthest = 0f;
        for (var t = 0f; t < 46f; t += 0.05f)
        {
            fake.Frame(0.05f, d => { world.Events.Tick(d); world.Tick(d); });
            for (var i = 0; i < 8; i++)
                if (world.Party.Get(i) is { } m) farthest = MathF.Max(farthest, new Vector2(m.Position.X, m.Position.Z).Length());
        }
        var log = DiagnosticLog.Snapshot();
        world.Despawn();

        Assert.That(log.Where(l => l.Contains("[Game] Kill:") && l.Contains(" died ")), Is.Empty);
        Assert.That(farthest, Is.LessThan(19.6f));
        Assert.That(Hits(log, ActionId.TrinePoint), Is.EqualTo(Enumerable.Repeat(0, 21)), "Trine");
        // The half room and the busters share one name: the half room first, then the two busters.
        Assert.That(Hits(log, wingsRight ? ActionId.WingsOfDestructionRight : ActionId.WingsOfDestructionLeft), Is.EqualTo(new[] { 0, 1, 1 }), "Wings of Destruction");
        Assert.That(log.Any(l => l.Contains("busters: far OffTank, near MainTank")), "the far buster on the off-tank and the near one on the main tank");
        Assert.That(Hits(log, ActionId.UltimateEmbrace), Is.EqualTo(new[] { 2 }), "Ultimate Embrace");
    }

    private static List<int> Hits(IReadOnlyList<string> log, uint actionId)
    {
        var marker = $"Resolve: {ActionLookup.Name(actionId)} from";
        return log.Where(l => l.Contains(marker))
            .Select(l => int.Parse(l[(l.LastIndexOf("-- ") + 3)..].Split(' ')[0]))
            .ToList();
    }
}
