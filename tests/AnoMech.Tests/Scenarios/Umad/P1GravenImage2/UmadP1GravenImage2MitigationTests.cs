using AnoMech.Scenarios.Umad.P1GravenImage2;

namespace AnoMech.Tests;

public class UmadP1GravenImage2MitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP1GravenImage2Mitigation.Plan(), "Graven Image 2");
    }
}
