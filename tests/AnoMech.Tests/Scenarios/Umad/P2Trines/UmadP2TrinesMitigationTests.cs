using AnoMech.Scenarios.Umad.P2Trines;

namespace AnoMech.Tests;

public class UmadP2TrinesMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP2TrinesMitigation.Plan(), "Trines", endsAt: 44f);
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP2TrinesMitigation.Hits.All(h => h.Raw > 0f));
    }
}
