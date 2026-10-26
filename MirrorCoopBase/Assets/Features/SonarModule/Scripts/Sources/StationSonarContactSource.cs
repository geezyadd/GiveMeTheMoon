using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // The next station, as the old radar's beacon: while the ship flies, the destination pad from the synced run
    // model, whatever its distance (the kind shows it on the scope's edge when it lies beyond the range).
    public sealed class StationSonarContactSource : ISonarContactSource, IStationSonarContactSource {
        private const string STATION_CONTACT_ID = "station";

        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly SonarConfiguration _sonarConfiguration;

        private bool IsFlying =>
            _shipRunModel.IsAvailable && _shipRunModel.Phase.IsFlight();

        public StationSonarContactSource(IReadOnlyShipRunModel shipRunModel, SonarConfiguration sonarConfiguration) {
            _shipRunModel = shipRunModel;
            _sonarConfiguration = sonarConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            if (IsFlying)
                results.Add(CreateStation());
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            return contactId == STATION_CONTACT_ID && TryGetStation(out contact);
        }

        public bool TryGetStation(out SonarContact station) {
            station = default;
            if (IsFlying == false)
                return false;

            station = CreateStation();
            return true;
        }

        private SonarContact CreateStation() =>
            new(STATION_CONTACT_ID, _sonarConfiguration.StationKind, _shipRunModel.TransitDestination, 0f);
    }
}
