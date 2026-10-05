using AnoMech.Core.UserActions;
using static AnoMech.Core.UserActions.JobId;

namespace AnoMech.Core.Game.PartyMit;

public enum MitClass { Tank, Physical, Magical }

// A fight's HP model: each class's max HP and the share of a raw hit it takes (tanks and magical
// jobs have more defence). TankCooldowns stands in for a bot tank's own cooldowns on a buster;
// between hits HP refills after TopUpGap seconds, standing in for healing.
public sealed record MitProfile(
    float TankMaxHp, float PhysicalMaxHp, float MagicalMaxHp,
    float TankTaken, float PhysicalTaken, float MagicalTaken,
    float TankCooldowns, float TopUpGap)
{
    public static MitClass ClassOf(JobId job) => job switch
    {
        Paladin or Warrior or DarkKnight or Gunbreaker => MitClass.Tank,
        WhiteMage or Astrologian or Scholar or Sage or BlackMage or Summoner or RedMage or Pictomancer => MitClass.Magical,
        _ => MitClass.Physical,
    };

    public float MaxHp(MitClass c) => c switch { MitClass.Tank => TankMaxHp, MitClass.Magical => MagicalMaxHp, _ => PhysicalMaxHp };

    public float Taken(MitClass c) => c switch { MitClass.Tank => TankTaken, MitClass.Magical => MagicalTaken, _ => PhysicalTaken };
}
