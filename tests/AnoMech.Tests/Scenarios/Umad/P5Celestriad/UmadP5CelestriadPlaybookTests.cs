using AnoMech.Core.Game.Party;
using AnoMech.Scenarios.Umad.P5Celestriad;

namespace AnoMech.Tests;

public class UmadP5CelestriadPlaybookTests
{
    private static readonly int[][] Permutations = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
    private static readonly int[] Debuffs = [0, 0, 1, 1, 2, 2, -1, -1];

    // Two seats per element and two free, rotated around the party.
    private static Dictionary<PartyRole, int> DebuffsShiftedBy(int shift)
        => Enumerable.Range(0, 8).ToDictionary(i => (PartyRole)i, i => Debuffs[(i + shift) % 8]);

    // One single tower per element and two for the doubled one, as the scenario lights them.
    private static int[][] ActiveTowers(int[] doubled)
        => [.. doubled.Select(d => Enumerable.Range(0, 3).SelectMany(e => e == d ? new[] { e * 3, e * 3 + 2 } : new[] { e * 3 + 1 }).ToArray())];

    private static IEnumerable<(UmadP5CelestriadState State, string Context)> States()
    {
        foreach (var doubled in Permutations)
            foreach (var sectors in Permutations)
                foreach (var shift in new[] { 0, 3, 5 })
                    foreach (var aero in new[] { 0, 1 })
                    {
                        var state = UmadP5CelestriadState.FromNetworkReplay(
                            doubled, DebuffsShiftedBy(shift), ActiveTowers(doubled), [aero, -1, 1 - aero], sectors)!;
                        yield return (state, $"double {string.Join("", doubled)} sectors {string.Join("", sectors)} shift {shift} aero {aero}");
                    }
    }

    [Test]
    public void CelestriadExplainsEveryMove()
    {
        foreach (var (state, context) in States())
        {
            for (var set = 0; set < 3; set++)
                PlaybookAssert.ExplainsEveryRole(UmadP5CelestriadPlaybook.Towers(state, set), $"{context} set {set}");
            foreach (var set in new[] { 0, 2 })
                PlaybookAssert.ExplainsEveryRole(UmadP5CelestriadPlaybook.CatastrophicChoiceHalf(state, set), $"{context} set {set}");
        }
    }

    // A doubled element's counterclockwise tower goes to its debuffed pair, the clockwise one to
    // the free pair, as UmadP5CelestriadAi.PlaceSet assigns them.
    [Test]
    public void CelestriadNamesEachRolesTowerOfADoubledPair()
    {
        foreach (var (state, context) in States())
            for (var set = 0; set < 3; set++)
                foreach (var role in Enum.GetValues<PartyRole>())
                {
                    var why = UmadP5CelestriadPlaybook.Towers(state, set).Why(role);
                    if (state.PlayerDebuffElement[role] is null)
                        Assert.That(why, Does.Contain("clockwise tower (right"), $"{role} {context} set {set}");
                    else if (state.ElementForSet(role, set) == state.DoubleElement[set])
                        Assert.That(why, Does.Contain("counterclockwise one (left"), $"{role} {context} set {set}");
                    else
                        Assert.That(why, Does.Not.Contain("two lit towers"), $"{role} {context} set {set}");
                }
    }

    [Test]
    public void CelestriadChoiceSendsAeroOutAndEarthIn()
    {
        var state = States().First(s => s.State.AeroVariant[0] == CatastrophicChoice.Aero).State;
        Assert.That(UmadP5CelestriadPlaybook.CatastrophicChoiceHalf(state, 0).Why(PartyRole.MainTank), Does.Contain("outer half of your"));
        Assert.That(UmadP5CelestriadPlaybook.CatastrophicChoiceHalf(state, 2).Why(PartyRole.MainTank), Does.Contain("inner half of your"));
    }
}
