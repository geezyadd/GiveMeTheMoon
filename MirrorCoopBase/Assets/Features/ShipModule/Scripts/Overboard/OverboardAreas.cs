using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using UnityEngine;

namespace Features.ShipModule.Scripts.Overboard {
    // The overboard areas on the ground now: the registered ones (the lobby's) and the current station pad's (a pad
    // spawns without a context, so it cannot register itself).
    public sealed class OverboardAreas {
        private readonly IReadOnlyComponentRegistry<OverboardArea> _registry;
        private readonly IShipStationPads _stationPads;
        private readonly IShipOverboardRule _shipOverboardRule;

        private ShipLandingPad _lastPad;
        private OverboardArea _padArea;

        public OverboardAreas(
            IReadOnlyComponentRegistry<OverboardArea> registry,
            IShipStationPads stationPads,
            IShipOverboardRule shipOverboardRule) {
            _registry = registry;
            _stationPads = stationPads;
            _shipOverboardRule = shipOverboardRule;
        }

        public bool Contains(Vector3 point) {
            IReadOnlyList<OverboardArea> areas = _registry.Items;
            for (int i = 0; i < areas.Count; i++)
                if (areas[i] != null && areas[i].Contains(_shipOverboardRule, point))
                    return true;

            OverboardArea padArea = PadArea();
            return padArea != null && padArea.Contains(_shipOverboardRule, point);
        }

        public bool IsOverFloor(Vector3 point) {
            IReadOnlyList<OverboardArea> areas = _registry.Items;
            for (int i = 0; i < areas.Count; i++)
                if (areas[i] != null && areas[i].IsOverFloor(_shipOverboardRule, point))
                    return true;

            OverboardArea padArea = PadArea();
            return padArea != null && padArea.IsOverFloor(_shipOverboardRule, point);
        }

        // The nearest area to the point (only those with a return point when asked), or false when there is none.
        public bool TryFindNearest(Vector3 point, bool withReturnPoint, out OverboardArea nearest, out float distance) {
            nearest = null;
            distance = float.PositiveInfinity;
            IReadOnlyList<OverboardArea> areas = _registry.Items;
            for (int i = 0; i < areas.Count; i++)
                Consider(areas[i], point, withReturnPoint, ref nearest, ref distance);

            Consider(PadArea(), point, withReturnPoint, ref nearest, ref distance);
            return nearest != null;
        }

        private static void Consider(OverboardArea area, Vector3 point, bool withReturnPoint, ref OverboardArea nearest,
            ref float distance) {
            if (area == null || area.isActiveAndEnabled == false || (withReturnPoint && area.HasReturnPoint == false))
                return;

            float areaDistance = area.DistanceTo(point);
            if (areaDistance >= distance)
                return;

            nearest = area;
            distance = areaDistance;
        }

        private OverboardArea PadArea() {
            ShipLandingPad pad = _stationPads.CurrentPad;
            if (pad != _lastPad) {
                _lastPad = pad;
                _padArea = pad != null ? pad.GetComponentInParent<OverboardArea>() : null;
            }

            return _padArea;
        }
    }
}
