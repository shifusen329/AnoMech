using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad;

namespace AnoMech.Tests;

public class PartyHpTests
{
    private static readonly MitProfile Profile = new(325200f, 226600f, 205200f, 0.61f, 1f, 0.92f, 0.69f, 3f);
    private static readonly MitSource Kefka = new("Kefka");
    private const uint BigHit = UmadConstants.ActionId.Forsaken;

    private FakeGame fake = null!;
    private Game game = null!;
    private SimWorld world = null!;
    private readonly List<(PartyRole Role, string Cause)> deaths = [];

    [SetUp]
    public void InstallGame()
    {
        fake = FakeGame.Install();
        fake.BattleCharas.Player.ClassJob = (byte)JobId.Sage;
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        DebugBotControl.Enabled = false;
        game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        world = game.World;
        world.SeatParty(new LocalPartyProvider().Roster((uint)JobId.Sage));
        deaths.Clear();
        game.PartyMemberKilled += (role, cause, _, _) => deaths.Add((role, cause));
    }

    [TearDown]
    public void RemoveGame()
    {
        game.Dispose();
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

    // One HP point of the character's own max: the bar rounds to whole HP.
    private const float ShareTolerance = 1e-4f;

    private float HpShare(PartyRole role) => (float)Slot(role).Proxy!.Health / Slot(role).Proxy!.MaxHealth;

    private void Hit(SimCharacter victim, int times = 1)
    {
        for (var i = 0; i < times; i++) world.Hp.Hit(victim, "hit", BigHit);
    }

    [Test]
    public void BeginStartsEveryoneOnFullHp()
    {
        world.Hp.Begin(Data());
        Assert.That(HpShare(PartyRole.MainTank), Is.EqualTo(1f));
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
        Assert.That(HpShare(PartyRole.ShieldHealer), Is.EqualTo(1f), "the player's Sage");
    }

    // The bar shows each class's pool as a share of the character's own max HP.
    [Test]
    public void AHitTakesEachClassItsShare()
    {
        world.Hp.Begin(Data());
        world.Hp.HitParty("hit", BigHit);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - 100000f) / 226600f).Within(ShareTolerance));
        Assert.That(HpShare(PartyRole.MainTank), Is.EqualTo((325200f - 61000f) / 325200f).Within(ShareTolerance));
        Assert.That(HpShare(PartyRole.ShieldHealer), Is.EqualTo((205200f - 92000f) / 205200f).Within(ShareTolerance));
    }

    [Test]
    public void ABarrierAbsorbsAndIsUsedUp()
    {
        world.Hp.Begin(Data(new MitPlanEntry(SheetColumn.OT, MitPlanEntry.PartyMit, 1f, "Big Hit", [5f])));
        Step(1.1f);
        Hit(Slot(PartyRole.MeleeDpsA));
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - (100000f - 22660f)) / 226600f).Within(ShareTolerance),
            "Divine Veil is 10% of the target's max HP");
        Assert.That(Slot(PartyRole.MeleeDpsA).HasStatus(1362), Is.False);
    }

    [Test]
    public void HpRefillsOnceNothingHasHitForAWhile()
    {
        world.Hp.Begin(Data());
        Hit(Slot(PartyRole.MeleeDpsA));
        Step(2f);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.LessThan(1f));
        Step(1.5f);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
    }

    [Test]
    public void TwoSurvivableHitsAddUp()
    {
        world.Hp.Begin(Data());
        Hit(Slot(PartyRole.MeleeDpsA), times: 2);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - 200000f) / 226600f).Within(ShareTolerance));
    }

    [Test]
    public void AHumanOutOfHpDies()
    {
        world.Hp.Begin(Data());
        Hit(world.Party.Player!, times: 3);
        Assert.That(world.Party.Player!.IsAlive(), Is.False);
        Assert.That(deaths, Has.Exactly(1).Matches<(PartyRole Role, string Cause)>(d => d.Role == PartyRole.ShieldHealer && d.Cause.Contains("ran out of HP")));
    }

    [Test]
    public void ABotOutOfHpKeepsASliverAndLives()
    {
        world.Hp.Begin(Data());
        Hit(Slot(PartyRole.MeleeDpsA), times: 3);
        Assert.That(Slot(PartyRole.MeleeDpsA).IsAlive());
        Assert.That(Slot(PartyRole.MeleeDpsA).Proxy!.Health, Is.EqualTo(1u));
        Assert.That(deaths, Is.Empty);
    }

    [Test]
    public void WithDeathsOffAHumanKeepsASliver()
    {
        world.Hp.HumansDie = false;
        world.Hp.Begin(Data());
        Hit(world.Party.Player!, times: 3);
        Assert.That(world.Party.Player!.IsAlive());
        Assert.That(fake.BattleCharas.Player.Health, Is.EqualTo(1u));
        Assert.That(deaths, Is.Empty);
    }

    [Test]
    public void GodModeReportsTheDeathAndLeavesASliver()
    {
        game.GodMode = true;
        world.Hp.Begin(Data());
        Hit(world.Party.Player!, times: 3);
        Assert.That(world.Party.Player!.IsAlive());
        Assert.That(fake.BattleCharas.Player.Health, Is.EqualTo(1u));
        Assert.That(game.Recap, Is.Not.Null, "the death is still reported");
    }

    [Test]
    public void EveryoneOutOfHpIsReported()
    {
        var ranOut = new List<SimCharacter>();
        world.Hp.RanOut += (who, _) => ranOut.Add(who);
        world.Hp.Begin(Data());
        Hit(Slot(PartyRole.MeleeDpsA), times: 3);
        Assert.That(ranOut, Is.EqualTo(new[] { Slot(PartyRole.MeleeDpsA) }));
    }

    [Test]
    public void HpMovesWithPracticeOff()
    {
        world.MitPractice.Enabled = false;
        world.Hp.Begin(Data());
        Hit(Slot(PartyRole.MeleeDpsA));
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.LessThan(1f));
    }

    [Test]
    public void ASoloRunHasNoHp()
    {
        world.Despawn();
        world.SeatParty(new LocalPartyProvider { Solo = true }.Roster((uint)JobId.Sage));
        world.Hp.Begin(Data());
        Assert.That(world.Hp.Active, Is.False);
    }

    [Test]
    public void AHitWithNoKnownDamageMovesNoHp()
    {
        world.Hp.Begin(Data());
        world.Hp.HitParty("unknown", BigHit);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
    }

    [Test]
    public void TheRunsEndClearsTheShieldOverlay()
    {
        world.Hp.Begin(Data());
        Step(0.1f);
        Assert.That(Slot(PartyRole.MeleeDpsA).ShieldOverride, Is.Not.Null);
        world.Despawn();
        Assert.That(Slot(PartyRole.MeleeDpsA).ShieldOverride, Is.Null);
    }

    [Test]
    public void TheRunsEndPutsThePlayerBackOnFullHp()
    {
        world.Hp.Begin(Data());
        Hit(world.Party.Player!);
        Assert.That(fake.BattleCharas.Player.Health, Is.LessThan(fake.BattleCharas.Player.MaxHealth));
        world.Despawn();
        Assert.That(fake.BattleCharas.Player.Health, Is.EqualTo(fake.BattleCharas.Player.MaxHealth));
    }
}
