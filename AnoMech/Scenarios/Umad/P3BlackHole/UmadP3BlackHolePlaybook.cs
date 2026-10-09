using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Umad.UmadConstants;
using TetherOrder = AnoMech.Scenarios.Umad.P3BlackHole.UmadP3BlackHoleAi.TetherOrder;

namespace AnoMech.Scenarios.Umad.P3BlackHole;

// What UmadP3BlackHoleAi's moves are for, per role, for the death recap. The Thunder III texts
// are shared with Limit Cut, which runs the same buster plan.
internal static class UmadP3BlackHolePlaybook
{
    private const string Rotation = "NAUR §3.16 (0:44:00)";
    private const string Stomps = "NAUR §3.21 (0:48:55) · old bh (n/s stomps)";
    public const string ThunderRuleSource = "NAUR §3.4 (0:35:00)";
    public const string Thunder1 = "NAUR §3.14 (0:42:40) · §3.4 (0:35:00)";
    public const string Thunder2 = "NAUR §3.17 (0:45:08) · §3.4 (0:35:00)";
    private const string ModifiedDeck = "P3 Modified DSA (Double Tethers) deck";

    private static readonly string[] WaveSources =
        ["NAUR §3.14 (0:41:49)", "NAUR §3.16 (0:44:00)", "NAUR §3.18 (0:45:55)", "NAUR §3.20 (0:48:19)"];

    private static readonly string[] SlapSources =
        ["NAUR §3.13 (0:41:20)", "NAUR §3.15 (0:42:46)", "NAUR §3.19 (0:46:27)"];

    // By D>S>A seat: the set whose laser is a seat's first. Each seat then takes the next two.
    private static readonly int[] FirstSet = [2, 5, 8, 3, 1, 4, 7, 6];

    // By D>S>A seat, Modified DSA's lasers: one player takes both of set 2, and one both of set 9.
    private static readonly string[] ModifiedSets =
        ["sets 1, 3 and 4", "sets 5, 6 and 7", "set 8, then both of set 9", "sets 3, 4 and 5",
         "both of set 2, then set 3", "sets 4, 5 and 6", "sets 7, 8 and 10", "sets 6, 7 and 8"];

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string Compass(Vector3 offset) => DeathRecap.Compass(Vector3.Zero, offset);

    private static string Compass(Direction direction) => Compass(direction.Apply(new Vector3(0f, 0f, -1f)));

    // A direction with Kefka as relative north (frame -Z), on the real compass.
    private static string Relative(Direction kefka, float x, float z) => Compass(kefka.Apply(new Vector3(x, 0f, z)));

    private static string Strat(TetherOrder order) => order switch
    {
        TetherOrder.SupportDpsAccretion => "Black Hole: S>D>A",
        TetherOrder.ModifiedDsa => "Modified DSA (double tethers, n/s stomps)",
        _ => "old bh (DSA, single tethers, n/s stomps)",
    };

    // Modified DSA only changes the first and last waves; the middle two follow NAUR.
    private static string WaveSource(TetherOrder order, int wave)
        => order == TetherOrder.ModifiedDsa && wave is 0 or 3 ? ModifiedDeck : WaveSources[wave];

    // ---- Earthquake and the Black Hole tethers ----

    public static readonly StratCue Earthquake = new(
        "Earthquake",
        role => role switch
        {
            PartyRole.MainTank => "Hold Chaos 6y east of the middle while OT holds Exdeath just west, so the party can stack between them.",
            PartyRole.OffTank => "Hold Exdeath 4y west of the middle while MT holds Chaos just east, so the party can stack between them.",
            _ => "Stack in the middle: Earthquake leaves everyone at 1 HP with Primordial Crust, and the healers need you together to top everyone up.",
        },
        "NAUR §3.11 (0:40:04)");

