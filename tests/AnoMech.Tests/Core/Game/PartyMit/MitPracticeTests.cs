using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad;

namespace AnoMech.Tests;

public class MitPracticeTests
{
    private static readonly MitProfile Profile = new(325200f, 226600f, 205200f, 0.61f, 1f, 0.92f, 0.69f, 3f);
    private static readonly MitSource Kefka = new("Kefka");

    private FakeGame fake = null!;
    private SimWorld world = null!;

    [SetUp]
    public void InstallGame()
    {
        fake = FakeGame.Install();
        fake.BattleCharas.Player.ClassJob = (byte)JobId.Sage;
        var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        world = game.World;
        world.SeatParty(new LocalPartyProvider().Roster((uint)JobId.Sage));
        world.MitPractice.Enabled = true;
    }

    [TearDown]
    public void RemoveGame()
    {
        world.Despawn();
        DalamudServices.Install(nameof(Plugin.Game), null!);
    }

    private static MitPlanData Data(params MitPlanEntry[] entries) => new(Profile,
        [new("hit", "Big Hit", 100000f, Kefka), new("unknown", "Mystery", null, Kefka)],
        [new(5f, ["hit"])], entries, Kefka);

    private void Step(float seconds)
    {
        world.Events.Tick(seconds);
        world.Tick(seconds);
    }

    private SimCharacter Slot(PartyRole role) => world.Party.Get(role)!;

    // One HP point of the character's own max: WriteHp rounds to whole HP.
    private const float ShareTolerance = 1e-4f;

    private float HpShare(PartyRole role) => (float)Slot(role).Proxy!.Health / Slot(role).Proxy!.MaxHealth;

    [Test]
    public void BotsPressThePlanWhenItSays()
    {
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(0.5f);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
        Step(0.6f);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), "the bot PLD's Divine Veil reaches the party");
    }

    [Test]
    public void ABotSkipsAPressInsideItsRecast()
    {
        world.MitPractice.Begin(Data(
            new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f]),
            new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 20f, "Again", [25f])));
        Step(1.1f);
        Slot(PartyRole.MeleeDpsA).RemoveStatus(1362);
        Step(19.5f);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False, "Divine Veil is a 90s recast");
    }

    [Test]
    public void APressPlannedBeforeTheStartIsAppliedForThePlayer()
    {
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.SGE, MitActionId.Holos, -6.5f, "Big Hit", [5f])));
        Step(0.1f);
        Assert.That(world.Party.Player!.HasStatus(3003), "the player's Sage had no chance to press Holos");
    }

    [Test]
    public void ADebuffGoesOnTheBossItsRowNames()
    {
        var chaos = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.ChaosP3))!;
        var exdeath = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.Exdeath))!;
        var chaosSource = new MitSource("Chaos");
        var exdeathSource = new MitSource("Exdeath");
        world.MitPractice.Begin(new MitPlanData(Profile, [new("hit", "Big Hit", null, exdeathSource)], [new(5f, ["hit"])],
                [new MitPlanEntry(SheetColumn.D4, MitActionId.Addle, 1f, "Big Hit", [5f], On: exdeathSource)], chaosSource),
            new Dictionary<MitSource, Func<SimCharacter?>> { [chaosSource] = () => chaos, [exdeathSource] = () => exdeath });
        Step(1.1f);
        Assert.That(exdeath.HasStatus(1203), "the row names Exdeath");
        Assert.That(chaos.HasStatus(1203), Is.False, "though the plan's debuffs go on Chaos");
    }

    [Test]
    public void BeginStartsEveryoneOnFullHp()
    {
        world.MitPractice.Begin(Data());
        Assert.That(HpShare(PartyRole.MainTank), Is.EqualTo(1f));
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
        Assert.That(HpShare(PartyRole.ShieldHealer), Is.EqualTo(1f), "the player's Sage");
    }

    // The bar shows each class's pool as a share of the character's own max HP.
    [Test]
    public void AHitTakesEachClassItsShare()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.HitParty("hit");
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - 100000f) / 226600f).Within(ShareTolerance));
        Assert.That(HpShare(PartyRole.MainTank), Is.EqualTo((325200f - 61000f) / 325200f).Within(ShareTolerance));
        Assert.That(HpShare(PartyRole.ShieldHealer), Is.EqualTo((205200f - 92000f) / 205200f).Within(ShareTolerance));
    }

    [Test]
    public void ABarrierAbsorbsAndIsUsedUp()
    {
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - (100000f - 22660f)) / 226600f).Within(ShareTolerance),
            "Divine Veil is 10% of the target's max HP");
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
    }

    [Test]
    public void HpRefillsOnceNothingHasHitForAWhile()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Step(2f);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.LessThan(1f));
        Step(1.5f);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
    }

    [Test]
    public void AWouldBeKillLeavesASliverAndNobodyDies()
    {
        world.MitPractice.Begin(Data());
        for (var i = 0; i < 3; i++) world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Assert.That(Slot(PartyRole.MeleeDpsA).Proxy!.Health, Is.EqualTo(1u));
        Assert.That(Slot(PartyRole.MeleeDpsA).IsAlive());
    }

    [Test]
    public void AHitWithNoKnownDamageMovesNoHp()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.HitParty("unknown");
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
    }

    [Test]
    public void OffDoesNothing()
    {
        world.MitPractice.Enabled = false;
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        world.MitPractice.HitParty("hit");
        Assert.That(world.MitPractice.IsActive, Is.False);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
    }

    [Test]
    public void ASoloRunHasNoPractice()
    {
        world.Despawn();
        world.SeatParty(new LocalPartyProvider { Solo = true }.Roster((uint)JobId.Sage));
        world.MitPractice.Begin(Data());
        Assert.That(world.MitPractice.IsActive, Is.False);
    }

    [Test]
    public void TheRunsEndPutsThePlayerBackOnFullHp()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.Hit(world.Party.Player, "hit");
        Assert.That(fake.BattleCharas.Player.Health, Is.LessThan(fake.BattleCharas.Player.MaxHealth));
        world.Despawn();
        Assert.That(fake.BattleCharas.Player.Health, Is.EqualTo(fake.BattleCharas.Player.MaxHealth));
    }
}
