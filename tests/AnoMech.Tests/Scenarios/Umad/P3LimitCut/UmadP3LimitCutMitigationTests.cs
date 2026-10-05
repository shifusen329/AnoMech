using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Tests;

public class UmadP3LimitCutMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP3LimitCutMitigation.Plan(), "Limit Cut");
    }

    [Test]
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP3LimitCutMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
