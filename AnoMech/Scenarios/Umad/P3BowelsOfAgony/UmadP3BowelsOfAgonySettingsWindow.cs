using System;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

public sealed class UmadP3BowelsOfAgonySettingsWindow
{
    public UmadP3BowelsOfAgonyStateOverrides Overrides { get; } = new();

    // Which seat the per-player rows are showing. UI state only; never broadcast.
    private PartyRole editingSeat = PartyRole.MainTank;

    private static readonly string[] WindSpotLabels = ["Random", "NE", "SE", "SW", "NW"];
    private static readonly int[] WindSpotValues = [3, 1, 7, 5];
    private static readonly string[] WaterLabels = ["Random", "Clockwise of wind", "Counter-clockwise of wind"];
    private static readonly string[] ShortLabels = ["Random", "Water (Dynamic Fluid)", "Fire (Entropy)"];
    private static readonly string[] ImplosionLabels = ["Random", "Latitudinal (sides first)", "Longitudinal (front/back first)"];
    private static readonly string[] DebuffLabels = ["Random", "None", "Dynamic Fluid", "Entropy"];
    private static readonly string[] WindLabels = ["Random", "Headwind", "Tailwind"];

    // Solo, the player's own picks sit in this panel; a host assigns seats from the Multiplayer
    // window.
    public void Draw()
    {
        var solo = !PerRole.SeatsActive;
        if (ImGui.Button("Auto"))
        {
            ResetAll();
            if (solo) ResetMine();
        }

        if (SettingsGrid.Begin("##p3bowels"))
        {
            SettingsGrid.Row("Wind crystal:");
            var wind = Overrides.WindSpot is { } w ? Array.IndexOf(WindSpotValues, w) + 1 : 0;
            SettingsGrid.ItemWidth(140);
            if (ImGui.Combo("##boawind", ref wind, WindSpotLabels, WindSpotLabels.Length))
                Overrides.WindSpot = wind == 0 ? null : WindSpotValues[wind - 1];

            SettingsGrid.Row("Water crystal:");
            var water = Overrides.WaterClockwise switch { true => 1, false => 2, null => 0 };
            SettingsGrid.ItemWidth(200);
            if (ImGui.Combo("##boawater", ref water, WaterLabels, WaterLabels.Length))
                Overrides.WaterClockwise = water switch { 1 => true, 2 => false, _ => null };

            SettingsGrid.Row("Short element:");
            var shortElement = Overrides.ShortElement switch { Element.Water => 1, Element.Fire => 2, _ => 0 };
            SettingsGrid.ItemWidth(200);
            if (ImGui.Combo("##boashort", ref shortElement, ShortLabels, ShortLabels.Length))
                Overrides.ShortElement = shortElement switch { 1 => Element.Water, 2 => Element.Fire, _ => null };

            SettingsGrid.Row("Implosion:");
            var implosion = Overrides.Latitudinal switch { true => 1, false => 2, null => 0 };
            SettingsGrid.ItemWidth(220);
            if (ImGui.Combo("##boaimplosion", ref implosion, ImplosionLabels, ImplosionLabels.Length))
                Overrides.Latitudinal = implosion switch { 1 => true, 2 => false, _ => null };

            if (solo) DrawPlayerRows();
            SettingsGrid.End();
        }
        // The Thunder III plan is drawn by UmadP3BowelsOfAgonyScenario.DrawMultiplayerSettings so it
        // stays editable while a multiplayer fight is running, as Black Hole's does.
    }

    public void DrawPerPlayer()
    {
        if (ImGui.Button("Auto")) ResetPerPlayer();
        if (SettingsGrid.Begin("##p3bowelsplayers"))
        {
            editingSeat = SettingsGrid.SeatRow("##boaseat", editingSeat);
            DrawPlayerRows();
            SettingsGrid.ForcedRecapRow("Elements set:", Overrides.Debuff);
            SettingsGrid.ForcedRecapRow("Winds set:", Overrides.Wind);
            SettingsGrid.End();
        }
        SettingsGrid.ConflictRows(Overrides.Validate());
    }

    private void DrawPlayerRows()
    {
        var whose = PerRole.SeatsActive ? "" : "Your ";

        SettingsGrid.Row($"{whose}element:");
        var debuff = Overrides.Debuff.Effective(editingSeat) switch
        {
            ElementDebuff.None => 1, ElementDebuff.DynamicFluid => 2, ElementDebuff.Entropy => 3, _ => 0,
        };
        SettingsGrid.ItemWidth(160);
        if (ImGui.Combo("##boadebuff", ref debuff, DebuffLabels, DebuffLabels.Length))
            Overrides.Debuff.Set(editingSeat, debuff switch
            {
                1 => ElementDebuff.None, 2 => ElementDebuff.DynamicFluid, 3 => ElementDebuff.Entropy, _ => null,
            });

        SettingsGrid.Row($"{whose}wind:");
        var wind = Overrides.Wind.Effective(editingSeat) switch { Wind.Headwind => 1, Wind.Tailwind => 2, _ => 0 };
        SettingsGrid.ItemWidth(140);
        if (ImGui.Combo("##boawinddebuff", ref wind, WindLabels, WindLabels.Length))
            Overrides.Wind.Set(editingSeat, wind switch { 1 => Wind.Headwind, 2 => Wind.Tailwind, _ => null });
    }

    private void ResetAll()
    {
        Overrides.WindSpot = null;
        Overrides.WaterClockwise = null;
        Overrides.ShortElement = null;
        Overrides.Latitudinal = null;
        Overrides.ThunderPlan = ThunderIIIAssignment.ShareMtFirst;
    }

    private void ResetPerPlayer()
    {
        Overrides.Debuff.Clear();
        Overrides.Wind.Clear();
    }

    // Solo's own picks only: a host's seat assignments are kept apart.
    private void ResetMine()
    {
        Overrides.Debuff.Mine = null;
        Overrides.Wind.Mine = null;
    }

    // Black Hole's planner for the buster after the short elements (hits at ~58.4s and ~61.5s).
    // Only the host's pick is read.
    public void DrawThunderIIIPlan()
    {
        ImGui.Separator();
        var mpGuest = Plugin.MultiplayerInstance is { IsConnected: true, IsHost: false };
        ImGui.TextUnformatted("Thunder III plan (planning tank bots will follow):");
        ImGui.BeginDisabled(mpGuest);
        ImGui.TextUnformatted("After the short elements (~58.4s):");
        ImGui.SameLine();
        DrawThunderIIIOption("MT invulns both##boathunder", ThunderIIIAssignment.MtInvulnsBoth);
        ImGui.SameLine();
        DrawThunderIIIOption("OT invulns both##boathunder", ThunderIIIAssignment.OtInvulnsBoth);
        ImGui.SameLine();
        DrawThunderIIIOption("Share, MT first##boathunder", ThunderIIIAssignment.ShareMtFirst);
        ImGui.SameLine();
        DrawThunderIIIOption("Share, OT first##boathunder", ThunderIIIAssignment.ShareOtFirst);
        ImGui.EndDisabled();
        if (mpGuest && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Only the host's plan is used in multiplayer.");
    }

    private void DrawThunderIIIOption(string label, ThunderIIIAssignment option)
    {
        if (ImGui.RadioButton(label, Overrides.ThunderPlan == option)) Overrides.ThunderPlan = option;
    }
}
