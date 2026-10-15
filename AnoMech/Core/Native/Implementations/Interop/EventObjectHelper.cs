using AnoMech.Core.Native.Implementations.Pointers;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace AnoMech.Core.Native.Implementations.Interop;

internal static unsafe class EventObjectHelper
{
    // Below BattleCharas' 0x4000FE00 packet-spawn ids; one per slot of the 40-slot pool.
    private const uint EntityIdBase = 0x4000FD00u;

    public static bool Create(SpawnObjectPacket* packet, out int slot, out GameObject* eventObject)
    {
        var manager = EventObjectManager.Instance();

        // Since the Packet Handle method does not return the ID, we do a manual check beforehand, and test if that ID was created.
        if (packet->ObjectIndex == 0xFF) // -1
        {
            var freeId = -1;

            for (int i = 0; i < 40; i++)
            {
                if (manager->EventObjects[i] == null)
                {
                    freeId = i;
                    break;
                }
            }

            if (freeId == -1)
            {
                slot = -1;
                eventObject = null;
                return false;
            }

            packet->ObjectIndex = (byte)freeId;
        }

        // A real EObj always has its own id. With 0 the object is nobody: BossMod's world state
        // can't look it up and throws on it every frame, skipping every actor after it.
        if (packet->EntityId == 0)
            packet->EntityId = EntityIdBase + packet->ObjectIndex;

        PacketDispatcher.HandleSpawnObjectPacket(0, packet);
        slot = packet->ObjectIndex;
        eventObject = EventObjectManagerPointers.GetEventObjectByIndex(manager, (uint)slot);
        return eventObject != null;
    }

    // Writes the EObj state field (actor[0x1B2]) and notifies the attached
    // SharedGroupLayoutInstance to switch sub-instances. Different SGs use
    // different state values to gate which sub-instances are visible —
    // simulator code finds the right value empirically per EObj row.
    public static void SetState(GameObject* obj, ushort state)
    {
        if (obj == null)
        {
            Plugin.Log.Warning("[EventObjectHelper.SetState] obj was null.");
            return;
        }

        EventObjectPointers.SetEventObjectState((EventObject*)obj, state, 1, 0);
    }
}
