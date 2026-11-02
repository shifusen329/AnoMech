using System;

namespace AnoMech.Core.Game.PartyHp;

// One member's HP. Until real heals exist, healing stands in as a steady refill of healPerSecond of
// max HP from the last hit on, and a hit more than topUpGap seconds after the last lands on full HP.
// A hit is reserved when it resolves and lands with its damage number, so hits in flight count
// against each other.
public sealed class HpPool(float max, float topUpGap, float healPerSecond = 0f)
{
    private float? lastHit;
    private float afterLastHit;

    public float Max { get; } = max;
    public float Hp { get; private set; } = max;
    public float Reserved { get; private set; }

    public void Reserve(float loss) => Reserved += loss;

    public void Release(float loss) => Reserved = MathF.Max(0f, Reserved - loss);

    // What's left at `at` once every reserved hit has landed.
    public float Projected(float at) => HpAt(at) - Reserved;

    // Returns the HP left, which may be below zero.
    public float Land(float now, float loss, bool fresh = false, bool reserved = false)
    {
        if (reserved) Release(loss);
        var left = (fresh ? Max : HpAt(now)) - loss;
        afterLastHit = MathF.Max(0f, left);
        Hp = left;
        lastHit = now;
        return left;
    }

    // One HP, for a member who ran out and lives on.
    public void KeepAlive()
    {
        if (Hp >= 1f) return;
        Hp = 1f;
        afterLastHit = MathF.Max(afterLastHit, 1f);
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
