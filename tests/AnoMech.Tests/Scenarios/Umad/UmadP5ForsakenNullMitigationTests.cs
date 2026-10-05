using AnoMech.Scenarios.Umad;

namespace AnoMech.Tests;

public class UmadP5ForsakenNullMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP5ForsakenNullMitigation.Plan(), "Forsaken Null");
    }

    [Test]
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP5ForsakenNullMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
