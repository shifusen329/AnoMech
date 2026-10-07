using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Tests;

public class UmadP3LimitCutMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP3LimitCutMitigation.Plan(), "Limit Cut",
            UmadP3LimitCutConstants.Timing.UmbraCastAt + UmadP3LimitCutConstants.Timing.KefkaReappearAfterUmbra);
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP3LimitCutMitigation.Hits.All(h => h.Raw > 0f));
    }
}
