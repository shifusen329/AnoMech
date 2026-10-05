using AnoMech.Scenarios.Umad.P1TeleTrouncing;

namespace AnoMech.Tests;

public class UmadP1TeleTrouncingMitigationTests
{
    [Test]
    public void ThePlanHoldsForEveryComp()
    {
        MitigationPlanAssert.IsSound(UmadP1TeleTrouncingMitigation.Plan(), "Tele-trouncing");
    }

    // Everyone is asleep or confused from 29.09s to 35.09s, so nobody can press.
    [Test]
    public void NothingIsPlannedWhileThePartyIsAsleepOrConfused()
    {
        Assert.That(UmadP1TeleTrouncingMitigation.Plan().Entries.Where(e => e.At is > 29.09f and < 35.09f), Is.Empty);
    }
}
