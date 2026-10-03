using AnoMech.Core.Game;
using AnoMech.Scenarios.Umad.P5Flood;

namespace AnoMech.Tests;

public class UmadP5FloodPlaybookTests
{
    [Test]
    public void FloodExplainsEveryMove()
    {
        foreach (var start in new[] { 0, 1, 2, 3 })
            foreach (var clockwise in new[] { false, true })
            {
                var state = UmadP5FloodState.FromNetworkReplay(false, false, true, start, clockwise, new EventScheduler())!;
                var context = $"start {start} clockwise {clockwise}";
                PlaybookAssert.ExplainsEveryRole(UmadP5FloodPlaybook.Start(start), context);
                foreach (var tick in new[] { 2, 3 })
                    PlaybookAssert.ExplainsEveryRole(UmadP5FloodPlaybook.Rotate(state, (start + tick - 1) % 4, tick), context);
            }
    }

    [Test]
    public void FloodRotationTextNamesTheTurnAndTheSide()
    {
        var state = UmadP5FloodState.FromNetworkReplay(false, false, true, 2, false, new EventScheduler())!;
        var why = UmadP5FloodPlaybook.Rotate(state, 1, 2).Why(default);
        Assert.That(why, Does.Contain("counterclockwise"));
        Assert.That(why, Does.Contain("east side"));
    }
}
