using AnoMech.Core.Game.PartyMit;

namespace AnoMech.Scenarios.Umad;

// Dancing Mad's mitigation practice model: each class's max HP at the fight's item level and the
// share of a raw hit it takes. TankCooldowns (a bot tank's own cooldowns on a buster) and
// TopUpGap (how long before HP is back to full) stand in for what isn't simulated: UNVERIFIED.
public static class UmadMitigation
{
    public static readonly MitProfile Profile = new(
        TankMaxHp: 325200f, PhysicalMaxHp: 226600f, MagicalMaxHp: 205200f,
        TankTaken: 0.61f, PhysicalTaken: 1f, MagicalTaken: 0.92f,
        TankCooldowns: 0.69f, TopUpGap: 3f);

    // Kefka's hits; the party's Reprisal, Feint and Addle go on him.
    public static readonly MitSource Kefka = new("Kefka");
}
