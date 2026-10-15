using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // A player who joins a running map appears on the ship's deck, at the next deck spawn point in turn.
    public sealed class ShipDeckJoinSpawn : ISessionJoinSpawn {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipDeckSpawnPoints _shipDeckSpawnPoints;

        public ShipDeckJoinSpawn(IShipRunBindingModel shipRunBindingModel, IShipDeckSpawnPoints shipDeckSpawnPoints) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipDeckSpawnPoints = shipDeckSpawnPoints;
        }

        public bool TryTakeJoinPose(out Pose pose) {
            pose = default;
            if (_shipRunBindingModel.Ship == null || _shipDeckSpawnPoints.HasSpawnPoint == false)
                return false;

            pose = _shipDeckSpawnPoints.TakeSpawnPose();
            return true;
        }
    }
}
