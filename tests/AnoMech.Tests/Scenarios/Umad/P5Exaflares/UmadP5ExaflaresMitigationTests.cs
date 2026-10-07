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
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP5ExaflaresMitigation.Hits.All(h => h.Raw > 0f));
    }
}
