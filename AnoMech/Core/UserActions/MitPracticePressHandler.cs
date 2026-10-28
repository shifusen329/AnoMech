using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// Tells mitigation practice what the player pressed, for grading. An effect handler, so a hard
// cast counts when it lands and an interrupted one never does.
internal sealed class MitPracticePressHandler : IUserActionHandler
{
    public void OnAction(ActionType actionType, uint actionId)
    {
        if (actionType != ActionType.Action || Plugin.Game?.Player is not { } player) return;
        Plugin.Game.World.MitPractice.RecordPress(player.Role, actionId);
    }
}
