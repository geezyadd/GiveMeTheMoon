using Features.FieldAgentModule.Scripts.Cargo;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Server;
using Mirror;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    public sealed class ParcelCrateService : IParcelCrateService {
        private const int NO_CARRIER = 0;

        private readonly ParcelServerModel _parcelServerModel;
        private readonly IParcelEntryService _parcelEntryService;
        private readonly IParcelTheftService _parcelTheftService;
        private readonly IFieldAgentCargoTargetRegistry _fieldAgentCargoTargetRegistry;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelCrateService(
            ParcelServerModel parcelServerModel,
            IParcelEntryService parcelEntryService,
            IParcelTheftService parcelTheftService,
            IFieldAgentCargoTargetRegistry fieldAgentCargoTargetRegistry,
            ParcelConfiguration parcelConfiguration) {
            _parcelServerModel = parcelServerModel;
            _parcelEntryService = parcelEntryService;
            _parcelTheftService = parcelTheftService;
            _fieldAgentCargoTargetRegistry = fieldAgentCargoTargetRegistry;
            _parcelConfiguration = parcelConfiguration;
        }

        public ParcelServerRecord ServerSpawn(Vector3 position, Quaternion rotation, long reward, int destinationLoop) {
            ParcelCrate crate = Object.Instantiate(_parcelConfiguration.CratePrefab, position, rotation);
            NetworkServer.Spawn(crate.gameObject);
            uint netId = crate.Identity.netId;
            ParcelServerRecord record = new(netId, crate, new ParcelCargoTarget(netId, _parcelTheftService), Time.time);
            _parcelServerModel.Add(record);
            _parcelEntryService.ServerAdd(new ParcelEntry(netId, reward, destinationLoop, ParcelState.Loose, NO_CARRIER, string.Empty, string.Empty));
            return record;
        }

        public void ServerDestroy(uint netId) {
            if (_parcelServerModel.TryGet(netId, out ParcelServerRecord record) == false)
                return;

            _fieldAgentCargoTargetRegistry.Remove(record.CargoTarget);
            _parcelServerModel.Remove(netId);
            _parcelEntryService.ServerRemove(netId);
            if (record.Crate != null)
                NetworkServer.Destroy(record.Crate.gameObject);
        }
    }
}
