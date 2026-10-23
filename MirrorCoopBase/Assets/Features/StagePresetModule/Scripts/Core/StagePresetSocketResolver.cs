using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.StagePresetModule.Scripts.Configurations;

namespace Features.StagePresetModule.Scripts.Core {
    // A preset against a ship's sockets: the run start and the EditMode guard test (ShipPlatform) use the same check.
    public sealed class StagePresetSocketResolver : IStagePresetSocketResolver {
        private const string MISSING_SOCKET_FORMAT = "Stage preset '{0}': the ship has no socket '{1}'.";
        private const string WRONG_SLOT_FORMAT = "Stage preset '{0}': socket '{1}' takes {2}, module '{3}' is {4}.";

        public IReadOnlyList<ShipSocket> ResolveSockets(StagePreset preset, IReadOnlyList<ShipSocket> sockets) {
            ShipSocket[] resolved = new ShipSocket[preset.Modules.Count];
            for (int i = 0; i < preset.Modules.Count; i++)
                resolved[i] = ResolveSocket(preset, preset.Modules[i], sockets);

            return resolved;
        }

        private static ShipSocket ResolveSocket(StagePreset preset, StagePresetSocketModule entry, IReadOnlyList<ShipSocket> sockets) {
            if (TryFindSocket(sockets, entry.SocketId, out ShipSocket socket) == false)
                throw new InvalidOperationException(string.Format(MISSING_SOCKET_FORMAT, preset.Id, entry.SocketId));

            if (socket.AcceptedType != entry.Module.SlotType)
                throw new InvalidOperationException(string.Format(WRONG_SLOT_FORMAT, preset.Id, entry.SocketId, socket.AcceptedType, entry.Module.Id, entry.Module.SlotType));

            return socket;
        }

        private static bool TryFindSocket(IReadOnlyList<ShipSocket> sockets, string socketId, out ShipSocket socket) {
            socket = null;
            for (int i = 0; i < sockets.Count; i++) {
                if (sockets[i].SocketId != socketId)
                    continue;

                socket = sockets[i];
                return true;
            }

            return false;
        }
    }
}
