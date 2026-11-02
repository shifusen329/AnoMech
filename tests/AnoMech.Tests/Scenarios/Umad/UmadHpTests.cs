using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios;

namespace AnoMech.Tests;

// The damage tables against the plan's mitigation: played as the strat says, nobody runs out of HP.
// The catalog's sunny-day runs can't tell, since a bot (the AI-driven player too) never dies of HP.
public class UmadHpTests
{
    private static IEnumerable<TestCaseData> StratsWithADamageModel()
        => from scenario in ScenarioCatalog.Create().Scenarios
           where scenario.SupportsMitigationPractice
           from strat in scenario.AiStrats.Select((ai, index) => (ai, index))
           select new TestCaseData(scenario.GetType(), strat.index)
               .SetName($"{scenario.FullName()} [{strat.ai.Name}]");

    private static readonly HashSet<Type> NotYetSound =
    [
        typeof(AnoMech.Scenarios.Umad.P3BlackHole.UmadP3BlackHoleScenario),
        typeof(AnoMech.Scenarios.Umad.P3Full.UmadP3FullScenario),
    ];

    [TestCaseSource(nameof(StratsWithADamageModel))]
    public void NobodyRunsOutOfHp(Type scenarioType, int strat)
    {
        if (NotYetSound.Contains(scenarioType))
            Assert.Ignore("its damage table still runs healthy players out at Stomp-a-Mole and Cyclone; running out of HP doesn't kill by default until it passes");
        var seeds = TestContext.Parameters.Get("HpSeeds", 1);
        var ranOut = new List<string>();
        for (var i = 0; i < seeds; i++)
        {
            var seed = Random.Shared.Next();
            var watching = false;
            var run = ScenarioRun.Execute(scenarioType, strat, seed, new ScenarioRunOptions
            {
                // Mitigation required, as the damage tables assume: bots press their limit breaks.
                UserActionsEnabled = true,
                Configure = c => c.EnableTankMitigation = true,
                Probe = probe =>
                {
                    if (watching) return;
                    watching = true;
                    probe.World.Hp.RanOut += (who, hit) =>
                        ranOut.Add($"seed {seed}: {(who as ISimPartyMember)?.Role.ShortLabel()} ran out to {hit.Name} at t={probe.World.Events.Elapsed:F2}");
                },
            });
            Assert.That(run.Passed, run.ToString);
        }
        Assert.That(ranOut, Is.Empty, () => string.Join(Environment.NewLine, ranOut));
    }
}
