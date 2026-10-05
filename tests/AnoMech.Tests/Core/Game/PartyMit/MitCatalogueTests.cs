using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class MitCatalogueTests
{
    private static readonly DataminingGameData Data = new();

    [Test]
    public void EveryButtonIsTheGamesAction()
    {
        foreach (var def in MitCatalogue.All)
        {
            var row = Data.Action(def.ActionId);
            Assert.That(row, Is.Not.Null, def.Name);
            Assert.That(row!.Name, Is.EqualTo(def.Name), $"action {def.ActionId}");
            Assert.That(def.Recast, Is.LessThanOrEqualTo(row.RecastSeconds + 0.01f), $"{def.Name}: traits only shorten a recast");
            foreach (var alias in def.Aliases ?? [])
                Assert.That(Data.Action(alias), Is.Not.Null, $"{def.Name} alias {alias}");
        }
    }

    // A button the damage model reads must leave a status it knows, for as long as the catalogue
    // says, or the plan's windows and the host's view of a peer's press drift apart.
    [Test]
    public void EveryEffectingButtonGrantsAKnownStatusForItsDuration()
    {
        foreach (var def in MitCatalogue.All.Where(d => d.Kind != MitKind.None))
        {
            var known = JobActions.GrantsOf(def.ActionId).Where(g => Mitigation.ByStatusId.ContainsKey(g.StatusId)).ToList();
            Assert.That(known, Is.Not.Empty, $"{def.Name} grants no status the damage model reads");
            Assert.That(known.Select(g => g.Duration), Does.Contain(def.Duration), def.Name);
            foreach (var alias in def.Aliases ?? [])
                Assert.That(JobActions.GrantsOf(alias), Is.EquivalentTo(JobActions.GrantsOf(def.ActionId)), $"{def.Name} alias {alias}");
        }
    }

    [Test]
    public void EveryRequiredStatusIsGrantedByAButtonAndSpentByThePress()
    {
        foreach (var def in MitCatalogue.All.Where(d => d.Requires != 0))
        {
            Assert.That(Data.StatusName(def.Requires), Is.Not.Null, def.Name);
            Assert.That(MitCatalogue.All.Any(g => JobActions.GrantsOf(g.ActionId).Any(s => s.StatusId == def.Requires)), def.Name);
            Assert.That(JobActions.StatusesSpentBy(def.ActionId), Does.Contain(def.Requires), def.Name);
        }
    }

    [TestCase(JobId.Paladin, MitActionId.DivineVeil)]
    [TestCase(JobId.Warrior, MitActionId.ShakeItOff)]
    [TestCase(JobId.DarkKnight, MitActionId.DarkMissionary)]
    [TestCase(JobId.Gunbreaker, MitActionId.HeartOfLight)]
    [TestCase(JobId.Bard, MitActionId.Troubadour)]
    [TestCase(JobId.Machinist, MitActionId.Tactician)]
    [TestCase(JobId.Dancer, MitActionId.ShieldSamba)]
    public void PartyMitResolvesPerJob(JobId job, uint expected)
    {
        Assert.That(MitCatalogue.For(MitPlanEntry.PartyMit, job)?.ActionId, Is.EqualTo(expected));
    }

    [Test]
    public void JobsWithoutAPartyMitGetNone()
    {
        Assert.That(MitCatalogue.For(MitPlanEntry.PartyMit, JobId.BlackMage), Is.Null);
        Assert.That(MitCatalogue.For(MitPlanEntry.TankLb3, JobId.WhiteMage), Is.Null);
        Assert.That(MitCatalogue.For(MitPlanEntry.TankLb3, JobId.Gunbreaker)?.ActionId, Is.EqualTo(MitActionId.GunmetalSoul));
    }

    [Test]
    public void AnAliasFindsItsButton()
    {
        Assert.That(MitCatalogue.Find(MitActionId.AspectedHelios)?.ActionId, Is.EqualTo(MitActionId.HeliosConjunction));
        Assert.That(MitCatalogue.Find(MitActionId.EukrasianPrognosis)?.ActionId, Is.EqualTo(MitActionId.EukrasianPrognosisII));
    }
}
