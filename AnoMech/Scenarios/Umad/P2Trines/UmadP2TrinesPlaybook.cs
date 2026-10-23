using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P2Trines;

using Geometry = UmadP2TrinesConstants.Geometry;

// What UmadP2TrinesAi's moves are for, per role, for the death recap.
internal static class UmadP2TrinesPlaybook
{
    private const string LightOfJudgment = "NAUR §2.11 (0:24:55)";
    private const string Trine = "NAUR §2.12 (0:28:34)";
    private const string Wings = "NAUR §2.13 (0:29:10)";
    private const string Busters = "NAUR §2.14 (0:29:26)";
    private const string Embrace = "NAUR §2.15 (0:30:08)";

    private static readonly string[] Bearings = ["the middle", "north", "east-north-east", "south-east", "south", "west-south-west", "north-west"];

    private static string Where(int triangle) => $"{Geometry.Name(triangle)} ({Bearings[triangle]})";

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static readonly StratCue Opening = new(
        "Light of Judgment II",
        role => role == PartyRole.MainTank
            ? "Hold Kefka facing north from 6y north of him: Trine's triangles and the first Wings of Destruction are read off his facing. Light of Judgment II is a raidwide."
            : "Stack just south of Kefka: Light of Judgment II is a raidwide, and Kefka stays facing north for Trine.",
        LightOfJudgment);

    public static StratCue WingsDodge(UmadP2TrinesState state) => StratCue.ForAll(
        "Wings of Destruction (half room)",
        $"Kefka lights his {(state.WingsRight ? "right" : "left")} wing: it hits his {(state.WingsRight ? "right" : "left")} half, the {(state.WingsRight ? "east" : "west")}, so stand 2y into the "
        + $"{(state.WingsRight ? "west" : "east")} half beside him. That's inside the centre triangle, which drops last, so stay there until the first set of triangles explodes.",
        Wings);

    public static StratCue Spots(UmadP2TrinesState state) => new(
        "Trine",
        role => role switch
        {
            PartyRole.MainTank => $"Take the near Wings of Destruction buster: about 9y out to the {(state.CentrePointsEast ? "west" : "south-west")}, as close to Kefka as the centre triangle's corners allow (aim for its flat edge), "
                                  + "and clear of any triangle still to drop. It hits the closest player, so no one else may stand nearer.",
            PartyRole.OffTank => $"Take the far Wings of Destruction buster: 18.5y out in {Where(state.TankTriangle)}, the first first-set triangle counter-clockwise from the north-west. "
                                 + "It hits the farthest player, so no one else may stand farther.",
            _ => $"Go to {Where(state.PartyTriangle)}, the first first-set triangle clockwise from north, once it has exploded: inside it, well out but not at the wall. "
                 + $"The buster circles are on the tanks, so stay over 7y from {Label(PartyRole.MainTank)} and {Label(PartyRole.OffTank)}.",
        },
        Trine);

    public static readonly StratCue BusterSpots = new(
        "Wings of Destruction (tank busters)",
        role => role switch
        {
            PartyRole.MainTank => "Stay the closest player to Kefka: a 7y buster lands on the nearest player and leaves a vulnerability, so the off-tank must be well away from you.",
            PartyRole.OffTank => "Stay the farthest player from Kefka: a 7y buster lands on the farthest player and leaves a vulnerability, so keep clear of the main tank.",
            _ => "Stay in your triangle: the busters land on the nearest and the farthest player, and that must be the two tanks.",
        },
        Busters);

    public static readonly StratCue UltimateEmbrace = new(
        "Ultimate Embrace II",
        role => role switch
        {
            PartyRole.MainTank => "Ultimate Embrace II is a 5y shared tank buster on you: hold still for the off-tank to join you. Alone it needs an invuln.",
            PartyRole.OffTank => $"Join {Label(PartyRole.MainTank)} for Ultimate Embrace II, a 5y tank buster the two of you share.",
            _ => "Stay over 5y from the tanks: Ultimate Embrace II is their shared buster, lethal to anyone else.",
        },
        Embrace);
}
