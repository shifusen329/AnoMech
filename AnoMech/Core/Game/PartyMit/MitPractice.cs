using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using Dalamud.Game.Text;

namespace AnoMech.Core.Game.PartyMit;

public enum MitRunEnd { Completed, Death, Reset }

// The running mitigation practice. Bots press the plan resolved for the real comp; hits move
// simulated HP, warning instead of killing; the local player's planned presses are graded when
// the run ends. Host-only, since it starts from scenario.Run, which a peer never runs. Keeps its
// own schedule rather than world.Events, so a scenario's end isn't held back by it.
public sealed class MitPractice
{
    // A planned press counts as missing from a hit landing this close to one of its covers.
    private const float MissingWithin = 1f;

    private readonly SimWorld world;
    private MitPlanData? data;
    private IReadOnlyList<ResolvedPress> plan = [];
    private IReadOnlyDictionary<MitSource, Func<SimCharacter?>> sources = new Dictionary<MitSource, Func<SimCharacter?>>();
    private Func<float> clock;
    private readonly List<ResolvedPress> pending = [];
    private readonly Dictionary<(PartyRole Slot, uint Action), float> lastPress = [];
    private readonly List<MitPress> presses = [];
    private readonly Dictionary<SimCharacter, MitHpPool> pools = [];
    private readonly Dictionary<SimCharacter, ShieldLedger> ledgers = [];
    private readonly Dictionary<SimCharacter, float> warnedAt = [];
    private bool hpMode;
    private bool grading;
    private bool finished;

    internal MitPractice(SimWorld world)
    {
        this.world = world;
        clock = () => world.Events.Elapsed;
    }

    public bool Enabled { get; set; }
    public bool IsActive => data != null && !finished;
    // Hits move HP bars and show their own damage numbers.
    public bool MovesHp => IsActive && hpMode;

    // At the scenario's start. `sources` resolves each MitSource to its live actor (the boss the
    // party's debuffs go on); `clock` is the scenario's, when it isn't world.Events.
    public void Begin(MitPlanData planData, IReadOnlyDictionary<MitSource, Func<SimCharacter?>>? actors = null, Func<float>? scenarioClock = null)
    {
        Clear();
        if (!Enabled || !world.Party.AllMembers().OfType<SimPartyNpc>().Any()) return;
        data = planData;
        sources = actors ?? new Dictionary<MitSource, Func<SimCharacter?>>();
        clock = scenarioClock ?? (() => world.Events.Elapsed);
        var jobs = world.Party.AllMembers().OfType<ISimPartyMember>().ToDictionary(m => m.Role, m => (JobId)m.ClassJob);
        plan = MitPlan.Resolve(planData.Entries, jobs);
        pending.AddRange(plan.Where(p => world.Party.Get(p.Slot) switch
                             {
                                 SimPartyNpc => planData.Entries[p.Entry].ActionId != MitPlanEntry.TankLb3,
                                 SimPlayer => p.IsPreStart,
                                 _ => false,
                             })
                             .OrderBy(p => p.At));
        hpMode = planData.Hits.Any(h => h.Raw != null);
        grading = Natives.UserActions.Enabled;
        if (hpMode)
            foreach (var member in world.Party.AllMembers())
                if (member is ISimPartyMember slot)
                {
                    var pool = new MitHpPool(planData.Profile.MaxHp(MitProfile.ClassOf((JobId)slot.ClassJob)), planData.Profile.TopUpGap,
                        planData.Profile.HealPerSecond);
                    pools[member] = pool;
                    Write(member, pool);
                }
        DiagnosticLog.Info($"[MitPractice] Begin: {plan.Count} planned presses for [{string.Join(", ", jobs.OrderBy(j => j.Key).Select(j => $"{j.Key}={j.Value}"))}], "
                           + $"{pending.Count} by bots, HP {(hpMode ? "on" : "off")}, grading {(grading ? "on" : "off")}.");
    }