    public static readonly StratCue AccretionHeals = StratCue.ForAll(
        "Accretion heals",
        "Everyone, tanks too, stacks in the middle: the two Accretion players are healed off one at a time, and each cleanse sets off a small raidwide.",
        "NAUR §3.12 (0:41:00)");

    private static int Seat(UmadP3BlackHoleState state, PartyRole role) => Array.IndexOf(state.Roles.List, role);

    // S>D>A trades the support and DPS seats of each line; the Accretion seats keep theirs.
    private static int DsaSeat(TetherOrder order, int seat)
        => order != TetherOrder.SupportDpsAccretion ? seat : seat switch
        {
            <= 2 => seat + 4,
            >= 4 and <= 6 => seat - 4,
            _ => seat,
        };

    private static string Turn(UmadP3BlackHoleState state, TetherOrder order, PartyRole role)
    {
        var seat = Seat(state, role);
        if (seat < 0) return "";
        var line = UmadP3BlackHoleState.SlotLine[seat] switch { 1 => "First", 2 => "Second", _ => "Third" };
        var kind = seat is 3 or 7 ? "Accretion" : seat < 3 ? "Support" : "DPS";
        var first = FirstSet[DsaSeat(order, seat)];
        var sets = order == TetherOrder.ModifiedDsa ? ModifiedSets[seat] : $"sets {first}, {first + 1} and {first + 2}";
        return $"You're {line} in Line ({kind}): your lasers are {sets}; "
               + "the first two stack Unbecoming and Meanest Existence, the third cleanses your Primordial Crust.";
    }

    private static string OrderText(TetherOrder order) => order switch
    {
        TetherOrder.SupportDpsAccretion => "non-Accretion Supports take the first tether, non-Accretion DPS the second, Accretion the third (NAUR puts the DPS first)",
        TetherOrder.ModifiedDsa => "non-Accretion DPS take the first tether, non-Accretion Supports the second, Accretion the third, "
                                   + "except that Support 1st in Line takes set 1's lone tether, DPS 1st in Line both of set 2's, "
                                   + "Support 3rd in Line both of set 9's and DPS 3rd in Line set 10's",
        _ => "non-Accretion DPS take the first tether, non-Accretion Supports the second, Accretion the third",
    };

    public static StratCue BlackHoleWave(UmadP3BlackHoleState state, TetherOrder order, int wave) => new(
        $"Black Hole wave {wave + 1}",
        role => "Wait in the middle, clear of the black holes: only the players whose turn it is go out, and they pull their lasers along the wall. "
                + Turn(state, order, role),
        $"{WaveSource(order, wave)} · {Strat(order)}");

    public static StratCue Grab(UmadP3BlackHoleState state, TetherOrder order, int tetherIndex) => new(
        "Black Hole: take your tether",
        role =>
        {
            var which = state.ScenarioObjects.Tethers.Count == 1 ? "only" : tetherIndex switch { 0 => "first", 1 => "second", _ => "third" };
            return $"Counting clockwise from Kefka ({Compass(state.ScenarioObjects.TetherSortFrom)}), {OrderText(order)}: "
                   + $"step into the {which} tether's line to take it. {Turn(state, order, role)}";
        },
        $"{Rotation} · {Strat(order)}");

    // Where Movement.Intercept first aims: the nearest point on the tether, `margin` short of
    // either end.
    public static Vector2 TetherGrabSpot(SimTether tether, SimCharacter player, float margin)
    {
        var at = new Vector2(player.Position.X, player.Position.Z);
        var hole = tether.A is { } a ? new Vector2(a.Position.X, a.Position.Z) : at;
        var holder = tether.B is { } b ? new Vector2(b.Position.X, b.Position.Z) : at;
        var line = holder - hole;
        var length = line.Length();
        if (length < 1e-6f) return hole;
        var inset = margin / length;
        var (min, max) = 2f * inset >= 1f ? (0.5f, 0.5f) : (inset, 1f - inset);
        return hole + line * Math.Clamp(Vector2.Dot(at - hole, line) / (length * length), min, max);
    }

