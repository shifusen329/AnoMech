using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// Tells mitigation practice what the player pressed, for grading. An effect handler, so a hard
// cast counts when it lands and an interrupted one never does.
internal sealed class MitPracticePressHandler : IUserActionHandler
{
    public void OnAction(ActionType actionType, uint actionId)
    {
        if (actionType != ActionType.Action || Plugin.GameInstance?.Player is not { } player) return;
        Plugin.GameInstance.World.MitPractice.RecordPress(player.Role, actionId);
    }
}
