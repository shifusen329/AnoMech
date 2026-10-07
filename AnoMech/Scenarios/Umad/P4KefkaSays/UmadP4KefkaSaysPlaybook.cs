using System;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using GazeLayout = AnoMech.Scenarios.Umad.P4KefkaSays.UmadP4KefkaSaysAi.GazeLayout;

namespace AnoMech.Scenarios.Umad.P4KefkaSays;

// What UmadP4KefkaSaysAi's moves are for, per role, for the death recap. Every text works from
// the run's truth, as the bots do, never from the orbs.
internal static class UmadP4KefkaSaysPlaybook
{
    private const string Strat = "kefkabin";

    private static readonly string[] MysterySources =
        ["NAUR §4.2 (0:51:46)", "NAUR §4.5 (0:54:08)", "NAUR §4.7 (0:54:54)"];

    private static string Compass(Vector3 offset) => DeathRecap.Compass(Vector3.Zero, offset);

    // ---- Mystery Magic: Blizzard III cones and Thrumming Thunder III lines ----

    private static string RealCones(MysteryCast m) => m.BlizzardOffset == 0 ? "south-east and north-west" : "north-east and south-west";

    private static string SafeCones(MysteryCast m) => m.BlizzardOffset == 0 ? "north-east and south-west" : "north-west and south-east";

    private static string LineRun(MysteryCast m) => m.LightningOrientation > 0f ? "north-east to south-west" : "north-west to south-east";

    // The 10y lane between the centre and the nearest real line: lines sit 5y and 15y either
    // side of the centre, and LightningOffset picks which two are real.
    private static string SafeLane(MysteryCast m)
    {
        var o = m.LightningOrientation > 0f ? 1f : -1f;
        var across = o > 0f ? new Vector3(1f, 0f, 1f) : new Vector3(1f, 0f, -1f);
        return Compass(across * (m.LightningOffset == 0 ? o : -o));
    }

    private static string MysteryText(MysteryCast m)
        => $"The ice cones that really hit point {RealCones(m)}, and the real lightning lines ({LineRun(m)}) leave the lane just {SafeLane(m)} of the middle safe.";

    public static readonly StratCue Gather = StratCue.ForAll(
        "Kefka Says",
        "Gather in the middle: Chaos and Neo Exdeath's raidwides need heals and mitigation on everyone, and every Mystery Magic is dodged from here.",
        $"NAUR §4.1 (0:51:26) · {Strat}");

    public static StratCue MysteryMagic(UmadP4KefkaSaysState state, int cast) => new(
        $"Mystery Magic {cast + 1}",
        _ => $"{MysteryText(state.Mystery[cast])} Everyone stacks on one spot a few yards off the middle, in the gap that clears both.",
        $"{MysterySources[cast]} · {Strat}");

    // ---- Flood of Naught ----

    private static string Colour(bool white) => white ? "White (NAUR: purple)" : "Black (NAUR: blue)";

    public static StratCue FloodOfNaught(UmadP4KefkaSaysState state) => new(
        "Flood of Naught",
        role =>
        {
            var i = Array.IndexOf(state.Wave3.List, role);
            if (i < 0) return "Every player takes one of the two Antilights: stand 8y off the centre line, where the Edge of Death kills.";
            var beyondDeath = i % 2 == 0;
            var white = state.Wounds[i];
            var needWhite = beyondDeath == white;
            var leftIsNeeded = (state.Antilights[0].Antilight == Antilight.White) == needWhite;
            var side = Compass(state.NeoExdeathDirection.Apply(new Vector3(leftIsNeeded ? -1f : 1f, 0f, 0f)));
            return $"You have {(beyondDeath ? "Beyond Death" : "Allagan Field")} and a {Colour(white)} Wound. "
                   + (beyondDeath ? "Beyond Death takes the beam of its own colour, whose lethal hit cleanses it; " : "Allagan Field takes the other colour, so you take no lethal hit; ")
                   + $"the {(needWhite ? "White" : "Black")} Antilight is on the {side} side. Stand 8y off the centre line, where the Edge of Death kills.";
        },
        $"NAUR §4.8 (0:55:55) · {Strat}");

