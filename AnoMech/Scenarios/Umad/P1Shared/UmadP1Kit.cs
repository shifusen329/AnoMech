using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Umad.P1Shared;

using Constants = UmadP1GravenConstants;

// What the Graven Image scenarios share: Kefka, his helpers, the Graven Image actors and the
// statue props, and the mechanics that repeat across them (Mystery Magic, Double-trouble Trap,
// the tank hits). One per run; the scenario owns the timeline and calls Teardown on the previous
// run's kit, since Game reuses the scenario instance.
internal sealed class UmadP1Kit
{
    // The real fight has at least 10: Mystery Magic's 8 lines and the trap stacks overlap. Ice
    // takes 0-3, thunder 4-7, the trap stacks 8-9.
    public const int HelperCount = 10;
    public const int IceHelperBase = 0;
    public const int ThunderHelperBase = 4;
    public const int TrapHelperBase = 8;

    public const float ArenaRadius = 20f;

    private readonly string tag;
    private readonly bool bindDirector;
    public SimWorld World { get; }
    public SimParty Party { get; }
    public SimEnemy? Kefka { get; private set; }

    private readonly SimEnemy?[] helpers = new SimEnemy?[HelperCount];
    private readonly List<SimEnemy?> gravenImages = [];
    private readonly List<SimEventObject?> props = [];

    public UmadP1Kit(SimWorld world, string tag, bool bindDirector)
    {
        World = world;
        Party = world.Party;
        this.tag = tag;
        this.bindDirector = bindDirector;
    }

    // --- Actors -----------------------------------------------------------------------------

