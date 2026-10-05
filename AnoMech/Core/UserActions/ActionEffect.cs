using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// What a player action does client-side, standing in for the firewalled server response.
// Composable: leaf effects (Gauge / Status) do the work; wrappers (Combo / Random) gate
// them. Authored via the factory helpers in JobActions (Gauge(...), Status(...), etc.).
internal interface IActionEffect
{
    void Apply(ActionContext ctx);
}

// The statuses an effect grants and for how long, read by JobActions.GrantsOf.
internal interface IGrantsStatus
{
    IEnumerable<(ushort StatusId, float Duration)> Grants { get; }
}

// Per-dispatch state an effect may need. The caster is the local player for a real press, or
// a bot for SimPartyNpc.UseAction.
internal sealed class ActionContext(uint actionId, ulong targetId, SimCharacter caster, Random rng)
{
    public ulong TargetId { get; } = targetId;
    public uint ActionId { get; } = actionId;
    public SimCharacter Caster { get; } = caster;
    // The job gauge and combo state are the local player's alone.
    public bool CasterIsPlayer => Caster is SimPlayer;
    public Random Rng { get; } = rng;
    // Multiplies the barriers this press grants (Zoe, a spread crit Adloquium, Rising Rhythm).
    public float ShieldScale { get; set; } = 1f;
}

// Leaf: add an amount to a job gauge (ResourceGauge.Add clamps to [0, Max]).
internal sealed unsafe class GaugeEffect(ResourceGauge gauge, int amount) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        if (!ctx.CasterIsPlayer) return;
        var jgm = JobGaugeManager.Instance();
        if (jgm != null) gauge.Add(jgm, amount);
    }
}

// Leaf: set a job gauge to an absolute value, clamped like Add.
internal sealed unsafe class SetGaugeEffect(ResourceGauge gauge, int value) : IActionEffect
{
    public void Apply(ActionContext ctx)
    {
        if (!ctx.CasterIsPlayer) return;
        var jgm = JobGaugeManager.Instance();
        if (jgm != null) gauge.Add(jgm, value - gauge.Read(jgm));
    }
}

// Leaf: grant a status to the player for `duration` seconds (replaces any existing copy).
// `stacks` sets Status.Param — 0 for a plain buff, or the stack count for a stacking buff
// (Requiescat, Sacred Sight); we don't decrement per-consume yet, it just expires.
internal sealed class StatusEffect(ushort statusId, float duration, int stacks = 0) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => [(statusId, duration)];

    public void Apply(ActionContext ctx)
    {
        ctx.Caster.RemoveStatus(statusId);
        ctx.Caster.AddStatusParam(statusId, stacks, duration).ShieldScale = ctx.ShieldScale;
        if (ctx.Caster is ISimPartyMember member) HostReport.RoleStatus([member.Role], statusId, duration);
    }
}

// Leaf: take a status off the caster (a press that ends what it was gated on).
internal sealed class RemoveStatusEffect(ushort statusId) : IActionEffect
{
    public void Apply(ActionContext ctx) => ctx.Caster.RemoveStatus(statusId);
}

// A peer runs no scenario logic: the host decides who lives, from the statuses on its own copy
// of each character. Only mitigation is sent, the one kind of status the host acts on.
internal static class HostReport
{
    public static void RoleStatus(IReadOnlyList<PartyRole> roles, ushort statusId, float duration)
    {
        if (Mitigation.ByStatusId.ContainsKey(statusId))
            Plugin.MultiplayerInstance?.ReportAppliedRoleStatus(roles, statusId, duration);
    }
}

// Leaf: grant a status to the action's friendly recipients (see ActionTargets). shieldScale sizes
// a barrier relative to its ByStatusId base.
internal sealed class TargetStatusEffect(ushort statusId, float duration, int stacks = 0, float shieldScale = 1f) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => [(statusId, duration)];

    public void Apply(ActionContext ctx) => Grant(ActionTargets.Friendly(ctx), ctx, statusId, duration, stacks, shieldScale);

    internal static void Grant(IReadOnlyList<SimCharacter> members, ActionContext ctx, ushort statusId, float duration, int stacks, float shieldScale)
    {
        var roles = new List<PartyRole>();
        foreach (var member in members)
        {
            member.RemoveStatus(statusId);
            member.AddStatusParam(statusId, stacks, duration).ShieldScale = shieldScale * ctx.ShieldScale;
            if (member is ISimPartyMember slot) roles.Add(slot.Role);
        }
        HostReport.RoleStatus(roles, statusId, duration);
    }
}

