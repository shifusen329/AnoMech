using System.Collections.Generic;
using AnoMech.Core.Game.Party;

namespace AnoMech.Core.UserActions;

// A peer runs no scenario logic: the host decides who lives, from the statuses on its own copy
// of each character. Only mitigation, the one kind of status the host acts on, and Sprint, which
// every other client shows, are sent.
internal static class HostReport
{
    public static bool IsReported(ushort statusId) => Mitigation.ByStatusId.ContainsKey(statusId) || statusId == SprintHandler.SprintStatusId;

    public static void RoleStatus(IReadOnlyList<PartyRole> roles, ushort statusId, float duration, int stacks)
    {
        if (IsReported(statusId))
            Plugin.Multiplayer?.ReportAppliedRoleStatus(roles, statusId, duration, (ushort)stacks);
    }
}
