using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Tests;

public class UmadP3BlackHoleMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP3BlackHoleMitigation.Plan(), "Black Hole");
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP3BlackHoleMitigation.Hits.All(h => h.Raw > 0f));
    }
}
