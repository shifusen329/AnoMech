using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P1Shared;

// Death-recap explanations shared by the P1 strats, after the NAUR guide (sections and video
// times) and the wtfdig images it checks against.
internal static class UmadP1Playbook
{
    public static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static string Side(PartyRole role) => UmadP1Roles.IsDps(role) ? "east" : "west";

    public static string Group(PartyRole role) => UmadP1Roles.IsDps(role) ? "DPS" : "Supports";

    // A quadrant's X/Z signs as a compass corner (north is -Z).
    public static string Corner(Vector2 signs) => (signs.X > 0f, signs.Y > 0f) switch
    {
        (true, true) => "south-east",
        (true, false) => "north-east",
        (false, true) => "south-west",
        (false, false) => "north-west",
    };

    public static string IceText(IceRoll ice)
    {
        var hit = ice.HitsNorthEast ? "north-east and south-west" : "north-west and south-east";
        return ice.IsLie
            ? $"The ice orb is fake, so the cones you see are the safe ones; the real cones hit {hit}."
            : $"The ice orb is real: the cones hit {hit}.";
    }

    public static StratCue Uptime(string section) => new(
        "Uptime",
        role => role == PartyRole.MainTank
            ? "You hold Kefka: stand north of him so his cones and Hyperdrive point away from the party."
            : $"Wait behind the boss, {Group(role)} on the south-{Side(role)} side, ready to split {Side(role)} for the next mechanic.",
        $"{section} · wtfdig kefka_p1/1_tankbuster");

    public static StratCue LightOfJudgment(string section) => new(
        "Light of Judgment + Hyperdrive",
        role => role == PartyRole.MainTank
            ? "You hold Kefka: Hyperdrive hits you three times with a 5y AoE, so stay north of him, away from everyone."
            : "Stay behind the boss, well clear of the tank: Hyperdrive's 5y AoE kills anyone else it catches.",
        section);
}
