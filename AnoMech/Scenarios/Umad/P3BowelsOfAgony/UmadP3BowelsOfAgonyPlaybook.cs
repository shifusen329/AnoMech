using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

// What UmadP3BowelsOfAgonyAi's moves are for, per role, for the death recap.
internal static class UmadP3BowelsOfAgonyPlaybook
{
    private const string Decisive = "NAUR §3.1 (0:32:15)";
    private const string Bowels = "NAUR §3.2 (0:32:54)";
    private const string ShortResolve = "NAUR §3.3 (0:34:17)";
    private const string Thunder = "NAUR §3.4 (0:35:00)";
    private const string Implosion1 = "NAUR §3.5 (0:35:25)";
    private const string Sg3k = "Icy Veins P3 · raidplan hbgw9v52x9syetck";

    private static readonly string[] SpotWords = ["south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"];

    private static string Spot(int spot) => SpotWords[((spot % 8) + 8) % 8];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string CrystalName(Element element) => element == Element.Water ? "water" : "fire";

    private static string DebuffName(Element element) => element == Element.Water ? "Dynamic Fluid" : "Entropy";

    private static string CrystalShot(Element element) => element == Element.Water ? "Tsunami" : "Inferno";

    private static bool IsRanged(PartyRole role) => role is PartyRole.PhysRangedDps or PartyRole.CasterDps;

    private static string WindFacing(UmadP3BowelsOfAgonyState state, PartyRole role, string source)
        => state.Winds[role] == Wind.Headwind ? $"you have Headwind, so face away from {source}" : $"you have Tailwind, so face {source}";

    public static readonly StratCue DecisiveBattleSplit = new(
        "The Decisive Battle",
        role => role is PartyRole.MainTank or PartyRole.RegenHealer or PartyRole.MeleeDpsA or PartyRole.MeleeDpsB
            ? "Stand by Chaos on the west side: the four players nearest him get Epic Hero, which locks you to him, and the rest get Fated Hero for Exdeath."
            : "Stand by Exdeath on the east side: the four players nearest Chaos get Epic Hero, and you four get Fated Hero, which locks you to Exdeath.",
        Decisive);

    public static readonly StratCue BossesToTheMiddle = new(
        "Bowels of Agony",
        role => role switch
        {
            PartyRole.MainTank => "Pull Chaos onto the middle: stand east of it so he walks to the centre. Bowels of Agony is a raidwide that hands out the debuffs.",
            PartyRole.OffTank => "Pull Exdeath onto the middle: stand west of it so he walks to the centre. Bowels of Agony is a raidwide that hands out the debuffs.",
            _ => "Hit your boss in the middle. Bowels of Agony is a raidwide that hands out Headwind or Tailwind to everyone and an element to two pairs.",
        },
        Bowels);

    public static StratCue Lb3Setup(UmadP3BowelsOfAgonyState state) => new(
        "Bowels of Agony: crystals (LB3)",
        role =>
        {
            var wind = Spot(state.WindSpot);
            if (role == PartyRole.MainTank) return $"Drag Chaos to the {wind} wind crystal: stand at that wall so he sits about 10y out, facing it.";
            if (role == PartyRole.OffTank) return "Keep Exdeath in the middle: he casts Thunder III there next, and you leave him for the wall when he starts.";
            if (IsRanged(role))
                return $"Go to the {Spot(state.ShortSpot)} {CrystalName(state.ShortElement)} crystal, the short one: its {CrystalShot(state.ShortElement)} fires at the two players nearest it, and that must be you two, 12y apart.";
            return $"Stack at the wall behind Chaos by the {wind} wind crystal, out of Exdeath's Thunder III. LB3 keeps every wind debuff for Limit Cut's Vacuum Wave, so stay away from the crystals.";
        },
        Bowels);

    public static StratCue Lb3ThunderAoe(UmadP3BowelsOfAgonyState state) => new(
        "Thunder III (large AoE)",
        role => role == PartyRole.OffTank
            ? $"Exdeath is casting Thunder III, an AoE that kills anyone within about 15y of him: leave him in the middle and join the stack at the {Spot(state.WindSpot)} wall."
            : "Exdeath is casting Thunder III, an AoE that kills anyone within about 15y of him: stay out at the wall.",
        ShortResolve);

