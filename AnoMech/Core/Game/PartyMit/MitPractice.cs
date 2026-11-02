using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using Dalamud.Game.Text;
using RuleSim.Actions;

namespace AnoMech.Core.Game.PartyMit;

public enum MitRunEnd { Completed, Death, Reset }

// A fight's mitigation plan, resolved for the real comp and pressed on time for every seat no one
// presses for: the bots, and the humans unless they're practising. Practising (Enabled), a human
// presses their own and is graded when the run ends. Host-only, since it starts from scenario.Run,
// which a peer never runs. Keeps its own schedule rather than world.Events, so a scenario's end
// isn't held back by it.
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
    private bool practising;
    private bool finished;

    internal MitPractice(SimWorld world)
    {
        this.world = world;
        clock = () => world.Events.Elapsed;
    }

    public bool Enabled { get; set; }
    public bool IsActive => data != null && !finished;

    // At the scenario's start, from PartyHp.Begin. `sources` resolves each MitSource to its live
    // actor (the boss the party's debuffs go on); `clock` is the scenario's, when it isn't world.Events.
    public void Begin(MitPlanData planData, IReadOnlyDictionary<MitSource, Func<SimCharacter?>>? actors = null, Func<float>? scenarioClock = null)
    {
        Clear();
        if (!world.Party.IsFull) return;
        data = planData;
        sources = actors ?? new Dictionary<MitSource, Func<SimCharacter?>>();
        clock = scenarioClock ?? (() => world.Events.Elapsed);
        practising = Enabled && Natives.UserActions.Enabled;
        var jobs = world.Party.AllMembers().OfType<ISimPartyMember>().ToDictionary(m => m.Role, m => (JobId)m.ClassJob);
        plan = MitPlan.Resolve(planData.Entries, jobs);
        pending.AddRange(plan.Where(p => planData.Entries[p.Entry].ActionId != MitPlanEntry.TankLb3
                                         && world.Party.Get(p.Slot) is { } member && PressedForThem(member, p))
                             .OrderBy(p => p.At));
        DiagnosticLog.Info($"[MitPractice] Begin: {plan.Count} planned presses for [{string.Join(", ", jobs.OrderBy(j => j.Key).Select(j => $"{j.Key}={j.Value}"))}], "
                           + $"{pending.Count} pressed by the sim, practising {(practising ? "on" : "off")}.");
    }

    // A press from before the scenario began had no chance to be made, so the sim makes it.
    private bool PressedForThem(SimCharacter member, ResolvedPress p)
        => p.IsPreStart || member is SimPartyNpc || world.Party.IsBotDriven(member) || !practising;

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
    }

    private void Press(ResolvedPress p, float now)
    {
        if (world.Party.Get(p.Slot) is not { } member || !member.IsAlive()) return;
        var key = (p.Slot, p.Action.ActionId);
        if (p.Action.Recast > 0f && lastPress.TryGetValue(key, out var last) && now - last < p.Action.Recast - 0.01f)
        {
            DiagnosticLog.Warn($"[MitPractice] {p.Slot} skips {p.Action.Name} at {now:F2}s: {now - last:F1}s into its {p.Action.Recast:F0}s recast ({p.Label}).");
            return;
        }
        if (p.Action.Requires != 0 && !member.HasStatus(p.Action.Requires))
        {
            DiagnosticLog.Warn($"[MitPractice] {p.Slot} skips {p.Action.Name} at {now:F2}s: needs {Natives.Data.StatusName(p.Action.Requires)} ({p.Label}).");
            return;
        }
        var target = p.Action.Kind == MitKind.Debuff ? DebuffTarget(p) : null;
        var targetId = (ulong)(target ?? member).GameObjectId;
        if (member is SimPartyNpc) world.Server.Request(member, p.Action.ActionId, targetId);
        else world.Server.Apply(member, p.Action.ActionId, targetId);
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

    // Planned presses meant for a hit around now that aren't in effect on the victim.
    internal IReadOnlyList<string> Missing(SimCharacter victim, float now)
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
        var granted = Rules.GrantsOf(p.Action.ActionId).Select(g => g.StatusId).Where(Mitigation.ByStatusId.ContainsKey).ToList();
        return p.Action.Kind switch
        {
            MitKind.None => true,
            MitKind.Debuff => DebuffTarget(p) is { } boss && granted.Any(boss.HasStatus),
            MitKind.Shield => presses.Any(x => x.Slot == p.Slot && MitCatalogue.Find(x.ActionId)?.ActionId == p.Action.ActionId
                                               && x.At <= now && now - x.At <= p.Action.Duration),
            _ => granted.Any(victim.HasStatus),
        };
    }

    // When the run ends, the local player's grading in chat, if they were practising.
    internal void Finish(MitRunEnd end)
    {
        if (!IsActive) return;
        finished = true;
        DiagnosticLog.Info($"[MitPractice] Finish ({end}) at {clock():F2}s: {presses.Count} presses recorded.");
        if (!Enabled || world.Party.Player is not { } player) return;
        foreach (var line in Summary(player, clock()))
            Plugin.ChatGui.Print(new XivChatEntry { Type = XivChatType.SystemMessage, Message = $"[AnoMech] {line}" });
    }

    private IReadOnlyList<string> Summary(SimPlayer player, float now)
    {
        if (!practising) return ["Mitigation: your presses weren't graded. Turn on \"Resolve your own actions\" to grade them."];
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
        practising = false;
        finished = false;
    }

    private SimCharacter? Source(MitSource source) => sources.TryGetValue(source, out var actor) ? actor() : null;

    private SimCharacter? DebuffTarget(ResolvedPress p)
        => (data!.Entries[p.Entry].On ?? data.DebuffTarget) is { } target ? Source(target) : sources.Values.FirstOrDefault()?.Invoke();

    private string Owner(PartyRole slot) => world.Party.Player is { } player && player.Role == slot ? "your" : $"{slot.ShortLabel()}'s";

    private static string JobName(JobId job) => Regex.Replace(job.ToString(), "(?<!^)([A-Z])", " $1");
}
