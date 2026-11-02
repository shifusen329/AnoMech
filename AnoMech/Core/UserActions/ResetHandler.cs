using AnoMech.Core.Native.Implementations;

namespace AnoMech.Core.UserActions;

// What the server resets for a fresh pull: every recast cleared, as it does at zone-in, and each
// seat's job state, the player's gauge included.
internal sealed class ResetHandler : IUserActionHandler
{
    public bool RunsWhenDisabled => true;

    public void OnScenarioStart()
    {
        RecastTimes.ClearAll();
        Plugin.Game?.World.Server.Begin();
    }
}
