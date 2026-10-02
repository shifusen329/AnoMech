using System;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P1Shared;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

public sealed class UmadP1GravenImage1SettingsWindow
{
    public UmadP1GravenImage1StateOverrides Overrides { get; } = new();

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        if (SettingsGrid.Begin("##p1gravenimage1"))
        {
            SettingsGrid.FightOnlyNote();
            UmadP1SettingsRows.TriState("Knockback tethers:", "gi1kb", "DPS", "Supports", Overrides.TetherDps, v => Overrides.TetherDps = v);
            UmadP1SettingsRows.IceRow("Ice 1 hits:", "gi1ice1", Overrides.Ice1RealOffset, v => Overrides.Ice1RealOffset = v);
            UmadP1SettingsRows.TriState("Ice 1 orb:", "gi1ice1lie", "Lie", "Truth", Overrides.Ice1IsLie, v => Overrides.Ice1IsLie = v);
            UmadP1SettingsRows.TriState("Fire:", "gi1fire", "Stack", "Spread", Overrides.FireIsStack, v => Overrides.FireIsStack = v);
            UmadP1SettingsRows.TriState("Fire orb:", "gi1firelie", "Lie", "Truth", Overrides.FireIsLie, v => Overrides.FireIsLie = v);
            UmadP1SettingsRows.IceRow("Ice 2 hits:", "gi1ice2", Overrides.Ice2RealOffset, v => Overrides.Ice2RealOffset = v);
            UmadP1SettingsRows.TriState("Ice 2 orb:", "gi1ice2lie", "Lie", "Truth", Overrides.Ice2IsLie, v => Overrides.Ice2IsLie = v);
            UmadP1SettingsRows.TriState("Thunder orb:", "gi1thunlie", "Lie", "Truth", Overrides.ThunderIsLie, v => Overrides.ThunderIsLie = v);
            UmadP1SettingsRows.TriState("Thunder flip:", "gi1thunflip", "Flipped", "Normal", Overrides.ThunderFlipped, v => Overrides.ThunderFlipped = v);
            UmadP1SettingsRows.ThunderOffsetRow("gi1thunoff", Overrides.ThunderRealOffset, v => Overrides.ThunderRealOffset = v);
#if DEBUG
            UmadP1SettingsRows.PropKnobs("gi1", Overrides.PropsBindDirector, v => Overrides.PropsBindDirector = v,
                Overrides.PropsForceActive, v => Overrides.PropsForceActive = v, Overrides.PropsBeatMode, v => Overrides.PropsBeatMode = v);
#endif
            SettingsGrid.End();
        }
    }

    private void ResetAll()
    {
        Overrides.TetherDps = null;
        Overrides.Ice1RealOffset = null;
        Overrides.Ice1IsLie = null;
        Overrides.FireIsStack = null;
        Overrides.FireIsLie = null;
        Overrides.Ice2RealOffset = null;
        Overrides.Ice2IsLie = null;
        Overrides.ThunderRealOffset = null;
        Overrides.ThunderFlipped = null;
        Overrides.ThunderIsLie = null;
        Overrides.PropsBindDirector = true;
        Overrides.PropsForceActive = false;
        Overrides.PropsBeatMode = PropBeatMode.ActorControl;
    }
}
