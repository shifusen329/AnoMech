using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class JobActionsTests
{
    [TestCase(24310u, (ushort)3003, 20f)]   // Holos
    [TestCase(24310u, (ushort)3365, 20f)]   // Holos → Holosakos
    [TestCase(37034u, (ushort)2609, 30f)]   // Eukrasian Prognosis II, through Zoe's boost
    [TestCase(37030u, (ushort)1921, 30f)]   // Helios Conjunction, under Neutral Sect
    [TestCase(25789u, (ushort)2697, 30f)]   // Improvised Finish, scaled by Rising Rhythm
    [TestCase(188u, (ushort)299, 15f)]      // Sacred Soil's zone
    [TestCase(16536u, (ushort)1873, 20f)]   // Temperance, on the party
    [TestCase(7560u, (ushort)1203, 15f)]    // Addle, on the enemy
    public void GrantsOfSeesThroughWrappers(uint actionId, ushort statusId, float duration)
    {
        Assert.That(JobActions.GrantsOf(actionId), Does.Contain((statusId, duration)));
    }

    [TestCase(37011u, (ushort)3881)]   // Divine Caress spends Divine Grace
    [TestCase(37031u, (ushort)3895)]   // Sun Sign spends Suntouched
    [TestCase(34686u, (ushort)3686)]   // Tempera Grassa spends the Coat
    public void ARequiredStatusIsSpentByItsAction(uint actionId, ushort statusId)
    {
        Assert.That(JobActions.StatusesSpentBy(actionId), Does.Contain(statusId));
    }

    [Test]
    public void EveryGrantedStatusIsARealStatus()
    {
        var data = new DataminingGameData();
        foreach (var actionId in new uint[] { 7549, 7560, 2887, 7405, 16889, 16012, 16014, 25789, 25857, 34685, 34686,
                     16536, 37011, 7433, 16559, 37031, 37030, 3601, 3613, 3585, 186, 37013, 25868, 188, 16545, 16546, 16538,
                     24298, 24300, 37034, 24292, 24310, 24311 })
            foreach (var (statusId, _) in JobActions.GrantsOf(actionId))
                Assert.That(data.StatusName(statusId), Is.Not.Null, $"action {actionId} grants unknown status {statusId}");
    }
}
