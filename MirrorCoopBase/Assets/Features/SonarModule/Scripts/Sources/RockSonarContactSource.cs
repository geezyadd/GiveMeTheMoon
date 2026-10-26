using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    // The dangers of the flight field from the data this peer has (IPeerFieldRockQuery: the published field, broken
    // rocks already left out), so a navigator on a client sees them as one on the host: every
    // giant and every big debris rock is a contact of its own, smaller rocks only count where a debris cell holds
    // enough of them.
    public sealed class RockSonarContactSource : ISonarContactSource {
        private const float HALF = 0.5f;

        private readonly IPeerFieldRockQuery _peerFieldRockQuery;
        private readonly IRockSonarContactIds _rockSonarContactIds;
        private readonly SonarConfiguration _sonarConfiguration;
        private readonly List<FieldRockDescription> _rocks = new();
        private readonly Dictionary<Vector2Int, DebrisCellSum> _debrisCells = new();

        public RockSonarContactSource(IPeerFieldRockQuery peerFieldRockQuery, IRockSonarContactIds rockSonarContactIds, SonarConfiguration sonarConfiguration) {
            _peerFieldRockQuery = peerFieldRockQuery;
            _rockSonarContactIds = rockSonarContactIds;
            _sonarConfiguration = sonarConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            _peerFieldRockQuery.FindInSphere(center, radius, _rocks);
            _debrisCells.Clear();
            for (int i = 0; i < _rocks.Count; i++) {
                FieldRockDescription rock = _rocks[i];
                if (TryGetSingleRockKind(rock, out SonarContactKind kind))
                    results.Add(CreateRockContact(rock, kind));
                else
                    CountInDebrisCell(rock);
            }

            foreach (KeyValuePair<Vector2Int, DebrisCellSum> cell in _debrisCells) {
                if (cell.Value.Count >= _sonarConfiguration.DebrisMinRocks)
                    results.Add(CreateDebrisContact(cell.Key, cell.Value));
            }
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            if (_rockSonarContactIds.TryParseRockId(contactId, out FieldRockId rockId))
                return TryGetSingleRock(rockId, out contact);

            return _rockSonarContactIds.TryParseDebrisId(contactId, out Vector2Int cell) && TryGetDebris(cell, out contact);
        }

        private bool TryGetSingleRock(FieldRockId rockId, out SonarContact contact) {
            contact = default;
            if (_peerFieldRockQuery.TryGetRock(rockId, out FieldRockDescription rock) == false || TryGetSingleRockKind(rock, out SonarContactKind kind) == false)
                return false;

            contact = CreateRockContact(rock, kind);
            return true;
        }

        // A giant or a big debris rock is a contact of its own; a smaller rock only counts in its debris cell.
        private bool TryGetSingleRockKind(FieldRockDescription rock, out SonarContactKind kind) {
            kind = null;
            if (rock.Size == FieldRockSize.Large)
                kind = _sonarConfiguration.LargeRockKind;
            else if (rock.Diameter >= _sonarConfiguration.BigRockMinDiameterMeters)
                kind = _sonarConfiguration.BigRockKind;

            return kind != null;
        }

        private bool TryGetDebris(Vector2Int cell, out SonarContact contact) {
            contact = default;
            float halfCell = _sonarConfiguration.DebrisCellMeters * HALF;
            Vector3 cellCenter = new Vector3((cell.x + HALF) * _sonarConfiguration.DebrisCellMeters, 0f, (cell.y + HALF) * _sonarConfiguration.DebrisCellMeters);
            Vector3 halfExtents = new Vector3(halfCell, _sonarConfiguration.DebrisCellHalfHeightMeters, halfCell);
            DebrisCellSum sum = default;
            _peerFieldRockQuery.FindInBox(cellCenter, halfExtents, Quaternion.identity, _rocks);
            for (int i = 0; i < _rocks.Count; i++) {
                FieldRockDescription rock = _rocks[i];
                if (TryGetSingleRockKind(rock, out _) == false && ToCell(rock.Center) == cell)
                    sum = sum.Add(rock.Center);
            }

            if (sum.Count < _sonarConfiguration.DebrisMinRocks)
                return false;

            contact = CreateDebrisContact(cell, sum);
            return true;
        }

        private void CountInDebrisCell(FieldRockDescription rock) {
            Vector2Int cell = ToCell(rock.Center);
            _debrisCells.TryGetValue(cell, out DebrisCellSum sum);
            _debrisCells[cell] = sum.Add(rock.Center);
        }

        private Vector2Int ToCell(Vector3 position) {
            float cellMeters = _sonarConfiguration.DebrisCellMeters;
            return new Vector2Int(Mathf.FloorToInt(position.x / cellMeters), Mathf.FloorToInt(position.z / cellMeters));
        }

        private SonarContact CreateRockContact(FieldRockDescription rock, SonarContactKind kind) =>
            new(_rockSonarContactIds.CreateRockId(rock.Id), kind, rock.Center, rock.Radius);

        private SonarContact CreateDebrisContact(Vector2Int cell, DebrisCellSum sum) =>
            new(_rockSonarContactIds.CreateDebrisId(cell), _sonarConfiguration.DebrisKind, sum.PositionSum / sum.Count, _sonarConfiguration.DebrisCellMeters * HALF);
    }
}
