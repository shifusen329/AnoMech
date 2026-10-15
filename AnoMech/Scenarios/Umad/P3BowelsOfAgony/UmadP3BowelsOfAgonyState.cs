using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Umad.P3BlackHole;
using AnoMech.Scenarios.Umad.P3LimitCut;

namespace AnoMech.Scenarios.Umad.P3BowelsOfAgony;

public sealed class UmadP3BowelsOfAgonyScenarioObjects
{
    public SimEnemy? Kefka { get; set; }
    public SimEnemy? Chaos { get; set; }
    public SimEnemy? Exdeath { get; set; }
}

// Per-run randomization: the wind crystal on any intercardinal with water a quarter-turn either
// way and fire across from water, either element short, a support and a DPS on each element, the
// winds split as in Limit Cut, and either Implosion.
public sealed class UmadP3BowelsOfAgonyState
{
    private readonly Rng rng = Rng.Detached;

    public UmadP3BowelsOfAgonyScenarioObjects Objects { get; } = new();
    public int WindSpot { get; }
    public int WaterSpot { get; }
    public Element ShortElement { get; }
    public IReadOnlyList<PartyRole> WaterHolders { get; }
    public IReadOnlyList<PartyRole> FireHolders { get; }
    public IReadOnlyDictionary<PartyRole, Wind> Winds { get; }
    public bool Latitudinal { get; }
    public ThunderIIIAssignment ThunderPlan { get; }

    public int FireSpot => Mod8(WaterSpot + 4);
    public int SpotOf(Element element) => element == Element.Water ? WaterSpot : FireSpot;
    public Element LongElement => ShortElement == Element.Water ? Element.Fire : Element.Water;
    public int ShortSpot => SpotOf(ShortElement);
    public int LongSpot => SpotOf(LongElement);
    public IReadOnlyList<PartyRole> HoldersOf(Element element) => element == Element.Water ? WaterHolders : FireHolders;
    public IReadOnlyList<PartyRole> ShortHolders => HoldersOf(ShortElement);
    public IReadOnlyList<PartyRole> LongHolders => HoldersOf(LongElement);
    public Element? ElementOf(PartyRole role)
        => WaterHolders.Contains(role) ? Element.Water : FireHolders.Contains(role) ? Element.Fire : null;
    public IReadOnlyList<PartyRole> Unmarked => AllRoles.Where(r => ElementOf(r) == null).ToList();

    // +1 when a crystal sits a quarter-turn counter-clockwise of wind on the map, -1 clockwise.
    public int SideOf(int spot) => Mod8(spot - WindSpot) == 2 ? 1 : -1;

    public UmadP3BowelsOfAgonyState(Rng rng, SimParty party, UmadP3BowelsOfAgonyStateOverrides overrides)
    {
        this.rng = rng;
        WindSpot = overrides.WindSpot ?? rng.NextObj(1, 3, 5, 7);
        WaterSpot = Mod8(WindSpot + ((overrides.WaterClockwise ?? rng.NextBool()) ? -2 : 2));
        ShortElement = overrides.ShortElement ?? (rng.NextBool() ? Element.Water : Element.Fire);
        Latitudinal = overrides.Latitudinal ?? rng.NextBool();
        ThunderPlan = overrides.ThunderPlan;
        (WaterHolders, FireHolders) = BuildHolders(party.PlayerRole, overrides);
        Winds = BuildWinds(party.PlayerRole, overrides);
    }

    // Each element goes to one support and one DPS. A forced debuff takes its place outright while
    // that half has room; a second seat asking for the same one keeps the fight's own roll.
    private (List<PartyRole> Water, List<PartyRole> Fire) BuildHolders(PartyRole localPlayerRole, UmadP3BowelsOfAgonyStateOverrides overrides)
    {
        var picks = new Dictionary<PartyRole, ElementDebuff>();
        foreach (var (role, debuff) in overrides.Debuff.Resolve(localPlayerRole))
        {
            var half = IsSupport(role);
            var room = debuff == ElementDebuff.None ? 2 : 1;
            if (picks.Count(kv => kv.Value == debuff && IsSupport(kv.Key) == half) >= room)
            {
                DiagnosticLog.Warn($"[UmadP3BowelsOfAgony] {role} asked for {debuff}, already taken -- leaving {role} to the roll.");
                continue;
            }
            picks[role] = debuff;
        }
        foreach (var half in new[] { true, false })
        {
            var open = rng.Shuffle(AllRoles.Where(r => IsSupport(r) == half && !picks.ContainsKey(r)).ToArray()).ToList();
            foreach (var debuff in new[] { ElementDebuff.DynamicFluid, ElementDebuff.Entropy })
                if (!picks.Any(kv => kv.Value == debuff && IsSupport(kv.Key) == half) && open.Count > 0)
                {
                    picks[open[0]] = debuff;
                    open.RemoveAt(0);
                }
        }
        return (Ordered(picks.Where(kv => kv.Value == ElementDebuff.DynamicFluid).Select(kv => kv.Key)),
            Ordered(picks.Where(kv => kv.Value == ElementDebuff.Entropy).Select(kv => kv.Key)));
    }

