using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3BowelsOfAgony;
using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Tests;

public class UmadP3BowelsOfAgonyStateTests
{
    private static (UmadP3BowelsOfAgonyState State, PartyRole Me) Roll(int seed, UmadP3BowelsOfAgonyStateOverrides? overrides = null)
    {
        var world = new SimWorld(new EventScheduler());
        world.Reseed(seed);
        return (new UmadP3BowelsOfAgonyState(world.Rng, world.Party, overrides ?? new UmadP3BowelsOfAgonyStateOverrides()), world.Party.PlayerRole);
    }

    [Test]
    public void EachElementGoesToOneSupportAndOneDps()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var (state, _) = Roll(seed);
            foreach (var holders in new[] { state.WaterHolders, state.FireHolders })
            {
                Assert.That(holders, Has.Count.EqualTo(2), $"seed {seed}");
                Assert.That(holders.Count(UmadP3BowelsOfAgonyState.IsSupport), Is.EqualTo(1), $"seed {seed}");
            }
            Assert.That(state.WaterHolders.Intersect(state.FireHolders), Is.Empty, $"seed {seed}");
            Assert.That(state.Unmarked, Has.Count.EqualTo(4), $"seed {seed}");
        }
    }

    [Test]
    public void WindSitsAQuarterTurnFromWaterAndFire()
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            var (state, _) = Roll(seed);
            Assert.That(state.WindSpot % 2, Is.EqualTo(1), $"seed {seed}");
            Assert.That((state.WaterSpot - state.WindSpot + 8) % 8, Is.AnyOf(2, 6), $"seed {seed}");
            Assert.That(state.SideOf(state.FireSpot), Is.EqualTo(-state.SideOf(state.WaterSpot)), $"seed {seed}");
        }
    }

    [Test]
    public void AForcedDebuffAndWindAreHonoured()
    {
        var overrides = new UmadP3BowelsOfAgonyStateOverrides();
        overrides.Debuff.Mine = ElementDebuff.Entropy;
        overrides.Wind.Mine = Wind.Headwind;
        for (var seed = 1; seed <= 50; seed++)
        {
            var (state, me) = Roll(seed, overrides);
            Assert.That(state.FireHolders, Does.Contain(me), $"seed {seed}");
            Assert.That(state.Winds[me], Is.EqualTo(Wind.Headwind), $"seed {seed}");
        }
    }

    [Test]
    public void AReplayTheFightCannotRollIsRefused()
    {
        Assert.That(UmadP3BowelsOfAgonyState.FromNetworkReplay(3, 5, Element.Water,
            [PartyRole.MainTank, PartyRole.OffTank], [PartyRole.MeleeDpsA, PartyRole.CasterDps], [], true, ThunderIIIAssignment.ShareMtFirst), Is.Null);
        Assert.That(UmadP3BowelsOfAgonyState.FromNetworkReplay(3, 4, Element.Water,
            [PartyRole.MainTank, PartyRole.MeleeDpsA], [PartyRole.OffTank, PartyRole.CasterDps], [], true, ThunderIIIAssignment.ShareMtFirst), Is.Null);
        Assert.That(UmadP3BowelsOfAgonyState.FromNetworkReplay(3, 5, Element.Water,
            [PartyRole.MainTank, PartyRole.MeleeDpsA], [PartyRole.OffTank, PartyRole.CasterDps], [], true, ThunderIIIAssignment.ShareMtFirst), Is.Not.Null);
    }
}
