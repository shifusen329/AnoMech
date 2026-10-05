using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class MitGraderTests
{
    private const PartyRole Me = PartyRole.ShieldHealer;

    private static readonly Dictionary<PartyRole, JobId> Sage = new() { [Me] = JobId.Sage, [PartyRole.CasterDps] = JobId.RedMage };

    // Kerachole (15s, 30s recast) twice: for hits at 20 and 60.
    private static IReadOnlyList<ResolvedPress> Plan() => MitPlan.Resolve([
        new(SheetColumn.SGE, MitActionId.Kerachole, 18f, "Hit 1", [20f]),
        new(SheetColumn.SGE, MitActionId.Kerachole, 58f, "Hit 2", [60f]),
        new(SheetColumn.Extras, MitActionId.MagickBarrier, 58f, "Hit 2", [60f], [JobId.RedMage]),
    ], Sage);

    private static IReadOnlyList<MitGrade> Grade(params float[] presses)
        => MitGrader.Grade(Plan(), Me, presses.Select(t => new MitPress(Me, MitActionId.Kerachole, t)).ToList(), 100f, complete: true);

    [Test]
    public void APressInsideTheWindowIsOnTime()
    {
        Assert.That(Grade(18f, 58f).Select(g => g.Kind), Is.EqualTo(new[] { MitGradeKind.OnTime, MitGradeKind.OnTime }));
    }

    [Test]
    public void APressBeforeTheWindowIsEarly()
    {
        // Hit 1's window opens at 20 - 15 + 0.3 = 5.3 at the latest, and its planned 18.
        var grades = MitGrader.Grade(MitPlan.Resolve([new(SheetColumn.SGE, MitActionId.Kerachole, 18f, "Hit", [20f, 32f])], Sage),
            Me, [new(Me, MitActionId.Kerachole, 10f)], 100f, complete: true);
        Assert.That(grades.Single().Kind, Is.EqualTo(MitGradeKind.Early));
    }

    [Test]
    public void ThePressAfterTheCloseBeforeTheHitIsLateAndUsedOnce()
    {
        var grades = Grade(19.95f);
        Assert.That(grades.Select(g => g.Kind), Is.EqualTo(new[] { MitGradeKind.Late, MitGradeKind.Missed }));
    }

    [Test]
    public void NoPressIsMissed()
    {
        Assert.That(Grade().Select(g => g.Kind), Is.EqualTo(new[] { MitGradeKind.Missed, MitGradeKind.Missed }));
    }

    [Test]
    public void TheLatestPressInTheWindowIsJudged()
    {
        var grades = Grade(5f, 18f);
        Assert.That(grades[0].PressedAt, Is.EqualTo(18f));
        Assert.That(grades[0].Kind, Is.EqualTo(MitGradeKind.OnTime));
    }

    [Test]
    public void ARowStillOpenWhenTheRunEndedIsNotReached()
    {
        var grades = MitGrader.Grade(Plan(), Me, [new(Me, MitActionId.Kerachole, 18f)], cutoff: 40f, complete: false);
        Assert.That(grades.Select(g => g.Kind), Is.EqualTo(new[] { MitGradeKind.OnTime, MitGradeKind.NotReached }));
    }

    [Test]
    public void AnAliasCountsForItsButton()
    {
        var plan = MitPlan.Resolve([new(SheetColumn.SGE, MitActionId.EukrasianPrognosisII, 10f, "Hit", [12f])], Sage);
        var grades = MitGrader.Grade(plan, Me, [new(Me, MitActionId.EukrasianPrognosis, 10f)], 100f, complete: true);
        Assert.That(grades.Single().Kind, Is.EqualTo(MitGradeKind.OnTime));
    }

    [Test]
    public void TheSummaryCountsAndExplainsEachMiss()
    {
        var plan = Plan();
        var presses = new List<MitPress> { new(Me, MitActionId.Kerachole, 18f) };
        var lines = MitGrader.Summary(MitGrader.Grade(plan, Me, presses, 100f, complete: true), 100f);
        Assert.That(lines, Is.EqualTo(new[]
        {
            "Mitigation: 1 of 2 planned presses on time (1 missed).",
            "Kerachole for Hit 2: not pressed (window 45.3–59.8s).",
        }));

        var rdm = MitGrader.Summary(MitGrader.Grade(plan, PartyRole.CasterDps, [], 100f, complete: true), 100f);
        Assert.That(rdm[1], Does.StartWith("Magick Barrier (Extras) for Hit 2: not pressed"));
    }

    [Test]
    public void TheSummaryNotesWhatTheRunDidntReach()
    {
        var lines = MitGrader.Summary(MitGrader.Grade(Plan(), Me, [new(Me, MitActionId.Kerachole, 18f)], 40f, complete: false), 40f);
        Assert.That(lines.Single(), Is.EqualTo("Mitigation: 1 of 1 planned presses on time. (1 not reached: the run ended at 40.0s.)"));
    }
}