    // ---- Stack, spread and Acceleration Bomb ----

    // Round 0 resolves at ~71s, round 1 at ~96s; each player's one bomb goes off in one of them.
    private static (int Round, bool Real)? Bomb(UmadP4KefkaSaysState state, PartyRole role)
    {
        var w1 = Array.IndexOf(state.Wave1.List, role) % 4;
        var w2 = Array.IndexOf(state.Wave2.List, role) % 4;
        return w2 == 0 ? (1, state.Wave2True)
             : w2 == 1 ? (0, state.Wave2True)
             : w1 == 0 ? (0, state.Wave1True)
             : w1 == 1 ? (1, state.Wave1True)
             : null;
    }

    private static string BombText(UmadP4KefkaSaysState state, int round, PartyRole role)
        => Bomb(state, role) is { } bomb && bomb.Round == round
            ? bomb.Real
                ? " Your Acceleration Bomb is real and goes off now: stop moving and acting before it does."
                : " Your Acceleration Bomb is fake and goes off now: keep moving as it does."
            : "";

    private static string ElementSpot(UmadP4KefkaSaysState state, int round, PartyRole role)
    {
        var j = Array.IndexOf(state.ElemRoles[round].List, role);
        var real = state.ElemTrue[round];
        var support = j is >= 0 and < 4;
        var (marker, spreads) = (j % 4) switch
        {
            2 => ("Forked Lightning", real),
            3 => ("Compressed Water", !real),
            _ => ((string?)null, false),
        };
        var stack = support ? "north" : "south";
        var spread = support ? "west" : "east";
        var mine = marker is null
            ? $"You have no stack or spread this round, so you stack {stack}."
            : $"Your {marker} came from a {(real ? "real" : "fake")} Grand Cross, so it's a {(spreads ? "spread" : "stack")}: you go {(spreads ? spread : stack)}.";
        return $"Supports stack north and DPS south; the Support spread goes west, the DPS spread east. {mine}";
    }

    public static StratCue Elements(UmadP4KefkaSaysState state, int round) => new(
        round == 0 ? "Short stack, spread and Acceleration Bomb" : "Blizzard III Blowout + long stack, spread and Acceleration Bomb",
        role => ElementSpot(state, round, role)
                + (round == 1 ? $" The real ice cones point {RealCones(state.Mystery[3])}, so each spot steps 1y off its cardinal into the safe {SafeCones(state.Mystery[3])} wedges." : "")
                + BombText(state, round, role),
        round == 0 ? $"NAUR §4.10 (0:57:05) · {Strat}" : $"NAUR §4.13 (0:58:34) · §4.10 (0:57:05) · {Strat}");

    public static readonly StratCue AccelerationBombFake = StratCue.ForAll(
        "Acceleration Bomb (fake)",
        "Your Acceleration Bomb came from a fake Grand Cross, so standing still when it goes off kills you: keep moving through it.",
        $"NAUR §4.10 (0:57:05) · {Strat}");

    // ---- Cursed Shriek ----

    public static StratCue Gaze(UmadP4KefkaSaysState state, GazeLayout layout, int round, bool facing) => new(
        $"{(round == 0 ? "Short" : "Long")} Cursed Shriek{(facing ? ": facing" : "")}",
        role =>
        {
            var wave = round == 0 ? state.Wave1 : state.Wave2;
            var real = round == 0 ? state.Wave1True : state.Wave2True;
            var shrieker = role == wave[0] || role == wave[4];
            var spot = GazeSpot(state, layout, round, shrieker, role.IsDps());
            var rule = real ? "The Shriek is real: everyone else looks away from the Shriek players." : "The Shriek is fake: everyone else looks at the Shriek players.";
            var turn = facing
                ? real ? " Step a little outward now: moving turns you away from the middle, where they stand." : " Step a little inward now: moving turns you toward the middle, where they stand."
                : "";
            return $"{spot} {rule}{turn}";
        },
        $"{(round == 0 ? "NAUR §4.11 (0:57:40)" : "NAUR §4.14 (0:58:56)")} · {Strat}{(layout == GazeLayout.SupportsNorthDpsSouth ? ", gazes: supports N / DPS S" : "")}");

