using AnoMech.Scenarios.Umad.P5Exaflares;

namespace AnoMech.Tests;

public class UmadP5ExaflaresMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP5ExaflaresMitigation.Plan(), "Exaflares");
    }

    [Test]
    public void NoHitHasKnownDamageYet()
    {
        Assert.That(UmadP5ExaflaresMitigation.Hits.All(h => h.Raw == null), "HP would start moving and the scenario's own damage numbers would stop showing");
    }
}
