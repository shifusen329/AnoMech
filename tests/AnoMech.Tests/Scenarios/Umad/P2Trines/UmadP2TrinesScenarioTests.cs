using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P2Trines;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Tests;

// The bots run the whole scenario headless on every triangle layout: nobody dies, nobody nears the
// edge, no corner or wing touches anyone, and the busters land on the two tanks.
public class UmadP2TrinesScenarioTests
{
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

    private static Action<object> Layout(int first, int second, bool centreEast, bool wingsRight) => o =>
    {
        var overrides = (UmadP2TrinesStateOverrides)o;
        overrides.FirstSet = first;
        overrides.SecondSet = second;
        overrides.CentrePointsEast = centreEast;
        overrides.WingsRight = wingsRight;
    };

    [TestCaseSource(nameof(Layouts))]
    public void TheBotsClearIt(int first, int second, bool centreEast, bool wingsRight, int seed)
    {
        var farthest = 0f;
        var run = ScenarioRun.Execute(typeof(UmadP2TrinesScenario), 0, seed, new ScenarioRunOptions
        {
            Overrides = Layout(first, second, centreEast, wingsRight),
            Probe = p =>
            {
                for (var i = 0; i < 8; i++)
                    if (p.World.Party.Get(i) is { } m) farthest = MathF.Max(farthest, new Vector2(m.Position.X, m.Position.Z).Length());
            },
        });
        var log = DiagnosticLog.Snapshot();

        Assert.That(run.Passed, run.ToString);
        Assert.That(farthest, Is.LessThan(19.6f));
        Assert.That(Hits(log, ActionId.TrinePoint), Is.EqualTo(Enumerable.Repeat(0, 21)), "Trine");
        // The half room and the busters share one name: the half room first, then the two busters.
        Assert.That(Hits(log, wingsRight ? ActionId.WingsOfDestructionRight : ActionId.WingsOfDestructionLeft), Is.EqualTo(new[] { 0, 1, 1 }), "Wings of Destruction");
        Assert.That(log.Any(l => l.Contains("busters: far OffTank, near MainTank")), "the far buster on the off-tank and the near one on the main tank");
        Assert.That(Hits(log, ActionId.UltimateEmbrace), Is.EqualTo(new[] { 2 }), "Ultimate Embrace");
    }

    // Like the P1 statues: bound to the director and spawned hidden, for their beats to bring up.
    [Test]
    public void TheTrianglesSpawnLikeTheRealProps()
    {
        var spawned = new List<SimEventObject>();
        ScenarioRun.Execute(typeof(UmadP2TrinesScenario), 0, 1, new ScenarioRunOptions
        {
            Overrides = Layout(first: 0, second: 4, centreEast: true, wingsRight: false),
            StopAt = 27f,
            Probe = p => spawned.AddRange(p.World.Children.OfType<SimEventObject>().Except(spawned)),
        });

        var triangles = spawned.Where(o => o.SpawnConfig.EObjId is EObjId.TriangleEast or EObjId.TriangleWest).ToList();
        Assert.That(triangles, Has.Count.EqualTo(7));
        Assert.That(triangles.Select(o => (uint)o.SpawnConfig.EventId), Is.All.EqualTo(EObjId.PropEventId));
        Assert.That(triangles.Select(o => o.SpawnConfig.TimelineState), Is.All.EqualTo(UmadP2TrinesConstants.TriangleSpawnState));
        Assert.That(triangles.Select(o => o.SpawnConfig.Arg2 & 0xFFFF), Is.All.EqualTo(0x3U));
    }

    private static List<int> Hits(IReadOnlyList<string> log, uint actionId)
    {
        var marker = $"Resolve: {Natives.Data.ActionName(actionId)} at";
        return log.Where(l => l.Contains(marker))
            .Select(l => int.Parse(l[(l.LastIndexOf("-- ") + 3)..].Split(' ')[0]))
            .ToList();
    }
}
