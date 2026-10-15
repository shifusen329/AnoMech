using AnoMech.Scenarios.Umad.P3BowelsOfAgony;

namespace AnoMech.Tests;

public class UmadP3BowelsOfAgonyMitigationTests
{
    [TestCase(Element.Water)]
    [TestCase(Element.Fire)]
    public void ThePlanHoldsForEveryComp(Element shortElement)
    {
        MitigationPlanAssert.IsSound(UmadP3BowelsOfAgonyMitigation.Plan(shortElement), $"Bowels of Agony, short {shortElement}", 78.35f);
    }

    [Test]
    public void EveryHitHasDamage()
    {
        Assert.That(UmadP3BowelsOfAgonyMitigation.Hits.All(h => h.Raw > 0f));
    }
}
