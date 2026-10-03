using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1GravenImage2;

// What UmadP1GravenImage2Ai's moves are for, per role, for the death recap.
internal static class UmadP1GravenImage2Playbook
{
    private const string FirstTethers = "NAUR §1.7 (0:05:24)";
    private const string SecondTethers = "NAUR §1.9 (0:06:50)";
    private const string Trap = "NAUR §1.10 (0:07:09)";

    public static readonly StratCue Uptime = UmadP1Playbook.Uptime("NAUR §1.6 (0:05:02)");
    public static readonly StratCue LightOfJudgment = UmadP1Playbook.LightOfJudgment("NAUR §1.11 (0:09:15)");

    public static StratCue FirstStack(UmadP1GravenImage2State state) => new(
        "Graven Image 2: first puddles",
        _ => "Everyone stacks on waymark A so the four Gravitas puddles land on top of each other; every Gravitas must hit all 8 or it wipes the party. "
             + $"{IceText(state.Ice)} So the stack sits on the {(state.Ice.HitsNorthEast ? "west" : "east")} side of A.",
        $"{FirstTethers} · wtfdig kefka_p1/8_graven2_first_puddle");

    public static readonly StratCue SecondStack = new(
        "Graven Image 2: second puddles",
        _ => "Everyone stacks on waymark C so the second set of puddles lands together in the south; every Gravitas must hit all 8 or it wipes the party.",
        $"{SecondTethers} · wtfdig kefka_p1/12_graven2_second_puddle");

    public static StratCue Spreads(UmadP1GravenImage2State state, int set) => new(
        $"Vitrophyre spreads (set {set + 1})",
        role => UmadP1Roles.IsDps(role) == state.PurpleDps[set] ? PurpleText(set) : YellowText(role, set),
        set == 0 ? $"{FirstTethers} · wtfdig kefka_p1/9_graven2_spreads" : $"{SecondTethers} · wtfdig kefka_p1/13_graven2_second_spreads");

    private static string PurpleText(int set)
        => $"Your tether is to the central (purple) statue, so your puddle dropped with the stack: go through the boss to the {(set == 0 ? "south" : "north")}, away from the spreads, and don't step back into a puddle.";

    private static string YellowText(PartyRole role, int set)
    {
        var fromNorth = set == 0;
        var east = UmadP1Roles.IsGroupOne(role) == fromNorth;
        var where = UmadP1Roles.IsTankOrMelee(role)
            ? $"to max melee on the {(east ? "east" : "west")}"
            : $"further out to the {(fromNorth ? "north" : "south")}-{(east ? "east" : "west")}";
        return $"Your tether is to the right (yellow) statue: a 5y spread that knocks anyone else away and sets off any puddle it touches. "
               + $"Group {(UmadP1Roles.IsGroupOne(role) ? 1 : 2)} fans {(UmadP1Roles.IsGroupOne(role) ? "left" : "right")} facing the boss; as {Label(role)} you go {where}, over 10y from the puddles.";
    }

    public static readonly StratCue Buster = new(
        "Revolting Ruin III",
        role => role == PartyRole.MainTank
            ? "You hold Kefka: stand north of him so both Revolting Ruin III cones point away from the party."
            : "Gather south of the boss, out of the tank's cones.",
        "NAUR §1.8 (0:06:29) · wtfdig kefka_p1/10_graven2_tankbuster");

    private static string CleaveText(UmadP1GravenImage2State state, int cleave)
        => state.CleaveWest[cleave]
            ? "The purple (left) orb glows, so the west half is cleaved: move to the east half."
            : "The yellow (right) orb glows, so the east half is cleaved: move to the west half.";

    public static StratCue FirstCleave(UmadP1GravenImage2State state) => new(
        "Half-room cleave 1",
        role => CleaveText(state, 0) + (role == PartyRole.MainTank ? " Stay north of the boss for the buster." : " Stay south of the boss, out of the tank's cones."),
        "NAUR §1.8 (0:06:29) · wtfdig kefka_p1/11_graven2_halfroom_cleave");

    public static StratCue SecondCleave(UmadP1GravenImage2State state) => new(
        "Half-room cleave 2",
        role => CleaveText(state, 1) + $" {Group(role)} wait {(UmadP1Roles.IsDps(role) ? "south" : "north")} of the boss, ready for the confetti.",
        $"{SecondTethers} · wtfdig kefka_p1/14_graven2_second_halfroom_cleave");

    public static StratCue Confetti(UmadP1GravenImage2State state) => new(
        "Double-trouble Trap 2 (confetti)",
        role => role == state.TrapSupport || role == state.TrapDps
            ? $"You hold the confetti: stand out at max melee to the {(UmadP1Roles.IsDps(role) ? "south" : "north")}, next to your side's puddles, so the knockback throws your group into the far puddles. Then step back into the puddles behind you."
            : $"Stack with the {Group(role)} inside the boss's hitbox on the {(UmadP1Roles.IsDps(role) ? "south" : "north")}: the confetti's knockback carries you across into the far puddles to pop them.",
        $"{Trap} · wtfdig kefka_p1/15_graven2_second_confetti_positions");

    public static StratCue Soak(UmadP1GravenImage2State state) => new(
        "Gravity III puddle soak",
        role =>
        {
            var holder = role == state.TrapSupport || role == state.TrapDps;
            var north = !UmadP1Roles.IsDps(role) == holder;
            return $"Four players per puddle set, and every puddle must be popped by 4 (Phase 1's puddle puzzle). "
                   + (holder ? $"As the holder, walk back into the {(north ? "north" : "south")} puddles behind you."
                             : $"The knockback carried you into the {(north ? "north" : "south")} puddles: stay until they pop.");
        },
        $"{Trap} · wtfdig kefka_p1/16_puddle_soaks");
}
