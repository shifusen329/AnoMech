using AnoMech.Core.Native.Interfaces;

namespace AnoMech.Tests;

// Off by default, as when the user hasn't enabled the module.
internal sealed class FakeUserActions : IUserActions
{
    public bool Enabled { get; set; }
    public event Action<PlayerAction>? Acted;

    // The player's client playing a step of their own action.
    public void Act(PlayerAction action) => Acted?.Invoke(action);
    public void Enable() => Enabled = true;
    public void Disable() => Enabled = false;
    public void OnSessionStart() { }
    public void OnScenarioStart() { }
    public void OnSessionEnd() { }
    public void Tick(float deltaSeconds) { }

    public List<ActionEffectData> OwnEffects { get; } = [];
    public List<byte[]> Gauges { get; } = [];
    private uint sequence;

    public uint ReceiveOwnEffect(ActionEffectData effect)
    {
        OwnEffects.Add(effect);
        return ++sequence;
    }

    public void ReceiveGauge(byte[] payload) => Gauges.Add(payload);

    public void Dispose() { }
}
