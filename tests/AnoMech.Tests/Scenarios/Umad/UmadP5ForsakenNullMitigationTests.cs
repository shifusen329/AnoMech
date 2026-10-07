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
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP5ForsakenNullMitigation.Hits.All(h => h.Raw > 0f));
    }
}