    internal void Tick()
    {
        if (!IsActive) return;
        var now = clock();
        while (pending.Count > 0 && pending[0].At <= now)
        {
            var press = pending[0];
            pending.RemoveAt(0);
            Press(press, now);
        }
        if (!hpMode) return;
        foreach (var (member, pool) in pools)
        {
            if (!member.IsAlive()) continue;
            if (pool.TopUp(now)) Write(member, pool);
            var ledger = Ledger(member);
            ledger.Sync(member, pool.Max);
            member.ShieldOverride = ledger.Remaining / pool.Max;
        }
    }

    private void Press(ResolvedPress p, float now)
    {
        // The player's press from before the scenario began: there was no chance to make it.
        if (world.Party.Get(p.Slot) is SimPlayer player && p.IsPreStart)
        {
            JobActions.ClearStatuses(player, p.Action.ActionId);
            JobActions.ApplyEffects(player, p.Action.ActionId, (ulong)player.GameObjectId, Random.Shared);
            presses.Add(new MitPress(p.Slot, p.Action.ActionId, p.At));
            return;
        }
        if (world.Party.Get(p.Slot) is not SimPartyNpc bot || !bot.IsAlive()) return;
        var key = (p.Slot, p.Action.ActionId);
        if (p.Action.Recast > 0f && lastPress.TryGetValue(key, out var last) && now - last < p.Action.Recast - 0.01f)
        {
            DiagnosticLog.Warn($"[MitPractice] {p.Slot} skips {p.Action.Name} at {now:F2}s: {now - last:F1}s into its {p.Action.Recast:F0}s recast ({p.Label}).");
            return;
        }
        if (p.Action.Requires != 0 && !bot.HasStatus(p.Action.Requires))
        {
            DiagnosticLog.Warn($"[MitPractice] {p.Slot} skips {p.Action.Name} at {now:F2}s: needs {StatusLookup.Name(p.Action.Requires)} ({p.Label}).");
            return;
        }
        bot.UseAction(p.Action.ActionId, p.Action.Kind == MitKind.Debuff ? DebuffTarget(p) : null);
        var pressedAt = p.IsPreStart ? p.At : now;
        lastPress[key] = pressedAt;
        presses.Add(new MitPress(p.Slot, p.Action.ActionId, pressedAt));
    }

    // The local player's press, from UserActions.
    internal void RecordPress(PartyRole slot, uint actionId)
    {
        if (!IsActive || MitCatalogue.Find(actionId) == null) return;
        presses.Add(new MitPress(slot, actionId, clock()));
    }

    public void Hit(SimCharacter? victim, string key)
    {
        if (victim != null) Hit([victim], key);
    }

    public void HitParty(string key) => Hit(world.Party.AllMembers().ToList(), key);

    public void Hit(IEnumerable<SimCharacter> victims, string key)
    {
        if (!IsActive || !hpMode || data!.Hits.FirstOrDefault(h => h.Key == key) is not { Raw: not null } hit) return;
        var now = clock();
        foreach (var victim in victims) Land(victim, hit, now);
    }

    private void Land(SimCharacter victim, MitHitDef hit, float now)
    {
        if (victim is not ISimPartyMember member || !victim.IsAlive() || !pools.TryGetValue(victim, out var pool)) return;
        var ledger = Ledger(victim);
        ledger.Sync(victim, pool.Max);
        var source = hit.Source is { } s ? Layers(Source(s)) : [];
        var result = MitDamage.Apply(hit, MitProfile.ClassOf((JobId)member.ClassJob), data!.Profile, Layers(victim), source,
            ledger.Shields, assumeTankCooldowns: world.Party.IsBotDriven(victim));
        ledger.Commit(victim);
        var hp = pool.Land(now, result.HpLoss, hit.Fresh);
        Write(victim, pool);
        victim.ShieldOverride = ledger.Remaining / pool.Max;
        if (result.HpLoss >= 1f && victim.Proxy is { Exists: true } chara) chara.ShowFlyText((uint)MathF.Round(result.HpLoss), hit.Name);
        if (hp > 0f) return;
        if (warnedAt.TryGetValue(victim, out var warned) && now - warned <= data.Profile.TopUpGap) return;
        warnedAt[victim] = now;
        world.Announce($"{hit.Name} would have killed {Who(member)}: {MitDamage.Explain(result, Missing(victim, now))}");
    }

