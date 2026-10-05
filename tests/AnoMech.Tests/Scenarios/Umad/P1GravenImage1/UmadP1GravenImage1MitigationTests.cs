using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad.P1GravenImage1;

namespace AnoMech.Tests;

public class UmadP1GravenImage1MitigationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ThePlanHoldsForEveryComp(bool extrasAtWaveCannon)
    {
        MitigationPlanAssert.IsSound(UmadP1GravenImage1Mitigation.Plan(extrasAtWaveCannon), $"Graven Image 1 (Extras at Wave Cannon: {extrasAtWaveCannon})");
    }

    [Test]
    public void TheBotsCarryThePlanWithoutExtras()
    {
        var bots = MitigationPlanAssert.Comps().First().Jobs;
        var plan = MitPlan.Resolve(UmadP1GravenImage1Mitigation.Plan(false).Entries, bots);
        Assert.That(plan.Where(p => p.IsExtra), Is.Empty);
        Assert.That(plan.Single(p => p.Slot == PartyRole.MainTank && p.Action.ActionId == MitActionId.ShakeItOff).At, Is.EqualTo(16.73f),
            "a WAR main tank's party mit is for Wave Cannon");
    }

    [TestCase(false, 37.51f)]
    [TestCase(true, 16.73f)]
    public void TheSettingMovesTheRedMagesBarrier(bool extrasAtWaveCannon, float at)
    {
        var rdm = new Dictionary<PartyRole, JobId>(MitigationPlanAssert.Comps().First().Jobs) { [PartyRole.CasterDps] = JobId.RedMage };
        var plan = MitPlan.Resolve(UmadP1GravenImage1Mitigation.Plan(extrasAtWaveCannon).Entries, rdm);
        Assert.That(plan.Single(p => p.Action.ActionId == MitActionId.MagickBarrier).At, Is.EqualTo(at));
    }
}
