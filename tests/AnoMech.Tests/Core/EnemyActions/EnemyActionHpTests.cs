using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;

namespace AnoMech.Tests;

// Forsaken: a raidwide, 7s cast, landing 1.91s after it resolves.
public class EnemyActionHpTests
{
    private static readonly MitProfile Profile = new(325200f, 226600f, 205200f, 0.61f, 1f, 0.92f, 0.69f, 3f);
    private const uint ForsakenId = UmadConstants.ActionId.Forsaken;
    private const float JustAfterTheRelease = 7.05f;
    private const float AfterItLands = 9.5f;

    private FakeGame fake = null!;
    private Game game = null!;
    private SimWorld world = null!;
    private SimEnemy kefka = null!;
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
        kefka = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.ChaosP3))!;
        deaths.Clear();
        game.PartyMemberKilled += (role, cause, _, _) => deaths.Add((role, cause));
    }

    [TearDown]
    public void RemoveGame()
    {
        game.Dispose();
        DalamudServices.Install(nameof(Plugin.Game), null!);
    }

    private static MitPlanData Data(float raw, uint actionId = ForsakenId) => new(Profile,
        [new("forsaken", "Forsaken", raw, null, ActionIds: [actionId])], [], []);

    private void Step(float seconds)
    {
        const float frame = 0.05f;
        for (var t = 0f; t < seconds - 1e-4f; t += frame)
        {
            world.Events.Tick(frame);
            world.Tick(frame);
        }
    }

    private SimCharacter Slot(PartyRole role) => world.Party.Get(role)!;

    private float HpShare(PartyRole role) => (float)Slot(role).Proxy!.Health / Slot(role).Proxy!.MaxHealth;

    private uint DamageShownOn(PartyRole role)
    {
        var effect = fake.BattleCharas.EffectsReceived.Last(e => e.ActionId == ForsakenId);
        var target = effect.Targets!.Single(t => t.Id == Slot(role).GameObjectId);
        return FakeBattleCharas.Amount(target.Effects!.Single(e => e.Type == 0x03));
    }

    [Test]
    public void TheBarDropsWithTheDamageNumberNotAtTheResolve()
    {
        world.Hp.Begin(Data(100000f));
        kefka.Cast(UmadActions.Forsaken);
        Step(JustAfterTheRelease);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
        Step(AfterItLands - JustAfterTheRelease);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo((226600f - 100000f) / 226600f).Within(1e-4f));
    }

    [Test]
    public void TheDamageNumberIsTheHpTaken()
    {
        world.Hp.Begin(Data(100000f));
        kefka.Cast(UmadActions.Forsaken);
        Step(JustAfterTheRelease);
        Assert.That(DamageShownOn(PartyRole.MeleeDpsA), Is.EqualTo(100000u));
        Assert.That(DamageShownOn(PartyRole.MainTank), Is.EqualTo(61000u));
    }

    [Test]
    public void AHumanOutOfHpDiesWhenItLands()
    {
        world.Hp.Begin(Data(250000f));
        kefka.Cast(UmadActions.Forsaken);
        Step(JustAfterTheRelease);
        Assert.That(world.Party.Player!.IsAlive());
        Step(AfterItLands - JustAfterTheRelease);
        Assert.That(world.Party.Player!.IsAlive(), Is.False);
        Assert.That(deaths.Single().Cause, Does.StartWith("Forsaken (ran out of HP: took 230k"));
        Assert.That(Slot(PartyRole.MeleeDpsA).IsAlive(), "a bot keeps a sliver");
    }

    [Test]
    public void AnotherKillRuleExplainsTheDeathFirst()
    {
        world.Hp.Begin(Data(250000f));
        world.Party.Player!.AddStatus(UmadConstants.StatusId.MagicVulnerabilityUp, 30f);
        kefka.Cast(UmadActions.Forsaken);
        Step(AfterItLands);
        Assert.That(deaths.Single().Cause, Does.Contain("had vuln up debuff").And.Not.Contain("ran out of HP"));
    }

    [Test]
    public void AHitStillInFlightCountsAgainstTheNext()
    {
        var ranOut = new List<SimCharacter>();
        world.Hp.RanOut += (who, _) => ranOut.Add(who);
        var exdeath = world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.Exdeath))!;
        world.Hp.Begin(Data(120000f));
        kefka.Cast(UmadActions.Forsaken);
        Step(0.5f);
        exdeath.Cast(UmadActions.Forsaken);
        Step(7.0f);
        Assert.That(ranOut, Does.Contain(Slot(PartyRole.MeleeDpsA)), "240k reserved on 226.6k before either landed");
    }

    [Test]
    public void AnActionOutsideTheTableKeepsTheBarAndItsStockNumber()
    {
        world.Hp.Begin(Data(100000f, actionId: UmadConstants.ActionId.LightOfJudgment));
        kefka.Cast(UmadActions.Forsaken);
        Step(AfterItLands);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
        Assert.That(DamageShownOn(PartyRole.MeleeDpsA), Is.EqualTo(Severity.Normal.FlyTextAmount(kills: false)));
    }

    [Test]
    public void AFightWithoutADamageModelKeepsTheBarStill()
    {
        kefka.Cast(UmadActions.Forsaken);
        Step(AfterItLands);
        Assert.That(HpShare(PartyRole.MeleeDpsA), Is.EqualTo(1f));
        Assert.That(world.Party.Player!.IsAlive());
    }

    [Test]
    public void AMitigationCheckLeavesTrackedBarriersToTheDamage()
    {
        fake.UserActions.Enabled = true;
        world.Hp.Begin(Data(100000f));
        world.Party.Player!.AddStatus(1362, 30f);
        var checkedHit = new EnemyAction(ForsakenId) { Effects = [Damage(Severity.Normal.MinMit(0.05f))], Timing = new() { DamageDelay = 1.91f } };
        kefka.Cast(checkedHit);
        Step(JustAfterTheRelease);
        Assert.That(world.Party.Player!.IsAlive());
        Assert.That(DamageShownOn(PartyRole.ShieldHealer), Is.EqualTo((uint)MathF.Round(92000f - 20520f)), "Divine Veil still absorbs 10% of max HP");
    }
}
