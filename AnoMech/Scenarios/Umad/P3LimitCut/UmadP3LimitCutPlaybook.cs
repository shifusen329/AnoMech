using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;

namespace AnoMech.Scenarios.Umad.P3LimitCut;

// What UmadP3LimitCutAi's moves are for, per role, for the death recap.
internal static class UmadP3LimitCutPlaybook
{
    private const string Umbra = "NAUR §3.6 (0:36:46)";
    private const string Vacuum = "NAUR §3.8 (0:37:10)";
    private const string Thunder = "NAUR §3.9 (0:38:27) · §3.4 (0:35:00)";

    // Spot index order: 0 = S, then on toward E.
    private static readonly string[] SpotWords = ["south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"];

    private static string Spot(int spot) => SpotWords[((spot % 8) + 8) % 8];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    public static StratCue Stack(UmadP3LimitCutState state) => new(
        "Umbra Smash: stack by the bosses",
        role =>
        {
            var corner = Spot(state.BossSpot);
            return role switch
            {
                PartyRole.MainTank => $"Hold Chaos on the {corner} intercardinal with Exdeath beside him and the party stacked just inside: Umbra Smash goes to whoever is farthest from Chaos, and that must be the bait alone.",
                PartyRole.OffTank => $"Hold Exdeath beside Chaos on the {corner} intercardinal, the party stacked just inside: Umbra Smash goes to whoever is farthest from Chaos, and that must be the bait alone.",
                _ when role == state.BaitRole => $"Stack with the party 5.5y out toward the bosses on the {corner} intercardinal for now: you run across the arena to bait Umbra Smash next.",
                _ => $"Stack 5.5y out toward the bosses on the {corner} intercardinal: Umbra Smash goes to whoever is farthest from Chaos, and that must be {Label(state.BaitRole)}, the bait, alone.",
            };
        },
        Umbra);

    public static StratCue UmbraBait(UmadP3LimitCutState state) => new(
        "Umbra Smash: bait",
        role => role == state.BaitRole
            ? $"You bait Umbra Smash: it goes to whoever is farthest from Chaos when he starts casting and kills anyone within 20y of where it lands, so stand at the {Spot(state.BossSpot + 4)} wall, across the arena from Chaos and the stack."
            : $"{Label(state.BaitRole)} baits Umbra Smash at the {Spot(state.BossSpot + 4)} wall; stay in the stack by the bosses, over 20y from where it lands.",
        Umbra);

    public static StratCue BaitBack(UmadP3LimitCutState state) => new(
        "Umbra Smash: back to the stack",
        role => role == state.BaitRole
            ? "Umbra Smash locked onto where you stood when the cast began, and Chaos lands there: run back into the stack now, over 20y from the impact."
            : $"{Label(state.BaitRole)} comes back from the Umbra Smash bait; stay in the stack, over 20y from where it lands.",
        Umbra);

    public static readonly StratCue TanksIntoStack = new(
        "Umbra Smash: tanks join the stack",
        role => role.IsTank()
            ? "Umbra Smash has locked onto the bait, so drop the bosses and join the stack: the whole party then takes Vacuum Wave's knockback from Exdeath together."
            : "Umbra Smash has locked onto the bait; the tanks join your stack, so the whole party takes Vacuum Wave's knockback from Exdeath together.",
        Umbra);

    public static StratCue VacuumWave(UmadP3LimitCutState state) => new(
        "Vacuum Wave",
        role => state.Winds[role] == Wind.Headwind
            ? "You have Headwind: face away from Exdeath, so Vacuum Wave hits your back, cleanses it and only pushes you 10y. More than 45° off throws you 40y off the arena; the tank LB3 covers the damage."
            : "You have Tailwind: face Exdeath, so Vacuum Wave hits your front, cleanses it and only pushes you 10y. More than 45° off throws you 40y off the arena; the tank LB3 covers the damage.",
        Vacuum);

    public static readonly StratCue Cyclones = StratCue.ForAll(
        "Cyclones",
        "Every wind carried into Vacuum Wave fires a Cyclone, a two-player stack whose vulnerability the next one lands on. Everyone packs together just past the middle, away from Exdeath, to split all of them under the tank LB3.",
        Vacuum);

    public static StratCue Charges(UmadP3LimitCutState state) => new(
        "Limit cut: Ultima Blaster charges",
        role =>
        {
            var number = state.NumberOf(role);
            if (number == 0) return "Every clone charges straight at its number: stand at the wall across from the clone with your number.";
            var from = state.ChargeSpot(number - 1);
            var fill = state.Clockwise ? "counterclockwise" : "clockwise";
            var other = number > 4 ? number - 4 : number + 4;
            return $"You're number {number}: clone {number} charges at you from the {Spot(from)} spot, deadly unless you're far away. "
                   + $"Stand at the wall across from it, a half-step {fill} of the {Spot(from + 4)} spot, since clone {other} charges from that spot itself. "
                   + $"Number 1 starts across from the first clone ({Spot(state.StartSpot)}) and the numbers fill {fill}, against the clones' turn.";
        },
        "NAUR §3.9 (0:37:50)");

    public static StratCue ClearForThunder(UmadP3LimitCutState state) => new(
        "Thunder III: clear Exdeath",
        role =>
        {
            var (first, second) = ThunderIIIPlanning.Roles(state.ThunderPlan);
            var plan = $"Exdeath's Thunder III hits whoever is closest to him twice, 3s apart, and {UmadP3BlackHolePlaybook.ThunderPlan(state.ThunderPlan)}";
            if (role == first) return $"{plan}: walk onto him.";
            if (role == second) return $"{plan}: wait on the {Spot(state.BossSpot)} intercardinal, clear of the first hit, until you swap in.";
            if (role.IsTank()) return $"{plan}: you're not on it, so wait on the {Spot(state.BossSpot)} intercardinal, over 10y from him.";
            return $"{plan}: group up 4y from the middle, away from the {Spot(state.BossSpot)} corner, over 10y from him.";
        },
        Thunder);

    public static StratCue ThunderTank(UmadP3LimitCutState state) => UmadP3BlackHolePlaybook.ThunderTank(state.ThunderPlan, Thunder);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting) => UmadP3BlackHolePlaybook.ThunderClearance(holder, waiting, Thunder);

    public static StratCue ThunderSwap(UmadP3LimitCutState state) => UmadP3BlackHolePlaybook.ThunderSwap(state.ThunderPlan, Thunder);

    public static StratCue Uptime(UmadP3LimitCutState state) => new(
        "The Decisive Battle (second)",
        role => $"Back to the bosses on the {Spot(state.BossSpot)} intercardinal: "
                + (role switch
                {
                    PartyRole.MainTank => "you hold Chaos",
                    PartyRole.OffTank => "you hold Exdeath",
                    _ => "stack 5.5y out beside them",
                })
                + ". The Decisive Battle locks MT, H1 and both melee to Chaos and the rest to Exdeath.",
        "NAUR §3.10 (0:39:41)");
}
