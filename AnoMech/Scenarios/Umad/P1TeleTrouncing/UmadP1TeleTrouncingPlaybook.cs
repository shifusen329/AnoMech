using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.P1Shared.UmadP1Playbook;

namespace AnoMech.Scenarios.Umad.P1TeleTrouncing;

// What UmadP1TeleTrouncingAi's moves are for, per role, for the death recap.
internal static class UmadP1TeleTrouncingPlaybook
{
    private const string Arrows = "NAUR §1.12 (0:09:32) · wtfdig kefka_p1/17_graven3_arrow_placement";

    private static string Name(TelePortentDirection direction) => direction.ToString().ToLowerInvariant();

    // The box side an arrow's teleporters line: each runs clockwise along it.
    private static string BoxSide(TelePortentDirection direction) => direction switch
    {
        TelePortentDirection.Right => "north",
        TelePortentDirection.Down => "east",
        TelePortentDirection.Left => "south",
        _ => "west",
    };

    public static StratCue Arrow(UmadP1TeleTrouncingState state, bool first) => new(
        first ? "Tele-trouncing: short arrow" : "Tele-trouncing: long arrow",
        role =>
        {
            var (shortArrow, longArrow) = state.ExpiryDirections(role);
            var box = "Every teleporter goes on one clockwise box (a 12y square), each lining the side its arrow runs along. ";
            if (shortArrow == longArrow)
                return box + $"Both your arrows point {Name(shortArrow)}, so both go on the {BoxSide(shortArrow)} side: "
                       + (first ? "the short one just counterclockwise of the cardinal" : "the long one on the cardinal")
                       + ". NAUR drops the short one on the cardinal first; only the order differs.";
            return box + $"Your arrows differ ({Name(shortArrow)} first, then {Name(longArrow)}), so you take the corner where the "
                   + $"{BoxSide(shortArrow)} and {BoxSide(longArrow)} sides meet, one teleporter each side of it, "
                   + (first ? "short arrow first." : "now the long one.");
        },
        Arrows);

    public static StratCue Confetti(UmadP1TeleTrouncingState state) => new(
        "Double-trouble Trap 3 (confetti)",
        role => (role == state.ConfettiStackSupport || role == state.ConfettiStackDps
                    ? "You hold the confetti: stand behind your group, "
                    : "Stack with your role on the boss ring, ")
                + $"{(UmadP1Roles.IsDps(role) ? "DPS south-east" : "Supports north-west")}, so the knockback throws the group through the boss and not into a teleporter.",
        "NAUR §1.13 (0:11:12) · wtfdig kefka_p1/18_graven3_third_confetti");

    private static string TetherSpot(PartyRole role) => role switch
    {
        PartyRole.MainTank => "north, just beyond the boss's hitbox",
        PartyRole.PhysRangedDps => "north, outside the teleporter box",
        PartyRole.OffTank => "west, just beyond the boss's hitbox",
        PartyRole.CasterDps => "west, outside the teleporter box",
        PartyRole.MeleeDpsA => "south, just beyond the boss's hitbox",
        PartyRole.RegenHealer => "south, outside the teleporter box",
        PartyRole.MeleeDpsB => "east, just beyond the boss's hitbox",
        _ => "east, outside the teleporter box",
    };

    public static readonly StratCue Tethers = new(
        "Graven Image 3: Sleep / Confused tethers",
        role => $"Whatever your tether, go to your fixed spot: {TetherSpot(role)}. Each cardinal pairs a Support with a DPS, "
                + "so the Confused one walks at the other across the teleporters and uses them; the Sleeping one just stands.",
        "NAUR §1.14 (0:11:30) · wtfdig kefka_p1/19_graven3_fixed_positions");

    private static string SpreadSpot(PartyRole role) => role switch
    {
        PartyRole.MeleeDpsA => "waymark 1",
        PartyRole.MainTank => "waymark 2",
        PartyRole.OffTank => "waymark 3",
        PartyRole.MeleeDpsB => "waymark 4",
        PartyRole.PhysRangedDps => "the north wall",
        PartyRole.CasterDps => "the west wall",
        PartyRole.RegenHealer => "the south wall",
        _ => "the east wall",
    };

    public static StratCue MysteryMagic3(UmadP1TeleTrouncingState state) => new(
        "Mystery Magic 3: fire + lightning + gaze",
        role =>
        {
            var fire = state.FireIsLie
                ? $"The fire orb is fake, so the {(state.FireShowsStackIcon ? "stack" : "spread")} marker means a {(state.FireIsStack ? "stack" : "spread")}. "
                : $"It's a real {(state.FireIsStack ? "stack" : "spread")}. ";
            var spot = state.FireIsStack
                ? $"{Group(role)} stack on waymark {(UmadP1Roles.IsDps(role) ? "1 (north-west)" : "3 (south-east)")}"
                : $"you spread at {SpreadSpot(role)}";
            var lightning = state.ThunderIsLie ? "the unmarked lines (the lightning orb is fake)" : "the marked lines";
            var gaze = state.GazeInverted ? "The gaze is fake (\"?\"): face the statue." : "The gaze is real: face away from the statue.";
            return $"{fire}As {Label(role)} {spot}, shifted off {lightning}. {gaze}";
        },
        "NAUR §1.15 (0:12:09) · wtfdig kefka_p1/20-22 (gaze static, spread, stack)");
}
