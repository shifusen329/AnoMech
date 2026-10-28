using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Tests;

public class UmadP3LimitCutMitigationTests
{
    private const float KefkaReappearsAt = 50.581f;

    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP3LimitCutMitigation.Plan(), "Limit Cut", KefkaReappearsAt);
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP3LimitCutMitigation.Hits.All(h => h.Raw > 0f));
    }
}
