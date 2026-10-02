using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AnoMech.Scenarios.Umad.P1Shared;

using Constants = UmadP1GravenConstants;
using CastBar = P1TeleTrouncing.UmadP1TeleTrouncingConstants.CastBar;
using AnimationLock = P1TeleTrouncing.UmadP1TeleTrouncingConstants.AnimationLock;

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
    public const float MagicVulnerabilityUpSeconds = 0.96f;
    private const float IceHalfAngle = MathF.PI / 4f;
    private const float TrapRadius = 6f;
    private const int TrapStackRequiredCount = 4;
    private const float HitVfxDuration = 2f;

    private readonly string tag;
    private readonly PropBeatMode beatMode;
    public SimWorld World { get; }
    public SimParty Party { get; }
    public DamageSolver Damage { get; }
    public SimEnemy? Kefka { get; private set; }

    private readonly SimEnemy?[] helpers = new SimEnemy?[HelperCount];
    private readonly List<(SimEnemy? Actor, Vector3 Position)> gravenImages = [];
    private readonly List<SimEventObject?> props = [];
    private SimEventObject? colossus;
    private (uint State, uint Bitmask)[] colossusCatchUp = [];
    private int colossusStage;
    private float colossusReadyAt;
    private readonly List<(SimEnemy Helper, uint ActionId)> iceReals = [];
    private readonly List<(SimEnemy Helper, uint ActionId)> thunderReals = [];

    public UmadP1Kit(SimWorld world, string tag, PropBeatMode beatMode)
    {
        World = world;
        Party = world.Party;
        this.tag = tag;
        this.beatMode = beatMode;
        Damage = new DamageSolver(Party);
        // A second hit while carrying it is lethal: Wave Cannon twice, or a vuln in a tower.
        Damage.SetStatuses(DamageType.Magic, UmadConstants.StatusId.MagicVulnerabilityUp);
    }

    // --- Actors -----------------------------------------------------------------------------

    public void SpawnKefka()
    {
        Kefka = World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: Constants.BNpcBaseId.Kefka, NameId: Constants.BNpcNameId.Kefka, Level: 100,
            Targetable: true, EnemyList: EnemyListMode.Always, IsVisible: true,
            Placement: new Placement(Vector3.Zero, -float.Pi)));
        for (var i = 0; i < helpers.Length; i++) helpers[i] = SpawnHelper();
    }

    private SimEnemy? SpawnHelper()
    {
        var real = World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: UmadConstants.BNpcBaseId.KefkaHelper, NameId: UmadConstants.BNpcNameId.Kefka,
            Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
            Placement: new Placement(Vector3.Zero, 0f),
            NpcSpawnTemplate: UmadRealPackets.HelperNpcSpawn, PacketSpawnEnableDraw: true));
        return real ?? SpawnPlainHelper();
    }

    private SimEnemy? SpawnPlainHelper()
        => World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: UmadConstants.BNpcBaseId.KefkaHelper, NameId: UmadConstants.BNpcNameId.Kefka,
            Level: 1, Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
            Placement: new Placement(Vector3.Zero, 0f)));

    // A packet helper the engine dropped is replaced by the plain KefkaHelper (no action VFX).
    public SimEnemy? Helper(int index)
    {
        var helper = helpers[index];
        if (helper is { PacketSpawnFailed: true })
        {
            helper.Despawn();
            helper = helpers[index] = SpawnPlainHelper();
        }
        if (helper is { PacketSpawnPending: true })
        {
            DiagnosticLog.Warn($"[{tag}] helper {index} is still awaiting its packet actor at cast time -- this cast is lost.");
            return null;
        }
        return helper is { IsActive: true } ? helper : null;
    }

    // The hidden Graven Image actors the tethers and statue attacks come from, all spawned at
    // once (a packet spawn is pending for a few frames) and moved per mechanic after that.
    public SimEnemy?[] SpawnGravenImages(params Vector3[] positions)
    {
        var spawned = new SimEnemy?[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            spawned[i] = SpawnGravenImage(positions[i]);
            gravenImages.Add((spawned[i], positions[i]));
        }
        return spawned;
    }

    private SimEnemy? SpawnGravenImage(Vector3 position)
    {
        var real = World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: Constants.BNpcBaseId.GravenImage, NameId: Constants.BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never,
            Placement: new Placement(position, 0f), NpcSpawnTemplate: UmadRealPackets.GravenImageNpcSpawn));
        if (real != null) return real;
        DiagnosticLog.Warn($"[{tag}] Graven Image packet spawn was refused -- falling back to a plain spawn.");
        return SpawnLalafellDoppel(position);
    }

    // Graven Image is ModelChara Type=0; the party doppels' Lalafell values, as Tele-trouncing's.
    private static readonly CustomizeData GravenImageCustomize = BuildGravenImageCustomize();

    private static CustomizeData BuildGravenImageCustomize()
    {
        var c = new CustomizeData
        {
            Race = 3, Tribe = 5, Sex = 1, BodyType = 1, Height = 50,
            Face = 1, Hairstyle = 1, SkinColor = 1,
            EyeColorRight = 1, EyeColorLeft = 1, HairColor = 1, HighlightsColor = 1, TattooColor = 1,
            Eyebrows = 1, Nose = 1, Jaw = 1, LipColorFurPattern = 1,
            MuscleMass = 50, TailShape = 1, BustSize = 50, FacePaintColor = 1,
        };
        var raw = System.Runtime.InteropServices.MemoryMarshal.AsBytes(new Span<CustomizeData>(ref c));
        raw[0x10] = 1; // EyeShape
        raw[0x13] = 1; // Mouth
        return c;
    }

    // Hidden like the real actor: only its tethers and casts should show.
    private SimEnemy? SpawnLalafellDoppel(Vector3 position)
        => World.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: Constants.BNpcBaseId.GravenImage, NameId: Constants.BNpcNameId.GravenImage, Level: 100,
            Targetable: false, EnemyList: EnemyListMode.Never, IsVisible: false,
            Placement: new Placement(position, 0f), Customize: GravenImageCustomize));

    // Returns the replacement when the engine dropped a packet spawn, so the caller's array
    // stays current.
    public SimEnemy? ReplaceIfDropped(SimEnemy? actor)
    {
        if (actor is not { PacketSpawnFailed: true }) return actor;
        DiagnosticLog.Warn($"[{tag}] Graven Image packet spawn was dropped by the engine -- falling back to a plain spawn.");
        var position = actor.Position;
        actor.Despawn();
        return SpawnLalafellDoppel(position);
    }

    public SimEventObject? SpawnProp(uint eobjId, uint entityId, uint arg2, Vector3 position, bool bindDirector, bool forceActive)
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
            MuteSound = true,
            ForceSharedGroupActive = forceActive,
        });
        if (prop == null)
            DiagnosticLog.Warn($"[{tag}] Statue prop 0x{eobjId:X} at {position} failed to spawn -- the mechanic still resolves, the statue just won't show.");
        props.Add(prop);
        return prop;
    }

    public void Beat(SimEventObject? prop, (uint State, uint Bitmask) beat) => prop?.PlayBeat(beat.State, beat.Bitmask, beatMode);

    // The colossus as its real pull-start spawn (hidden), then caught up to the stage the fight
    // has reached by the scenario's start, one timeline at a time once its SharedGroup attaches.
    public void SpawnColossus((uint State, uint Bitmask)[] catchUp, bool bindDirector, bool forceActive)
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
            TimelineState = Constants.EObjId.SpawnState,
            MuteSound = true,
            ForceSharedGroupActive = forceActive,
        });
        colossusCatchUp = catchUp;
        colossusStage = 0;
        colossusReadyAt = 0f;
        if (colossus == null) DiagnosticLog.Warn($"[{tag}] The colossus (0x1EBFB4) failed to spawn -- EObj pool full?");
    }

    public void BeatColossus((uint State, uint Bitmask) beat) => Beat(colossus, beat);

    private const float ColossusCatchUpMaxWait = 8f;

    // Stages 0..n-1 are the beats; stage n unmutes the prop once the last one has played, so
    // its later stage changes sound.
    public void TickColossus(float elapsed)
    {
        if (colossus == null || colossusStage > colossusCatchUp.Length) return;
        if (!colossus.IsSharedGroupAttached || elapsed < colossusReadyAt) return;
        var waitedOut = elapsed >= colossusReadyAt + ColossusCatchUpMaxWait;
        if (colossusStage > 0 && colossus.IsSharedGroupTimelinePlaying && !waitedOut) return;
        if (colossusStage == colossusCatchUp.Length)
        {
            colossus.MuteSound = false;
            colossusStage++;
            return;
        }
        Beat(colossus, colossusCatchUp[colossusStage]);
        colossusStage++;
        colossusReadyAt = elapsed + 0.5f;
    }

    // The props' SharedGroups outlive the EObj actor, so each is parked back in the hidden state
    // first, or a reset mid-run leaves a statue standing until the zone reloads.
    public void Teardown()
    {
        foreach (var prop in props.Append(colossus))
        {
            if (prop == null) continue;
            if (prop.IsAlive) Beat(prop, StatueBeat.Despawn);
            prop.Despawn();
        }
        props.Clear();
        colossus = null;
        foreach (var (actor, _) in gravenImages) actor?.Despawn();
        gravenImages.Clear();
        for (var i = 0; i < helpers.Length; i++)
        {
            helpers[i]?.Despawn();
            helpers[i] = null;
        }
        iceReals.Clear();
        thunderReals.Clear();
    }

    // --- Kefka ------------------------------------------------------------------------------

    public void KefkaCast(uint actionId, float castSeconds, float animationLock, SimCharacter? target = null)
        => Kefka?.Cast(actionId, castSeconds: castSeconds, fireDelay: castSeconds > 0f ? CastBar.FireDelay : null,
            animationLock: animationLock, targetId: target?.GameObjectId ?? Kefka?.GameObjectId);

    public void AttachKefkaLockon(uint lockonId) => Kefka?.AttachLockonVfx(lockonId, persistent: false);

    // Kefka holds the main tank the whole time in these scenarios (no swap modelled).
    public SimCharacter? AggroTank
        => Party.Get(PartyRole.MainTank) is { } mt && mt.IsAlive() ? mt
         : Party.Get(PartyRole.OffTank) is { } ot && ot.IsAlive() ? ot : null;

    public void FaceAggroTank()
    {
        if (Kefka is { } boss && AggroTank is { } tank) boss.Face(tank.Position);
    }

    public void AutoAttack()
    {
        if (AggroTank is not { } tank) return;
        Kefka?.Cast(UmadConstants.ActionId.AutoAttack1, castSeconds: 0f, targetId: tank.GameObjectId, animationLock: AnimationLock.AutoAttack);
    }

    // An untelegraphed 5y circle on the aggro tank: lethal to anyone else in it.
    public void Hyperdrive()
    {
        if (AggroTank is not { } tank) return;
        Kefka?.Cast(Constants.ActionId.Hyperdrive, castSeconds: 0f, targetId: tank.GameObjectId, animationLock: AnimationLock.Helper);
        Damage.Resolve(IPositioned.From(tank.Position), Constants.ActionId.Hyperdrive, [DamageType.TankBuster], []);
    }

    // Revolting Ruin III: a 90-degree cone at the aggro tank, lethal to anyone else in it (the
    // width is kefkasim's, not measured).
    public const float RevoltingRuinHalfAngle = MathF.PI / 4f;

    public void ResolveRevoltingRuin(uint actionId, bool instantCast)
    {
        if (AggroTank is not { } tank) return;
        FaceAggroTank();
        if (instantCast)
            Kefka?.Cast(actionId, castSeconds: 0f, targetId: tank.GameObjectId, animationLock: AnimationLock.Helper);
        Damage.Resolve(Kefka, actionId, [DamageType.TankBuster], [], size: RevoltingRuinHalfAngle);
    }

    // Static: RunInstanceEvents runs on peers too, which never build a kit.
    public static void DirectorLine(SimWorld world, uint line, bool broadcast)
        => world.Map.DirectorUpdate(Constants.KefkaLine.Category, line, Constants.KefkaLine.Arg2,
            Constants.KefkaLine.NameId, Constants.KefkaLine.Actor, broadcast: broadcast);

    // --- Mystery Magic ----------------------------------------------------------------------

    public void AttachIceOrb(IceRoll ice)
        => AttachKefkaLockon(ice.IsLie ? Constants.LockonId.ColdLie : Constants.LockonId.ColdTruth);

    public void AttachThunderOrb(ThunderRoll thunder)
        => AttachKefkaLockon(thunder.IsLie ? Constants.LockonId.LightningLie : Constants.LockonId.LightningTruth);

    public void AttachFireOrbAndMarkers(FireRoll fire)
    {
        AttachKefkaLockon(fire.IsLie ? Constants.LockonId.FireLie : Constants.LockonId.FireTruth);
        foreach (var role in UmadP1Roles.All)
        {
            if (Party.Get(role) is not { } member) continue;
            if (!fire.ShowsStackIcon) member.AttachLockonVfx(Constants.LockonId.FireSpread, persistent: false);
            else if (role == fire.StackSupport || role == fire.StackDps) member.AttachLockonVfx(Constants.LockonId.FireStack, persistent: false);
        }
    }

    // Truth: Real on the real slots only. Lie: FakeAnim (untelegraphed, still lethal) on the real
    // slots and FakeOmen (telegraphed, harmless) on the safe ones.
    public void CastIce(IceRoll ice)
    {
        iceReals.Clear();
        for (var i = 0; i < 4; i++)
        {
            var real = ice.IsReal(i);
            var actionId = real
                ? ice.IsLie ? UmadConstants.ActionId.BlizzardIIIBlowout_FakeAnim : UmadConstants.ActionId.BlizzardIIIBlowout_Real
                : ice.IsLie ? UmadConstants.ActionId.BlizzardIIIBlowout_FakeOmen : 0u;
            if (actionId == 0 || Helper(IceHelperBase + i) is not { } helper) continue;
            helper.SetPosition(new Placement(Vector3.Zero, IceRoll.Heading(i)));
            helper.Cast(actionId, castSeconds: CastBar.Long, fireDelay: CastBar.FireDelay, animationLock: AnimationLock.Helper);
            if (real) iceReals.Add((helper, actionId));
        }
        DiagnosticLog.Info($"[{tag}] Ice: real slots offset {ice.RealOffset} (NE hit={ice.HitsNorthEast}), lie={ice.IsLie}.");
    }

    public void ResolveIce()
    {
        foreach (var (helper, actionId) in iceReals)
            Damage.Resolve(helper, actionId, [DamageType.Lethal], [], size: IceHalfAngle);
        iceReals.Clear();
    }

    // Truth: Real1 on the reals only. Lie: Real2 (untelegraphed) on the reals and the harmless
    // Fake telegraph on the others.
    public void CastThunder(ThunderRoll thunder)
    {
        thunderReals.Clear();
        var realId = thunder.IsLie ? Constants.ActionId.ThrummingThunderReal2 : Constants.ActionId.ThrummingThunderReal1;
        for (var i = 0; i < 4; i++)
        {
            var real = thunder.IsReal(i);
            if (!real && !thunder.IsLie) continue;
            if (Helper(ThunderHelperBase + i) is not { } helper) continue;
            helper.SetPosition(thunder.Anchor(i));
            var actionId = real ? realId : Constants.ActionId.ThrummingThunderFake;
            helper.Cast(actionId, castSeconds: CastBar.Long, fireDelay: CastBar.FireDelay, animationLock: AnimationLock.Helper);
            if (real) thunderReals.Add((helper, actionId));
        }
        DiagnosticLog.Info($"[{tag}] Thunder: offset {thunder.RealOffset} flipped={thunder.Flipped} lie={thunder.IsLie}.");
    }

    public void ResolveThunder()
    {
        foreach (var (helper, actionId) in thunderReals)
            Damage.Resolve(helper, actionId, [DamageType.Lethal], []);
        thunderReals.Clear();
    }

    public void ResolveFire(FireRoll fire)
    {
        if (fire.IsStack)
        {
            ResolveFireStack(fire.StackSupport, 0);
            ResolveFireStack(fire.StackDps, 1);
            return;
        }
        var hits = new List<SimCharacter>();
        var helperIndex = 0;
        foreach (var role in UmadP1Roles.All)
        {
            if (Party.Get(role) is not { } member || !member.IsAlive()) continue;
            if (Helper(helperIndex++) is not { } helper) continue;
            helper.SetPosition(new Placement(member.Position, 0f));
            helper.Cast(Constants.ActionId.FlagrantFireSpread, castSeconds: 0f, targetId: member.GameObjectId, animationLock: AnimationLock.Helper);
            hits.AddRange(Damage.Resolve(helper, Constants.ActionId.FlagrantFireSpread, [DamageType.Lethal], [], killTargets: false));
        }
        // Two spreads on one player is lethal.
        foreach (var group in hits.GroupBy(c => c))
        {
            var count = group.Count();
            group.Key.AddStatus(UmadConstants.StatusId.MagicVulnerabilityUp, MagicVulnerabilityUpSeconds);
            for (var i = 0; i < count; i++)
                Damage.ApplyDamage(group.Key, 0.6f, Constants.ActionId.FlagrantFireSpread, "fire spread", lethal: count >= 2 && i == count - 1);
        }
    }

    private void ResolveFireStack(PartyRole holderRole, int helperIndex)
    {
        if (Party.Get(holderRole) is not { } holder || !holder.IsAlive()) return;
        var caster = Helper(helperIndex);
        caster?.SetPosition(new Placement(holder.Position, 0f));
        caster?.Cast(Constants.ActionId.FlagrantFireStack, castSeconds: 0f, targetId: holder.GameObjectId, animationLock: AnimationLock.Helper);
        Damage.Resolve(holder, Constants.ActionId.FlagrantFireStack, [DamageType.Magic],
            [(UmadConstants.StatusId.MagicVulnerabilityUp, MagicVulnerabilityUpSeconds)], stackMinTargets: 4);
    }

    // --- Double-trouble Trap ----------------------------------------------------------------

    public void ApplyTrap(PartyRole role, float seconds)
        => Party.Get(role)?.AddStatus(Constants.StatusId.DoubleTroubleTrap, seconds);

    // The holder's 6y stack: exactly 4 bodies (holder included) knocks the other 3 away 14y; any
    // other count kills everyone but the holder. Returns who it hit, for the trap's jump.
    public List<SimCharacter> ResolveTrapStack(PartyRole holderRole, int helperIndex, string mechanic)
    {
        if (Party.Get(holderRole) is not { } holder || !holder.IsAlive()) return [];
        var others = new List<SimCharacter>();
        foreach (var role in UmadP1Roles.All)
        {
            if (Party.Get(role) is not { } member || !member.IsAlive() || ReferenceEquals(member, holder)) continue;
            var dx = member.Position.X - holder.Position.X;
            var dz = member.Position.Z - holder.Position.Z;
            if (dx * dx + dz * dz <= TrapRadius * TrapRadius) others.Add(member);
        }

        var caster = Helper(helperIndex);
        caster?.SetPosition(new Placement(holder.Position, 0f));
        foreach (var m in others)
        {
            caster?.Cast(Constants.ActionId.DoubleTroubleTrapStack, castSeconds: 0f, targetId: m.GameObjectId, animationLock: AnimationLock.Helper);
            m.AddStatus(UmadConstants.StatusId.MagicVulnerabilityUp, MagicVulnerabilityUpSeconds);
        }
        holder.AddVfx(Constants.VfxPath.DoubleTroubleTrapStackHit, persistent: false);

        if (others.Count + 1 != TrapStackRequiredCount)
        {
            foreach (var m in others) m.Die($"Failed Double-trouble Trap stack ({others.Count + 1}/{TrapStackRequiredCount}) on {holderRole} during {mechanic}");
            return [];
        }
        Party.Knockback(holder.Position, Constants.KnockbackId.DoubleTroubleTrapStack, TrapRadius, exclude: holder);
        return others;
    }

    // The trap jumps to one of the players its stack hit, by role order.
    public static PartyRole? TrapJumpTarget(IReadOnlyList<SimCharacter> hit, int pick)
    {
        var roles = hit.OfType<ISimPartyMember>().Select(m => m.Role).OrderBy(r => r).ToList();
        return roles.Count == 0 ? null : roles[pick % roles.Count];
    }

    // --- Statue knockback -------------------------------------------------------------------

    public void KnockbackFrom(SimCharacter member, Vector3 source, uint knockbackId)
    {
        if (!KnockbackLookup.TryGet(knockbackId, out var distance, out var speed)) return;
        (member as ISimPartyMember)?.Knockback(source, distance, speed);
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

    // The colossus's own stages: appear 11s into the pull, then 59.97s and 127.75s.
    public static readonly (uint State, uint Bitmask) ColossusStage2 = (0x0010, 0x0020);
    public static readonly (uint State, uint Bitmask) ColossusStage3 = (0x0040, 0x0080);
}
