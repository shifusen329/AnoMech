using AnoMech.Scenarios.Umad.P5Flood;

namespace AnoMech.Tests;

public class UmadP5FloodMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP5FloodMitigation.Plan(), "Flood");
    }

    [Test]
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP5FloodMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