    public static StratCue GrabBoth(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: take both tethers",
        role => "This pair of tethers is yours alone: you've stepped into one tether's line, now step into the other's, "
                + $"so both black holes are tethered to you. {Turn(state, order, role)}",
        $"{ModifiedDeck} · {Strat(order)}");

    public static StratCue HoldBoth(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: hold both tethers",
        role => "Hold both tethers about 12y out on the intercardinal between their two black holes, stepping clear of any small black hole there, "
                + $"so both lasers point past the edge, away from the stack in the middle. {Turn(state, order, role)}",
        $"{ModifiedDeck} · {Strat(order)}");

    public static readonly StratCue ExdeathToFirstTether = new(
        "Exdeath toward the first tether",
        role => role == PartyRole.OffTank
            ? "Take Exdeath to the intercardinal just clockwise of the first tether's black hole, standing about 14y out so he sits about 11y out, "
              + "and take Thunder III there, across the arena from the second pair's lasers."
            : "Stay in the middle: the off-tank holds Exdeath on the intercardinal just clockwise of the first tether's black hole for Thunder III, "
              + "across the arena from the second pair's lasers.",
        ModifiedDeck);

    public static StratCue Pull(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: pull your tether",
        role => "Stretch your tether 60° clockwise of its black hole and hold it about 10y out, near Chaos's max melee "
                + $"rather than at the wall, so its laser passes clear of the stack in the middle. {Turn(state, order, role)}",
        $"{Rotation} · {Strat(order)}");

    public static StratCue ReturnToMiddle(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole: back to the middle",
        role => $"Your time on this tether is over: go back to the stack in the middle, out of every laser's path. {Turn(state, order, role)}",
        $"{Rotation} · {Strat(order)}");

    public static StratCue LookUponSplit(UmadP3BlackHoleState state, TetherOrder order) => new(
        "Black Hole set 10 + Kefka's body slam",
        role =>
        {
            var holder = state.Roles[order is TetherOrder.SupportDpsAccretion or TetherOrder.ModifiedDsa ? 6 : 2];
            var line = $"Kefka's body slam cuts a 16y line through the middle from the {Compass(state.KefkaPosition[4])} wall";
            return role == holder
                ? $"{line}. You hold the last tether: ride it out to the wall 45° to whichever side of its black hole clears the line, so the final laser points at the wall, away from everyone."
                : $"{line}: go to the wall opposite {Label(holder)}, who holds the last tether, clear of both the line and the final laser.";
        },
        $"{(order == TetherOrder.ModifiedDsa ? ModifiedDeck : "NAUR §3.20 (0:48:19)")} · {Strat(order)}");

    // ---- Kefka, Chaos and Exdeath around the tethers ----

    public static StratCue Slap(UmadP3BlackHoleState state, int slapIndex, int kefkaIndex) => new(
        $"Kefka's hand slam {slapIndex + 1}",
        role =>
        {
            var kefka = state.KefkaPosition[kefkaIndex];
            var where = $"Kefka ({Compass(kefka)}) is relative north.";
            if (state.SlapAttacks[slapIndex] == ActionId.SlapHappy_Left)
                return $"{where} He slaps the {Relative(kefka, -1f, 0f)} half, then drops a party stack that needs all eight: "
                       + $"everyone stacks together on the {Relative(kefka, 1f, 0f)} side.";
            var (group, rel, z) = role.IsTank() ? ("tanks", "north-west", -1f) : role.IsDps() ? ("DPS", "south-west", 1f) : ("healers", "west", 0f);
            return $"{where} He slaps the {Relative(kefka, 1f, 0f)} half, then fires three cones at a tank, a healer and a DPS, "
                   + $"each shared by its role: {group} stand relative {rel}, to the {Relative(kefka, -1f, z)}.";
        },
        SlapSources[slapIndex]);

