using System.Linq;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P2Trines;

using Geometry = UmadP2TrinesConstants.Geometry;

public sealed class UmadP2TrinesSettingsWindow
{
    public UmadP2TrinesStateOverrides Overrides { get; } = new();

    private static readonly string[] FirstSetLabels =
        ["Random", .. UmadP2TrinesState.FirstSets.Select(UmadP2TrinesState.Describe)];
    private static readonly string[] CentreLabels = ["Random (2 in 3 east)", "Points east", "Points west"];
    private static readonly string[] WingsLabels = ["Random", "Right wing (west safe)", "Left wing (east safe)"];

    public void Draw()
    {
        if (ImGui.Button("Auto")) ResetAll();
        ImGui.TextDisabled("Triangles: A is 11° clockwise of north, then B, C, D, E, F every 60°; O is the centre.");

        if (SettingsGrid.Begin("##p2trines"))
        {
            SettingsGrid.Row("First set:");
            var first = Overrides.FirstSet is { } f ? f + 1 : 0;
            SettingsGrid.ItemWidth(120);
            if (ImGui.Combo("##trinefirst", ref first, FirstSetLabels, FirstSetLabels.Length))
            {
                Overrides.FirstSet = first == 0 ? null : first - 1;
                if (Overrides.FirstSet is { } set && Overrides.SecondSet is { } s && UmadP2TrinesState.FirstSets[set].Contains(s))
                    Overrides.SecondSet = null;
            }

            SettingsGrid.Row("Second set:");
            var choices = Enumerable.Range(1, 6)
                .Where(t => Overrides.FirstSet is not { } set || !UmadP2TrinesState.FirstSets[set].Contains(t))
                .ToArray();
            var secondLabels = new[] { "Random" }.Concat(choices.Select(Geometry.Name)).ToArray();
            var second = Overrides.SecondSet is { } sec ? System.Array.IndexOf(choices, sec) + 1 : 0;
            SettingsGrid.ItemWidth(120);
            if (ImGui.Combo("##trinesecond", ref second, secondLabels, secondLabels.Length))
                Overrides.SecondSet = second == 0 ? null : choices[second - 1];

            SettingsGrid.Row("Centre triangle:");
            var centre = Overrides.CentrePointsEast switch { true => 1, false => 2, null => 0 };
            SettingsGrid.ItemWidth(180);
            if (ImGui.Combo("##trinecentre", ref centre, CentreLabels, CentreLabels.Length))
                Overrides.CentrePointsEast = centre switch { 1 => true, 2 => false, _ => null };

            SettingsGrid.Row("Wings of Destruction:");
            var wings = Overrides.WingsRight switch { true => 1, false => 2, null => 0 };
            SettingsGrid.ItemWidth(200);
            if (ImGui.Combo("##trinewings", ref wings, WingsLabels, WingsLabels.Length))
                Overrides.WingsRight = wings switch { 1 => true, 2 => false, _ => null };

            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void ResetAll()
    {
        Overrides.FirstSet = null;
        Overrides.SecondSet = null;
        Overrides.CentrePointsEast = null;
        Overrides.WingsRight = null;
    }
}
