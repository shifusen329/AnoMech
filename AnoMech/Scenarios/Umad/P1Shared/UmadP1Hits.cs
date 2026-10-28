using System.Collections.Generic;
using AnoMech.Core.Game.PartyMit;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Phase 1's damaging hits, raw on a physical DPS. The Graven Image's own hits come from the
// statue, so debuffs on Kefka don't reduce them.
public static class UmadP1Hits
{
    public const string FlagrantFire = "flagrant_fire";
    public const string WaveCannon = "wave_cannon";
    public const string Explosion = "explosion";
    public const string DoubleTroubleTrap = "double_trouble_trap";
    public const string LightOfJudgment = "light_of_judgment";
    public const string Hyperdrive = "hyperdrive";
    public const string Gravitas = "gravitas";
    public const string Vitrophyre = "vitrophyre";
    public const string GravityIII = "gravity_iii";
    public const string IdyllicWill = "idyllic_will";
    public const string IndulgentWill = "indulgent_will";

    public static readonly IReadOnlyList<MitHitDef> All =
    [
        new(FlagrantFire, "Flagrant Fire III", 208000f, UmadMitigation.Kefka,
            ActionIds: [UmadConstants.ActionId.FlagrantFireSpread, UmadConstants.ActionId.FlagrantFireStack]),
        new(WaveCannon, "Wave Cannon", 308000f, null, ActionIds: [UmadP1GravenConstants.ActionId.WaveCannon]),
        new(Explosion, "Explosion", 311000f, UmadMitigation.Kefka, ActionIds: [UmadP1GravenConstants.ActionId.Explosion]),
        new(DoubleTroubleTrap, "Double-trouble Trap", 168000f, UmadMitigation.Kefka, ActionIds: [UmadConstants.ActionId.DoubleTroubleTrapStack]),
        new(LightOfJudgment, "Light of Judgment", 293000f, UmadMitigation.Kefka, ActionIds: [UmadP1GravenConstants.ActionId.LightOfJudgment]),
        new(Hyperdrive, "Hyperdrive", 568000f, UmadMitigation.Kefka, TankBuster: true, Fresh: true, ActionIds: [UmadP1GravenConstants.ActionId.Hyperdrive]),
        new(Gravitas, "Gravitas", 53000f, null, ActionIds: [UmadP1GravenConstants.ActionId.Gravitas]),
        new(Vitrophyre, "Vitrophyre", 21000f, null, ActionIds: [UmadP1GravenConstants.ActionId.Vitrophyre]),
        new(GravityIII, "Gravity III", 62000f, UmadMitigation.Kefka, ActionIds: [UmadP1GravenConstants.ActionId.GravityIII]),
        new(IdyllicWill, "Idyllic Will", 209000f, null, ActionIds: [UmadConstants.ActionId.IdyllicWill]),
        new(IndulgentWill, "Indulgent Will", 206000f, null, ActionIds: [UmadConstants.ActionId.IndulgentWill]),
    ];
}
