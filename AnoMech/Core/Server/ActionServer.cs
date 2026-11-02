using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;
using RuleSim.Actions;

namespace AnoMech.Core.Server;

// Answers actions the way the game server does, for the player and the bots: the statuses an action
// spends and grants, the ActionEffect, the caster's gauge, and each target's EffectResult.
public sealed class ActionServer
{
    // UNVERIFIED: retail's EffectResult arrives 0.5-1 s after the ActionEffect.
    private const float EffectResultDelay = 0.5f;

    private readonly SimWorld world;
    private readonly SimWorldView view;
    private readonly Resolver resolver = new(new GameDataActionSheet());
    private readonly Dictionary<SimCharacter, JobState> states = [];
    private readonly List<PendingResult> pendingResults = [];
    private readonly List<PendingFollowUp> pendingFollowUps = [];
    private float clock;

    private sealed record PendingResult(float At, SimCharacter Target, uint Sequence, byte Index);
    private sealed record PendingFollowUp(float At, SimCharacter Caster, uint ActionId, bool Shown);

    internal ActionServer(SimWorld world)
    {
        this.world = world;
        view = new SimWorldView(world);
    }

    internal Random Rng { get; set; } = new();

    // A fresh pull: every gauge emptied, the player's seeded with what their job walks in with.
    public void Begin()
    {
        states.Clear();
        pendingResults.Clear();
        pendingFollowUps.Clear();
        if (world.Party.Player is not { } player) return;
        var job = JobOf(player);
        var state = new JobState(job);
        states[player] = state;
        Natives.UserActions.ReceiveGauge(resolver.Begin(view, SimWorldView.Of(player), state, job, Rng));
    }

    // Shown to everyone: the ActionEffect aimed and listing its targets, with `sequence` naming the
    // player's own request.
    public ActionResult? Request(SimCharacter caster, uint actionId, ulong targetId, ushort sequence = 0)
    {
        if (Resolve(caster, actionId, targetId) is not { } result) return null;
        ApplyResult(result);
        var targets = result.Targets.Select(view.Character).OfType<SimCharacter>().ToList();
        var effect = Effect(caster, result, targets, sequence);
        var globalSequence = caster is SimPlayer
            ? Natives.UserActions.ReceiveOwnEffect(effect)
            : caster.Casting.NativeActionEffect(effect.ActionId, effect.AnimationLock, effect.SpellId, 0, ActionType.Action, 0,
                animationTargetId: effect.AnimationTarget, targets: effect.Targets);
        if (globalSequence != 0)
            for (var i = 0; i < targets.Count; i++)
                pendingResults.Add(new PendingResult(clock + EffectResultDelay, targets[i], globalSequence, (byte)i));
        SendGauge(caster, result.Gauge);
        FollowUp(caster, result, shown: true);
        return result;
    }

    // What the action does without anything shown, for a seat whose client shows its own presses.
    public ActionResult? Apply(SimCharacter caster, uint actionId, ulong targetId)
    {
        if (Resolve(caster, actionId, targetId) is not { } result) return null;
        ApplyResult(result);
        SendGauge(caster, result.Gauge);
        FollowUp(caster, result, shown: false);
        return result;
    }

    public void CastStarted(SimCharacter caster, uint actionId, ulong targetId)
    {
        var result = resolver.CastStarted(view, new ActionRequest((ulong)caster.GameObjectId, actionId, targetId), StateOf(caster), Rng);
        ApplyStatuses(result.Statuses);
        SendGauge(caster, result.Gauge);
    }

    public void CastInterrupted(SimCharacter caster) => resolver.Interrupted(StateOf(caster));

    public void AutoAttack(SimCharacter caster)
        => SendGauge(caster, resolver.AutoAttack(view, SimWorldView.Of(caster), StateOf(caster), Rng));

    internal void Tick(float deltaSeconds)
    {
        clock += deltaSeconds;
        DeliverEffectResults();
        DeliverFollowUps();
        foreach (var (caster, state) in states.ToList())
        {
            if (caster is SimPlayer && !Natives.UserActions.Enabled) continue;
            var moving = caster is SimPlayer && (Natives.PlayerInput.MovementInputActive || Natives.PlayerInput.IsJumping);
            var result = resolver.Tick(view, SimWorldView.Of(caster), state, deltaSeconds, moving, Rng);
            ApplyStatuses(result.Statuses);
            SendGauge(caster, result.Gauge);
        }
    }

    internal void Clear()
    {
        states.Clear();
        pendingResults.Clear();
        pendingFollowUps.Clear();
        clock = 0f;
    }

    private ActionResult? Resolve(SimCharacter caster, uint actionId, ulong targetId)
    {
        var result = resolver.Resolve(view, new ActionRequest((ulong)caster.GameObjectId, actionId, targetId), StateOf(caster), Rng);
        foreach (var warning in result.Warnings) DiagnosticLog.Warn($"[ActionServer] {warning}");
        if (result.Rejected is { } reason)
        {
            DiagnosticLog.Info($"[ActionServer] {Natives.Data.ActionName(actionId)} rejected: {reason}");
            return null;
        }
        return result;
    }

