using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyMit;

// What's left of each barrier on one member, in the order they went up. A barrier status is
// sized the first time it's seen, so one a peer reported counts at its default size.
internal sealed class ShieldLedger
{
    private const ushort PanhaimaStatus = 2613;
    private const ushort PanhaimatinonStatus = 2643;

    private readonly List<(SimStatus Status, MitShield Shield)> entries = [];

    public IReadOnlyList<MitShield> Shields => entries.Select(e => e.Shield).ToList();

    public float Remaining => entries.Sum(e => e.Shield.Spent ? 0f : e.Shield.HitsLeft > 0 ? e.Shield.PerHit : e.Shield.Amount);

    public void Sync(SimCharacter member, float maxHp)
    {
        entries.RemoveAll(e => !e.Status.IsActive);
        foreach (var status in member.ActiveStatuses)
        {
            if (!Mitigation.ByStatusId.TryGetValue(status.StatusId, out var m) || !m.IsShield) continue;
            if (entries.Any(e => ReferenceEquals(e.Status, status))) continue;
            var amount = m.ShieldFractionOfMaxHp(maxHp) * maxHp * status.ShieldScale;
            entries.Add((status, new MitShield(status.StatusId, StatusLookup.Name(status.StatusId), amount, m.ShieldHits)));
        }
    }

    // After a hit: a used-up barrier comes off, and Panhaima's stack counter follows its hits.
    public void Commit(SimCharacter member)
    {
        foreach (var (status, shield) in entries)
        {
            if (shield.StatusId == PanhaimaStatus && !shield.Spent)
                member.AddStatus(PanhaimatinonStatus, stacks: shield.HitsLeft, overrideStacks: true);
            if (!shield.Spent) continue;
            if (shield.StatusId == PanhaimaStatus) member.RemoveStatus(PanhaimatinonStatus);
            status.Despawn();
        }
        entries.RemoveAll(e => e.Shield.Spent);
    }
}
