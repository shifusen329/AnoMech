using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;

namespace AnoMech.Core.Game.PartyMit;

public enum MitGradeKind { OnTime, Early, Late, Missed, NotReached }

public readonly record struct MitPress(PartyRole Slot, uint ActionId, float At);

public sealed record MitGrade(ResolvedPress Entry, MitGradeKind Kind, float? PressedAt);

public static class MitGrader
{
    private const float Epsilon = 0.01f;

    // kefkasim's rules, button by button: the latest press after that button's previous row closed
    // and no later than this row's close is the one judged (early if before the row opened). With
    // none, the first unclaimed press after the close but before the last cover and the next row
    // opening is late. A row still open when an unfinished run ended was not reached.
    public static IReadOnlyList<MitGrade> Grade(IReadOnlyList<ResolvedPress> plan, PartyRole slot, IReadOnlyList<MitPress> presses,
        float cutoff, bool complete)
    {
        var grades = new List<MitGrade>();
        var claimed = new HashSet<int>();
        foreach (var button in plan.Where(p => p.Slot == slot && !p.IsPreStart).GroupBy(p => p.Action.ActionId))
        {
            var rows = button.OrderBy(p => p.Close).ToList();
            var mine = presses.Select((p, i) => (Press: p, Index: i))
                              .Where(x => x.Press.Slot == slot && MitCatalogue.Find(x.Press.ActionId)?.ActionId == button.Key)
                              .OrderBy(x => x.Press.At)
                              .ToList();
            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                var after = r > 0 ? rows[r - 1].Close : float.NegativeInfinity;
                var counting = mine.LastOrDefault(x => !claimed.Contains(x.Index) && x.Press.At > after + Epsilon && x.Press.At <= row.Close + Epsilon);
                if (counting.Press != default)
                {
                    claimed.Add(counting.Index);
                    grades.Add(new(row, counting.Press.At < row.Open - Epsilon ? MitGradeKind.Early : MitGradeKind.OnTime, counting.Press.At));
                    continue;
                }
                if (!complete && row.Close > cutoff)
                {
                    grades.Add(new(row, MitGradeKind.NotReached, null));
                    continue;
                }
                var lastCover = row.Covers.Length > 0 ? row.Covers[^1] : row.Close + 3f;
                var nextOpen = r + 1 < rows.Count ? rows[r + 1].Open : float.PositiveInfinity;
                var late = mine.FirstOrDefault(x => !claimed.Contains(x.Index) && x.Press.At > row.Close + Epsilon
                                                    && x.Press.At < lastCover && x.Press.At < nextOpen);
                if (late.Press != default)
                {
                    claimed.Add(late.Index);
                    grades.Add(new(row, MitGradeKind.Late, late.Press.At));
                    continue;
                }
                grades.Add(new(row, MitGradeKind.Missed, null));
            }
        }
        return grades.OrderBy(g => g.Entry.Close).ToList();
    }

    // The chat lines for one player: a headline, then one line per press that wasn't on time.
    public static IReadOnlyList<string> Summary(IReadOnlyList<MitGrade> grades, float endedAt)
    {
        var judged = grades.Where(g => g.Kind != MitGradeKind.NotReached).ToList();
        var counts = new List<string>();
        foreach (var (kind, word) in new[] { (MitGradeKind.Early, "early"), (MitGradeKind.Late, "late"), (MitGradeKind.Missed, "missed") })
            if (judged.Count(g => g.Kind == kind) is var n and > 0) counts.Add($"{n} {word}");
        var headline = $"Mitigation: {judged.Count(g => g.Kind == MitGradeKind.OnTime)} of {judged.Count} planned presses on time"
                       + (counts.Count > 0 ? $" ({string.Join(", ", counts)})" : "") + ".";
        var notReached = grades.Count - judged.Count;
        if (notReached > 0) headline += $" ({notReached} not reached: the run ended at {endedAt:F1}s.)";

        var lines = new List<string> { headline };
        foreach (var g in judged.Where(g => g.Kind != MitGradeKind.OnTime))
            lines.Add(Line(g));
        return lines;
    }

    private static string Line(MitGrade g)
    {
        var e = g.Entry;
        var what = $"{e.Action.Name}{(e.IsExtra ? " (Extras)" : "")} for {e.Label}";
        var lastHit = e.Covers.Length > 0 ? e.Covers[^1] : e.Close;
        return g.Kind switch
        {
            MitGradeKind.Early => $"{what}: pressed too early, at {g.PressedAt:F1}s (window {e.Open:F1}–{e.Close:F1}s): it wears off before the hit at {lastHit:F1}s.",
            MitGradeKind.Late => $"{what}: pressed late, at {g.PressedAt:F1}s (window closed at {e.Close:F1}s).",
            _ => $"{what}: not pressed (window {e.Open:F1}–{e.Close:F1}s).",
        };
    }
}
