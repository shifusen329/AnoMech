using System;
using System.Collections.Generic;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Geometry;
using AnoMech.Core.Game.Motion;
using AnoMech.Core.Game.Party;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.UserActions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.SimObjects;

public sealed class SimPartyNpc : SimNpc, ISimPartyMember
{
    public PartyRole Role { get; set; }
    public byte ClassJob { get; }
    public string DisplayName { get; }

    internal SimPartyNpc(IBattleCharaProxy proxy, Coordinates coordinates, PartyRole role, byte classJob, string name, ObstacleField obstacles)
        : base(proxy, coordinates,
            self => new SimulatedMotion(() => self.Proxy, coordinates, self.ActorControl, obstacles, turnSpeed: null, npc: false))
    {
        Role = role;
        ClassJob = classJob;
        DisplayName = name;
    }

    // level 1-3. The limit break this bot would press; 0 if KO'd, mitigation is not required, or the
    // job has none at that level. Bots press their own limit break only while mitigation is required.
    internal uint LimitBreakAction(int level)
        => this.IsAlive() && !ActionsLocked && Mitigation.Required ? LimitBreakHandler.ActionId(ClassJob, level) : 0;

    public void UseSprint(float duration)
    {
        if (!ActionsLocked) SprintHandler.Apply(this, duration);
    }

    public override void Despawn()
    {
        base.Despawn();
    }

    protected override void OnPacketSpawned(IBattleCharaProxy chara) => ActorControl.ZoneIn();

    public void OnKilled(bool byEnvironment)
    {
        GoDown(byEnvironment);
    }
}
