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
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP4KefkaSaysMitigation.Hits.All(h => h.Raw > 0f));
    }
}
