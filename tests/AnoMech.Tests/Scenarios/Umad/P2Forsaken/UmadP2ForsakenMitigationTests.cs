using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad.P2Forsaken;

namespace AnoMech.Tests;

public class UmadP2ForsakenMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP2ForsakenMitigation.Plan(), "Forsaken");
    }

    [Test]
    public void TheOpeningBarriersArePressedBeforeTheScenarioStarts()
    {
        var sage = new Dictionary<PartyRole, JobId>(MitigationPlanAssert.Comps().First().Jobs) { [PartyRole.ShieldHealer] = JobId.Sage };
        var plan = MitPlan.Resolve(UmadP2ForsakenMitigation.Plan().Entries, sage);
        Assert.That(plan.Single(p => p.Action.ActionId == MitActionId.Holos).IsPreStart);
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP2ForsakenMitigation.Hits.All(h => h.Raw > 0f));
    }
}
