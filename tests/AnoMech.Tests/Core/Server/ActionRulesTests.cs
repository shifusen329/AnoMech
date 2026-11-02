using RuleSim.Actions;

namespace AnoMech.Tests;

// The shared rules against this game's own sheets.
public class ActionRulesTests
{
    [Test]
    public void EveryGrantedStatusIsARealStatus()
    {
        var data = new DataminingGameData(new FakeRsvFunctions());
        foreach (var actionId in Rules.ActionIds)
            foreach (var (statusId, _) in Rules.GrantsOf(actionId))
                Assert.That(data.Status(statusId), Is.Not.Null, $"action {actionId} grants unknown status {statusId}");
    }

    [Test]
    public void EveryRuledActionIsInTheActionSheet()
    {
        var data = new DataminingGameData(new FakeRsvFunctions());
        Assert.That(Rules.ActionIds.Where(id => data.Action(id) == null), Is.Empty);
    }
}
