using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Umad.P5Exaflares;

// What UmadP5ExaflaresAi's moves are for, per role, for the death recap.
internal static class UmadP5ExaflaresPlaybook
{
    private const string Apocalypse = "NAUR §5.8 (1:10:18) · strat by Wydox";
    private const string Entropy = "NAUR §5.9 (1:10:56) · strat by Wydox";

    private static bool IsInner(PartyRole role) => role.IsTank() || role is PartyRole.MeleeDpsA or PartyRole.MeleeDpsB;

    private static bool FromNorthwest(int wave) => wave % 2 == 0;

    // The pair is centred on Kefka only for lines 2 and 5; 1+4 and 3+6 leave an open lane beyond them.
    private static bool PairOffCentre(UmadP5ExaflaresState state, int wave)
    {
        var order = FromNorthwest(wave) ? state.LeftOrder : state.RightOrder;
        var first = wave / 2 * 2;
        return order[first] + order[first + 1] != 7;
    }

    public static readonly StratCue FanOut = StratCue.ForAll(
        "Stray Apocalypse: opening spread",
        "Before the first wave, fan out from the spawn huddle into the melee ring around Kefka on your own bearing, "
        + "so nobody shares a spot when the first strafe starts. NAUR sets no opening spots.",
        Apocalypse);

    public static StratCue Wave(UmadP5ExaflaresState state, int wave) => new(
        $"Stray Apocalypse: wave {wave + 1} of 6",
        role =>
        {
            var lane = IsInner(role)
                ? "into the gap between its two lines, as close to Kefka as the gap allows, for uptime"
                : PairOffCentre(state, wave)
                    ? "into the nearest safe lane: the gap between its two lines or, as they sit off-centre, the open lane beyond them (NAUR always takes the gap)"
                    : "to the nearest spot in the gap between its two lines, apart from the others";
            var hold = wave == 0 ? "" : " Never step forward or back: that crosses the last wave's fire, still rolling.";
            return $"Wave {wave + 1} comes from the {(FromNorthwest(wave) ? "northwest" : "northeast")}: face it and strafe only left or right, {lane}.{hold}";
        },
        Apocalypse);

    public static readonly StratCue Spread = new(
        "Stray Entropy: spread",
        role => "Stray Entropy puts a 5y AoE on everyone, and two overlapping kill. "
                + (IsInner(role)
                    ? "Tanks and melee spread around Kefka inside max melee but off his centre, as far from each other as that allows."
                    : "Healers and ranged go out to about 13y from Kefka on their own bearing and push about 10y apart, leaving the boss ring to the melee."),
        Entropy);
}
