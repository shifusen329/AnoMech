using System;
using System.Collections.Generic;
using System.Linq;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.PartyMit;

// One status as the damage model reads it, on the victim or on the hit's source.
public readonly record struct MitLayer(ushort StatusId, string Name, Mitigation Value);

// What's left of one barrier on one victim. A per-hit barrier (Panhaima) absorbs up to PerHit from
// each hit and loses a hit each time; any other loses what it absorbs.
public sealed class MitShield(ushort statusId, string name, float amount, int hits = 0)
{
    public ushort StatusId { get; } = statusId;
    public string Name { get; } = name;
    public float Amount { get; private set; } = amount;
    public int HitsLeft { get; private set; } = hits;
    public float PerHit { get; } = amount;
    public bool Spent => HitsLeft > 0 ? false : Amount <= 0f;

    internal float Absorb(float damage)
    {
        if (HitsLeft > 0)
        {
            HitsLeft--;
            if (HitsLeft == 0) Amount = 0f;
            return MathF.Min(damage, PerHit);
        }
        var absorbed = MathF.Min(damage, Amount);
        Amount -= absorbed;
        return absorbed;
    }
}

// Applied: each reduction that counted, as "Name NN%".
public readonly record struct MitHitResult(float Raw, float AfterMit, float Absorbed, float HpLoss, IReadOnlyList<string> Applied);

public static class MitDamage
{
    // kefkasim's order: the class's share of the raw hit (a tank buster takes it whole), every
    // reduction on the victim (each status once, multiplicatively, typed by the hit), the party's
    // debuffs on a source that has them, a bot tank's own cooldowns on a buster, then barriers in
    // the order they went up. An invuln takes nothing.
    public static MitHitResult Apply(MitHitDef hit, MitClass victim, MitProfile profile, IReadOnlyList<MitLayer> victimLayers,
        IReadOnlyList<MitLayer> sourceLayers, IReadOnlyList<MitShield> shields, bool assumeTankCooldowns)
    {
        var raw = (hit.Raw ?? 0f) * (hit.TankBuster ? 1f : profile.Taken(victim));
        var applied = new List<string>();
        if (victimLayers.FirstOrDefault(l => !l.Value.OnEnemy && l.Value.Damage >= 1f) is { Name: not null } invuln)
            return new(raw, 0f, 0f, 0f, [$"{invuln.Name} (invulnerable)"]);

        var taken = raw;
        taken = Reduce(taken, victimLayers.Where(l => !l.Value.OnEnemy && !l.Value.IsShield), hit.Kind, applied);
        if (hit.Source != null)
            taken = Reduce(taken, sourceLayers.Where(l => l.Value.OnEnemy), hit.Kind, applied);
        if (hit.TankBuster && assumeTankCooldowns)
        {
            taken *= 1f - profile.TankCooldowns;
            applied.Add($"own cooldowns {Percent(profile.TankCooldowns)} (assumed)");
        }

        var afterMit = taken;
        var absorbed = 0f;
        foreach (var shield in shields)
        {
            if (taken <= 0f) break;
            if (shield.Spent) continue;
            var a = shield.Absorb(taken);
            taken -= a;
            absorbed += a;
        }
        return new(raw, afterMit, absorbed, taken, applied);
    }

    private static float Reduce(float taken, IEnumerable<MitLayer> layers, DamageKind kind, List<string> applied)
    {
        var seen = new HashSet<ushort>();
        foreach (var layer in layers)
        {
            if (!seen.Add(layer.StatusId)) continue;
            var through = layer.Value.Taken(kind);
            if (through >= 1f) continue;
            taken *= through;
            applied.Add($"{layer.Name} {Percent(1f - through)}");
        }
        return taken;
    }

    // kefkasim's death reason, for a hit that would have killed: what got through, what cut it,
    // and the planned presses that weren't up.
    public static string Explain(MitHitResult r, IReadOnlyList<string> missing)
    {
        var cuts = r.Applied.Count > 0 ? "; " + string.Join(", ", r.Applied) : "";
        var shields = r.Absorbed > 0f ? $"; shields absorbed {Thousands(r.Absorbed)}" : "";
        var text = $"took {Thousands(r.HpLoss)} after mitigation (raw {Thousands(r.Raw)}{cuts}{shields}).";
        if (missing.Count > 0) text += $" Missing from the plan: {string.Join(", ", missing)}.";
        return text;
    }

    private static string Percent(float fraction) => $"{MathF.Round(fraction * 100f):0}%";

    private static string Thousands(float hp) => $"{MathF.Round(hp / 1000f):0}k";
}