    public static StratCue Lb3Resolve(UmadP3BowelsOfAgonyState state, Element element) => new(
        element == state.ShortElement ? $"Short {CrystalName(element)} (LB3)" : $"Long {CrystalName(element)} (LB3)",
        role =>
        {
            var crystal = $"{Spot(state.SpotOf(element))} {CrystalName(element)} crystal";
            var holds = state.ElementOf(role) == element;
            if (IsRanged(role))
            {
                var spot = element == state.ShortElement
                    ? $"Stand by the {crystal}, 12y from the other ranged"
                    : $"Stand on the line from Chaos's back toward the {crystal}, one of you near and one far, over 10y apart";
                var own = holds ? $" Your {DebuffName(element)} runs out here too, clear of everyone." : "";
                return $"{spot}: its {CrystalShot(element)} fires at the two players nearest it, and that must be you two.{own}";
            }
            if (element == Element.Water)
                return holds
                    ? "Your Dynamic Fluid runs out: its donut misses anyone within 4y of you, so stay in the tight stack at the wall."
                    : "Stay in the tight stack at the wall: the Dynamic Fluid donuts miss anyone within 4y of their holder.";
            var side = UmadP3BowelsOfAgonyState.IsSupport(role) ? "left" : "right";
            return holds
                ? $"Your Entropy runs out, a 5y circle on you: step out to the {side} of your group along the wall, over 5y from everyone."
                : $"Supports stack left and DPS right, facing Chaos, so you're in the {side} group; the Entropy holders step out past you.";
        },
        element == state.ShortElement ? ShortResolve : "NAUR §3.5 (0:35:48)");

    public static readonly StratCue Lb3Regroup = new(
        "Thunder III: keep clear",
        role => IsRanged(role)
            ? "Stay by the crystal, over 10y from Exdeath: his Thunder III buster hits whoever is closest to him."
            : "Back into one stack at the wall, over 10y from Exdeath: his Thunder III buster hits whoever is closest to him.",
        Thunder);

    public static StratCue Lb3PreImplosion(UmadP3BowelsOfAgonyState state) => new(
        "Implosion: set up",
        role =>
        {
            if (role == PartyRole.MainTank) return $"Back to the {Spot(state.WindSpot)} wall, so Chaos faces it when he casts Implosion.";
            if (role == PartyRole.OffTank) return $"Drag Exdeath to the {Spot(state.WindSpot)} wind crystal, onto Chaos's spot about 10y out, and leave him there through Implosion: Limit Cut starts with both bosses on that corner.";
            if (IsRanged(role)) return $"Cross to the long {CrystalName(state.LongElement)} crystal ({Spot(state.LongSpot)}): you dodge Implosion there and then take its shots.";
            return "Stay at the wall by Chaos: you dodge Implosion close to him.";
        },
        Implosion1);

    public static StratCue Implosion(UmadP3BowelsOfAgonyState state, UmadP3BowelsOfAgonyAi.Strat strat, bool firstHit) => new(
        firstHit ? "Implosion (first cleave)" : "Implosion (second cleave)",
        role =>
        {
            var sidesHit = firstHit == state.Latitudinal;
            var name = state.Latitudinal ? "Latitudinal" : "Longitudinal";
            var hit = sidesHit ? "his sides" : "his front and back";
            var safe = sidesHit ? "in front of or behind him" : "beside him";
            var rule = $"{name} Implosion: this cleave hits {hit} (two 90° cones), so stand {safe}, just off the diagonal";
            if (strat == UmadP3BowelsOfAgonyAi.Strat.Lb3)
                return IsRanged(role)
                    ? $"{rule}, on the side toward the long {CrystalName(state.LongElement)} crystal, where you take its shots next."
                    : $"{rule}, close to Chaos: supports on his left, DPS on his right (facing him from the wall), the sides you take the long {CrystalName(state.LongElement)} on.";
            return state.ElementOf(role) != null
                ? $"{rule}: element players dodge toward the long {CrystalName(state.LongElement)} crystal ({Spot(state.LongSpot)})."
                : $"{rule}: players without an element dodge toward the wind crystal ({Spot(state.WindSpot)}).";
        },
        strat == UmadP3BowelsOfAgonyAi.Strat.Lb3 ? Implosion1 : Sg3k);

