using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // The ore lives in the live field model (Ores), so every peer gets it with the field. A field holds a few dozen
    // pieces at most, so a lookup walks the list (no allocation). The list is emptied with the field.
    public sealed class LiveFieldOres : IFieldOres, IFieldOreWriter, IFieldOreReset {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFlightFieldSyncBindingModel _flightFieldSyncBindingModel;

        public bool HasField => _liveFieldModel.IsAvailable && _liveFieldModel.Number != LiveFieldNumbers.NO_FIELD;
        public Vector3 Origin => _liveFieldModel.Origin;
        public Quaternion Rotation => Quaternion.LookRotation(_liveFieldModel.Forward, Vector3.up);
        public int Count => _liveFieldModel.Ores.Count;
        public bool IsAvailable => _flightFieldSyncBindingModel.IsBound;

        public LiveFieldOres(IReadOnlyLiveFieldModel liveFieldModel, IFlightFieldSyncBindingModel flightFieldSyncBindingModel) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSyncBindingModel = flightFieldSyncBindingModel;
        }

        public FieldOreEntry Get(int index) =>
            _liveFieldModel.Ores[index];

        public bool TryFindIndex(int id, out int index) {
            IReadOnlyList<FieldOreEntry> ores = _liveFieldModel.Ores;
            for (index = 0; index < ores.Count; index++) {
                if (ores[index].Id == id)
                    return true;
            }

            index = -1;
            return false;
        }

        public void ServerAdd(FieldOreEntry entry) =>
            _flightFieldSyncBindingModel.Bridge.ServerAddOres(entry);

        public void ServerSet(int index, FieldOreEntry entry) =>
            _flightFieldSyncBindingModel.Bridge.ServerSetOres(index, entry);

        public void ServerRemoveAt(int index) =>
            _flightFieldSyncBindingModel.Bridge.ServerRemoveOresAt(index);

        // The bridge is gone when the game scene stops before the run is reset; then there is nothing to clear.
        public void ServerReset() {
            if (_flightFieldSyncBindingModel.IsBound)
                _flightFieldSyncBindingModel.Bridge.ServerClearOres();
        }
    }
}
