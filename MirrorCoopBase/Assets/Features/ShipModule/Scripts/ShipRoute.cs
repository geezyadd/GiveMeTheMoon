using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The route of one flight: destination, transit progress, speed, alignment and ETA, written into the run model.
    internal sealed class ShipRoute : IShipRoute {
        private readonly ShipRunModel _model;
        private readonly ShipRunConfig _config;
        private readonly ShipFlightSettings _flightSettings;
        private readonly IShipRunBinding _binding;
        private readonly IShipStationPads _pads;
        private readonly IShipFlightStatService _flightStats;
        private readonly ShipTransit _transit = new ShipTransit();

        private Vector3 _launchForward = Vector3.forward;
        private Vector3 _destinationPoint;
        private Vector3 _destinationForward = Vector3.forward;
        private ShipFlightMode _modeOverride;
        private bool _hasModeOverride;

        public Vector3 LaunchForward => _launchForward;
        public Vector3 DestinationPoint => _destinationPoint;
        public Vector3 DestinationForward => _destinationForward;
        public ShipFlightMode SelectedFlightMode => _hasModeOverride ? _modeOverride : _flightSettings.FlightMode;

        public bool IsTravel {
            get {
                ShipBase ship = _binding.Ship;
                if (ship != null && ship.IsFlying)
                    return ship.ActiveFlightMode == ShipFlightMode.TravelInSpace;

                return SelectedFlightMode == ShipFlightMode.TravelInSpace;
            }
        }

        public bool HasArrived {
            get {
                if (IsTravel == false)
                    return _transit.HasArrived;

                return HorizontalDistance(_binding.Ship.transform.position, _destinationPoint) <= _config.ApproachDistance;
            }
        }

        public ShipRoute(
            ShipRunModel model,
            ShipRunConfig config,
            ShipFlightSettings flightSettings,
            IShipRunBinding binding,
            IShipStationPads pads,
            IShipFlightStatService flightStats) {
            _model = model;
            _config = config;
            _flightSettings = flightSettings;
            _binding = binding;
            _pads = pads;
            _flightStats = flightStats;
        }

        public Vector3 PlanLaunch(Vector3 from, Vector3 shipForward) {
            _launchForward = FlattenForward(shipForward);
            Vector3 hover = from
                + _launchForward * _config.TakeoffForward
                + Vector3.up * _config.TakeoffHeight;
            ChooseDestination(hover);
            return hover;
        }

        public void BeginRoute(float cruiseSeconds) =>
            _transit.BeginRoute(cruiseSeconds, _destinationForward);

        public void BeginCruise() =>
            _transit.SetSpeed(ReadFlightSpeed());

        public void RefreshPreview() {
            ShipBase ship = _binding.Ship;
            float work = _flightStats.Sample(ship, _model.LoopIndex).CruiseSeconds;
            Vector3 destination = ship != null
                ? FlattenForward(ship.transform.forward)
                : _launchForward;
            _transit.PreviewRoute(work, ReadFlightSpeed(), destination);
            if (_destinationPoint.sqrMagnitude < 0.0001f)
                _destinationPoint = RadarPoint(
                    ship != null ? ship.transform.position : Vector3.zero,
                    destination);
            CopyTransitToModel();
        }

        public void ApplyFrame(bool tickWork) {
            ShipBase ship = _binding.Ship;
            if (IsTravel && ship != null && _model.Phase != ShipRunPhase.Build) {
                float distance = HorizontalDistance(ship.transform.position, _destinationPoint);
                float speed = ReadTravelMetersPerSecond();
                _transit.PreviewRoute(distance, speed, _destinationForward);
                _transit.SetAlignment(EvaluateTransitAlignment());
                CopyTransitToModel();
                return;
            }

            _transit.SetSpeed(ReadFlightSpeed());
            _transit.SetAlignment(EvaluateTransitAlignment());
            if (tickWork)
                _transit.Tick(Time.deltaTime);

            CopyTransitToModel();
        }

        public Vector3 PadForward(Vector3 face) =>
            FlattenForward(face.sqrMagnitude > 0.0001f ? face : _launchForward);

        public void SetDestinationPoint(Vector3 point) =>
            _destinationPoint = point;

        public void ShiftDestination(Vector3 delta) =>
            _destinationPoint += delta;

        public void EndRoute() {
            _transit.Reset();
            _hasModeOverride = false;
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            CopyTransitToModel();
        }

        public void Reset() {
            _launchForward = Vector3.forward;
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            _transit.Reset();
        }

        public void DebugUseFlightMode(ShipFlightMode mode) {
            _modeOverride = mode;
            _hasModeOverride = true;
        }

        public void DebugClearFlightMode() =>
            _hasModeOverride = false;

        private void ChooseDestination(Vector3 cruiseStart) {
            float halfCone = _config.DestinationConeDegrees * 0.5f;
            float yaw = Random.Range(-halfCone, halfCone);
            _destinationForward = FlattenForward(Quaternion.AngleAxis(yaw, Vector3.up) * _launchForward);
            if (SelectedFlightMode == ShipFlightMode.TravelInSpace) {
                float distance = Mathf.Max(
                    _config.ApproachDistance * 3f,
                    _flightStats.EvaluateRouteWork(_model.LoopIndex) * Mathf.Max(1f, _flightSettings.CruiseSpeed));
                ShipLandingPad currentPad = _pads.CurrentPad;
                float padY = currentPad != null
                    ? currentPad.transform.position.y + _config.TakeoffHeight
                    : cruiseStart.y;
                _destinationPoint = cruiseStart + _destinationForward * distance;
                _destinationPoint.y = padY;
                return;
            }

            _destinationPoint = RadarPoint(cruiseStart, _destinationForward);
        }

        private Vector3 RadarPoint(Vector3 origin, Vector3 direction) {
            float range = Mathf.Max(80f, _config.StationSpacing);
            ShipLandingPad currentPad = _pads.CurrentPad;
            float padY = currentPad != null
                ? currentPad.transform.position.y + _config.TakeoffHeight
                : origin.y;
            Vector3 point = origin + FlattenForward(direction) * range;
            point.y = padY;
            return point;
        }

        private void CopyTransitToModel() {
            ShipBase ship = _binding.Ship;
            _model.TransitWorkRemaining = _transit.WorkRemaining;
            _model.TransitSpeed = _transit.Speed;
            _model.TransitAlignment = _transit.Alignment;
            _model.TransitDestination = _destinationPoint.sqrMagnitude > 0.0001f
                ? _destinationPoint
                : RadarPoint(
                    ship != null ? ship.transform.position : Vector3.zero,
                    _transit.DestinationDirection);
            _model.TransitSecondsRemaining = _transit.SecondsRemaining;
            _model.CruiseEndNetworkTime = _model.Phase == ShipRunPhase.Cruise
                ? NetworkTime.time + _model.TransitSecondsRemaining
                : 0d;
        }

        private float ReadFlightSpeed() =>
            _flightStats.ReadFlightSpeed(_binding.Ship);

        private float ReadTravelMetersPerSecond() =>
            _flightSettings.CruiseSpeed * ReadFlightSpeed();

        private float EvaluateTransitAlignment() {
            ShipBase ship = _binding.Ship;
            if (ship == null)
                return 1f;

            return ShipTransit.EvaluateAlignment(
                FlattenForward(ship.transform.forward),
                _destinationForward);
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to) {
            float x = from.x - to.x;
            float z = from.z - to.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static Vector3 FlattenForward(Vector3 forward) {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
