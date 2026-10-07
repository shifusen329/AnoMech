using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.UserActions;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Tests;

// What every scenario's mitigation plan must hold, for the fixed bots and for each job the player
// can bring to each slot.
internal static class MitigationPlanAssert
{
    private static readonly Dictionary<PartyRole, JobId> Bots = new()
    {
        [PartyRole.MainTank] = Warrior, [PartyRole.OffTank] = Paladin,
        [PartyRole.RegenHealer] = WhiteMage, [PartyRole.ShieldHealer] = Scholar,
        [PartyRole.MeleeDpsA] = Dragoon, [PartyRole.MeleeDpsB] = Monk,
        [PartyRole.PhysRangedDps] = Bard, [PartyRole.CasterDps] = BlackMage,
    };

    private static readonly Dictionary<PartyRole, JobId[]> PlayerJobs = new()
    {
        [PartyRole.MainTank] = [Warrior, Paladin, DarkKnight, Gunbreaker],
        [PartyRole.OffTank] = [Paladin, Warrior, DarkKnight, Gunbreaker],
        [PartyRole.RegenHealer] = [WhiteMage, Astrologian],
        [PartyRole.ShieldHealer] = [Scholar, Sage],
        [PartyRole.MeleeDpsA] = [Dragoon, Monk, Ninja, Samurai, Reaper, Viper],
        [PartyRole.MeleeDpsB] = [Monk, Dragoon, Ninja, Samurai, Reaper, Viper],
        [PartyRole.PhysRangedDps] = [Bard, Machinist, Dancer],
        [PartyRole.CasterDps] = [BlackMage, Summoner, RedMage, Pictomancer],
    };

    public static IEnumerable<(string Name, Dictionary<PartyRole, JobId> Jobs)> Comps()
    {
        yield return ("the bots", Bots);
        foreach (var (slot, jobs) in PlayerJobs)
            foreach (var job in jobs)
                yield return ($"{job} in {slot}", new Dictionary<PartyRole, JobId>(Bots) { [slot] = job });
    }

    // endsAt is the scenario's end, when a press with nothing to cover there can come after its last hit.
    public static void IsSound(MitPlanData data, string scenario, float? endsAt = null)
    {
        var scheduled = data.Schedule.Select(s => s.At).ToList();
        var lastHit = scheduled.Max();
        foreach (var hit in data.Schedule.SelectMany(s => s.Keys))
            Assert.That(data.Hits.Any(h => h.Key == hit), $"{scenario}: scheduled hit '{hit}' isn't in the hit table");

        foreach (var e in data.Entries)
        {
            var context = $"{scenario}: {e.Column} {e.ActionId} at {e.At} ({e.Label})";
            if (e.ActionId is not (MitPlanEntry.PartyMit or MitPlanEntry.TankLb3))
                Assert.That(MitCatalogue.Find(e.ActionId), Is.Not.Null, context);
            Assert.That(e.At, Is.LessThanOrEqualTo(endsAt ?? lastHit), context);
            if (!e.IsPreStart) Assert.That(e.At, Is.GreaterThanOrEqualTo(0f), context);
            foreach (var cover in e.Covers)
            {
                Assert.That(scheduled.Any(t => MathF.Abs(t - cover) < 0.05f), $"{context}: cover {cover} isn't a scheduled hit");
                Assert.That(cover, Is.GreaterThan(e.At), $"{context}: covers a hit before it's pressed");
            }
            if (e.IsExtra) Assert.That(e.Jobs, Is.Not.Null.And.Not.Empty, $"{context}: an Extras row names its jobs");
        }

        foreach (var (name, jobs) in Comps())
        {
            var plan = MitPlan.Resolve(data.Entries, jobs);
            Assert.That(MitPlan.Validate(plan), Is.Empty, $"{scenario}, {name}");
            foreach (var extra in plan.Where(p => p.IsExtra))
                Assert.That(extra.Job, Is.AnyOf(Machinist, RedMage, Pictomancer), $"{scenario}, {name}: {extra.Action.Name}");
        }
    }
}
