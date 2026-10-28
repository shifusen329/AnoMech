using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// A wipe resets every cooldown, so each scenario starts with the player's actions ready. Only
// recasts started inside the sim zone are cleared: the send firewall keeps those presses off the
// server, so the client's timer is the only record of them. A recast from before the sim (Return,
// food) is the server's and is left alone. Runs even when the module is disabled, like Sprint.
internal sealed unsafe class CooldownResetHandler : IUserActionHandler
{
    private readonly HashSet<int> startedGroups = [];

    public void OnAction(ActionType actionType, uint actionId)
    {
        var am = ActionManager.Instance();
        if (am == null) return;
        startedGroups.Add(am->GetRecastGroup((int)actionType, actionId));
        startedGroups.Add(am->GetAdditionalRecastGroup(actionType, actionId));
    }

    public void OnScenarioStart() => ClearStartedRecasts();

    public void ClearStartedRecasts()
    {
        var am = ActionManager.Instance();
        if (am == null) return;
        foreach (var group in startedGroups)
        {
            if (group < 0) continue;
            var detail = am->GetRecastGroupDetail(group);
            if (detail == null) continue;
            detail->IsActive = false;
            detail->Elapsed = 0f;
        }
        startedGroups.Clear();
    }
}
