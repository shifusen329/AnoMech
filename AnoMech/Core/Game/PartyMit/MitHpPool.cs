namespace AnoMech.Core.Game.PartyMit;

// One member's simulated HP. Nothing heals; instead a hit more than topUpGap seconds after the last
// lands on full HP, and hits closer together than that pile up.
public sealed class MitHpPool(float max, float topUpGap)
{
    private float? lastHit;

    public float Max { get; } = max;
    public float Hp { get; private set; } = max;

    // Returns the HP left, which may be below zero: the caller warns, nobody dies of it.
    public float Land(float now, float loss, bool fresh = false)
    {
        if (fresh || lastHit is not { } last || now - last > topUpGap) Hp = Max;
        Hp -= loss;
        lastHit = now;
        return Hp;
    }

    // True when the pool just refilled.
    public bool TopUp(float now)
    {
        if (Hp >= Max || lastHit is not { } last || now - last <= topUpGap) return false;
        Hp = Max;
        return true;
    }
}