    public void SpawnKefka()
    {
        Kefka = World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: UmadConstants.BNpcBaseId.KefkaP3, NameId: UmadConstants.BNpcNameId.Kefka, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            Placement: new Placement(Vector3.Zero, -float.Pi), LayoutId: UmadConstants.BNpcLayoutId.KefkaP3));
        Kefka?.KeepFacing(Party.Get(PartyRole.MainTank));
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper()
        => World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: UmadConstants.BNpcBaseId.KefkaHelper, NameId: UmadConstants.BNpcNameId.Kefka,
            Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(Vector3.Zero, 0f)));

    // A helper the engine dropped after all is replaced so its casts still land; one still in
    // flight at cast time can't cast this frame.
    public SimEnemy? Helper(int index)
    {
        var helper = helpers[index];
        if (helper is { PacketSpawnFailed: true })
        {
            helper.Despawn();
            helper = helpers[index] = SpawnHelper();
        }
        if (helper is { PacketSpawnPending: true })
        {
            DiagnosticLog.Warn($"[{tag}] helper {index} is still awaiting its packet actor at cast time -- this cast is lost.");
            return null;
        }
        return helper is { IsActive: true } ? helper : null;
    }

    // The hidden Graven Image actors the tethers and statue attacks come from, all spawned at
    // once and moved per mechanic after that.
    public SimEnemy?[] SpawnGravenImages(params Vector3[] positions)
    {
        var spawned = positions.Select(p => World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: UmadConstants.BNpcBaseId.GravenImage, NameId: UmadConstants.BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(p, 0f), LayoutId: UmadConstants.BNpcLayoutId.GravenImage))).ToArray();
        gravenImages.AddRange(spawned);
        return spawned;
    }

    public SimEventObject? SpawnProp(uint eobjId, uint entityId, uint arg2, Vector3 position)
    {
        var prop = World.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = eobjId,
            EventId = bindDirector ? Constants.EObjId.PropEventId : 0u,
            EntityId = entityId,
            TargetableStatus = 5,
            Arg2 = arg2,
            Placement = new Placement(position, 0f),
            TimelineState = Constants.EObjId.SpawnState,
        });
        if (prop == null)
            DiagnosticLog.Warn($"[{tag}] Statue prop 0x{eobjId:X} at {position} failed to spawn -- the mechanic still resolves, the statue just won't show.");
        props.Add(prop);
        return prop;
    }

    // Gravitas' puddle where it landed; Teardown clears any still standing.
    public SimEventObject? SpawnPuddle(Vector3 position, int serial)
    {
        var puddle = World.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = Constants.Puddle.EObjId,
            EventId = bindDirector ? Constants.EObjId.PropEventId : 0u,
            TargetableStatus = 5,
            Arg2 = ((Constants.Puddle.Arg2Serial + (uint)serial) << 16) | 0x3U,
            Placement = new Placement(position, 0f),
        });
        props.Add(puddle);
        return puddle;
    }

    public static void Beat(SimEventObject? prop, (uint State, uint Bitmask) beat) => prop?.PlayBeat(beat.State, beat.Bitmask);

    // The colossus, already standing in the state its beats before the scenario leave it in.
    private SimEventObject? colossus;

    public void SpawnColossus(ushort timelineState)
    {
        colossus = World.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = Constants.EObjId.GravenStatue,
            LayoutId = Constants.EObjId.GravenStatueLayoutId,
            EventId = bindDirector ? Constants.EObjId.PropEventId : 0u,
            EntityId = 0x4000EB8Au,
            TargetableStatus = 5,
            Arg2 = Constants.EObjId.GravenStatueArg2,
            Placement = new Placement(Vector3.Zero, 0f),
            TimelineState = timelineState,
        });
        if (colossus == null) DiagnosticLog.Warn($"[{tag}] The colossus (0x1EBFB4) failed to spawn -- EObj pool full?");
        props.Add(colossus);
    }

    public void BeatColossus((uint State, uint Bitmask) beat) => Beat(colossus, beat);

    // The props' SharedGroups outlive the EObj actor, so each is parked back in the hidden state
    // first, or a reset mid-run leaves a statue standing until the zone reloads.
    public void Teardown()
    {
        foreach (var prop in props)
        {
            if (prop == null) continue;
            if (prop.IsAlive) Beat(prop, StatueBeat.Despawn);
            prop.Despawn();
        }
        props.Clear();
        colossus = null;
        foreach (var actor in gravenImages) actor?.Despawn();
        gravenImages.Clear();
        for (var i = 0; i < helpers.Length; i++)
        {
            helpers[i]?.Despawn();
            helpers[i] = null;
        }
    }

    // --- Kefka ------------------------------------------------------------------------------

    public void KefkaCast(uint actionId, float animationLock) => Kefka?.Cast(actionId, animationLock: animationLock);

    // Kefka holds the main tank the whole time in these scenarios (no swap modelled).
    public SimCharacter? AggroTank
        => Party.Get(PartyRole.MainTank) is { } mt && mt.IsAlive() ? mt
         : Party.Get(PartyRole.OffTank) is { } ot && ot.IsAlive() ? ot : null;

    public void AutoAttack()
    {
        if (AggroTank is { } tank) Kefka?.Cast(UmadActions.AutoAttack, tank);
    }

    public void Hyperdrive()
    {
        if (AggroTank is { } tank) Kefka?.Cast(UmadActions.Hyperdrive, tank);
    }

    public void RevoltingRuin(bool followUp)
    {
        if (AggroTank is { } tank) Kefka?.Cast(followUp ? UmadActions.RevoltingRuinIIIFollowUp : UmadActions.RevoltingRuinIII, tank);
    }

    public void LightOfJudgment() => Kefka?.Cast(UmadActions.LightOfJudgmentP1);

    // The battle talk names its speaker by entity id.
    public void KefkaLine(uint line)
    {
        if (Kefka is { } boss)
            World.Map.DirectorUpdate(Constants.KefkaLine.Category, line, Constants.KefkaLine.Arg2, Constants.KefkaLine.NameId, boss.EntityId);
    }

    // --- Mystery Magic ----------------------------------------------------------------------

    public void AttachIceOrb(IceRoll ice)
        => Kefka?.AttachLockonVfx(ice.IsLie ? Constants.LockonId.ColdLie : Constants.LockonId.ColdTruth);

    public void AttachThunderOrb(ThunderRoll thunder)
        => Kefka?.AttachLockonVfx(thunder.IsLie ? Constants.LockonId.LightningLie : Constants.LockonId.LightningTruth);

    public void AttachFireOrbAndMarkers(FireRoll fire)
    {
        Kefka?.AttachLockonVfx(fire.IsLie ? Constants.LockonId.FireLie : Constants.LockonId.FireTruth);
        foreach (var role in UmadP1Roles.All)
        {
            if (Party.Get(role) is not { } member) continue;
            if (!fire.ShowsStackIcon) member.AttachLockonVfx(Constants.LockonId.FireSpread);
            else if (role == fire.StackSupport || role == fire.StackDps) member.AttachLockonVfx(Constants.LockonId.FireStack);
        }
    }

    // Truth: the real cone on the real slots only. Lie: the untelegraphed one (still lethal) on the
    // real slots and the harmless telegraph on the safe ones.
    public void CastIce(IceRoll ice)
    {
        for (var i = 0; i < 4; i++)
        {
            var real = ice.IsReal(i);
            if (!real && !ice.IsLie) continue;
            if (Helper(IceHelperBase + i) is not { } helper) continue;
            helper.SetPosition(new Placement(Vector3.Zero, IceRoll.Heading(i)));
            if (real) helper.Cast(ice.IsLie ? UmadActions.BlizzardIIIBlowoutLie : UmadActions.BlizzardIIIBlowout);
            else helper.Cast(UmadConstants.ActionId.BlizzardIIIBlowout_FakeOmen, animationLock: 1.1f);
        }
        DiagnosticLog.Info($"[{tag}] Ice: real slots offset {ice.RealOffset} (NE hit={ice.HitsNorthEast}), lie={ice.IsLie}.");
    }

    // Truth: the telegraphed real lines only. Lie: the untelegraphed reals and the harmless
    // telegraph on the others.
    public void CastThunder(ThunderRoll thunder)
    {
        for (var i = 0; i < 4; i++)
        {
            var real = thunder.IsReal(i);
            if (!real && !thunder.IsLie) continue;
            if (Helper(ThunderHelperBase + i) is not { } helper) continue;
            helper.SetPosition(thunder.Anchor(i));
            if (real) helper.Cast(thunder.IsLie ? UmadActions.ThrummingThunderReal2 : UmadActions.ThrummingThunderReal1);
            else helper.Cast(UmadConstants.ActionId.ThrummingThunderIII_FakeOmen, animationLock: 1.1f);
        }
        DiagnosticLog.Info($"[{tag}] Thunder: offset {thunder.RealOffset} flipped={thunder.Flipped} lie={thunder.IsLie}.");
    }

    public void ResolveFire(FireRoll fire)
    {
        if (fire.IsStack)
        {
            CastFireStack(fire.StackSupport, 0);
            CastFireStack(fire.StackDps, 1);
            return;
        }
        var helperIndex = 0;
        foreach (var role in UmadP1Roles.All)
        {
            if (Party.Get(role) is not { } member || !member.IsAlive()) continue;
            Helper(helperIndex++)?.Cast(UmadActions.FlagrantFireSpread, member);
        }
    }

    private void CastFireStack(PartyRole holderRole, int helperIndex)
    {
        if (Party.Get(holderRole) is { } holder && holder.IsAlive())
            Helper(helperIndex)?.Cast(UmadActions.FlagrantFireStack, holder);
    }

    // --- Double-trouble Trap ----------------------------------------------------------------

    public void ApplyTrap(PartyRole role, float seconds)
        => Party.Get(role)?.AddStatus(Constants.StatusId.DoubleTroubleTrap, seconds);

    public void ResolveTrapStack(PartyRole holderRole, int helperIndex)
    {
        if (Party.Get(holderRole) is { } holder && holder.IsAlive())
            Helper(helperIndex)?.Cast(UmadActions.DoubleTroubleTrapStack, holder);
    }

    // The trap jumps to one of the players its stack hit, by role order.
    public static PartyRole? TrapJumpTarget(IEnumerable<SimCharacter> hit, int pick)
    {
        var roles = hit.OfType<ISimPartyMember>().Select(m => m.Role).OrderBy(r => r).ToList();
        return roles.Count == 0 ? null : roles[pick % roles.Count];
    }
}

// The statue props' EObjAnimation beats, packed (Param1, Param2) as the log's 273 lines carry
// them: shown 1/2, gone 10/20, telegraph 40/80, attack 100/200, and the hidden state.
internal static class StatueBeat
{
    public static readonly (uint State, uint Bitmask) Appear = (0x0001, 0x0002);
    public static readonly (uint State, uint Bitmask) Vanish = (0x0010, 0x0020);
    public static readonly (uint State, uint Bitmask) Telegraph = (0x0040, 0x0080);
    public static readonly (uint State, uint Bitmask) Fire = (0x0100, 0x0200);
    public static readonly (uint State, uint Bitmask) Despawn = (0x0004, 0x0008);

    // The colossus's own stages: appear 11s into the pull, then 59.97s and 127.75s. Each leaves it
    // resting in its State.
    public static readonly (uint State, uint Bitmask) ColossusStage2 = (0x0010, 0x0020);
    public static readonly (uint State, uint Bitmask) ColossusStage3 = (0x0040, 0x0080);
}
