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
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP5FloodMitigation.Hits.All(h => h.Raw > 0f));
    }
}