    private static string GazeSpot(UmadP4KefkaSaysState state, GazeLayout layout, int round, bool shrieker, bool dps)
    {
        var thunder = state.Mystery[3];
        var lane = $"Thrumming Thunder's real lines leave the lane just {SafeLane(thunder)} of the middle safe";
        if (round == 0 && layout == GazeLayout.SupportsNorthDpsSouth)
        {
            var north = thunder.LightningOrientation > 0f ? "north-east" : "north-west";
            var south = thunder.LightningOrientation > 0f ? "south-west" : "south-east";
            return shrieker
                ? $"You carry the Cursed Shriek. {lane}: stand 2.2y from the middle along its edge, to the {(dps ? south : north)}."
                : $"{lane}: line up along its edge, Supports to the {north}, DPS to the {south}, 7.5-10y out, away from the two Shriek players.";
        }
        if (round == 0)
            return shrieker
                ? $"You carry the Cursed Shriek. {lane}: stand just off the middle in it, 1y from the other Shriek player."
                : $"{lane}: line up 5y off the middle along it, around the two Shriek players.";
        if (layout == GazeLayout.SupportsNorthDpsSouth)
            return shrieker
                ? $"You carry the Cursed Shriek: stand 2y {(dps ? "south" : "north")} of the middle, across it from the other Shriek player."
                : $"Supports wait 7.5-9y north of the middle, DPS south, away from the two Shriek players 2y either side of it.";
        return shrieker
            ? $"You carry the Cursed Shriek: stand 2y {(dps ? "south" : "north")} of the middle, across it from the other Shriek player."
            : "Ring the two Shriek players 6y out from the middle.";
    }

    // ---- Stray Flames and Stray Spray ----

    public static readonly StratCue FireBait = StratCue.ForAll(
        "Entropy bait",
        "Entropy drops a 6y AoE where each of you stands when it runs out: everyone baits it together in the middle so the AoEs overlap, then reacts to its shape.",
        $"NAUR §4.12 (0:58:13) · {Strat}");

    public static StratCue StrayFlames(UmadP4KefkaSaysState state) => StratCue.ForAll(
        "Stray Flames",
        state.InfernoMystery.SolutionIsChariot
            ? "Inferno was real, so Entropy's AoE is a point-blank circle: run straight out to your stack or spread spot for the next round."
            : "Inferno was fake, so Entropy's AoE is a donut: stay in the middle, inside its 6y hole.",
        $"NAUR §4.12 (0:58:13) · {Strat}");

    public static readonly StratCue WaterBait = StratCue.ForAll(
        "Dynamic Fluid bait",
        "Dynamic Fluid drops a 6y AoE where each of you stands when it runs out: everyone baits it together in the middle, then reacts to its shape and to Mana Release.",
        $"NAUR §4.15 (0:59:04) · {Strat}");

    public static StratCue StraySprayAndManaRelease(UmadP4KefkaSaysState state) => StratCue.ForAll(
        "Stray Spray + Mana Release",
        (state.TsunamiMystery.SolutionIsChariot
            ? "Tsunami was fake, so Dynamic Fluid's AoE is a point-blank circle: get out past 6y. "
            : "Tsunami was real, so Dynamic Fluid's AoE is a donut: stay within 6y of the middle. ")
        + $"Mana Release replays Kefka's ice and lightning at the same time. {MysteryText(state.Mystery[4])} "
        + "Everyone stacks on the one spot that clears all three.",
        $"NAUR §4.15 (0:59:04) · {Strat}");
}
