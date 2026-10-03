using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P1Shared;
using static AnoMech.Scenarios.Umad.UmadConstants;

namespace AnoMech.Scenarios.Umad.P2Forsaken.Ai;

// What each Forsaken strat's moves are for, per role, for the death recap. The strats share one
// move timeline but differ in spots and flex rules, so each gets its own instance. Left and right
// are NAUR's: facing Kefka from the towers, which puts the helpers' +X tower on the left.
public sealed class UmadP2ForsakenPlaybook
{
    private enum OddSpots { Na, Eu }

    private enum EvenSpots { DiamondMarkers, WeekOne }

    private enum FirstSetStacks { RoleOrder, SupportLeft, ConeRoleLeft }

    private enum Flex { Partner, South }

    private const string KroxyRinonPlan = "raidplan.io/plan/UATE__aDcw1-bgVv";
    private const string SouthAdjustPlan = "raidplan.io/plan/uq7zdjvuu7uuw8fj";

    public static readonly UmadP2ForsakenPlaybook KroxyRinon =
        new("Kroxy-Rinon", KroxyRinonPlan, OddSpots.Na, EvenSpots.DiamondMarkers, FirstSetStacks.RoleOrder, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook KroxyRinonOld =
        new("Kroxy-Rinon", KroxyRinonPlan, OddSpots.Na, EvenSpots.WeekOne, FirstSetStacks.RoleOrder, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook SouthFlex =
        new("South Flex", SouthAdjustPlan, OddSpots.Na, EvenSpots.DiamondMarkers, FirstSetStacks.RoleOrder, Flex.South);

    public static readonly UmadP2ForsakenPlaybook SouthFlexOld =
        new("South Flex", SouthAdjustPlan, OddSpots.Na, EvenSpots.WeekOne, FirstSetStacks.RoleOrder, Flex.South);

    public static readonly UmadP2ForsakenPlaybook LpduBuddies =
        new("LPDU Buddies", "raidplan.io/plan/142oXOZpPc_jh3dd", OddSpots.Eu, EvenSpots.DiamondMarkers, FirstSetStacks.SupportLeft, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook P3ZBuddyMeow =
        new("p3Z Buddy Meow", "raidplan.io/plan/lZWqxfxvyhF9sp3Z", OddSpots.Eu, EvenSpots.DiamondMarkers, FirstSetStacks.ConeRoleLeft, Flex.Partner);

    public static readonly UmadP2ForsakenPlaybook ZP6SouthAdjust =
        new("zP6 South adjust", "raidplan.io/plan/rtc1FcuZFMuyBzP6", OddSpots.Eu, EvenSpots.DiamondMarkers, FirstSetStacks.RoleOrder, Flex.South);

    private readonly string name;
    private readonly string plan;
    private readonly OddSpots odd;
    private readonly EvenSpots even;
    private readonly FirstSetStacks firstSetStacks;
    private readonly Flex flex;

    private UmadP2ForsakenPlaybook(string name, string plan, OddSpots odd, EvenSpots even, FirstSetStacks firstSetStacks, Flex flex)
    {
        this.name = name;
        this.plan = plan;
        this.odd = odd;
        this.even = even;
        this.firstSetStacks = firstSetStacks;
        this.flex = flex;
    }

    public StratCue Lineup => new(
        "Forsaken (raidwide)",
        role => $"Wait close to Kefka with the {(UmadP1Roles.IsDps(role) ? "DPS on his east side" : "Supports on his west side")} while Forsaken's raidwide goes off; "
                + "mitigate and shield it, then read your marker and your partner's.",
        $"NAUR §2.2 (0:15:46) · {plan}");

    // `soakers` is the helper's live list for the group soaking set index+1; the Why runs right
    // after the move is built, when its order still ranks same-marker players as the move did.
    public StratCue Towers(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers) => new(
        index % 2 == 0
            ? $"Forsaken towers, set {index + 1} (odd layout)"
            : $"Forsaken towers, set {index + 1} (even layout) + {EndName(EndOf(state, index))}",
        role => soakers.Contains(role) ? SoakerWhy(state, index, soakers, role) : OutsideWhy(state, index, role),
        TowerSource(index));

    public StratCue EndBait(UmadP2ForsakenState state, int occurrence)
    {
        var future = state.EndAttacks[occurrence] != EndAttack.PastsEnd;
        if (occurrence == 3)
            return new($"{EndName(state.EndAttacks[occurrence])}: last clones",
                _ => future
                    ? "It's Future's End: the last clones turned to the party between the last towers and cleave their front half, so once they start casting everyone runs through to the far side, behind them."
                    : "It's Past's End: the last clones cleave their back half, so everyone stays grouped between the last towers, in front of them.",
                $"NAUR §2.10 (0:24:17) · {plan}");
        var nextSet = occurrence * 2 + 3;
        return new($"{EndName(state.EndAttacks[occurrence])}: All Things Ending",
            _ => future
                ? $"It's Future's End: everyone groups 15y out on the side away from set {nextSet}'s towers, because Kefka and his clones turn to the party and cleave their front half. Once they start casting, run to the towers behind them."
                : $"It's Past's End: everyone groups 13y out between set {nextSet}'s towers, because Kefka and his clones turn to the party and cleave their back half, which leaves the towers safe once they start casting.",
            $"NAUR §2.6 (0:20:34) · wtfdig kefka_p2/9_future_past_baits · {plan}");
    }

    public StratCue LastClonesGather(UmadP2ForsakenState state) => new(
        $"{EndName(state.EndAttacks[3])}: last clones",
        _ => "Everyone groups 13y out between the last towers so the last clones turn to face the party. "
             + (state.EndAttacks[3] == EndAttack.PastsEnd
                 ? "It's Past's End, so you'll stay here, in front of them, while they cleave behind."
                 : "It's Future's End, so once they start casting you'll run through to the far side, behind them."),
        $"NAUR §2.10 (0:24:17) · {plan}");

    // The tower side the helper gives `role` this set (true = left), or null outside the soakers.
    internal bool? SoaksLeft(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
        => SoakSpot(state, index, soakers, role)?.Left;

    private (uint Marker, bool Left)? SoakSpot(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
    {
        if (!soakers.Contains(role)) return null;
        var marker = Marker(state, role);
        if (index % 2 == 0)
        {
            if (marker == LockonId.ForsakenStack)
            {
                var (left, right) = Stacks(state, index, soakers);
                return role == left ? (marker, true) : role == right ? (marker, false) : null;
            }
            if (marker == LockonId.ForsakenCone && role == Nth(state, soakers, marker, 0)) return (marker, true);
            if (marker == LockonId.ForsakenChariot && role == Nth(state, soakers, marker, 0)) return (marker, false);
            return null;
        }
        if (marker is not (LockonId.ForsakenCone or LockonId.ForsakenChariot)) return null;
        if (role == Nth(state, soakers, marker, 0)) return (marker, true);
        if (role == Nth(state, soakers, marker, 1)) return (marker, false);
        return null;
    }

    private (PartyRole? Left, PartyRole? Right) Stacks(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers)
    {
        var first = Nth(state, soakers, LockonId.ForsakenStack, 0);
        var second = Nth(state, soakers, LockonId.ForsakenStack, 1);
        if (index != 0 || first is not { } f) return (first, second);
        var keep = firstSetStacks switch
        {
            FirstSetStacks.SupportLeft => !UmadP1Roles.IsDps(f),
            FirstSetStacks.ConeRoleLeft => RoleMarker(state, f) == LockonId.ForsakenCone,
            _ => true,
        };
        return keep ? (first, second) : (second, first);
    }

    private static PartyRole? Nth(UmadP2ForsakenState state, IReadOnlyList<PartyRole> soakers, uint marker, int n)
        => soakers.Where(r => Marker(state, r) == marker).Skip(n).Select(r => (PartyRole?)r).FirstOrDefault();

    private static uint Marker(UmadP2ForsakenState state, PartyRole role)
        => state.Lockons.TryGetValue(role, out var marker) ? marker : 0;

    // The cone-or-spread marker the stack holder's role drew on set 1.
    private static uint RoleMarker(UmadP2ForsakenState state, PartyRole stackRole)
    {
        foreach (var (role, marker) in state.Lockons)
            if (UmadP1Roles.IsDps(role) == UmadP1Roles.IsDps(stackRole) && marker != LockonId.ForsakenStack)
                return marker;
        return 0;
    }

    private string SoakerWhy(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role)
    {
        if (SoakSpot(state, index, soakers, role) is not { } soak)
            return $"Your group soaks set {index + 1}: take your marker's spot in a tower, two soakers per tower, or the tower wipes the party.";
        var spot = index % 2 == 0 ? OddSoakSpot(soak.Marker, soak.Left) : EvenSoakSpot(EndOf(state, index), soak.Marker, soak.Left);
        return $"{spot} {SideReason(state, index, soakers, role, soak.Marker)}";
    }

    private string OddSoakSpot(uint marker, bool left) => (odd, marker, left) switch
    {
        (OddSpots.Na, LockonId.ForsakenStack, true) =>
            "With a stack, take the left tower's inner edge on Kefka's hitbox ring: the cone soaker and the outside tank make it the 3 a stack needs.",
        (OddSpots.Na, LockonId.ForsakenStack, false) =>
            "With a stack, take the right tower's north part, nudged toward the middle: the two outside DPS make it the 3 a stack needs.",
        (OddSpots.Na, LockonId.ForsakenCone, _) =>
            "With the cone, take the left tower's south part, a little in from the edge: you're the left stack's third player, and your cone fires at the nearest player, the outside healer past the edge, away from everyone.",
        (OddSpots.Na, _, _) =>
            "With the spread, take the right tower's south end, over 5y from the right stack, so your circle hits no one else.",
        (_, LockonId.ForsakenStack, true) =>
            "With a stack, take the left tower's north-east part, toward Kefka: the cone soaker and the outside tank make it the 3 a stack needs.",
        (_, LockonId.ForsakenStack, false) =>
            "With a stack, take the right tower's south-west side, toward the other tower, where the two outside DPS make it 3; NAUR has this stack on the tower's north part.",
        (_, LockonId.ForsakenCone, _) =>
            "With the cone, take the left tower's south-east part: you're the left stack's third player, and your cone fires at the outside healer just past the south edge, away from everyone.",
        _ =>
            "With the spread, take the right tower's north-east part, over 5y from the right stack; NAUR has the spread at the tower's south end.",
    };

    private string EvenSoakSpot(EndAttack end, uint marker, bool left)
    {
        var side = left ? "left" : "right";
        var baiter = left ? "healer" : "ranged";
        var compass = left ? "west" : "east";
        if (marker == LockonId.ForsakenChariot)
            return $"With a spread, take the {side} tower's {(even == EvenSpots.WeekOne ? "outer" : "south")} edge, as far back as possible, so your circle misses the cone soaker and the baiters.";
        return even == EvenSpots.WeekOne
            ? $"With a cone, take the {side} tower's inner edge, closest to Kefka: as one of the four nearest him you bait a {EndName(end)} hit, and your cone fires out at the outside {baiter} to the relative {compass}."
            : $"With a cone, take the {side} tower's north edge, where it crosses Kefka's inner hitbox ring: as one of the four nearest him you bait a {EndName(end)} hit, and your cone fires out at the outside {baiter} on the relative {compass} waymark.";
    }

    private string SideReason(UmadP2ForsakenState state, int index, IReadOnlyList<PartyRole> soakers, PartyRole role, uint marker)
    {
        var oddSet = index % 2 == 0;
        if (oddSet && marker != LockonId.ForsakenStack)
            return "Odd sets always put the cone left and the spread right.";
        if (oddSet && index == 0 && firstSetStacks == FirstSetStacks.ConeRoleLeft)
            return $"This strat ({name}) puts the stack of the role holding cones left on set 1; NAUR always puts the Support stack left.";
        if (flex == Flex.South && index is not (0 or 3))
            return $"This strat ({name}) keeps you in your last tower unless your tower-mate shares your marker, and then the one further south flexes; NAUR re-sorts by role every set.";
        if (oddSet)
        {
            var sameRole = soakers.Any(r => r != role && Marker(state, r) == LockonId.ForsakenStack && UmadP1Roles.IsDps(r) == UmadP1Roles.IsDps(role));
            return !sameRole
                ? "The Support stack goes left and the DPS stack right."
                : UmadP1Roles.IsDps(role)
                    ? "Both stacks are on DPS: the ranged keeps the DPS side, right, and the melee flexes left."
                    : "Both stacks are on Supports: the healer keeps the Supports' side, left, and the tank flexes right.";
        }
        var partner = Partner(role);
        if (!UmadP1Roles.IsTankOrMelee(role))
            return $"Supports go left and DPS right; as the {Kind(role)} you keep your side, and your partner {Label(partner)} flexes if your markers match.";
        return Marker(state, partner) == marker
            ? $"Supports go left and DPS right, but your marker matches your partner {Label(partner)}'s, so you flex."
            : $"Supports go left and DPS right; your marker differs from your partner {Label(partner)}'s, so you keep your side.";
    }

    private string OutsideWhy(UmadP2ForsakenState state, int index, PartyRole role)
    {
        var soakingGroup = index is < 3 or 7 ? "A" : "B";
        var spot = index % 2 == 0 ? OddOutsideSpot(role) : EvenOutsideSpot(EndOf(state, index), role);
        return $"Group {soakingGroup} soaks set {index + 1}, so you stand outside the towers. {spot}";
    }

    private string OddOutsideSpot(PartyRole role) => (Kind(role), odd) switch
    {
        ("healer", _) =>
            "Stand just past the left tower's south edge: you're nearest its cone soaker, so the cone fires out at you, away from the party; keep over 5y from the left stack.",
        ("tank", OddSpots.Na) =>
            "Stand just outside the left tower's north-east edge, toward Kefka, as the third player in the left stack.",
        ("tank", _) =>
            "Stand between Kefka and the left tower, just outside it, as the third player in the left stack.",
        (_, OddSpots.Na) =>
            "Stand with the other outside DPS just past the right tower's north-west edge: you two fill the right stack to 3.",
        _ =>
            "Stand with the other outside DPS just past the right tower's south-west side, between the towers, to fill the right stack to 3; NAUR has you on its north-west edge.",
    };

    private string EvenOutsideSpot(EndAttack end, PartyRole role)
    {
        var weekOne = even == EvenSpots.WeekOne;
        switch (Kind(role))
        {
            case "healer":
            case "ranged":
                var left = Kind(role) == "healer";
                var compass = left ? "west" : "east";
                return weekOne
                    ? $"Stand out to the relative {compass}, a little toward the {(left ? "left" : "right")} tower: you're nearest its cone soaker, so the cone fires out at you; NAUR stands on the waymark instead."
                    : $"Stand on the relative {compass} waymark (outer corner of a numbered one, inner edge of a lettered one): you're nearest the {(left ? "left" : "right")} tower's cone soaker, so its cone fires out at you, clear of the {EndName(end)} baits.";
            default:
                var tank = Kind(role) == "tank";
                return $"Stand relative {(tank ? "north-west" : "north-east")} {(weekOne ? "close to Kefka" : "on Kefka's hitbox")}: with the {(tank ? "melee" : "tank")} and the two cone soakers you're one of the four nearest him, so you bait a {EndName(end)} hit.";
        }
    }

    private string TowerSource(int index)
    {
        var section = index switch
        {
            0 => "NAUR §2.4 (0:17:08)",
            1 => "NAUR §2.5 (0:18:59)",
            2 => "NAUR §2.7 (0:21:10)",
            3 => "NAUR §2.8 (0:22:53)",
            7 => "NAUR §2.10 (0:24:17)",
            _ => "NAUR §2.9 (0:23:43)",
        };
        var image = (index % 2 == 0, odd, even) switch
        {
            (true, OddSpots.Na, _) => " · wtfdig kefka_p2/3_odd_towers",
            (false, _, EvenSpots.DiamondMarkers) => " · wtfdig kefka_p2/6_even_towers_diamond_box_markers",
            _ => "",
        };
        return $"{section}{image} · {plan}";
    }

    private static EndAttack EndOf(UmadP2ForsakenState state, int index) => state.EndAttacks[(index - 1) / 2];

    private static string EndName(EndAttack end) => end == EndAttack.PastsEnd ? "Past's End" : "Future's End";

    private static string Label(PartyRole role) => SettingsGrid.RoleLabel(role);

    private static string Kind(PartyRole role) => role switch
    {
        PartyRole.MainTank or PartyRole.OffTank => "tank",
        PartyRole.RegenHealer or PartyRole.ShieldHealer => "healer",
        PartyRole.MeleeDpsA or PartyRole.MeleeDpsB => "melee",
        _ => "ranged",
    };

    private static PartyRole Partner(PartyRole role)
        => (PartyRole)(UmadP1Roles.IsDps(role) ? ((int)role - 2) % 4 + 4 : ((int)role + 2) % 4);
}
