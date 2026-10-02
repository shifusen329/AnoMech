using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

// Per-run rolls for Graven Image 1 through Hyperdrive. Wave Cannon always takes 2 Supports and
// 2 DPS; which ones is UNVERIFIED (random here).
public sealed class UmadP1GravenImage1State
{
    public bool TetherDps { get; }
    public IceRoll Ice1 { get; }
    public FireRoll Fire { get; }
    public IReadOnlyList<PartyRole> WaveTargets { get; }
    public PartyRole TrapSupport { get; }
    public PartyRole TrapDps { get; }
    // Which of the 3 players a trap stack hit it jumps to (by role order).
    public int TrapJumpSupport { get; }
    public int TrapJumpDps { get; }
    public IceRoll Ice2 { get; }
    public ThunderRoll Thunder { get; }

    public IEnumerable<PartyRole> TetheredRoles => UmadP1Roles.Of(TetherDps);

    public UmadP1GravenImage1State(Rng rng, UmadP1GravenImage1StateOverrides overrides)
    {
        TetherDps = overrides.TetherDps ?? rng.NextBool();
        Ice1 = IceRoll.Roll(rng, overrides.Ice1RealOffset, overrides.Ice1IsLie);
        Fire = FireRoll.Roll(rng, overrides.FireIsStack, overrides.FireIsLie);
        WaveTargets = [.. rng.Shuffle(UmadP1Roles.Supports).Take(2), .. rng.Shuffle(UmadP1Roles.Dps).Take(2)];
        TrapSupport = rng.NextSupportRole();
        TrapDps = rng.NextDpsRole();
        TrapJumpSupport = rng.NextInt(3);
        TrapJumpDps = rng.NextInt(3);
        Ice2 = IceRoll.Roll(rng, overrides.Ice2RealOffset, overrides.Ice2IsLie);
        Thunder = ThunderRoll.Roll(rng, overrides.ThunderRealOffset, overrides.ThunderFlipped, overrides.ThunderIsLie);
    }

    // A peer's shadow of the host's roll (see IMultiplayerReplayable): nothing rolled.
    private UmadP1GravenImage1State(bool tetherDps, IceRoll ice1, FireRoll fire, IReadOnlyList<PartyRole> waveTargets,
        PartyRole trapSupport, PartyRole trapDps, int trapJumpSupport, int trapJumpDps, IceRoll ice2, ThunderRoll thunder)
    {
        TetherDps = tetherDps;
        Ice1 = ice1;
        Fire = fire;
        WaveTargets = waveTargets;
        TrapSupport = trapSupport;
        TrapDps = trapDps;
        TrapJumpSupport = trapJumpSupport;
        TrapJumpDps = trapJumpDps;
        Ice2 = ice2;
        Thunder = thunder;
    }

    public static UmadP1GravenImage1State? FromNetworkReplay(UmadP1GravenImage1AiReplayStateMessage m)
    {
        PartyRole[] roles = [m.FireStackSupport, m.FireStackDps, m.TrapSupport, m.TrapDps, .. m.WaveTargets];
        if (m.WaveTargets.Length != 4 || m.WaveTargets.Distinct().Count() != 4 || roles.Any(r => !Enum.IsDefined(r))) return null;
        if (new[] { m.Ice1RealOffset, m.Ice2RealOffset, m.ThunderRealOffset }.Any(o => o is < 0 or > 1)) return null;
        if (m.TrapJumpSupport is < 0 or > 2 || m.TrapJumpDps is < 0 or > 2) return null;
        return new(m.TetherDps, new IceRoll(m.Ice1RealOffset, m.Ice1IsLie),
            new FireRoll(m.FireIsStack, m.FireIsLie, m.FireStackSupport, m.FireStackDps), m.WaveTargets,
            m.TrapSupport, m.TrapDps, m.TrapJumpSupport, m.TrapJumpDps,
            new IceRoll(m.Ice2RealOffset, m.Ice2IsLie), new ThunderRoll(m.ThunderRealOffset, m.ThunderFlipped, m.ThunderIsLie));
    }

    public UmadP1GravenImage1AiReplayStateMessage ToReplayMessage()
        => new(TetherDps, Ice1.RealOffset, Ice1.IsLie, Fire.IsStack, Fire.IsLie, Fire.StackSupport, Fire.StackDps,
            [.. WaveTargets], TrapSupport, TrapDps, TrapJumpSupport, TrapJumpDps,
            Ice2.RealOffset, Ice2.IsLie, Thunder.RealOffset, Thunder.Flipped, Thunder.IsLie);
}
