using System.Collections.Generic;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Configuration;
using Features.SonarModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Sonar {
    // Every stolen parcel this peer shows (under its thief, adrift or on a rope) is a contact of the stolen parcel
    // kind, so the navigator leads the chase by it; a parcel lying on board is none.
    public sealed class ParcelSonarContactSource : ISonarContactSource {
        private const string PARCEL_ID_PREFIX = "parcel:";
        private const string PARCEL_ID_FORMAT = PARCEL_ID_PREFIX + "{0}";
        private const float CRATE_RADIUS_METERS = 0.5f;

        private readonly IReadOnlyParcelsModel _parcelsModel;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelSonarContactSource(IReadOnlyParcelsModel parcelsModel, ParcelConfiguration parcelConfiguration) {
            _parcelsModel = parcelsModel;
            _parcelConfiguration = parcelConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            foreach (ParcelEntry entry in _parcelsModel.Entries) {
                if (TryGetStolenPosition(entry, out Vector3 position) && Vector3.Distance(position, center) <= radius)
                    results.Add(CreateContact(entry.NetId, position));
            }
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            if (TryParseId(contactId, out uint netId) == false)
                return false;

            foreach (ParcelEntry entry in _parcelsModel.Entries) {
                if (entry.NetId != netId || TryGetStolenPosition(entry, out Vector3 position) == false)
                    continue;

                contact = CreateContact(netId, position);
                return true;
            }

            return false;
        }

        private static bool TryGetStolenPosition(in ParcelEntry entry, out Vector3 position) {
            position = Vector3.zero;
            if (entry.State == ParcelState.Loose || NetworkClient.spawned.TryGetValue(entry.NetId, out NetworkIdentity crate) == false)
                return false;

            position = crate.transform.position;
            return true;
        }

        private SonarContact CreateContact(uint netId, Vector3 position) =>
            new(string.Format(PARCEL_ID_FORMAT, netId), _parcelConfiguration.StolenSonarKind, position, CRATE_RADIUS_METERS);

        private static bool TryParseId(string contactId, out uint netId) {
            netId = 0;
            return contactId.StartsWith(PARCEL_ID_PREFIX) && uint.TryParse(contactId.Substring(PARCEL_ID_PREFIX.Length), out netId);
        }
    }
}
