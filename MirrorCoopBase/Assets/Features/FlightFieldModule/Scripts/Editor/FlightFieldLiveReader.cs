using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Server;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Editor {
    // Reads the live field of the game in Play mode from the game's containers. Only a server (host) keeps the
    // generated grid, so a client editor sees no live field here.
    public sealed class FlightFieldLiveReader {
        public bool TryRead(out FlightFieldLiveSnapshot snapshot) {
            snapshot = default;
            if (!TryFindGameContainer(out DiContainer container))
                return false;

            IReadOnlyFlightFieldServerModel serverModel = container.Resolve<IReadOnlyFlightFieldServerModel>();
            if (!serverModel.HasField)
                return false;

            IReadOnlyLiveFieldModel liveFieldModel = container.Resolve<IReadOnlyLiveFieldModel>();
            FieldFrame liveFrame = new(liveFieldModel.Origin, liveFieldModel.Forward, liveFieldModel.CellSize, liveFieldModel.SideCells);
            bool hasShip = container.Resolve<IShipPositionSource>().TryGetShipPosition(out Vector3 shipPosition);
            snapshot = new FlightFieldLiveSnapshot(serverModel.Field, serverModel.Number, liveFrame, hasShip, shipPosition);
            return true;
        }

        private static bool TryFindGameContainer(out DiContainer container) {
            container = null;
            foreach (SceneContext context in Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (!context.HasResolved || !context.Container.HasBinding<IShipPositionSource>())
                    continue;

                container = context.Container;
                return true;
            }

            return false;
        }
    }
}
