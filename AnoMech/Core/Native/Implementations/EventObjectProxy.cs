using System.Numerics;
using AnoMech.Core.Native.Implementations.Interop;
using AnoMech.Core.Native.Implementations.Pointers;
using AnoMech.Core.Native.Interfaces;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace AnoMech.Core.Native.Implementations;

internal sealed unsafe class EventObjectProxy(int slot, GameObject* obj) : IEventObjectProxy
{
    private const uint NoTarget = 0xE0000000;

    private readonly uint entityId = obj->EntityId;
    private LoadState? lastLoadState;

    private EventObject* EventObject => (EventObject*)obj;

    public int Slot => slot;
    public uint EntityId => obj->EntityId;

    public Vector3 Position => obj->Position;
    public float Rotation => obj->Rotation;

    public void ActorControl(uint category, uint arg1 = 0, uint arg2 = 0, uint arg3 = 0, uint arg4 = 0, uint arg5 = 0, uint arg6 = 0, uint arg7 = 0, uint arg8 = 0)
    {
        EventObjects.Log($"0x{obj->BaseId:X} ActorControl {category} (0x{arg1:X}, 0x{arg2:X}, 0x{arg3:X}, 0x{arg4:X}) from: {Describe()}");
        PacketDispatcher.HandleActorControlPacket(obj->EntityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, NoTarget, false);
    }

    public void Despawn()
    {
        EventObjects.Log($"0x{obj->BaseId:X} despawned from: {Describe()}");
        byte[] packet = [(byte)slot];
        fixed (byte* packetPtr = packet)
            PacketDispatcherPointers.HandleDespawnObjectPacket(0, packetPtr);
    }

    internal string Describe()
        => $"slot={slot} entity=0x{obj->EntityId:X} base=0x{obj->BaseId:X} EventId=0x{(uint)obj->EventId:X} SharedTimelineState=0x{EventObject->SharedTimelineState:X} "
         + $"Flags=0x{EventObject->Flags:X} DrawObject=0x{(nint)obj->DrawObject:X} RenderFlags={obj->RenderFlags} ReadyToDraw={obj->IsReadyToDraw()} "
         + $"-- SG: {LayoutInstanceDiagnostics.Describe(EventObject->SharedGroupLayoutInstance)}";

    public void LogLoadStateChange()
    {
        // The game can clear or reuse the slot behind our back, as on a zone change.
        if (obj->EntityId != entityId) return;
        var current = ReadLoadState();
        if (current == lastLoadState) return;
        lastLoadState = current;
        EventObjects.Log($"0x{obj->BaseId:X} now {current}: {Describe()}");
    }

    private LoadState ReadLoadState()
    {
        var sg = EventObject->SharedGroupLayoutInstance;
        if (sg == null) return new(false, EventObject->SharedTimelineState, false, false, 0);
        var instance = (ILayoutInstance*)sg;
        var playing = 0u;
        for (var i = 0; i < 16; i++)
            if (sg->IsTimelineIndexValid((uint)i) && sg->IsTimelinePlaying((uint)i)) playing |= 1u << i;
        return new(true, EventObject->SharedTimelineState, instance->IsActive, instance->IsPrimaryReady(), playing);
    }

    private readonly record struct LoadState(bool SharedGroupAttached, ushort TimelineState, bool SharedGroupActive, bool PrimaryReady, uint PlayingTimelines);
}
