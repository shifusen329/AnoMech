using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

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
        DalamudServices.Install(nameof(Plugin.GameInstance), game);
        world = game.World;
        world.CreateParty((uint)JobId.Sage);
        world.MitPractice.Enabled = true;
    }

    [TearDown]
    public void RemoveGame()
    {
        world.Despawn();
        DalamudServices.Install(nameof(Plugin.GameInstance), null!);
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

    private uint Hp(PartyRole role) => Slot(role).Proxy!.Health;

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
    public void BeginPutsEveryoneOnTheirClassMaxHp()
    {
        world.MitPractice.Begin(Data());
        Assert.That(Slot(PartyRole.MainTank).Proxy!.MaxHealth, Is.EqualTo(325200u));
        Assert.That(Slot(PartyRole.MeleeDpsA).Proxy!.MaxHealth, Is.EqualTo(226600u));
        Assert.That(Slot(PartyRole.ShieldHealer).Proxy!.MaxHealth, Is.EqualTo(205200u), "the player's Sage");
    }

    [Test]
    public void AHitTakesEachClassItsShare()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.HitParty("hit");
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.EqualTo(226600u - 100000u));
        Assert.That(Hp(PartyRole.MainTank), Is.EqualTo(325200u - 61000u));
        Assert.That(Hp(PartyRole.ShieldHealer), Is.EqualTo(205200u - 92000u));
    }

    [Test]
    public void ABarrierAbsorbsAndIsUsedUp()
    {
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.EqualTo(226600u - (100000u - 22660u)), "Divine Veil is 10% of the target's max HP");
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
    }

    [Test]
    public void HpRefillsOnceNothingHasHitForAWhile()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Step(2f);
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.LessThan(226600u));
        Step(1.5f);
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.EqualTo(226600u));
    }

    [Test]
    public void AWouldBeKillLeavesASliverAndNobodyDies()
    {
        world.MitPractice.Begin(Data());
        for (var i = 0; i < 3; i++) world.MitPractice.Hit(Slot(PartyRole.MeleeDpsA), "hit");
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.EqualTo(1u));
        Assert.That(Slot(PartyRole.MeleeDpsA).IsAlive());
    }

    [Test]
    public void AHitWithNoKnownDamageMovesNoHp()
    {
        world.MitPractice.Begin(Data());
        world.MitPractice.HitParty("unknown");
        Assert.That(Hp(PartyRole.MeleeDpsA), Is.EqualTo(226600u));
    }

    [Test]
    public void OffDoesNothing()
    {
        world.MitPractice.Enabled = false;
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Assert.That(world.MitPractice.IsActive, Is.False);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
        Assert.That(Slot(PartyRole.MeleeDpsA).Proxy!.MaxHealth, Is.Not.EqualTo(226600u));
    }

    [Test]
    public void ASoloRunHasNoPractice()
    {
        world.Despawn();
        world.CreateParty((uint)JobId.Sage, solo: true);
        world.MitPractice.Begin(Data());
        Assert.That(world.MitPractice.IsActive, Is.False);
    }

    [Test]
    public void DespawnRestoresThePlayersRealMaxHp()
    {
        world.MitPractice.Begin(Data());
        Assert.That(fake.BattleCharas.Player.MaxHealth, Is.EqualTo(205200u));
        world.Despawn();
        Assert.That(fake.BattleCharas.Player.MaxHealth, Is.EqualTo(100000u));
    }
}
