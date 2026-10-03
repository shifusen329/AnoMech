using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using static AnoMech.Scenarios.Umad.P5Celestriad.UmadP5CelestriadConstants;

namespace AnoMech.Scenarios.Umad.P5Celestriad;

// What UmadP5CelestriadAi's moves are for, per role, for the death recap.
internal static class UmadP5CelestriadPlaybook
{
    private const string Celestriad = "NAUR §5.5 (1:06:58) · strat by RoarkGit";
    private const string Choice = "NAUR §5.6 (1:08:13) · strat by RoarkGit";

    private static string Name(CelestriadElement element)
        => element == CelestriadElement.Fire ? "fire" : element == CelestriadElement.Lightning ? "lightning" : "ice";

    private static string WithArticle(CelestriadElement element) => element == CelestriadElement.Ice ? "an ice" : $"a {Name(element)}";

    // The other role sharing this role's debuff (or lack of one): its tower partner every set.
    private static string WithPartner(UmadP5CelestriadState state, PartyRole role)
    {
        foreach (var (other, element) in state.PlayerDebuffElement)
            if (other != role && element == state.PlayerDebuffElement[role])
                return $" with {SettingsGrid.RoleLabel(other)}";
        return "";
    }

    private static string TowerText(UmadP5CelestriadState state, int set, PartyRole role)
    {
        var target = Name(state.ElementForSet(role, set));
        var partner = WithPartner(state, role);
        if (state.PlayerDebuffElement[role] is not { } own)
            return $"You have no vulnerability, so you fill {target}, the element with two lit towers this set: "
                   + $"take its clockwise tower (right, facing out){partner}; the debuffed pair takes the other.";

        var rotate = set switch
        {
            0 => $"You carry {WithArticle(own)} vulnerability, so {WithArticle(own)} tower kills you: go one element clockwise, to {target}.",
            1 => $"Rotate one more element clockwise, to {target}: the tower you just soaked left you vulnerable to its element.",
            _ => $"Rotate one more element clockwise, back to {target}: your first {Name(own)} vulnerability runs out just before this set resolves.",
        };
        var tower = state.ElementForSet(role, set) == state.DoubleElement[set]
            ? $"It has two lit towers: take the counterclockwise one (left, facing out){partner}; the free players take the other."
            : $"Soak its lit tower{partner}, each of you 1y off its centre.";
        return $"{rotate} {tower}";
    }

    public static StratCue Towers(UmadP5CelestriadState state, int set) => new(
        $"Celestriad set {set + 1} towers",
        role => TowerText(state, set, role)
                + (CelestriadTiming.CcAt[set] is null ? "" : " Hold there until Catastrophic Choice shows which half of the tower is safe."),
        Celestriad);

    public static StratCue CatastrophicChoiceHalf(UmadP5CelestriadState state, int set) => new(
        $"Celestriad set {set + 1}: Catastrophic Choice",
        role =>
        {
            var target = Name(state.ElementForSet(role, set));
            return state.AeroVariant[set] == CatastrophicChoice.Aero
                ? $"Kefka's choice is Aero (green): this strat reads it as a point-blank AoE and steps to the outer half of your {target} tower. "
                  + "NAUR doesn't say where to stand, and its notes make wind the donut, which would mean the inner half."
                : $"Kefka's choice is Earth (brown): this strat reads it as a donut and steps to the inner half of your {target} tower, toward him. "
                  + "NAUR doesn't say where to stand, and its notes make earth the point-blank, which would mean the outer half.";
        },
        Choice);
}
