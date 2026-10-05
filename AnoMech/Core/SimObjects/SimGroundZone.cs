using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Party;

namespace AnoMech.Core.SimObjects;

// A placed circle that holds `statusId` on every party member standing in it, and takes it off
// whoever leaves (Sacred Soil). Despawned with the world, so nothing outlives the run.
public sealed class SimGroundZone : ISimObject
{
    private readonly SimParty party;
    private readonly Vector3 center;
    private readonly float radius;
    private readonly ushort statusId;
    private readonly float duration;
    private readonly Action<PartyRole, float>? onEnter;
    private readonly HashSet<SimCharacter> inside = [];
    private float elapsed;

    public bool IsActive { get; private set; } = true;

    internal SimGroundZone(SimParty party, Vector3 center, float radius, ushort statusId, float duration, Action<PartyRole, float>? onEnter)
    {
        this.party = party;
        this.center = center;
        this.radius = radius;
        this.statusId = statusId;
        this.duration = duration;
        this.onEnter = onEnter;
        Stamp();
    }

    public void Tick(float deltaSeconds)
    {
        if (!IsActive) return;
        elapsed += deltaSeconds;
        if (elapsed >= duration)
        {
            Despawn();
            return;
        }
        Stamp();
    }

    private void Stamp()
    {
        var remaining = duration - elapsed;
        foreach (var member in party.ActiveMembers())
        {
            var dx = member.Position.X - center.X;
            var dz = member.Position.Z - center.Z;
            var reach = radius + member.HitboxRadius;
            var isInside = dx * dx + dz * dz <= reach * reach;
            if (isInside && inside.Add(member))
            {
                member.AddStatus(statusId, remaining);
                if (member is ISimPartyMember slot) onEnter?.Invoke(slot.Role, remaining);
            }
            else if (!isInside && inside.Remove(member))
            {
                member.RemoveStatus(statusId);
            }
        }
    }

    public void Despawn()
    {
        if (!IsActive) return;
        IsActive = false;
        foreach (var member in inside) member.RemoveStatus(statusId);
        inside.Clear();
    }
}
