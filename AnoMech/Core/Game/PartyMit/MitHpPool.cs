using System;

namespace AnoMech.Core.Game.PartyMit;

// One member's simulated HP. Healing stands in as a steady refill of healPerSecond of max HP from
// the last hit on, and a hit more than topUpGap seconds after the last lands on full HP.
public sealed class MitHpPool(float max, float topUpGap, float healPerSecond = 0f)
{
    private float? lastHit;
    private float afterLastHit;

    public float Max { get; } = max;
    public float Hp { get; private set; } = max;

    // Returns the HP left, which may be below zero: the caller warns, nobody dies of it.
    public float Land(float now, float loss, bool fresh = false)
    {
        var left = (fresh ? Max : HpAt(now)) - loss;
        afterLastHit = MathF.Max(0f, left);
        Hp = left;
        lastHit = now;
        return left;
    }

    // True when the pool just healed.
    public bool TopUp(float now)
    {
        var hp = HpAt(now);
        if (hp <= Hp) return false;
        Hp = hp;
        return true;
    }

    private float HpAt(float now)
    {
        if (lastHit is not { } last || now - last > topUpGap) return Max;
        return MathF.Min(Max, afterLastHit + healPerSecond * Max * (now - last));
    }
}