    public static readonly StratCue Edict = StratCue.ForAll(
        "Damning Edict",
        "Chaos cleaves the half of the room in front of him: tuck 3y behind him, as close to the middle as that allows, so the slap dodge after it is a short step.",
        "NAUR §3.15 (0:42:46)");

    public static StratCue EdictAndBodySlam(UmadP3BlackHoleState state) => new(
        "Damning Edict 2 + Kefka's body slam",
        _ => $"Damning Edict and Kefka's body slam (a 16y line through the middle from the {Compass(state.KefkaPosition[2])} wall) "
             + "go off 1.4s apart, too close to dodge one then the other: hold one spot behind Chaos and outside the line.",
        "NAUR §3.17 (0:44:54)");

    public static StratCue ImplosionAnchor(UmadP3BlackHoleState state) => new(
        "Implosion: place Chaos",
        role =>
        {
            var anchor = Compass(state.KefkaPosition[3].Rotate(1));
            return role == PartyRole.MainTank
                ? $"Park Chaos 5y out to the {anchor}, 45° clockwise of Kefka, so his Implosion cones lock onto a known angle."
                : $"MT parks Chaos 5y out to the {anchor}, 45° clockwise of Kefka, so his Implosion cones lock onto a known angle.";
        },
        "NAUR §3.19 (0:46:27)");

    public static StratCue Implosion(UmadP3BlackHoleState state, int shockwave) => new(
        shockwave == 0 ? "Implosion: first cones" : "Implosion: second cones",
        _ =>
        {
            var longitudinal = state.ImplosionAttack == ActionId.LongitudinalImplosion;
            var safe = Relative(state.KefkaPosition[3], state.SlapAttacks[2] == ActionId.SlapHappy_Left ? 1f : -1f, 0f);
            return $"{(longitudinal ? "Longitudinal" : "Latitudinal")} Implosion cleaves Chaos's {(longitudinal ? "front and back, then his sides" : "sides, then his front and back")}. "
                   + $"The strat stands on a diagonal from Chaos, the gap both pairs miss, leaning ~10° away from the cones about to fire, on the {safe} side Kefka's next slap leaves safe. "
                   + $"NAUR instead dodges to his {(longitudinal ? "sides" : "front or back")} first, close to him.";
        },
        "NAUR §3.19 (0:46:27)");

    // ---- Thunder III (also Limit Cut's) ----

    private const string ThunderRule = "Exdeath's Thunder III hits whoever is closest to him twice, 3s apart";

    public static string ThunderPlan(ThunderIIIAssignment plan) => plan switch
    {
        ThunderIIIAssignment.MtInvulnsBoth => "MT stands on him and invulns both hits",
        ThunderIIIAssignment.OtInvulnsBoth => "OT stands on him and invulns both hits",
        ThunderIIIAssignment.ShareMtFirst => "MT takes the first hit and OT the second, since one hit's Lightning Resistance Down makes a second lethal",
        _ => "OT takes the first hit and MT the second, since one hit's Lightning Resistance Down makes a second lethal",
    };

    public static StratCue ThunderTank(ThunderIIIAssignment plan, string source) => new(
        "Thunder III (tank buster)",
        role => role == ThunderIIIPlanning.Roles(plan).First
            ? $"{ThunderRule}, and {ThunderPlan(plan)}: walk onto Exdeath so you're the closest."
            : $"{ThunderRule}, and {ThunderPlan(plan)}: stay over 10y from him.",
        source);

    public static StratCue ThunderClearance(PartyRole holder, PartyRole? waiting, string source = ThunderRuleSource) => new(
        "Thunder III: keep clear",
        role => role == waiting
            ? $"{ThunderRule}: wait 10y to the side of {Label(holder)} so the first hit can't pick you, then swap onto Exdeath for the second."
            : $"{ThunderRule}, and {Label(holder)} is on him: step out to 10y from Exdeath so it can't pick you.",
        source);

