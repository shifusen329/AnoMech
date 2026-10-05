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
        new(FlagrantFire, "Flagrant Fire III", 208000f, UmadMitigation.Kefka),
        new(WaveCannon, "Wave Cannon", 308000f, null),
        new(Explosion, "Explosion", 311000f, UmadMitigation.Kefka),
        new(DoubleTroubleTrap, "Double-trouble Trap", 168000f, UmadMitigation.Kefka),
        new(LightOfJudgment, "Light of Judgment", 293000f, UmadMitigation.Kefka),
        new(Hyperdrive, "Hyperdrive", 568000f, UmadMitigation.Kefka, TankBuster: true, Fresh: true),
        new(Gravitas, "Gravitas", 53000f, null),
        new(Vitrophyre, "Vitrophyre", 21000f, null),
        new(GravityIII, "Gravity III", 62000f, UmadMitigation.Kefka),
        new(IdyllicWill, "Idyllic Will", 209000f, null),
        new(IndulgentWill, "Indulgent Will", 206000f, null),
    ];
}