// Leaf: grant a status to every party member within `range` of the caster, for actions whose
// sheet row only targets the caster (an aura, or a pet's party-wide follow-up).
internal sealed class PartyStatusEffect(ushort statusId, float duration, float range) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => [(statusId, duration)];

    public void Apply(ActionContext ctx)
        => TargetStatusEffect.Grant(ActionTargets.PartyAround(ctx.Caster, range), ctx, statusId, duration, 0, 1f);
}

// Leaf: a circle at the caster that gives `statusId` to whoever stands in it (Sacred Soil).
internal sealed class GroundZoneEffect(ushort statusId, float radius, float duration) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => [(statusId, duration)];

    public void Apply(ActionContext ctx)
        => Plugin.GameInstance?.World.PlaceGroundZone(ctx.Caster.Position, radius, statusId, duration,
            (role, remaining) => HostReport.RoleStatus([role], statusId, remaining));
}

// Wrapper: apply the inner effects only while the caster holds `statusId` (Neutral Sect's barrier).
internal sealed class IfCasterHasEffect(ushort statusId, IActionEffect[] inner) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => inner.OfType<IGrantsStatus>().SelectMany(g => g.Grants);

    public void Apply(ActionContext ctx)
    {
        if (!ctx.Caster.HasStatus(statusId)) return;
        foreach (var e in inner) e.Apply(ctx);
    }
}

// Wrapper: a held `statusId` multiplies the inner barriers by `factor` and is spent (Zoe).
internal sealed class BoostEffect(ushort statusId, float factor, IActionEffect[] inner) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => inner.OfType<IGrantsStatus>().SelectMany(g => g.Grants);

    public void Apply(ActionContext ctx)
    {
        var boosted = ctx.Caster.HasStatus(statusId);
        if (boosted) ctx.ShieldScale *= factor;
        foreach (var e in inner) e.Apply(ctx);
        if (!boosted) return;
        ctx.ShieldScale /= factor;
        ctx.Caster.RemoveStatus(statusId);
    }
}

// Wrapper: the caster's stacks of `statusId` pick the inner barriers' scale, then are spent
// (Improvised Finish on Rising Rhythm). scaleByStacks[n] is the scale at n stacks.
internal sealed class ScaleByStacksEffect(ushort statusId, float[] scaleByStacks, IActionEffect[] inner) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => inner.OfType<IGrantsStatus>().SelectMany(g => g.Grants);

    public void Apply(ActionContext ctx)
    {
        var stacks = ctx.Caster.FindStatus(statusId)?.Stacks ?? 0;
        var scale = scaleByStacks[Math.Clamp((int)stacks, 0, scaleByStacks.Length - 1)];
        ctx.ShieldScale *= scale;
        foreach (var e in inner) e.Apply(ctx);
        ctx.ShieldScale /= scale;
        ctx.Caster.RemoveStatus(statusId);
    }
}

// Leaf: debuff the enemies the action hits (Reprisal).
internal sealed class EnemyStatusEffect(ushort statusId, float duration, int stacks = 0) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => [(statusId, duration)];

    public void Apply(ActionContext ctx)
    {
        var enemies = ActionTargets.Hostile(ctx);
        foreach (var enemy in enemies)
        {
            enemy.RemoveStatus(statusId);
            enemy.AddStatusParam(statusId, stacks, duration);
        }
        Plugin.MultiplayerInstance?.ReportAppliedEnemyStatus(enemies, statusId, duration);
    }
}

// Wrapper: apply the inner effects only when the action lands as a valid combo continuation.
internal sealed class ComboEffect(IActionEffect[] inner) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => inner.OfType<IGrantsStatus>().SelectMany(g => g.Grants);

    public void Apply(ActionContext ctx)
    {
        if (!ctx.CasterIsPlayer || !PlayerCombo.IsActiveContinuation(ctx.ActionId)) return;
        foreach (var e in inner) e.Apply(ctx);
    }
}

// Wrapper: apply the inner effects with `chance` probability (procs, e.g. DNC feathers).
internal sealed class RandomEffect(float chance, IActionEffect[] inner) : IActionEffect, IGrantsStatus
{
    public IEnumerable<(ushort StatusId, float Duration)> Grants => inner.OfType<IGrantsStatus>().SelectMany(g => g.Grants);

    public void Apply(ActionContext ctx)
    {
        if (ctx.Rng.NextSingle() >= chance) return;
        foreach (var e in inner) e.Apply(ctx);
    }
}