    public static StratCue ThunderSwap(ThunderIIIAssignment plan, string source) => new(
        "Thunder III: tank swap",
        role =>
        {
            if (ThunderIIIPlanning.Roles(plan) is not (var first, { } second))
                return $"{ThunderRule}, and {ThunderPlan(plan)}: stay over 10y from him.";
            if (role == first)
                return $"You took the first Thunder III hit and carry Lightning Resistance Down, so a second would kill you: trade places with {Label(second)}, who steps onto Exdeath.";
            if (role == second)
                return $"Step onto Exdeath now, trading places with {Label(first)}: you take the second Thunder III hit, which would kill {Label(first)} through Lightning Resistance Down.";
            return $"{ThunderRule}: the tanks trade places between the hits, so stay over 10y from him.";
        },
        source);

    public static StratCue ThunderStack(ThunderIIIAssignment plan, string source) => StratCue.ForAll(
        "Thunder III: stack",
        $"Stack in the middle: {ThunderRule}, and {ThunderPlan(plan)}. Everyone else is then stepped out to 10y from him.",
        source);

    // ---- Blizzard III and Stomp a Mole ----

    private static bool GroupOne(PartyRole role) => (int)role % 2 == 0;

    public static StratCue StompPreposition(UmadP3BlackHoleState state) => new(
        "Blizzard III + Stomp a Mole: start",
        role => $"Kefka's last teleport ({Compass(state.KefkaPosition[4])}) is now relative north. "
                + $"{(role.IsDps() ? "DPS wait 9y relative south" : "Supports wait 9y relative north")} of the middle, so the first Blizzard III puddles drop clear of it.",
        Stomps);

    public static StratCue StompCorners(UmadP3BlackHoleState state) => new(
        "Blizzard III: second puddles",
        role =>
        {
            var west = GroupOne(role);
            var north = !role.IsDps();
            var world = Relative(state.KefkaPosition[4], west ? -1f : 1f, north ? -1f : 1f);
            return $"Step out of the first puddles to the relative {(north ? "north" : "south")}-{(west ? "west" : "east")} corner ({world}) to drop the second one there: "
                   + "Supports north, DPS south, Group 1 west, Group 2 east, two to a corner.";
        },
        Stomps);

    private static string Tower(UmadP3BlackHoleState state, PartyRole role)
    {
        var west = GroupOne(role);
        return $"Group {(west ? 1 : 2)} takes the relative {(west ? "west" : "east")} tower ({Relative(state.KefkaPosition[4], west ? -1f : 1f, 0f)}), two per tower; an unsoaked tower wipes the party.";
    }

    public static StratCue StompStackAndTowers(UmadP3BlackHoleState state) => new(
        "Knock Down 1 + Stomp a Mole towers",
        role =>
        {
            var marked = state.StackTargets[0];
            var stackers = marked.IsDps() ? "DPS" : "Supports";
            return role.IsDps() == marked.IsDps()
                ? $"The first stack is on {Label(marked)}, so the {stackers} dip into the middle and take it together: it needs four."
                : $"The first stack is on {Label(marked)}, so the {stackers} take it in the middle and your role soaks Kefka's towers. {Tower(state, role)}";
        },
        Stomps);

    public static StratCue StompSwap(UmadP3BlackHoleState state, bool west) => new(
        west ? "Knock Down 2 + towers: west swap" : "Knock Down 2 + towers: east swap",
        role =>
        {
            const string timing = "The west pairs trade first, since the west tower goes off 1.3s before the east.";
            return role.IsDps() == state.StackTargets[0].IsDps()
                ? $"Roles swap: you took the first stack, so now you soak a tower. {Tower(state, role)} {timing}"
                : $"Roles swap: back to the middle for the second stack, on {Label(state.StackTargets[1])}. {timing}";
        },
        Stomps);
}
