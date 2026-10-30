using AnoMech.Scenarios.Umad.P3BowelsOfAgony;
using AnoMech.Scenarios.Umad.P3Full;

namespace AnoMech.Tests;

public class UmadP3FullMitigationTests
{
    [TestCase(Element.Water)]
    [TestCase(Element.Fire)]
    public void TheChainedPlanHoldsForEveryComp(Element shortElement)
        => MitigationPlanAssert.IsSound(UmadP3FullScenario.MitigationPlan(shortElement), $"Full P3, short {shortElement}");
}
