using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using AnoMech.Scenarios.Umad;

namespace AnoMech.Tests;

public class TetherEndTests
{
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
        for (var i = 0; i < 8; i++)
            world.Party.Get(i)?.SetPosition(new Vector3(6f, 0f, 6f));
    }

    [TearDown]
    public void RemoveGame()
    {
        world.Despawn();
        DalamudServices.Install(nameof(Plugin.GameInstance), null!);
    }

    private SimEnemy Hole(float x, float z) =>
        world.SpawnEnemy(new EnemySpawnConfig(UmadConstants.BNpcBaseId.BlackHole, Placement: new Placement(new Vector3(x, 0f, z), 0f)))!;

    private SimTether Grabby(SimEnemy hole, SimCharacter holder) =>
        world.Tether(hole, End.Passable(holder), UmadConstants.TetherId.GrabbyTether);

    private void Step()
    {
        world.Events.Tick(0.1f);
        world.Tick(0.1f);
    }

    [Test]
    public void AMemberHoldingOneBlackHoleTetherTakesASecondBySteppingIntoItsLine()
    {
        var holder = world.Party.Get(PartyRole.MainTank)!;
        var taker = world.Party.Get(PartyRole.MeleeDpsA)!;
        holder.SetPosition(Vector3.Zero);
        var west = Grabby(Hole(-17f, 0f), holder);
        var north = Grabby(Hole(0f, -17f), holder);

        taker.SetPosition(new Vector3(-5f, 0f, 0f));
        Step();
        Assert.That(west.B, Is.SameAs(taker));

        taker.SetPosition(new Vector3(0f, 0f, -5f));
        Step();
        Assert.That(north.B, Is.SameAs(taker), "holding the west hole's tether doesn't stop them taking the north one");
        Assert.That(west.B, Is.SameAs(taker), "nobody stands between them and the west hole");
    }

    [Test]
    public void AMemberStandingOnTheHolderDoesNotTakeTheTether()
    {
        var holder = world.Party.Get(PartyRole.MainTank)!;
        var stacked = world.Party.Get(PartyRole.MeleeDpsA)!;
        holder.SetPosition(Vector3.Zero);
        stacked.SetPosition(new Vector3(-0.05f, 0f, 0f));
        var west = Grabby(Hole(-17f, 0f), holder);

        Step();
        Assert.That(west.B, Is.SameAs(holder));
    }
}
