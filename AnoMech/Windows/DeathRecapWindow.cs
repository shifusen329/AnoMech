using System.Numerics;
using AnoMech.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows;

// The death recap's text: why you died, where the strat had you, and why it had you there.
public sealed class DeathRecapWindow : Window
{
    private static readonly Vector4 HeadingColor = new(1f, 0.85f, 0.4f, 1f);
    private static readonly Vector4 MutedColor = new(0.65f, 0.65f, 0.65f, 1f);

    private readonly Game game;

    public DeathRecapWindow(Game game) : base("Why you died###AnoMechDeathRecap")
    {
        this.game = game;
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
        IsOpen = true;
    }

    public override bool DrawConditions() => game.Recap != null;

    public override void Draw()
    {
        if (game.Recap is not { } recap) return;
        var wrap = 420f * ImGuiHelpers.GlobalScale;
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrap);

        Section("Why you died");
        ImGui.TextUnformatted(recap.Cause);
        if (recap.Aoe is { } aoe && !recap.Cause.Contains(aoe.Name))
            ImGui.TextColored(MutedColor, $"Caught in {aoe.Name}.");
        if (recap.Progress is { } progress) ImGui.TextUnformatted(progress);

        Section("Where you should have been");
        if (recap.Strat?.Mechanic is { } mechanic) ImGui.TextUnformatted(mechanic);
        ImGui.TextUnformatted(recap.WhereText);
        if (recap.TimingText is { } timing) ImGui.TextColored(MutedColor, timing);

        if (recap.Strat?.Why is { Length: > 0 } why)
        {
            Section("Why there");
            ImGui.TextUnformatted(why);
            if (recap.Strat.Source is { Length: > 0 } source) ImGui.TextColored(MutedColor, source);
        }

        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (ImGui.Button(game.RestartWaitingOnRecap ? "Close & restart" : "Close")) game.CloseRecap();
    }

    private static void Section(string title)
    {
        ImGui.Spacing();
        ImGui.TextColored(HeadingColor, title);
        ImGui.Separator();
    }
}
