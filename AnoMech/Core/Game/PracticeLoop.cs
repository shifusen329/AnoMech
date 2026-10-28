using AnoMech.Core.Game.Party;
using AnoMech.Scenarios;

namespace AnoMech.Core.Game;

// Solo practice affordances that span runs: the clean-run streak and auto-restart.
public sealed class PracticeLoop
{
    private readonly Game game;
    private RunScenarioParams? lastRun;
    private IScenario? lastScenario;
    // An auto-restart held back until the death recap is closed.
    private RunScenarioParams? restartAfterRecap;

    // Restarts the same scenario with the same parameters, on the runs AutoRestartOn picks: a
    // finished run as soon as it settles, a failed one once its post-death freeze begins, so the
    // failure is still seen.
    public bool AutoRestart { get; set; }
    public AutoRestartTrigger AutoRestartOn { get; set; } = AutoRestartTrigger.AfterSuccess;
    public int Streak { get; private set; }
    public bool RestartWaitingOnRecap => restartAfterRecap != null;

    private bool RestartsAfterSuccess => AutoRestart && AutoRestartOn != AutoRestartTrigger.AfterDeath;
    private bool RestartsAfterDeath => AutoRestart && AutoRestartOn != AutoRestartTrigger.AfterSuccess;

    public PracticeLoop(Game game)
    {
        this.game = game;
        game.RunStarted += OnRunStarted;
        game.RunFinished += OnRunFinished;
        game.RunFrozen += OnRunFrozen;
        game.RecapClosed += OnRecapClosed;
        game.PartyMemberKilled += OnPartyMemberKilled;
        game.MechanicFailed += _ => EndStreak();
    }

    private void OnRunStarted(RunScenarioParams p, bool local)
    {
        // Restarting the same scenario is the normal way to extend a streak.
        if (p.Scenario != lastScenario)
            Streak = 0;
        lastScenario = p.Scenario;
        // A host rerunning on its own would desync the session.
        lastRun = local ? p : null;
        restartAfterRecap = null;
    }

    private void OnRunFinished(bool clean)
    {
        if (clean) Streak++;
        if (RestartsAfterSuccess && lastRun is { } p)
            game.StartScenario(p);
    }

    private void OnRunFrozen()
    {
        if (!RestartsAfterDeath || lastRun is not { } p) return;
        if (game.Recap != null) restartAfterRecap = p;
        else game.StartScenario(p);
    }

    private void OnRecapClosed()
    {
        if (restartAfterRecap is not { } p) return;
        restartAfterRecap = null;
        game.StartScenario(p);
    }

    private void OnPartyMemberKilled(PartyRole role, string cause, uint? actionId, ushort? statusId) => EndStreak();

    private void EndStreak()
    {
        Streak = 0;
        if (!RestartsAfterDeath) AutoRestart = false;
    }
}

// Which runs PracticeLoop.AutoRestart restarts.
public enum AutoRestartTrigger
{
    AfterSuccess,
    AfterDeath,
    Both,
}