    public static StratCue Sg3kSetup(UmadP3BowelsOfAgonyState state) => new(
        "Bowels of Agony: crystals (SG3K)",
        role => role switch
        {
            PartyRole.MainTank => $"Drag Chaos between the short {CrystalName(state.ShortElement)} crystal ({Spot(state.ShortSpot)}) and the wind crystal ({Spot(state.WindSpot)}).",
            PartyRole.OffTank => $"Take Exdeath to the wall at the long {CrystalName(state.LongElement)} crystal ({Spot(state.LongSpot)}): his Thunder III then can't reach the others.",
            _ => Sg3kResolve(state, state.ShortElement).Why(role),
        },
        Sg3k);

    public static StratCue Sg3kResolve(UmadP3BowelsOfAgonyState state, Element element) => new(
        element == state.ShortElement ? $"Short {CrystalName(element)} (SG3K)" : $"Long {CrystalName(element)} (SG3K)",
        role =>
        {
            var crystal = $"{Spot(state.SpotOf(element))} {CrystalName(element)} crystal";
            var resolvers = state.HoldersOf(element);
            var other = element == Element.Water ? Element.Fire : Element.Water;
            var cleanse = state.HoldersOf(other);
            if (role == resolvers[0])
                return $"Your {DebuffName(element)} runs out: stand at the wall behind the {crystal}. You and {Label(resolvers[1])} are the two nearest it, so its {CrystalShot(element)} fires at you two.";
            if (role == resolvers[1])
                return $"Your {DebuffName(element)} runs out: stand 7y out toward the {crystal}. Its {CrystalShot(element)} on you knocks {Label(cleanse[0])} and {Label(cleanse[1])} back, which takes their winds.";
            if (state.ElementOf(role) == other)
            {
                var where = element == Element.Water ? "inside its 5y circle" : "in its donut, 4-10y from them";
                return $"Stand just behind {Label(resolvers[1])}, {where}: the {crystal}'s {CrystalShot(element)} knocks you back and takes your wind; {WindFacing(state, role, Label(resolvers[1]))}.";
            }
            return "Stand by the wind crystal with your partner: each wind a crystal takes makes a Cyclone, a two-player stack on whoever stands nearest the wind crystal.";
        },
        Sg3k);

    public static StratCue Sg3kCleanseFacing(UmadP3BowelsOfAgonyState state, Element resolving) => new(
        "Crystal knockback",
        role =>
        {
            var other = resolving == Element.Water ? Element.Fire : Element.Water;
            var centre = Label(state.HoldersOf(resolving)[1]);
            return state.ElementOf(role) == other
                ? $"The {CrystalShot(resolving)} on {centre} knocks you back: {WindFacing(state, role, centre)}, and the push only takes your wind. Facing wrong throws you off the platform."
                : $"The {CrystalShot(resolving)} on {centre} knocks the {DebuffName(other)} players back to take their winds; stay where you are.";
        },
        Sg3k);

    public static StratCue Sg3kPreImplosion(UmadP3BowelsOfAgonyState state) => new(
        "Implosion: set up",
        role => role switch
        {
            PartyRole.MainTank => "Pull Chaos to the middle and stand north of him, so he faces north: Implosion's cones then split on the intercardinals.",
            PartyRole.OffTank => $"Take Exdeath toward the wind crystal ({Spot(state.WindSpot)}), where Limit Cut needs him.",
            _ => state.ElementOf(role) != null
                ? $"Head for the long {CrystalName(state.LongElement)} crystal ({Spot(state.LongSpot)}): element players dodge Implosion toward it."
                : $"Head for the wind crystal ({Spot(state.WindSpot)}): players without an element dodge Implosion toward it.",
        },
        Sg3k);

    public static StratCue ThunderTank(UmadP3BowelsOfAgonyState state) => UmadP3BlackHolePlaybook.ThunderTank(state.ThunderPlan, Thunder);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting) => UmadP3BlackHolePlaybook.ThunderClearance(holder, waiting, Thunder);

    public static StratCue ThunderSwap(UmadP3BowelsOfAgonyState state) => UmadP3BlackHolePlaybook.ThunderSwap(state.ThunderPlan, Thunder);
}
