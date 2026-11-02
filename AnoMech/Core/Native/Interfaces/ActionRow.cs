namespace AnoMech.Core.Native.Interfaces;

// The Action-sheet columns the sim reads. Omen paths are the Omen sheet's raw Path (usually a bare
// name under vfx/omen/eff/); null when the action has none.
public sealed record ActionRow(
    uint Id,
    string Name,
    float CastSeconds,
    CastType CastType,
    byte EffectRange,
    byte XAxisModifier,
    string? OmenPath,
    string? OmenAltPath,
    uint ActionCategory,
    bool CanTargetSelf,
    bool CanTargetParty,
    sbyte AttackType,
    // Aimed at a ground point: the server's ActionEffect then carries no animation target.
    bool TargetArea,
    bool IsPlayerAction,
    float RecastSeconds = 0f,
    // The action this one continues in a linear combo; 0 when it starts or isn't in one.
    uint ComboFrom = 0,
    bool PreservesCombo = false,
    byte PrimaryCostType = 0,
    ushort PrimaryCostValue = 0);
