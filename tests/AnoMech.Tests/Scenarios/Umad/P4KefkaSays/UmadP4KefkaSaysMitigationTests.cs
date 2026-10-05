using AnoMech.Scenarios.Umad.P4KefkaSays;

namespace AnoMech.Tests;

public class UmadP4KefkaSaysMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP4KefkaSaysMitigation.Plan(), "Kefka Says");
    }

    [Test]
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP4KefkaSaysMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
