using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Game.PartyMit;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyHp;

// A hit reserved at its resolve, landed with its damage number. Kills is decided at the resolve,
// with the rest of the action's deaths.
internal sealed record HpLanding(SimCharacter Target, MitHitDef Hit, MitHitResult Result, bool RanOut, bool Kills, string Detail);

// Every party member's HP in a fight with a damage model: each hit in the plan's table takes the
// victim's class share, cut by mitigation and barriers. A human out of HP dies; a bot is left on
// 1 HP. Host-only, since it starts from scenario.Run, which a peer never runs. Starts the plan's
// mitigation presses, which the damage assumes.
public sealed class PartyHp
{
    // A would-be death warning for one member holds off the next for this long.
    private const float WarnHold = 3f;
    // How often a refilling bar is redrawn; a hit draws at once. UNVERIFIED cosmetic rate.
    private const float RedrawInterval = 0.25f;

    private readonly SimWorld world;
    private MitPlanData? data;
    private IReadOnlyDictionary<MitSource, Func<SimCharacter?>> sources = new Dictionary<MitSource, Func<SimCharacter?>>();
    private Func<float> clock;
    private readonly Dictionary<SimCharacter, HpPool> pools = [];
    private readonly Dictionary<SimCharacter, ShieldLedger> ledgers = [];
    private readonly Dictionary<SimCharacter, float> warnedAt = [];
    private readonly Dictionary<SimCharacter, float> drawnAt = [];
    private readonly HashSet<uint> unmodelled = [];

    internal PartyHp(SimWorld world)
    {
        this.world = world;
        clock = () => world.Events.Elapsed;
    }

    public bool Active => data != null;

    // Off, a human who runs out of HP is left on 1 HP with a warning, as a bot always is.
    public bool HumansDie { get; set; } = true;

    // Each hit that takes a member to 0 HP, whether it kills them or not.
    internal event Action<SimCharacter, MitHitDef>? RanOut;

    // At the scenario's start, in place of MitPractice.Begin. A solo run has no HP.
    public void Begin(MitPlanData planData, IReadOnlyDictionary<MitSource, Func<SimCharacter?>>? actors = null, Func<float>? scenarioClock = null)
    {
        Clear();
        world.MitPractice.Begin(planData, actors, scenarioClock);
        if (!world.Party.IsFull || !planData.Hits.Any(h => h.Raw != null)) return;
        data = planData;
        sources = actors ?? new Dictionary<MitSource, Func<SimCharacter?>>();
        clock = scenarioClock ?? (() => world.Events.Elapsed);
        var now = clock();
        foreach (var member in world.Party.AllMembers())
            if (member is ISimPartyMember slot)
            {
                var pool = new HpPool(planData.Profile.MaxHp(MitProfile.ClassOf((JobId)slot.ClassJob)), planData.Profile.TopUpGap,
                    planData.Profile.HealPerSecond);
                pools[member] = pool;
                Draw(member, pool, now);
            }
        DiagnosticLog.Info($"[PartyHp] Begin: {pools.Count} members, {planData.Hits.Count(h => h.Raw != null)} hits with damage, humans {(HumansDie ? "die" : "are warned")}.");
    }

    public bool Tracks(SimCharacter member) => pools.ContainsKey(member);

    public float? Hp(SimCharacter member) => pools.TryGetValue(member, out var pool) ? pool.Hp : null;

    public float? MaxOf(SimCharacter member) => pools.TryGetValue(member, out var pool) ? pool.Max : null;

    // The HP bar's value: the pool as a share of the character's own max HP, so the bar reads right
    // whatever max the client has, the player's real one or a bot's.
    internal uint? ClientHealth(SimCharacter member)
    {
        if (!pools.TryGetValue(member, out var pool) || member.Proxy is not { Exists: true } chara) return null;
        if (!member.IsAlive()) return 0;
        return (uint)Math.Clamp(MathF.Round(pool.Hp / pool.Max * chara.MaxHealth), 1f, chara.MaxHealth);
    }

    internal void Tick()
    {
        if (!Active) return;
        var now = clock();
        foreach (var (member, pool) in pools)
        {
            if (!member.IsAlive()) continue;
            var ledger = Ledger(member);
            ledger.Sync(member, pool.Max);
            var shield = ledger.Remaining / pool.Max;
            var shieldChanged = member.ShieldOverride != shield;
            member.ShieldOverride = shield;
            var healed = pool.TopUp(now);
            if (shieldChanged || (healed && (pool.Hp >= pool.Max || now - drawnAt.GetValueOrDefault(member, float.MinValue) >= RedrawInterval)))
                Draw(member, pool, now);
        }
    }

