using System;
using System.Collections.Generic;
using AnoMech.Core.Game.Party;
using AnoMech.Multiplayer;
using AnoMech.Scenarios.Umad.P1Shared;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

// Per-run rolls for Graven Image 2 through Hyperdrive. The two tether sets and the two cleaves
// roll independently.
public sealed class UmadP1GravenImage2State
{
    public IReadOnlyList<bool> PurpleDps { get; }
    public IceRoll Ice { get; }
    public IReadOnlyList<bool> CleaveWest { get; }
    // Holding Double-trouble Trap from the first trap's jump.
    public PartyRole TrapSupport { get; }
    public PartyRole TrapDps { get; }

    public IEnumerable<PartyRole> PurpleRoles(int set) => UmadP1Roles.Of(PurpleDps[set]);
    public IEnumerable<PartyRole> YellowRoles(int set) => UmadP1Roles.Of(!PurpleDps[set]);

    public UmadP1GravenImage2State(Rng rng, UmadP1GravenImage2StateOverrides overrides)
    {
        PurpleDps = [overrides.PurpleDps1 ?? rng.NextBool(), overrides.PurpleDps2 ?? rng.NextBool()];
        Ice = IceRoll.Roll(rng, overrides.IceRealOffset, overrides.IceIsLie);
        CleaveWest = [overrides.CleaveWest1 ?? rng.NextBool(), overrides.CleaveWest2 ?? rng.NextBool()];
        TrapSupport = rng.NextSupportRole();
        TrapDps = rng.NextDpsRole();
    }

    // A peer's shadow of the host's roll (see IMultiplayerReplayable): nothing rolled.
    private UmadP1GravenImage2State(IReadOnlyList<bool> purpleDps, IceRoll ice, IReadOnlyList<bool> cleaveWest, PartyRole trapSupport, PartyRole trapDps)
    {
        PurpleDps = purpleDps;
        Ice = ice;
        CleaveWest = cleaveWest;
        TrapSupport = trapSupport;
        TrapDps = trapDps;
    }

    public static UmadP1GravenImage2State? FromNetworkReplay(UmadP1GravenImage2AiReplayStateMessage m)
    {
        if (m.PurpleDps.Length != 2 || m.CleaveWest.Length != 2 || m.IceRealOffset is < 0 or > 1) return null;
        if (!Enum.IsDefined(m.TrapSupport) || !Enum.IsDefined(m.TrapDps)) return null;
        return new(m.PurpleDps, new IceRoll(m.IceRealOffset, m.IceIsLie), m.CleaveWest, m.TrapSupport, m.TrapDps);
    }

    public UmadP1GravenImage2AiReplayStateMessage ToReplayMessage()
        => new([.. PurpleDps], Ice.RealOffset, Ice.IsLie, [.. CleaveWest], TrapSupport, TrapDps);
}
