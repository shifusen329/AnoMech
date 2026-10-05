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
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP3BlackHoleMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