    // An EnemyAction's resolve: the table's hit for that action is reserved on everyone it shows
    // damage on and hasn't already killed. Each lands with the action's EffectResult.
    internal IReadOnlyList<HpLanding> Resolve(EnemyActionContext ctx, float landsIn)
    {
        if (!Active) return [];
        var damaged = ctx.Outcomes.Where(o => !o.Killed && o.Damage is { Amount: > 0 } && pools.ContainsKey(o.Target)).Select(o => o.Target).ToList();
        if (damaged.Count == 0) return [];
        var actionId = ctx.Action.ActionId;
        if (data!.Hits.FirstOrDefault(h => h.Raw != null && h.ActionIds?.Contains(actionId) == true) is not { } hit)
        {
            if (unmodelled.Add(actionId))
                DiagnosticLog.Info($"[PartyHp] {Natives.Data.ActionName(actionId)} ({actionId}) has no damage in the plan; its hits move no HP.");
            return [];
        }
        var landsAt = clock() + landsIn;
        return damaged.Select(victim => Reserve(victim, hit, landsAt)).OfType<HpLanding>().ToList();
    }

    internal void Land(HpLanding landing)
    {
        var victim = landing.Target;
        if (!pools.TryGetValue(victim, out var pool)) return;
        if (!victim.IsAlive())
        {
            pool.Release(landing.Result.HpLoss);
            return;
        }
        var now = clock();
        var ledger = Ledger(victim);
        ledger.Commit(victim);
        if (pool.Land(now, landing.Result.HpLoss, landing.Hit.Fresh, reserved: true) < 1f) pool.KeepAlive();
        if (landing.RanOut && !landing.Kills) Warn(victim, landing, now);
        victim.ShieldOverride = ledger.Remaining / pool.Max;
        Draw(victim, pool, now);
    }

    // A hit the scenario lands itself, with no EnemyAction; `actionId` names it in a death.
    public void Hit(SimCharacter victim, string key, uint actionId)
    {
        if (!Active || data!.Hits.FirstOrDefault(h => h.Key == key) is not { Raw: not null } hit) return;
        if (Reserve(victim, hit, clock()) is not { } landing) return;
        Land(landing);
        if (landing.Kills) victim.Die(actionId, Explanation(landing));
    }

    public void HitParty(string key, uint actionId)
    {
        foreach (var member in world.Party.AllMembers().ToList()) Hit(member, key, actionId);
    }

    internal static string Explanation(HpLanding landing) => $"ran out of HP: {landing.Detail}";

    private HpLanding? Reserve(SimCharacter victim, MitHitDef hit, float landsAt)
    {
        if (victim is not ISimPartyMember member || !victim.IsAlive() || !pools.TryGetValue(victim, out var pool)) return null;
        var ledger = Ledger(victim);
        ledger.Sync(victim, pool.Max);
        var source = hit.Source is { } s ? Layers(Source(s)) : [];
        var bot = world.Party.IsBotDriven(victim);
        var result = MitDamage.Apply(hit, MitProfile.ClassOf((JobId)member.ClassJob), data!.Profile, Layers(victim), source,
            ledger.Shields, assumeTankCooldowns: bot || !Mitigation.Required);
        pool.Reserve(result.HpLoss);
        var left = hit.Fresh ? pool.Max - result.HpLoss : pool.Projected(landsAt);
        var ranOut = left <= 0f;
        if (ranOut) RanOut?.Invoke(victim, hit);
        var detail = MitDamage.Explain(result, ranOut ? world.MitPractice.Missing(victim, clock()) : []);
        return new HpLanding(victim, hit, result, ranOut, ranOut && HumansDie && !bot, detail);
    }

    private void Warn(SimCharacter victim, HpLanding landing, float now)
    {
        if (warnedAt.TryGetValue(victim, out var warned) && now - warned <= WarnHold) return;
        warnedAt[victim] = now;
        var text = $"{landing.Hit.Name} would have killed {Who(victim)}: {landing.Detail}";
        if (world.Party.IsBotDriven(victim) && !world.MitPractice.Enabled) DiagnosticLog.Warn($"[PartyHp] {text}");
        else world.Announce(text);
    }

    private void Draw(SimCharacter member, HpPool pool, float now)
    {
        drawnAt[member] = now;
        if (ClientHealth(member) is { } health) member.ShowHp(health);
    }

    internal void Clear()
    {
        foreach (var member in pools.Keys) member.ShieldOverride = null;
        data = null;
        sources = new Dictionary<MitSource, Func<SimCharacter?>>();
        clock = () => world.Events.Elapsed;
        pools.Clear();
        ledgers.Clear();
        warnedAt.Clear();
        drawnAt.Clear();
        unmodelled.Clear();
    }

    private SimCharacter? Source(MitSource source) => sources.TryGetValue(source, out var actor) ? actor() : null;

    private ShieldLedger Ledger(SimCharacter member)
    {
        if (!ledgers.TryGetValue(member, out var ledger)) ledgers[member] = ledger = new ShieldLedger();
        return ledger;
    }

    private static IReadOnlyList<MitLayer> Layers(SimCharacter? character)
        => character?.ActiveStatuses
                    .Where(s => Mitigation.ByStatusId.ContainsKey(s.StatusId))
                    .Select(s => new MitLayer(s.StatusId, Natives.Data.StatusName(s.StatusId), Mitigation.ByStatusId[s.StatusId]))
                    .ToList()
           ?? [];

    private static string Who(SimCharacter member)
        => member is ISimPartyMember slot ? member is SimPlayer ? $"you ({slot.Role.ShortLabel()})" : slot.Role.ShortLabel() : "?";
}
