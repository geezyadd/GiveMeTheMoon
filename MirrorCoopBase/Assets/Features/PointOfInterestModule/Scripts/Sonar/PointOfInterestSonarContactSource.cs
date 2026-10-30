using System;
using System.Collections.Generic;
using System.Globalization;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Sonar {
    // Every point whose reward container still stands is a contact poi:<id> of the point of interest kind, whatever
    // its distance: the kind shows it on the scope's edge beyond the range and pulses it, so the navigator sees it
    // from far off the course.
    public sealed class PointOfInterestSonarContactSource : ISonarContactSource {
        private const string CONTACT_PREFIX = "poi:";

        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly PointOfInterestConfiguration _pointOfInterestConfiguration;

        public PointOfInterestSonarContactSource(IPointsOfInterest pointsOfInterest, IPointOfInterestContentCatalog pointOfInterestContentCatalog,
                                                 PointOfInterestConfiguration pointOfInterestConfiguration) {
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _pointOfInterestConfiguration = pointOfInterestConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            for (int i = 0; i < _pointsOfInterest.Count; i++) {
                PointOfInterestEntry point = _pointsOfInterest.Get(i);
                if (point.State != PointOfInterestState.Opened)
                    results.Add(CreateContact(point));
            }
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            if (!TryParsePointId(contactId, out int pointId) || !_pointsOfInterest.TryFindIndex(pointId, out int index))
                return false;

            PointOfInterestEntry point = _pointsOfInterest.Get(index);
            if (point.State == PointOfInterestState.Opened)
                return false;

            contact = CreateContact(point);
            return true;
        }

        private static bool TryParsePointId(string contactId, out int pointId) {
            pointId = 0;
            return contactId != null && contactId.StartsWith(CONTACT_PREFIX, StringComparison.Ordinal)
                && int.TryParse(contactId.AsSpan(CONTACT_PREFIX.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out pointId);
        }

        private SonarContact CreateContact(in PointOfInterestEntry point) =>
            new(CONTACT_PREFIX + point.Id.ToString(CultureInfo.InvariantCulture), _pointOfInterestConfiguration.SonarKind, point.Position,
                _pointOfInterestContentCatalog.GetRequired(point.ContentId).ContainerRadiusMeters);
    }
}
