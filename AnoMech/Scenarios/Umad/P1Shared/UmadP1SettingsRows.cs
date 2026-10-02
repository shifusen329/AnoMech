using System;
using AnoMech.Core.SimObjects;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Settings rows the Graven Image panels share, in Tele-trouncing's Auto / A / B style.
internal static class UmadP1SettingsRows
{
    public static void TriState(string label, string id, string trueLabel, string falseLabel, bool? value, Action<bool?> set)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Auto##{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{trueLabel}##{id}", value == true)) set(true);
        ImGui.SameLine();
        if (ImGui.RadioButton($"{falseLabel}##{id}", value == false)) set(false);
    }

    // IceRoll.RealOffset 1 = NE + SW, 0 = NW + SE.
    public static void IceRow(string label, string id, int? value, Action<int?> set)
    {
        SettingsGrid.Row(label);
        if (ImGui.RadioButton($"Auto##{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"NE + SW##{id}", value == 1)) set(1);
        ImGui.SameLine();
        if (ImGui.RadioButton($"NW + SE##{id}", value == 0)) set(0);
    }

    public static void ThunderOffsetRow(string id, int? value, Action<int?> set)
    {
        SettingsGrid.Row("Thunder reals:");
        if (ImGui.RadioButton($"Auto##{id}", value == null)) set(null);
        ImGui.SameLine();
        if (ImGui.RadioButton($"Slots 0,2##{id}", value == 0)) set(0);
        ImGui.SameLine();
        if (ImGui.RadioButton($"Slots 1,3##{id}", value == 1)) set(1);
    }

#if DEBUG
    private static readonly string[] BeatModeLabels = ["ActorControl 413", "PlayAnimation", "SetSharedTimelineState"];

    public static void PropKnobs(string id, bool bind, Action<bool> setBind, bool force, Action<bool> setForce,
        PropBeatMode mode, Action<PropBeatMode> setMode)
    {
        SettingsGrid.Row("Statue props (debug):");
        if (ImGui.Checkbox($"Bind to director##{id}bind", ref bind)) setBind(bind);
        ImGui.SameLine();
        if (ImGui.Checkbox($"Force SG active##{id}force", ref force)) setForce(force);
        ImGui.SameLine();
        var modeIdx = (int)mode;
        SettingsGrid.ItemWidth(170);
        if (ImGui.Combo($"##{id}beatmode", ref modeIdx, BeatModeLabels, BeatModeLabels.Length)) setMode((PropBeatMode)modeIdx);
    }
#endif
}
