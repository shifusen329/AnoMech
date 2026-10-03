using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1GravenImage1;

// What UmadP1GravenImage1Ai's moves are for, per role, for the death recap.
internal static class UmadP1GravenImage1Playbook
{
    private const string Gi1 = "NAUR §1.2 (0:00:48)";
    private const string Beams = "NAUR §1.3 (0:02:31) · wtfdig kefka_p1/5_graven1_laser_positions";

    public static readonly StratCue Uptime = UmadP1Playbook.Uptime("NAUR §1.1 (0:00:28)");
    public static readonly StratCue LightOfJudgment = UmadP1Playbook.LightOfJudgment("NAUR §1.6 (0:05:02)");

    private static string FireText(UmadP1GravenImage1State state)
    {
        var shown = state.Fire.ShowsStackIcon ? "stack" : "spread";
        var actual = state.Fire.IsStack ? "stack" : "spread";
        return state.Fire.IsLie
            ? $"The fire orb is fake, so the {shown} marker means a {actual}."
            : $"The fire orb is real: it's a {actual}.";
    }

    private static string SpreadSpot(PartyRole role) => ((int)role % 4) switch
    {
        0 => "the north or south edge of the safe quadrant, by the boss's hitbox",
        1 => "the east or west edge of the safe quadrant, by the boss's hitbox",
        2 => "further in, between the wall and the hitbox",
        _ => "at the wall on the east-west line",
    };

    private static string QuadrantSpotText(UmadP1GravenImage1State state, PartyRole role)
    {
        var quadrant = Corner(state.Ice1.SafeQuadrant(west: !UmadP1Roles.IsDps(role)));
        var spot = state.Fire.IsStack
            ? $"stack with the {Group(role)} on the boss's hitbox in the {quadrant} quadrant"
            : $"as {Label(role)}, spread {SpreadSpot(role)} ({quadrant})";
        return $"{IceText(state.Ice1)} {FireText(state)} {Group(role)} take the {Side(role)} safe quadrant: {spot}.";
    }

    public static StratCue KnockbackTether(UmadP1GravenImage1State state) => new(
        "Graven Image 1: knockback tether",
        role => "You're tethered: Pulse Wave throws you 13y straight away from the statue in the north, "
                + $"so you stand where it lands you on your spot. {QuadrantSpotText(state, role)}",
        $"{Gi1} · wtfdig kefka_p1/2_graven1");

    public static StratCue MysteryMagic1(UmadP1GravenImage1State state) => new(
        "Mystery Magic 1: fire + ice",
        role => QuadrantSpotText(state, role),
        $"{Gi1} · wtfdig kefka_p1/{(state.Fire.IsStack ? "3_graven1_stack_positions" : "4_graven1_spread_positions")}");

    public static readonly StratCue WaveCannonLineup = new(
        "Wave Cannon lineup",
        role => $"Line up west to east H2, H1, OT, MT | M1, M2, R1, R2, so each beam from the statue hits only its own target. You're {Label(role)}.",
        Beams);

    public static StratCue Towers(UmadP1GravenImage1State state) => new(
        "Wave Cannon towers",
        role => state.WaveTargets.Contains(role)
            ? "A beam hit you, leaving Magic Vulnerability Up: soaking a tower now kills you, so step clear of every tower."
            : $"No beam hit you, so you soak a {Group(role)} tower on the {Side(role)} side: going west to east, the outermost unhit player takes the outermost tower.",
        Beams);

    public static StratCue Confetti(UmadP1GravenImage1State state) => new(
        "Double-trouble Trap 1 (confetti)",
        role => role == state.TrapSupport || role == state.TrapDps
            ? $"You hold the confetti: stand behind your group at max melee on the {Side(role)}, so its knockback throws them through the boss."
            : $"Stack with the {Group(role)} on the boss's hitbox on the {Side(role)}: the confetti needs exactly 4 and its knockback carries you across.",
        "NAUR §1.4 (0:03:09) · wtfdig kefka_p1/6_graven1_confetti_positions");

    public static StratCue MysteryMagic2(UmadP1GravenImage1State state) => new(
        "Mystery Magic 2: lightning + ice",
        _ => $"Right after the confetti knockback, step to the nearest spot clear of the ice and the lightning. {IceText(state.Ice2)} "
             + (state.Thunder.IsLie
                 ? "The lightning orb is fake: the lines you see are safe and the unmarked ones hit."
                 : "The lightning orb is real: dodge the lines you see."),
        "NAUR §1.5 (0:03:26) · wtfdig kefka_p1/7_graven1_lightning_ice_resolution");
}