    // Planned presses meant for a hit around now that aren't in effect on the victim.
    private IReadOnlyList<string> Missing(SimCharacter victim, float now)
    {
        var missing = new List<string>();
        foreach (var p in plan)
        {
            if (!p.Covers.Any(c => MathF.Abs(c - now) <= MissingWithin) || InEffect(p, victim, now)) continue;
            missing.Add($"{Owner(p.Slot)} {p.Action.Name}");
        }
        return missing.Distinct().ToList();
    }

    private bool InEffect(ResolvedPress p, SimCharacter victim, float now)
    {
        var granted = JobActions.GrantsOf(p.Action.ActionId).Select(g => g.StatusId).Where(Mitigation.ByStatusId.ContainsKey).ToList();
        return p.Action.Kind switch
        {
            MitKind.None => true,
            MitKind.Debuff => DebuffTarget(p) is { } boss && granted.Any(boss.HasStatus),
            MitKind.Shield => presses.Any(x => x.Slot == p.Slot && MitCatalogue.Find(x.ActionId)?.ActionId == p.Action.ActionId
                                               && x.At <= now && now - x.At <= p.Action.Duration),
            _ => granted.Any(victim.HasStatus),
        };
    }

    // When the run ends, the local player's grading in chat.
    internal void Finish(MitRunEnd end)
    {
        if (!IsActive) return;
        finished = true;
        DiagnosticLog.Info($"[MitPractice] Finish ({end}) at {clock():F2}s: {presses.Count} presses recorded.");
        if (world.Party.Player is not { } player) return;
        foreach (var line in Summary(player, clock()))
            Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = $"[AnoMech] {line}" });
    }

    private IReadOnlyList<string> Summary(SimPlayer player, float now)
    {
        if (!grading) return ["Mitigation: your presses weren't graded. Turn on \"Resolve your own actions\" to grade them."];
        if (!plan.Any(p => p.Slot == player.Role && !p.IsPreStart))
            return [$"Mitigation: no planned presses for your {JobName((JobId)player.ClassJob)} in {player.Role.ShortLabel()}."];
        return MitGrader.Summary(MitGrader.Grade(plan, player.Role, presses, now), now);
    }

    internal void Clear()
    {
        data = null;
        plan = [];
        sources = new Dictionary<MitSource, Func<SimCharacter?>>();
        clock = () => world.Events.Elapsed;
        pending.Clear();
        lastPress.Clear();
        presses.Clear();
        pools.Clear();
        ledgers.Clear();
        warnedAt.Clear();
        hpMode = false;
        grading = false;
        finished = false;
    }

    private SimCharacter? Source(MitSource source) => sources.TryGetValue(source, out var actor) ? actor() : null;

    private SimCharacter? DebuffTarget(ResolvedPress p)
        => (data!.Entries[p.Entry].On ?? data.DebuffTarget) is { } target ? Source(target) : sources.Values.FirstOrDefault()?.Invoke();

    private ShieldLedger Ledger(SimCharacter member)
    {
        if (!ledgers.TryGetValue(member, out var ledger)) ledgers[member] = ledger = new ShieldLedger();
        return ledger;
    }

    private static IReadOnlyList<MitLayer> Layers(SimCharacter? character)
        => character?.ActiveStatuses
                    .Where(s => Mitigation.ByStatusId.ContainsKey(s.StatusId))
                    .Select(s => new MitLayer(s.StatusId, StatusLookup.Name(s.StatusId), Mitigation.ByStatusId[s.StatusId]))
                    .ToList()
           ?? [];

    private static void Write(SimCharacter member, MitHpPool pool)
        => member.WriteHp((uint)MathF.Max(1f, MathF.Round(pool.Hp)), (uint)MathF.Round(pool.Max));

    private string Owner(PartyRole slot) => world.Party.Player is { } player && player.Role == slot ? "your" : $"{slot.ShortLabel()}'s";

    private static string Who(ISimPartyMember member) => member is SimPlayer ? $"you ({member.Role.ShortLabel()})" : member.Role.ShortLabel();

    private static string JobName(JobId job) => Regex.Replace(job.ToString(), "(?<!^)([A-Z])", " $1");
}
