using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Overboard;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // Every player drifting overboard on this peer, one contact each of the overboard kind: on the scope and, for
    // everyone but the drifter itself, as a marker on the screen.
    public sealed class OverboardSonarContactSource : ISonarContactSource, IOverboardSonarContactSource {
        private const string OVERBOARD_ID_PREFIX = "overboard:";
        private const float CONTACT_RADIUS_METERS = 1f;

        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarContactKind Kind => _sonarConfiguration.OverboardKind;

        public OverboardSonarContactSource(IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders, SonarConfiguration sonarConfiguration) {
            _overboardRiders = overboardRiders;
            _sonarConfiguration = sonarConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                if (IsDrifting(riders[i]) && Vector3.Distance(riders[i].Position, center) <= radius)
                    results.Add(CreateContact(riders[i]));
            }
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                if (IsDrifting(riders[i]) == false || contactId != OVERBOARD_ID_PREFIX + riders[i].Key.Id)
                    continue;

                contact = CreateContact(riders[i]);
                return true;
            }

            return false;
        }

        public void CollectOtherDrifterPositions(List<Vector3> results) {
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                if (IsDrifting(riders[i]) && riders[i].isLocalPlayer == false)
                    results.Add(riders[i].Position + Vector3.up * CONTACT_RADIUS_METERS);
            }
        }

        private static bool IsDrifting(ShipOverboardRider overboardRider) =>
            overboardRider != null && overboardRider.IsOverboard;

        private SonarContact CreateContact(ShipOverboardRider overboardRider) =>
            new(OVERBOARD_ID_PREFIX + overboardRider.Key.Id, _sonarConfiguration.OverboardKind, overboardRider.Position, CONTACT_RADIUS_METERS);
    }
}
