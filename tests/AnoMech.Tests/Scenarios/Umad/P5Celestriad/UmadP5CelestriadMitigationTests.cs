using AnoMech.Scenarios.Umad.P5Celestriad;

namespace AnoMech.Tests;

public class UmadP5CelestriadMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP5CelestriadMitigation.Plan(), "Celestriad");
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP5CelestriadMitigation.Hits.All(h => h.Raw > 0f));
    }
}
