using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Stand;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    public sealed class ParcelStandSpawnService : IParcelStandSpawnService {
        private readonly ParcelServerModel _parcelServerModel;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelStandSpawnService(ParcelServerModel parcelServerModel, ParcelConfiguration parcelConfiguration) {
            _parcelServerModel = parcelServerModel;
            _parcelConfiguration = parcelConfiguration;
        }

        public void ServerPlaceAt(ShipLandingPad pad) {
            ServerRemove();
            Transform padTransform = pad.transform;
            ParcelStand stand = Object.Instantiate(_parcelConfiguration.StandPrefab, padTransform.TransformPoint(_parcelConfiguration.StandPadOffset), padTransform.rotation);
            NetworkServer.Spawn(stand.gameObject);
            _parcelServerModel.SetStand(stand);
        }

        public void ServerRemove() {
            ParcelStand stand = _parcelServerModel.Stand;
            _parcelServerModel.SetStand(null);
            if (stand != null)
                NetworkServer.Destroy(stand.gameObject);
        }
    }
}
