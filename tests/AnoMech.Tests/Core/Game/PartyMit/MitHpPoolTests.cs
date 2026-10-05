using AnoMech.Core.Game.PartyMit;

namespace AnoMech.Tests;

public class MitHpPoolTests
{
    [Test]
    public void HitsCloserThanTheGapPileUp()
    {
        var pool = new MitHpPool(100f, 3f);
        pool.Land(10f, 30f);
        Assert.That(pool.Land(12f, 30f), Is.EqualTo(40f));
        Assert.That(pool.Land(12f, 30f), Is.EqualTo(10f), "simultaneous hits don't top up between");
    }

    [Test]
    public void AHitAfterTheGapLandsOnFullHp()
    {
        var pool = new MitHpPool(100f, 3f);
        pool.Land(10f, 70f);
        Assert.That(pool.Land(13.5f, 70f), Is.EqualTo(30f));
    }

    [Test]
    public void AFreshHitLandsOnFullHpRegardless()
    {
        var pool = new MitHpPool(100f, 3f);
        pool.Land(10f, 70f);
        Assert.That(pool.Land(11f, 20f, fresh: true), Is.EqualTo(80f));
    }

    [Test]
    public void TopUpRefillsOnlyOnceTheGapHasPassed()
    {
        var pool = new MitHpPool(100f, 3f);
        pool.Land(10f, 70f);
        Assert.That(pool.TopUp(12f), Is.False);
        Assert.That(pool.TopUp(13.1f), Is.True);
        Assert.That(pool.Hp, Is.EqualTo(100f));
        Assert.That(pool.TopUp(14f), Is.False, "already full");
    }

    [Test]
    public void HpCanGoBelowZero()
    {
        Assert.That(new MitHpPool(100f, 3f).Land(0f, 150f), Is.EqualTo(-50f));
    }
}
