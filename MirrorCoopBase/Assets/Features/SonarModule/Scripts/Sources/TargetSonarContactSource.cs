using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // The target balloons of the flight field from the data this peer has (IPeerFieldTargetQuery: the published
    // balloons, popped ones already left out), one contact each, of the kind of its reward's tier.
    public sealed class TargetSonarContactSource : ISonarContactSource {
        private const string TARGET_ID_PREFIX = "target:";
        private const string TARGET_ID_FORMAT = TARGET_ID_PREFIX + "{0}";

        private readonly IPeerFieldTargetQuery _peerFieldTargetQuery;
        private readonly SonarConfiguration _sonarConfiguration;
        private readonly List<FieldTargetDescription> _targets = new();

        public TargetSonarContactSource(IPeerFieldTargetQuery peerFieldTargetQuery, SonarConfiguration sonarConfiguration) {
            _peerFieldTargetQuery = peerFieldTargetQuery;
            _sonarConfiguration = sonarConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            _peerFieldTargetQuery.FindInSphere(center, radius, _targets);
            for (int i = 0; i < _targets.Count; i++)
                results.Add(CreateContact(_targets[i]));
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            if (TryParseIndex(contactId, out int index) == false || _peerFieldTargetQuery.TryGetTarget(new FieldTargetId(index), out FieldTargetDescription target) == false)
                return false;

            contact = CreateContact(target);
            return true;
        }

        private SonarContact CreateContact(FieldTargetDescription target) =>
            new(string.Format(TARGET_ID_FORMAT, target.Id.Index), GetKind(target.Reward), target.Center, target.Radius);

        // The validator keeps the tiers rising from reward 0, so the first one always fits.
        private SonarContactKind GetKind(int reward) {
            IReadOnlyList<SonarTargetTier> tiers = _sonarConfiguration.TargetTiers;
            SonarContactKind kind = tiers[0].Kind;
            for (int i = 1; i < tiers.Count && tiers[i].MinReward <= reward; i++)
                kind = tiers[i].Kind;

            return kind;
        }

        private static bool TryParseIndex(string contactId, out int index) {
            index = 0;
            return contactId.StartsWith(TARGET_ID_PREFIX) && int.TryParse(contactId.Substring(TARGET_ID_PREFIX.Length), out index);
        }
    }
}
