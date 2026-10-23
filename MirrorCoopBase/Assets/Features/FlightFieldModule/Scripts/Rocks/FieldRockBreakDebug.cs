#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Looks for the nearest breakable rock around the ship in growing spheres and breaks it.
    public sealed class FieldRockBreakDebug : IFieldRockBreakDebug {
        private static readonly float[] _searchRadii = { 150f, 400f, 900f };

        private readonly IShipPositionSource _shipPositionSource;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldRockBreaker _fieldRockBreaker;
        private readonly List<FieldRockDescription> _rocks = new();

        public FieldRockBreakDebug(IShipPositionSource shipPositionSource, IFieldRockQuery fieldRockQuery, IFieldRockBreaker fieldRockBreaker) {
            _shipPositionSource = shipPositionSource;
            _fieldRockQuery = fieldRockQuery;
            _fieldRockBreaker = fieldRockBreaker;
        }

        public FieldRockBreakResult DebugBreakNearestRock(out FieldRockId id) {
            id = default;
            if (!_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition) || !TryFindNearest(shipPosition, out id))
                return FieldRockBreakResult.NotFound;

            return _fieldRockBreaker.ServerBreak(id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));
        }

        private bool TryFindNearest(Vector3 shipPosition, out FieldRockId id) {
            id = default;
            foreach (float radius in _searchRadii) {
                _fieldRockQuery.FindInSphere(shipPosition, radius, _rocks);
                float nearest = float.MaxValue;
                foreach (FieldRockDescription rock in _rocks) {
                    float distance = (rock.Center - shipPosition).sqrMagnitude;
                    if (!rock.IsBreakable || distance >= nearest)
                        continue;

                    nearest = distance;
                    id = rock.Id;
                }

                if (nearest < float.MaxValue)
                    return true;
            }

            return false;
        }
    }
}
#endif
