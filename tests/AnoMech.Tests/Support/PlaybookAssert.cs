using AnoMech.Core.Game.Ai;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Tests;

// A strat move's recap explanation must name its mechanic and NAUR section, and say something
// for every role.
internal static class PlaybookAssert
{
    public static void ExplainsEveryRole(StratCue cue, string context)
    {
        Assert.That(cue.Mechanic, Is.Not.Empty, context);
        Assert.That(cue.Source, Does.StartWith("NAUR §"), context);
        foreach (var role in UmadP1Roles.All)
            Assert.That(cue.Why(role), Has.Length.GreaterThan(20), $"{cue.Mechanic} for {role}, {context}");
    }
}
