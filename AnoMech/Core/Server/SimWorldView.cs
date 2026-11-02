using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Native.Interfaces;
using AnoMech.Core.SimObjects;
using RuleSim.Actions;

namespace AnoMech.Core.Server;

// The sim as the resolver reads it.
internal sealed class SimWorldView(SimWorld world) : IWorld
{
    public IActor? Find(ulong id) => Character(id) is { } c ? Of(c) : null;

    public IEnumerable<IActor> Party => world.Party.ActiveMembers().Select(Of);

    public IEnumerable<IActor> Enemies => ActiveEnemies().Select(Of);

    public SimCharacter? Character(ulong id)
        => world.Party.ActiveMembers().FirstOrDefault(m => (ulong)m.GameObjectId == id)
           ?? ActiveEnemies().FirstOrDefault(e => (ulong)e.GameObjectId == id);

    public static IActor Of(SimCharacter character) => new SimActor(character);

    private IEnumerable<SimCharacter> ActiveEnemies() => world.Children.OfType<SimEnemy>().Where(e => e.IsActive);

    private sealed class SimActor(SimCharacter c) : IActor
    {
        public ulong Id => (ulong)c.GameObjectId;
        public Job Job => c is ISimPartyMember m ? (Job)m.ClassJob : Job.None;
        public bool IsAlive => c.IsAlive();
        public Vector3 Position => c.Position;
        public float HitboxRadius => c.HitboxRadius;
        public IReadOnlyList<StatusView> Statuses
            => c.ActiveStatuses.Select(s => new StatusView(s.StatusId, s.Stacks, s.RemainingTime, (ulong)s.SourceObject)).ToList();
    }
}

internal sealed class GameDataActionSheet : IActionSheet
{
    public ActionInfo? Action(uint id)
        => Natives.Data.Action(id) is { } row
            ? new ActionInfo(
                row.Id,
                (byte)row.ActionCategory,
                (byte)row.CastType,
                row.EffectRange,
                row.CanTargetSelf,
                row.CanTargetParty,
                row.CastSeconds,
                row.RecastSeconds,
                row.ComboFrom,
                row.PreservesCombo,
                row.PrimaryCostType,
                row.PrimaryCostValue)
            : null;

    public bool IsComboPrerequisite(uint id) => Natives.Data.IsComboPrerequisite(id);
}
