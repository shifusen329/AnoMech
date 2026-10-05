using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class MitDamageTests
{
    private static readonly MitProfile Profile = new(325200f, 226600f, 205200f, 0.61f, 1f, 0.92f, 0.69f, 3f);
    private static readonly MitSource Kefka = new("Kefka");

    private static MitLayer Layer(ushort id, string name) => new(id, name, Mitigation.ByStatusId[id]);

    private static MitHitResult Hit(MitHitDef hit, MitClass victim = MitClass.Physical, MitLayer[]? on = null, MitLayer[]? source = null,
        MitShield[]? shields = null, bool botTank = false)
        => MitDamage.Apply(hit, victim, Profile, on ?? [], source ?? [], shields ?? [], botTank);

    private static readonly MitHitDef LightOfJudgment = new("loj", "Light of Judgment", 293000f, Kefka);
    private static readonly MitHitDef WaveCannon = new("wave", "Wave Cannon", 308000f, null);
    private static readonly MitHitDef Hyperdrive = new("hd", "Hyperdrive", 568000f, Kefka, TankBuster: true, Fresh: true);

    [Test]
    public void TheClassTakesItsShareOfTheRawHit()
    {
        Assert.That(Hit(LightOfJudgment).HpLoss, Is.EqualTo(293000f).Within(1f));
        Assert.That(Hit(LightOfJudgment, MitClass.Magical).HpLoss, Is.EqualTo(293000f * 0.92f).Within(1f));
        Assert.That(Hit(LightOfJudgment, MitClass.Tank).HpLoss, Is.EqualTo(293000f * 0.61f).Within(1f));
    }

    [Test]
    public void TwoTenPercentsLeaveEightyOne()
    {
        var r = Hit(LightOfJudgment, on: [Layer(2618, "Kerachole"), Layer(3003, "Holos")]);
        Assert.That(r.HpLoss / r.Raw, Is.EqualTo(0.81f).Within(1e-4f));
        Assert.That(r.Applied, Is.EqualTo(new[] { "Kerachole 10%", "Holos 10%" }));
    }

    [Test]
    public void TheSameStatusTwiceCountsOnce()
    {
        var r = Hit(LightOfJudgment, on: [Layer(2618, "Kerachole"), Layer(2618, "Kerachole")]);
        Assert.That(r.HpLoss / r.Raw, Is.EqualTo(0.9f).Within(1e-4f));
    }

    [Test]
    public void BossDebuffsCutOnlyHitsFromThatBoss()
    {
        var reprisal = new[] { Layer(1193, "Reprisal") };
        Assert.That(Hit(LightOfJudgment, source: reprisal).HpLoss, Is.EqualTo(293000f * 0.9f).Within(1f));
        Assert.That(Hit(WaveCannon, source: reprisal).HpLoss, Is.EqualTo(308000f).Within(1f), "the Graven Image carries no debuffs");
    }

    [Test]
    public void AMagicOnlyReductionSkipsAPhysicalHit()
    {
        var physical = LightOfJudgment with { Kind = DamageKind.Physical };
        Assert.That(Hit(physical, on: [Layer(2707, "Magick Barrier")]).HpLoss, Is.EqualTo(293000f).Within(1f));
        Assert.That(Hit(LightOfJudgment, on: [Layer(2707, "Magick Barrier")]).HpLoss, Is.EqualTo(293000f * 0.9f).Within(1f));
    }

    [Test]
    public void ShieldsAbsorbBeforeHpInTheOrderTheyWentUp()
    {
        var first = new MitShield(3903, "Divine Caress", 30000f);
        var second = new MitShield(297, "Galvanize", 30000f);
        var r = Hit(WaveCannon with { Raw = 40000f }, shields: [first, second]);
        Assert.That((r.Absorbed, r.HpLoss), Is.EqualTo((40000f, 0f)));
        Assert.That((first.Amount, second.Amount), Is.EqualTo((0f, 20000f)));
        Assert.That(first.Spent && !second.Spent);
    }

    [Test]
    public void APerHitBarrierAbsorbsUpToItsSizeFromEachHit()
    {
        var panhaima = new MitShield(2613, "Panhaima", 19111f, hits: 5);
        var r = Hit(WaveCannon with { Raw = 30000f }, shields: [panhaima]);
        Assert.That(r.Absorbed, Is.EqualTo(19111f).Within(1f));
        Assert.That(panhaima.HitsLeft, Is.EqualTo(4));
        for (var i = 0; i < 4; i++) Hit(WaveCannon with { Raw = 30000f }, shields: [panhaima]);
        Assert.That(panhaima.Spent);
    }

    [Test]
    public void ABotTankBusterAssumesItsOwnCooldownsAndAHumansDoesNot()
    {
        Assert.That(Hit(Hyperdrive, MitClass.Tank, botTank: true).HpLoss, Is.EqualTo(568000f * 0.31f).Within(1f));
        Assert.That(Hit(Hyperdrive, MitClass.Tank).HpLoss, Is.EqualTo(568000f).Within(1f), "a buster ignores the class share");
    }

    [Test]
    public void AnInvulnTakesNothing()
    {
        var r = Hit(Hyperdrive, MitClass.Tank, on: [Layer(409, "Holmgang")]);
        Assert.That(r.HpLoss, Is.EqualTo(0f));
        Assert.That(r.Applied.Single(), Is.EqualTo("Holmgang (invulnerable)"));
    }

    [Test]
    public void AnUnknownHitDoesNoDamage()
    {
        Assert.That(Hit(LightOfJudgment with { Raw = null }).HpLoss, Is.EqualTo(0f));
    }

    [Test]
    public void ExplainSaysWhatGotThroughAndWhatWasMissing()
    {
        var r = Hit(LightOfJudgment, MitClass.Magical, on: [Layer(2618, "Kerachole")], source: [Layer(1193, "Reprisal")],
            shields: [new MitShield(3903, "Divine Caress", 25000f)]);
        Assert.That(MitDamage.Explain(r, ["your Kerachole", "OT's Reprisal"]),
            Is.EqualTo("took 193k after mitigation (raw 270k; Kerachole 10%, Reprisal 10%; shields absorbed 25k). Missing from the plan: your Kerachole, OT's Reprisal."));
    }
}
