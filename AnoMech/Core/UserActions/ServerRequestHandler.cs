using FFXIVClientStructs.FFXIV.Client.Game;

namespace AnoMech.Core.UserActions;

// Hands the player's actions to the sim's server, which the firewall keeps them from reaching.
internal sealed unsafe class ServerRequestHandler : IUserActionHandler
{
    public void OnAction(ActionType actionType, uint actionId, ulong targetId)
    {
        if (actionType != ActionType.Action || Plugin.Game is not { Player: { } player } game) return;
        var am = ActionManager.Instance();
        game.World.Server.Request(player, actionId, targetId, am != null ? am->LastUsedActionSequence : (ushort)0);
    }

    public void OnCastStart(ActionType actionType, uint actionId)
    {
        if (actionType == ActionType.Action && Plugin.Game is { Player: { } player } game)
            game.World.Server.CastStarted(player, actionId, 0);
    }

    public void OnCastInterrupted(ActionType actionType, uint actionId)
    {
        if (Plugin.Game is { Player: { } player } game) game.World.Server.CastInterrupted(player);
    }
}
