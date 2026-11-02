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
        DebugBotControl.Enabled = false;
        var game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        world = game.World;
        world.SeatParty(new LocalPartyProvider().Roster((uint)JobId.Sage));
        world.MitPractice.Enabled = true;
        fake.UserActions.Enabled = true;
    }

    [TearDown]
    public void RemoveGame()
    {
        world.Despawn();
        DalamudServices.Install(nameof(Plugin.Game), null!);
    }

    private static MitPlanData Data(params MitPlanEntry[] entries) => new(Profile,
        [new("hit", "Big Hit", 100000f, Kefka)], [new(5f, ["hit"])], entries, Kefka);

    private void Step(float seconds)
    {
        world.Events.Tick(seconds);
        world.Tick(seconds);
    }

    private SimCharacter Slot(PartyRole role) => world.Party.Get(role)!;

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
    public void BotsPressWithPracticeOff()
    {
        world.MitPractice.Enabled = false;
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362));
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
    public void PractisingThePlayerPressesTheirOwn()
    {
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.SGE, MitActionId.Holos, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Assert.That(world.Party.Player!.HasStatus(3003), Is.False);
    }

    [Test]
    public void WithPracticeOffThePlayersRowsArePressedForThem()
    {
        world.MitPractice.Enabled = false;
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.SGE, MitActionId.Holos, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Assert.That(world.Party.Player!.HasStatus(3003));
    }

    [Test]
    public void WithoutResolvingTheirOwnActionsThePlayersRowsArePressedForThem()
    {
        fake.UserActions.Enabled = false;
        world.MitPractice.Begin(Data(new MitPlanEntry(SheetColumn.SGE, MitActionId.Holos, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Assert.That(world.Party.Player!.HasStatus(3003));
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
    public void APlayersDebuffRowPressedForThemGoesOnTheBoss()
    {
        world.Despawn();
        fake.BattleCharas.Player.ClassJob = (byte)JobId.Monk;
        world.SeatParty(new LocalPartyProvider().Roster((uint)JobId.Monk));
        world.MitPractice.Enabled = false;
        var kefka = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.ChaosP3))!;
        var column = world.Party.Player!.Role == PartyRole.MeleeDpsA ? SheetColumn.D1 : SheetColumn.D2;
        world.MitPractice.Begin(Data(new MitPlanEntry(column, MitActionId.Feint, 1f, "Big Hit", [5f])),
            new Dictionary<MitSource, Func<SimCharacter?>> { [Kefka] = () => kefka });
        Step(1.1f);
        Assert.That(kefka.HasStatus(1195), "Feint goes on the boss");
        Assert.That(world.Party.Player!.HasStatus(1195), Is.False);
    }

    [Test]
    public void ASoloRunHasNoPractice()
    {
        world.Despawn();
        world.SeatParty(new LocalPartyProvider { Solo = true }.Roster((uint)JobId.Sage));
        world.MitPractice.Begin(Data());
        Assert.That(world.MitPractice.IsActive, Is.False);
    }
}
