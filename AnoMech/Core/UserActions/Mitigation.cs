using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.SimObjects;

namespace AnoMech.Core.UserActions;

public enum DamageKind { Physical, Magic }

// What one status contributes to surviving a hit. Everything is a fraction (0.20f = 20%)
// except ShieldPotency and ShieldAbsolute (HP); a status sets only what it grants. ShieldHp and
// MaxHp are fractions of the target's max HP. OnEnemy statuses sit on the attacker and shrink what
// it deals. ShieldHits: a barrier that absorbs up to its size once per hit, that many hits.
public readonly record struct Mitigation(
    float Damage = 0f, float Magic = 0f, float Physical = 0f,
    float ShieldHp = 0f, float ShieldPotency = 0f, float MaxHp = 0f,
    float ShieldAbsolute = 0f, bool OnEnemy = false, int ShieldHits = 0)
{
    // Rough: fitted to a tank's own shields. A healer's potency is worth more HP per point.
    public const float ShieldHpPerPotency = 0.00015f;

    private const float Invulnerable = 1f;

    public bool IsShield => ShieldHp > 0f || ShieldPotency > 0f || ShieldAbsolute > 0f;

    // maxHp converts an absolute barrier; 0 leaves it uncounted.
    public float ShieldFractionOfMaxHp(float maxHp)
        => ShieldHp + ShieldPotency * ShieldHpPerPotency + (maxHp > 0f ? ShieldAbsolute / maxHp : 0f);

    // The share of a hit of `kind` this status lets through.
    public float Taken(DamageKind kind) => (1f - Damage) * (1f - (kind == DamageKind.Magic ? Magic : Physical));

    public static bool IsInvuln(ushort statusId) => ByStatusId.TryGetValue(statusId, out var m) && m.Damage >= Invulnerable;

    public static readonly IReadOnlyDictionary<ushort, Mitigation> ByStatusId = new Dictionary<ushort, Mitigation>
    {
        // Tank role
        [1191] = new(Damage: 0.20f),   // Rampart
        [1193] = new(Damage: 0.10f, OnEnemy: true),   // Reprisal

        // Melee / caster role, MCH
        [1195] = new(Physical: 0.10f, Magic: 0.05f, OnEnemy: true),   // Feint
        [1203] = new(Magic: 0.10f, Physical: 0.05f, OnEnemy: true),   // Addle
        [860]  = new(Damage: 0.10f, OnEnemy: true),   // Dismantled

        // PLD
        [82]   = new(Damage: Invulnerable),                   // Hallowed Ground
        [3829] = new(Damage: 0.40f),   // Guardian
        [3830] = new(ShieldPotency: 1000f),   // Guardian's Will
        [77]   = new(Damage: 0.20f),   // Bulwark (guaranteed block; the real reduction scales with the shield)
        [2674] = new(Damage: 0.15f),   // Holy Sheltron
        [2675] = new(Damage: 0.15f),   // Knight's Resolve
        [1174] = new(Damage: 0.10f),   // Intervention
        [1362] = new(ShieldHp: 0.10f), // Divine Veil
        [196]  = new(Damage: 0.80f),   // Last Bastion

        // WAR
        [409]  = new(Damage: Invulnerable),   // Holmgang
        [3832] = new(Damage: 0.40f),   // Damnation
        [87]   = new(MaxHp: 0.20f),    // Thrill of Battle
        [2678] = new(Damage: 0.10f),   // Bloodwhetting
        [2679] = new(Damage: 0.10f),   // Stem the Flow
        [2680] = new(ShieldPotency: 400f),    // Stem the Tide
        [1858] = new(Damage: 0.10f),   // Nascent Glint
        [1457] = new(ShieldHp: 0.15f), // Shake It Off
        [863]  = new(Damage: 0.80f),   // Land Waker

        // DRK
        [810]  = new(Damage: Invulnerable),   // Living Dead
        [3835] = new(Damage: 0.40f),   // Shadowed Vigil
        [746]  = new(Magic: 0.20f, Physical: 0.10f),   // Dark Mind
        [2682] = new(Damage: 0.10f),   // Oblation
        [1178] = new(ShieldHp: 0.25f), // The Blackest Night
        [1894] = new(Magic: 0.10f, Physical: 0.05f),   // Dark Missionary
        [864]  = new(Damage: 0.80f),   // Dark Force

        // GNB
        [1836] = new(Damage: Invulnerable),   // Superbolide
        [3838] = new(Damage: 0.40f, MaxHp: 0.20f),     // Great Nebula
        [1832] = new(Damage: 0.10f),   // Camouflage
        [2683] = new(Damage: 0.15f),   // Heart of Corundum
        [2684] = new(Damage: 0.15f),   // Clarity of Corundum
        [1839] = new(Magic: 0.10f, Physical: 0.05f),   // Heart of Light
        [1931] = new(Damage: 0.80f),   // Gunmetal Soul

        // Ranged party mitigation
        [1934] = new(Damage: 0.15f),   // Troubadour
        [1951] = new(Damage: 0.15f),   // Tactician
        [1826] = new(Damage: 0.15f),   // Shield Samba
        [2697] = new(ShieldHp: 0.05f), // Improvised Finish (0 Rising Rhythm; stacks scale the instance)

        // Caster
        [2707] = new(Magic: 0.10f),    // Magick Barrier
        [3686] = new(ShieldHp: 0.20f), // Tempera Coat (UNVERIFIED id; 4114 is the other)
        [3687] = new(ShieldHp: 0.10f), // Tempera Grassa (UNVERIFIED id; 4115 is the other)

        // Healers. A healing-potency barrier is the same HP on everyone, sized for DMU gear.
        [1873] = new(Damage: 0.10f),   // Temperance, on the party (UNVERIFIED id; 1872 is the WHM's own)
        [1219] = new(Damage: 0.10f),   // Confession (Plenary Indulgence)
        [3903] = new(ShieldAbsolute: 32563f),   // Divine Caress
        [3896] = new(Damage: 0.10f),   // Sun Sign
        [849]  = new(Damage: 0.10f),   // Collective Unconscious
        [1921] = new(ShieldAbsolute: 32056f),   // Neutral Sect's barrier
        [2711] = new(Damage: 0.10f),   // Desperate Measures (Expedient)
        [299]  = new(Damage: 0.10f),   // Sacred Soil
        [317]  = new(Magic: 0.05f),    // Fey Illumination
        [297]  = new(ShieldAbsolute: 32200f),   // Galvanize (Succor/Concitation; a spread crit Adloquium is bigger)
        [1917] = new(ShieldAbsolute: 19105f),   // Seraphic Veil
        [2618] = new(Damage: 0.10f),   // Kerachole
        [3003] = new(Damage: 0.10f),   // Holos
        [3365] = new(ShieldAbsolute: 26792f),   // Holosakos
        [2609] = new(ShieldAbsolute: 31516f),   // Eukrasian Prognosis (UNVERIFIED id; 2866 is the other)
        [2613] = new(ShieldAbsolute: 19111f, ShieldHits: 5),   // Panhaima
    };

    // The single damage reduction that would let a full-HP target survive the same hit, 0..1:
    // 0.5 means a hit of twice its max HP is survivable. Percentage reductions stack
    // multiplicatively and shrink the hit; shields and bonus max HP add to the pool it lands on.
    // enemyStatusIds are the attacker's: OnEnemy statuses count from there and nowhere else.
    public static float Effective(IEnumerable<ushort> statusIds, DamageKind kind, float maxHp = 0f, IEnumerable<ushort>? enemyStatusIds = null)
    {
        var taken = 1f;
        var pool = 1f;
        foreach (var id in statusIds.Distinct())
        {
            if (!ByStatusId.TryGetValue(id, out var m) || m.OnEnemy) continue;
            taken *= m.Taken(kind);
            pool += m.ShieldFractionOfMaxHp(maxHp) + m.MaxHp;
        }
        foreach (var id in (enemyStatusIds ?? []).Distinct())
            if (ByStatusId.TryGetValue(id, out var m) && m.OnEnemy)
                taken *= m.Taken(kind);
        return 1f - taken / pool;
    }

    // Banked shield as a fraction of max HP: what the gold overlay on the HP bar shows.
    public static float ShieldFraction(IEnumerable<ushort> statusIds, float maxHp)
    {
        var shield = 0f;
        foreach (var id in statusIds.Distinct())
            if (ByStatusId.TryGetValue(id, out var m) && !m.OnEnemy)
                shield += m.ShieldFractionOfMaxHp(maxHp);
        return shield;
    }

    // A shield is used up by the hit it was counted against. A status carries either a
    // shield or a reduction, never both, so dropping the whole status loses nothing else.
    public static void SpendShields(SimCharacter target)
    {
        foreach (var status in target.ActiveStatusSnapshot)
            if (ByStatusId.TryGetValue(status.StatusId, out var m) && m.IsShield)
                target.RemoveStatus(status.StatusId);
    }
}
