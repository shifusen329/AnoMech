using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

public sealed class UmadP1GravenImage2SettingsWindow
{
    public UmadP1GravenImage2StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##p1gravenimage2"))
        {
            SettingsGrid.FightOnlyNote();
            UmadP1SettingsRows.TriState("Puddles, set 1:", "gi2purple1", "DPS", "Supports", Overrides.PurpleDps1, v => Overrides.PurpleDps1 = v);
            UmadP1SettingsRows.TriState("Puddles, set 2:", "gi2purple2", "DPS", "Supports", Overrides.PurpleDps2, v => Overrides.PurpleDps2 = v);
            UmadP1SettingsRows.IceRow("Ice hits:", "gi2ice", Overrides.IceRealOffset, v => Overrides.IceRealOffset = v);
            UmadP1SettingsRows.TriState("Ice orb:", "gi2icelie", "Lie", "Truth", Overrides.IceIsLie, v => Overrides.IceIsLie = v);
            UmadP1SettingsRows.TriState("Cleave 1:", "gi2cleave1", "West", "East", Overrides.CleaveWest1, v => Overrides.CleaveWest1 = v);
            UmadP1SettingsRows.TriState("Cleave 2:", "gi2cleave2", "West", "East", Overrides.CleaveWest2, v => Overrides.CleaveWest2 = v);
#if DEBUG
            UmadP1SettingsRows.PropKnobs("gi2", Overrides.PropsBindDirector, v => Overrides.PropsBindDirector = v,
                Overrides.PropsForceActive, v => Overrides.PropsForceActive = v, Overrides.PropsBeatMode, v => Overrides.PropsBeatMode = v);
#endif
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.PurpleDps1 = null;
        Overrides.PurpleDps2 = null;
        Overrides.IceRealOffset = null;
        Overrides.IceIsLie = null;
        Overrides.CleaveWest1 = null;
        Overrides.CleaveWest2 = null;
        Overrides.PropsBindDirector = true;
        Overrides.PropsForceActive = false;
        Overrides.PropsBeatMode = PropBeatMode.ActorControl;
    }
}
