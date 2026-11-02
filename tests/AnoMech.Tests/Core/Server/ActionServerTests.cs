using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad;

namespace AnoMech.Tests;

public class ActionServerTests
{
    private const ushort StandardStep = 1818, StandardFinish = 1821, LastDanceReady = 3867, TechnicalFinish = 1822;
    private const int StepsIndex = 4, StepIndexIndex = 8;

    private FakeGame fake = null!;
    private Game game = null!;
    private SimWorld world = null!;
    private SimEnemy boss = null!;

    private void Install(JobId job)
    {
        fake = FakeGame.Install();
        fake.BattleCharas.Player.ClassJob = (byte)job;
        fake.UserActions.Enabled = true;
        DalamudServices.Install(nameof(Plugin.Config), new Configuration());
        DebugBotControl.Enabled = false;
        game = new Game();
        DalamudServices.Install(nameof(Plugin.Game), game);
        world = game.World;
        world.SeatParty(new LocalPartyProvider().Roster((uint)job));
        boss = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.ChaosP3))!;
        world.Server.Rng = new Random(1);
        world.Server.Begin();
    }

    [TearDown]
    public void RemoveGame()
    {
        game.Dispose();
        DalamudServices.Install(nameof(Plugin.Game), null!);
    }

    private SimPlayer Player => world.Party.Player!;

    private void Press(uint actionId, SimCharacter? target = null, ushort sequence = 7)
        => world.Server.Request(Player, actionId, (ulong)(target ?? boss).GameObjectId, sequence);

    private void Step(float seconds)
    {
        const float frame = 0.05f;
        for (var t = 0f; t < seconds - 1e-4f; t += frame) world.Tick(frame);
    }

    private byte[] Gauge => fake.UserActions.Gauges.Last();

    [Test]
    public void APressIsAnsweredWithAnEffectNamingIt()
    {
        Install(JobId.Machinist);
        Press(7411, sequence: 42);
        var effect = fake.UserActions.OwnEffects.Single();
        Assert.That(effect.ActionId, Is.EqualTo(7411u));
        Assert.That(effect.SourceSequence, Is.EqualTo((ushort)42));
        Assert.That(effect.AnimationTarget, Is.EqualTo(boss.GameObjectId));
        Assert.That(effect.AnimationLock, Is.EqualTo(0.6f));
        Assert.That(effect.Targets!.Single().Effects!.Single().Type, Is.EqualTo((byte)0x1B), "the combo starter's marker");
    }

    [Test]
    public void TheGaugeGoesToThePlayersClient()
    {
        Install(JobId.Machinist);
        Press(7411);
        Press(7412);
        Assert.That(Gauge[0], Is.EqualTo((byte)JobId.Machinist));
        Assert.That(Gauge[6], Is.EqualTo(10), "Heat after two combo steps");
    }

    [Test]
    public void EachTargetGetsItsEffectResultAfterTheEffect()
    {
        Install(JobId.Machinist);
        Press(7411);
        Assert.That(fake.BattleCharas.EffectResults, Is.Empty);
        Step(0.6f);
        Assert.That(fake.BattleCharas.EffectResults.Single().EntityId, Is.EqualTo(boss.Proxy!.EntityId));
    }

    [Test]
    public void APaladinStartsTheScenarioWithFullOath()
    {
        Install(JobId.Paladin);
        Assert.That(Gauge[0], Is.EqualTo((byte)JobId.Paladin));
        Assert.That(Gauge[2], Is.EqualTo(100));
    }

    [Test]
    public void ADancerFinishAfterStepsEndsTheDance()
    {
        Install(JobId.Dancer);
        Press(15997, Player);
        Assert.That(Player.HasStatus(StandardStep));
        for (var i = 0; i < 2; i++) Press(15998u + Gauge[StepsIndex + i], Player);
        Assert.That(Gauge[StepIndexIndex], Is.EqualTo(2));
        Press(16192, Player);
        Assert.That(Player.HasStatus(StandardStep), Is.False);
        Assert.That(Player.HasStatus(StandardFinish));
        Assert.That(Player.HasStatus(LastDanceReady));
        Assert.That(Gauge[StepsIndex..(StepIndexIndex + 1)], Is.All.Zero);
    }

    [Test]
    public void TechnicalFinishsPartyBuffFollowsAsItsOwnAction()
    {
        Install(JobId.Dancer);
        Press(15998, Player);
        for (var i = 0; i < 4; i++) Press(15998u + Gauge[StepsIndex + i], Player);
        Press(16196, Player);
        Assert.That(Player.HasStatus(TechnicalFinish), Is.False);
        Step(0.7f);
        Assert.That(Player.HasStatus(TechnicalFinish));
        Assert.That(world.Party.Get(PartyRole.MainTank)!.HasStatus(TechnicalFinish), "on the party around");
        Assert.That(fake.UserActions.OwnEffects.Last().ActionId, Is.EqualTo(33218u));
    }

    [Test]
    public void ABotsDebuffIsAimedAtTheBoss()
    {
        Install(JobId.Sage);
        var melee = (SimPartyNpc)world.Party.Get(PartyRole.MeleeDpsA)!;
        world.Server.Request(melee, 7549, (ulong)boss.GameObjectId);   // Feint
        var effect = fake.BattleCharas.EffectsReceived.Last(e => e.ActionId == 7549);
        Assert.That(effect.AnimationTarget, Is.EqualTo(boss.GameObjectId));
        Assert.That(effect.Targets!.Select(t => t.Id), Does.Contain(boss.GameObjectId));
        Assert.That(boss.HasStatus(1195));
        Assert.That(fake.UserActions.OwnEffects, Is.Empty, "a bot's press isn't the player's");
    }

    [Test]
    public void APressAppliedForAnotherSeatShowsNothing()
    {
        Install(JobId.Paladin);
        var before = fake.UserActions.OwnEffects.Count;
        world.Server.Apply(Player, 7531, (ulong)Player.GameObjectId);   // Rampart
        Assert.That(Player.HasStatus(1191));
        Assert.That(fake.UserActions.OwnEffects, Has.Count.EqualTo(before));
    }

    [Test]
    public void LiliesFillWhileThePlayerResolvesTheirOwnActions()
    {
        Install(JobId.WhiteMage);
        Step(20.5f);
        Assert.That(Gauge[6], Is.EqualTo(1));
    }

    [Test]
    public void NothingTicksOnThePlayersGaugeWhenTheyDontResolveTheirOwnActions()
    {
        Install(JobId.WhiteMage);
        fake.UserActions.Enabled = false;
        var sent = fake.UserActions.Gauges.Count;
        Step(21f);
        Assert.That(fake.UserActions.Gauges, Has.Count.EqualTo(sent));
    }
}
