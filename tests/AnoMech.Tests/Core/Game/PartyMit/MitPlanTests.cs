using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class MitPlanTests
{
    private static Dictionary<PartyRole, JobId> Comp(params (PartyRole Slot, JobId Job)[] swaps)
    {
        var jobs = new Dictionary<PartyRole, JobId>
        {
            [PartyRole.MainTank] = JobId.Warrior, [PartyRole.OffTank] = JobId.Paladin,
            [PartyRole.RegenHealer] = JobId.WhiteMage, [PartyRole.ShieldHealer] = JobId.Scholar,
            [PartyRole.MeleeDpsA] = JobId.Dragoon, [PartyRole.MeleeDpsB] = JobId.Monk,
            [PartyRole.PhysRangedDps] = JobId.Bard, [PartyRole.CasterDps] = JobId.BlackMage,
        };
        foreach (var (slot, job) in swaps) jobs[slot] = job;
        return jobs;
    }

    private static MitPlanEntry Row(SheetColumn column, uint action, float at, params float[] covers)
        => new(column, action, at, "test", covers);

    [Test]
    public void ARowWithNoCoversCountsThreeSecondsEitherSide()
    {
        var (open, close, hard, soft) = MitPlan.Window(10f, [], 20f);
        Assert.That((open, close, hard.Length, soft.Length), Is.EqualTo((7f, 13f, 0, 0)));
    }

    [Test]
    public void ThePressMustLandBeforeTheFirstCoverAndOutlastTheLast()
    {
        // Kerachole (15s) at 5.5 for hits at 14 and 18.23: close 13.8, open min(5.5, 18.23 - 15 + 0.3).
        var (open, close, hard, soft) = MitPlan.Window(5.5f, [14f, 18.23f], 15f);
        Assert.That(close, Is.EqualTo(13.8f).Within(1e-4f));
        Assert.That(open, Is.EqualTo(3.53f).Within(1e-4f));
        Assert.That(hard, Is.EqualTo(new[] { 14f, 18.23f }));
        Assert.That(soft, Is.Empty);
    }

    [Test]
    public void ACoverNoPressCanReachTurnsSoft()
    {
        // Sacred Soil (15s) for 43.00, 45.40 and 60.00: pressed by 42.8 it's gone before 60.
        var (open, close, hard, soft) = MitPlan.Window(41.5f, [43f, 45.4f, 60f], 15f);
        Assert.That(close, Is.EqualTo(42.8f).Within(1e-4f));
        Assert.That(hard, Is.EqualTo(new[] { 43f, 45.4f }));
        Assert.That(soft, Is.EqualTo(new[] { 60f }));
        Assert.That(open, Is.EqualTo(30.7f).Within(1e-4f));
    }

    [Test]
    public void ACoverAPressCanStillReachStaysHard()
    {
        // 57.23 - 15 + 0.3 = 42.53, inside the window that closes at 42.8.
        var (_, _, hard, soft) = MitPlan.Window(41.5f, [43f, 45.4f, 57.23f], 15f);
        Assert.That(hard, Has.Length.EqualTo(3));
        Assert.That(soft, Is.Empty);
    }

    [Test]
    public void HealerColumnsFollowTheJob()
    {
        var rows = new[] { Row(SheetColumn.WHM, MitActionId.Temperance, 10f), Row(SheetColumn.AST, MitActionId.CollectiveUnconscious, 10f),
                           Row(SheetColumn.SCH, MitActionId.Expedient, 10f), Row(SheetColumn.SGE, MitActionId.Kerachole, 10f) };
        Assert.That(MitPlan.Resolve(rows, Comp()).Select(p => p.Action.Name), Is.EqualTo(new[] { "Temperance", "Expedient" }));
        var swapped = MitPlan.Resolve(rows, Comp((PartyRole.RegenHealer, JobId.Astrologian), (PartyRole.ShieldHealer, JobId.Sage)));
        Assert.That(swapped.Select(p => (p.Slot, p.Action.Name)), Is.EqualTo(new[]
        {
            (PartyRole.RegenHealer, "Collective Unconscious"), (PartyRole.ShieldHealer, "Kerachole"),
        }));
    }

    [TestCase(JobId.Warrior, "Shake It Off")]
    [TestCase(JobId.DarkKnight, "Dark Missionary")]
    [TestCase(JobId.Gunbreaker, "Heart of Light")]
    public void PartyMitIsTheSlotJobs(JobId mainTank, string expected)
    {
        var plan = MitPlan.Resolve([Row(SheetColumn.MT, MitPlanEntry.PartyMit, 10f)], Comp((PartyRole.MainTank, mainTank)));
        Assert.That(plan.Single().Action.Name, Is.EqualTo(expected));
    }

    [Test]
    public void AJobFilterDropsTheRowForOtherJobs()
    {
        var row = new MitPlanEntry(SheetColumn.MT, MitPlanEntry.PartyMit, 10f, "test", [], [JobId.Gunbreaker, JobId.DarkKnight]);
        Assert.That(MitPlan.Resolve([row], Comp()), Is.Empty);
        Assert.That(MitPlan.Resolve([row], Comp((PartyRole.MainTank, JobId.DarkKnight))), Has.Count.EqualTo(1));
    }

    [TestCase(JobId.Bard, "Troubadour")]
    [TestCase(JobId.Machinist, "Tactician")]
    [TestCase(JobId.Dancer, "Shield Samba")]
    public void RangedPartyMitFollowsTheJob(JobId ranged, string expected)
    {
        var plan = MitPlan.Resolve([Row(SheetColumn.D3, MitPlanEntry.PartyMit, 10f)], Comp((PartyRole.PhysRangedDps, ranged)));
        Assert.That(plan.Single().Action.Name, Is.EqualTo(expected));
    }

    [Test]
    public void ARowTheSlotsJobCantPressIsDropped()
    {
        Assert.That(MitPlan.Resolve([Row(SheetColumn.D1, MitActionId.Feint, 10f)], Comp((PartyRole.MeleeDpsA, JobId.WhiteMage))), Is.Empty);
        Assert.That(MitPlan.Resolve([Row(SheetColumn.D4, MitPlanEntry.PartyMit, 10f)], Comp()), Is.Empty);
    }

    [TestCase(JobId.RedMage, "Magick Barrier")]
    [TestCase(JobId.Pictomancer, "Tempera Grassa")]
    public void ExtrasFindTheJobWhereverItSits(JobId job, string expected)
    {
        var rows = new[]
        {
            new MitPlanEntry(SheetColumn.Extras, MitActionId.MagickBarrier, 10f, "test", [], [JobId.RedMage]),
            new MitPlanEntry(SheetColumn.Extras, MitActionId.TemperaGrassa, 10f, "test", [], [JobId.Pictomancer]),
        };
        Assert.That(MitPlan.Resolve(rows, Comp()), Is.Empty, "the fixed bots have no Extras");
        var inR2 = MitPlan.Resolve(rows, Comp((PartyRole.CasterDps, job)));
        Assert.That(inR2.Select(p => (p.Slot, p.Action.Name, p.IsExtra)), Is.EqualTo(new[] { (PartyRole.CasterDps, expected, true) }));
        var inMt = MitPlan.Resolve(rows, Comp((PartyRole.MainTank, job)));
        Assert.That(inMt.Single().Slot, Is.EqualTo(PartyRole.MainTank), "a Role override still finds it");
    }

    [Test]
    public void APressBeforeTheScenarioIsPreStart()
    {
        var plan = MitPlan.Resolve([Row(SheetColumn.SGE, MitActionId.Holos, -6.5f, 9.46f)], Comp((PartyRole.ShieldHealer, JobId.Sage)));
        Assert.That(plan.Single().IsPreStart);
    }

    [Test]
    public void ValidateCatchesARecastAndAMissingRequirement()
    {
        var plan = MitPlan.Resolve([
            Row(SheetColumn.SGE, MitActionId.Kerachole, 10f, 11f),
            Row(SheetColumn.SGE, MitActionId.Kerachole, 30f, 31f),
            Row(SheetColumn.SGE, MitActionId.Kerachole, 50f, 51f),
        ], Comp((PartyRole.ShieldHealer, JobId.Sage)));
        Assert.That(MitPlan.Validate(plan), Has.Count.EqualTo(2), "Kerachole is 30s");

        var caress = MitPlan.Resolve([Row(SheetColumn.WHM, MitActionId.DivineCaress, 20f, 21f)], Comp());
        Assert.That(MitPlan.Validate(caress).Single(), Does.Contain("needs status 3881"));

        var afterTemperance = MitPlan.Resolve([
            Row(SheetColumn.WHM, MitActionId.Temperance, 10f, 11f),
            Row(SheetColumn.WHM, MitActionId.DivineCaress, 20f, 21f),
        ], Comp());
        Assert.That(MitPlan.Validate(afterTemperance), Is.Empty);
    }

    [Test]
    public void GrassaWithinTenSecondsOfTheCoatIsValid()
    {
        var pct = Comp((PartyRole.CasterDps, JobId.Pictomancer));
        var rows = new[]
        {
            new MitPlanEntry(SheetColumn.Extras, MitActionId.TemperaCoat, 36.81f, "test", [], [JobId.Pictomancer]),
            new MitPlanEntry(SheetColumn.Extras, MitActionId.TemperaGrassa, 37.51f, "test", [38.51f], [JobId.Pictomancer]),
        };
        Assert.That(MitPlan.Validate(MitPlan.Resolve(rows, pct)), Is.Empty);
        rows[1] = rows[1] with { At = 48f, Covers = [49f] };
        Assert.That(MitPlan.Validate(MitPlan.Resolve(rows, pct)), Is.Not.Empty);
    }
}
