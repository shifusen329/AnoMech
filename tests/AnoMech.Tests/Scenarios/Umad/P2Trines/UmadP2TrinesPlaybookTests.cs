using AnoMech.Scenarios.Umad.P2Trines;

namespace AnoMech.Tests;

public class UmadP2TrinesPlaybookTests
{
    private static IEnumerable<(UmadP2TrinesState State, string Context)> States()
    {
        var i = 0;
        foreach (var first in UmadP2TrinesState.FirstSets)
        {
            i++;
            var second = Enumerable.Range(1, 6).First(t => !first.Contains(t));
            yield return (UmadP2TrinesState.FromNetworkReplay(first, second, i % 3 != 0, i % 2 == 0)!, $"first {UmadP2TrinesState.Describe(first)}");
        }
    }

    [Test]
    public void EveryMoveIsExplainedForEveryRole()
    {
        foreach (var (state, context) in States())
            foreach (var cue in new[]
            {
                UmadP2TrinesPlaybook.Opening, UmadP2TrinesPlaybook.WingsDodge(state), UmadP2TrinesPlaybook.Spots(state),
                UmadP2TrinesPlaybook.BusterSpots, UmadP2TrinesPlaybook.UltimateEmbrace,
            })
                PlaybookAssert.ExplainsEveryRole(cue, context);
    }
}