    private Dictionary<PartyRole, Wind> BuildWinds(PartyRole localPlayerRole, UmadP3BowelsOfAgonyStateOverrides overrides)
    {
        var headwindTarget = rng.NextInt(100) switch
        {
            < 39 => 4,
            < 71 => 5,
            < 96 => 3,
            _ => 2,
        };
        var winds = new Dictionary<PartyRole, Wind>();
        foreach (var (role, wind) in overrides.Wind.Resolve(localPlayerRole)) winds[role] = wind;
        var forcedHeadwinds = winds.Count(kv => kv.Value == Wind.Headwind);
        var remaining = rng.Shuffle(AllRoles.Where(r => !winds.ContainsKey(r)).ToArray()).ToList();
        var stillHeadwind = Math.Clamp(headwindTarget - forcedHeadwinds, 0, remaining.Count);
        for (var i = 0; i < remaining.Count; i++)
            winds[remaining[i]] = i < stillHeadwind ? Wind.Headwind : Wind.Tailwind;
        return winds;
    }

    private UmadP3BowelsOfAgonyState(int windSpot, int waterSpot, Element shortElement, IReadOnlyList<PartyRole> waterHolders,
        IReadOnlyList<PartyRole> fireHolders, IReadOnlyDictionary<PartyRole, Wind> winds, bool latitudinal, ThunderIIIAssignment thunderPlan)
    {
        WindSpot = windSpot;
        WaterSpot = waterSpot;
        ShortElement = shortElement;
        WaterHolders = waterHolders;
        FireHolders = fireHolders;
        Winds = winds;
        Latitudinal = latitudinal;
        ThunderPlan = thunderPlan;
    }

    public static UmadP3BowelsOfAgonyState? FromNetworkReplay(int windSpot, int waterSpot, Element shortElement, IReadOnlyList<PartyRole> waterHolders,
        IReadOnlyList<PartyRole> fireHolders, IReadOnlyList<PartyRole> headwinds, bool latitudinal, ThunderIIIAssignment thunderPlan)
    {
        if (windSpot is not (1 or 3 or 5 or 7) || Mod8(waterSpot - windSpot) is not (2 or 6)) return null;
        if (!Enum.IsDefined(shortElement) || !Enum.IsDefined(thunderPlan)) return null;
        var holders = waterHolders.Concat(fireHolders).ToList();
        if (waterHolders.Count != 2 || fireHolders.Count != 2 || holders.Distinct().Count() != 4 || holders.Any(r => !AllRoles.Contains(r))) return null;
        if (waterHolders.Count(IsSupport) != 1 || fireHolders.Count(IsSupport) != 1) return null;
        if (headwinds.Any(r => !AllRoles.Contains(r))) return null;
        return new(windSpot, waterSpot, shortElement, Ordered(waterHolders), Ordered(fireHolders),
            AllRoles.ToDictionary(r => r, r => headwinds.Contains(r) ? Wind.Headwind : Wind.Tailwind), latitudinal, thunderPlan);
    }

    public static bool IsSupport(PartyRole role) => role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.RegenHealer or PartyRole.ShieldHealer;

    // Party-list order: the support first.
    private static List<PartyRole> Ordered(IEnumerable<PartyRole> roles) => roles.OrderBy(r => Array.IndexOf(AllRoles, r)).ToList();

    private static int Mod8(int v) => ((v % 8) + 8) % 8;

    public static readonly PartyRole[] AllRoles =
    [
        PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer,
        PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps,
    ];
}