    private void ApplyResult(ActionResult result)
    {
        ApplyStatuses(result.Statuses);
        foreach (var zone in result.Zones)
            world.PlaceGroundZone(zone.Position, zone.Radius, zone.Status, zone.Duration,
                (role, remaining) => HostReport.RoleStatus([role], zone.Status, remaining, 0));
    }

    private void ApplyStatuses(IReadOnlyList<StatusChange> changes)
    {
        foreach (var change in changes)
        {
            if (view.Character(change.Target) is not { } target) continue;
            switch (change.Kind)
            {
                case StatusChangeKind.Grant:
                    target.RemoveStatus(change.Status);
                    target.AddStatusParam(change.Status, change.Param, change.Duration).ShieldScale = change.ShieldScale;
                    break;
                case StatusChangeKind.AddStack:
                    target.AddStatus(change.Status, change.Duration);
                    break;
                case StatusChangeKind.Consume:
                    target.AddStatus(change.Status, 0f, -1);
                    break;
                case StatusChangeKind.Remove:
                    target.RemoveStatus(change.Status);
                    break;
            }
        }
        ReportGrants(changes);
    }

    // A peer runs no scenario logic, so the host learns of what its presses grant.
    private void ReportGrants(IReadOnlyList<StatusChange> changes)
    {
        foreach (var grant in changes.Where(c => c.Kind == StatusChangeKind.Grant).GroupBy(c => (c.Status, c.Duration, c.Param)))
        {
            var characters = grant.Select(c => view.Character(c.Target)).OfType<SimCharacter>().ToList();
            var roles = characters.OfType<ISimPartyMember>().Select(m => m.Role).ToList();
            if (roles.Count > 0) HostReport.RoleStatus(roles, grant.Key.Status, grant.Key.Duration, grant.Key.Param);
            var enemies = characters.OfType<SimEnemy>().ToList();
            if (enemies.Count > 0) Plugin.Multiplayer?.ReportAppliedEnemyStatus(enemies, grant.Key.Status, grant.Key.Duration, grant.Key.Param);
        }
    }

    private static ActionEffectData Effect(SimCharacter caster, ActionResult result, IReadOnlyList<SimCharacter> targets, ushort sequence)
    {
        var aimedAt = targets.FirstOrDefault() ?? caster;
        var entries = targets.Select((t, i) => new ActionEffectTarget(t.GameObjectId,
            i == 0 && result.Combo != ComboRole.None ? [ActionEffectEntry.ActionMarker(result.ActionId, result.Combo == ComboRole.Continuation)] : null)).ToList();
        if (entries.Count == 0) entries.Add(new ActionEffectTarget(caster.GameObjectId));
        var proxy = caster.Proxy;
        return new ActionEffectData(result.ActionId, ActionType.Action, result.AnimationLock, (ushort)result.ActionId, 0, 0,
            proxy?.Rotation ?? caster.Rotation, proxy?.Position ?? default,
            AnimationTarget: aimedAt.GameObjectId, Targets: entries, SourceSequence: sequence);
    }

    private void FollowUp(SimCharacter caster, ActionResult result, bool shown)
    {
        if (result.FollowUp is { } followUp)
            pendingFollowUps.Add(new PendingFollowUp(clock + followUp.Delay, caster, followUp.ActionId, shown));
    }

    private void DeliverFollowUps()
    {
        foreach (var due in pendingFollowUps.Where(f => f.At <= clock).ToList())
        {
            pendingFollowUps.Remove(due);
            if (!due.Caster.IsAlive()) continue;
            var self = (ulong)due.Caster.GameObjectId;
            if (due.Shown) Request(due.Caster, due.ActionId, self);
            else Apply(due.Caster, due.ActionId, self);
        }
    }

    private void DeliverEffectResults()
    {
        for (var i = pendingResults.Count - 1; i >= 0; i--)
        {
            var pending = pendingResults[i];
            if (pending.At > clock) continue;
            pendingResults.RemoveAt(i);
            if (pending.Target.Proxy is not { Exists: true } proxy) continue;
            proxy.ReceiveEffectResult(pending.Sequence, pending.Index, world.Hp.ClientHealth(pending.Target) ?? proxy.Health);
        }
    }

    private static void SendGauge(SimCharacter caster, byte[]? gauge)
    {
        if (gauge != null && caster is SimPlayer) Natives.UserActions.ReceiveGauge(gauge);
    }

    private JobState StateOf(SimCharacter caster)
    {
        var job = JobOf(caster);
        if (states.TryGetValue(caster, out var state) && state.Job == job) return state;
        return states[caster] = new JobState(job);
    }

    private static Job JobOf(SimCharacter caster) => caster is ISimPartyMember m ? (Job)m.ClassJob : Job.None;
}
