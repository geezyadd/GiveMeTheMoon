using Features.GameCoreModule.Contracts;
using Game.Connection;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunService : IGameplaySession, IShipFloorReferenceProvider, IShipRiderRelease {
        private const float WRECK_PITCH_DEGREES = 12f;
        private const float WRECK_ROLL_DEGREES = 18f;
        private const float DROP_LATERAL = 7.5f;
        private const float DROP_ALONG_ORIGIN = 2f;
        private const float DROP_ALONG_STEP = 1.8f;
        private const float DROP_HEIGHT = 1f;

        private readonly ShipRunModel _model;
        private readonly ShipRunConfig _config;
        private readonly ShipFlightSettings _flightSettings;
        private readonly ShipStationCatalog _stations;
        private readonly IFlightStatContributor[] _contributors = System.Array.Empty<IFlightStatContributor>();

        private ShipRunDirector _director;
        private ShipBase _ship;
        private ShipLandingPad _currentPad;
        private ShipLandingPad _previousPad;
        private float _wreckUntil;
        private Vector3 _launchForward = Vector3.forward;
        private Vector3 _destinationPoint;
        private Vector3 _destinationForward = Vector3.forward;
        private readonly ShipTransit _transit = new ShipTransit();
        private ShipRadarService _radar;
        private ShipFlightMode _modeOverride;
        private bool _hasModeOverride;

        internal int WorldShiftCount => _ship != null ? _ship.PoseSync.WorldShift.Count : 0;

        public ShipRunService(
            ShipRunModel model,
            ShipRunConfig config,
            ShipFlightSettings flightSettings,
            ShipStationCatalog stations,
            ShipRadarService radar) {
            _model = model;
            _config = config;
            _flightSettings = flightSettings;
            _stations = stations;
            _radar = radar;
        }

        internal void DebugUseFlightMode(ShipFlightMode mode) {
            _modeOverride = mode;
            _hasModeOverride = true;
        }

        internal void DebugClearFlightMode() {
            _hasModeOverride = false;
        }

        public void CleanupGameplay() {
            ResetRun();
        }

        public void RestartGameplay() {
            ResetRun();
        }

        public bool TryGetWalkableFloorY(out bool isFlying, out float floorY) {
            isFlying = false;
            floorY = 0f;
            ShipRunPhase phase = _model.Phase;
            bool flying = phase == ShipRunPhase.Takeoff
                || phase == ShipRunPhase.Cruise
                || phase == ShipRunPhase.Landing;
            if (flying) {
                if (_ship == null || TryGetDeckSurfaceWorldY(_ship, out floorY) == false)
                    return false;

                isFlying = true;
                return true;
            }

            if (_currentPad == null)
                return false;

            floorY = _currentPad.BuildBerth.position.y;
            return true;
        }

        public void ServerReleaseRider(NetworkIdentity player) {
            if (NetworkServer.active == false)
                throw new System.InvalidOperationException("ServerReleaseRider can only be called on the server.");

            // No ship outside a run (lobby): there is nothing to release.
            if (_ship == null)
                return;

            if (player.TryGetComponent(out ShipRider rider) == false)
                throw new System.InvalidOperationException(player.name + " has no " + nameof(ShipRider) + ".");

            _ship.ServerStand(rider);
            _ship.UnregisterRider(rider);
        }

        internal void Bind(ShipRunDirector director, ShipBase ship, ShipLandingPad startPad) {
            _director = director;
            _ship = ship;
            _currentPad = startPad;
            _previousPad = null;
            _model.ResetMatch();
            if (ship != null && startPad != null)
                ship.ServerResetForBuild(startPad.BuildBerth.position, startPad.BuildBerth.rotation);

            if (_radar != null)
                _radar.BindShip(ship);
            RefreshTransitPreview();
            Publish();
        }

        internal void Unbind(ShipRunDirector director) {
            if (_director != director)
                return;

            ResetRun();
        }

        private void ResetRun() {
            if (NetworkServer.active && _ship != null && _currentPad != null)
                _ship.ServerResetForBuild(_currentPad.BuildBerth.position, _currentPad.BuildBerth.rotation);

            _director = null;
            _ship = null;
            _currentPad = null;
            _previousPad = null;
            _wreckUntil = 0f;
            _launchForward = Vector3.forward;
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            _transit.Reset();
            if (_radar != null)
                _radar.UnbindShip();
            _model.ResetMatch();
        }

        internal bool ServerTryLaunch(ShipBase ship) {
            if (NetworkServer.active == false || ship == null || ship != _ship)
                return false;

            if (_model.Phase != ShipRunPhase.Build || _model.LaunchLocked)
                return false;

            if (ship.CanLaunch == false)
                return false;

            FlightRunStats stats = SampleStats(ship);
            Vector3 from = ship.transform.position;
            _launchForward = FlattenForward(ship.transform.forward);
            Vector3 hover = from
                + _launchForward * _config.TakeoffForward
                + Vector3.up * _config.TakeoffHeight;
            ChooseDestination(hover);
            ShipFlightMode mode = SelectedFlightMode();
            Quaternion heading = Quaternion.LookRotation(_launchForward, Vector3.up);
            Quaternion routeHeading = Quaternion.LookRotation(_destinationForward, Vector3.up);
            ship.BeginTakeoff(
                from,
                hover,
                heading,
                routeHeading,
                _config.TakeoffSeconds,
                stats.DodgeRangeScale,
                mode);
            if (mode == ShipFlightMode.TravelInSpace)
                SpawnNextPad(_destinationPoint, _destinationForward, true);

            _transit.BeginRoute(stats.CruiseSeconds, _destinationForward);
            ApplyTransitFrame(false);
            _model.LastAbortReason = ShipRunAbortReason.None;
            _model.Phase = ShipRunPhase.Takeoff;
            Publish();
            return true;
        }

        internal void ServerAbort(ShipRunAbortReason reason) {
            if (NetworkServer.active == false || _ship == null || _director == null)
                return;

            if (_model.Phase != ShipRunPhase.Takeoff
                && _model.Phase != ShipRunPhase.Cruise
                && _model.Phase != ShipRunPhase.Landing)
                return;

            _model.LastAbortReason = reason;
            FinishAtCurrentPose();
        }

        internal void ServerTick() {
            if (NetworkServer.active == false || _ship == null || _director == null)
                return;

            if (_model.Phase == ShipRunPhase.Wreck) {
                if (Time.time < _wreckUntil)
                    return;

                _model.Phase = ShipRunPhase.Build;
                RefreshTransitPreview();
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Build) {
                RefreshTransitPreview();
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Takeoff) {
                ApplyTransitFrame(false);
                Publish();
                if (_ship.IsTakeoffComplete == false)
                    return;

                _ship.BeginCruise();
                _transit.SetSpeed(ReadFlightSpeed());
                ApplyTransitFrame(false);
                _model.Phase = ShipRunPhase.Cruise;
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Cruise) {
                ApplyTransitFrame(CruiseIsTravel() == false);
                Publish();
                if (CruiseArrived()) {
                    _ship.ServerLockFlight();
                    BeginLanding();
                }

                return;
            }

            if (_model.Phase == ShipRunPhase.Landing) {
                if (_ship.HasLanded)
                    FinishLanded();
            }
        }

        private void BeginLanding() {
            if (CruiseIsTravel()) {
                if (_currentPad == null) {
                    FinishAtCurrentPose();
                    return;
                }

                _destinationPoint = _currentPad.LandingPoint.position;
                _ship.BeginLanding(_destinationPoint, _config.LandingSeconds, _destinationForward);
                _model.Phase = ShipRunPhase.Landing;
                Publish();
                return;
            }

            ShipLandingPad next = SpawnNextPad(_destinationPoint, _destinationForward, false);
            if (next == null) {
                FinishAtCurrentPose();
                return;
            }

            _ship.BeginLanding(next.LandingPoint.position, _config.LandingSeconds, _destinationForward);
            _model.Phase = ShipRunPhase.Landing;
            Publish();
        }

        private void FinishLanded() {
            _ship.ServerSettleAfterLanding();
            EnterStation(_currentPad, ShipRunPhase.Build);
            // After EnterStation's origin shift: riders still bound are carried by it, released ones would stay behind.
            _ship.ServerReleaseRiders();
        }

        private void FinishAtCurrentPose() {
            Vector3 wreckPos = _ship.transform.position;
            Quaternion wreckRot = _ship.transform.rotation * Quaternion.Euler(WRECK_PITCH_DEGREES, 0f, WRECK_ROLL_DEGREES);
            _ship.ServerFinishFlight();
            _director.ServerPlaceWreck(wreckPos, wreckRot);

            if (_model.Phase != ShipRunPhase.Landing) {
                Vector3 origin = _ship != null ? _ship.transform.position : Vector3.zero;
                SpawnNextPad(origin + _destinationForward * _config.StationSpacing, _destinationForward, false);
            }

            ShipLandingPad pad = _currentPad;
            Vector3 berth = pad != null ? pad.BuildBerth.position : wreckPos;
            Quaternion berthRot = pad != null
                ? pad.BuildBerth.rotation
                : Quaternion.LookRotation(_launchForward, Vector3.up);
            _ship.ServerResetForBuild(berth, berthRot);
            _wreckUntil = Time.time + _config.WreckSettleSeconds;
            EnterStation(pad, ShipRunPhase.Wreck);
        }

        private void EnterStation(ShipLandingPad pad, ShipRunPhase phase) {
            RecenterIfFar();
            SpawnDrops(pad);
            if (pad != null)
                ConnectionNetworkManager.SetSpawn(pad.PlayerSpawn.position, pad.PlayerSpawn.rotation);

            _model.LoopIndex += 1;
            _model.LaunchLocked = _config.MaxLoops > 0 && _model.LoopIndex >= _config.MaxLoops;
            _model.Phase = phase;
            _transit.Reset();
            _hasModeOverride = false;
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            CopyTransitToModel();
            Publish();
        }

        private ShipLandingPad SpawnNextPad(Vector3 padPos, Vector3 face, bool matchLandingPoint) {
            GameObject prefab = _stations != null ? _stations.PadPrefab : null;
            if (prefab == null)
                return _currentPad;

            Vector3 origin = _ship != null ? _ship.transform.position : padPos;
            if (matchLandingPoint == false) {
                float padY = _currentPad != null ? _currentPad.transform.position.y : origin.y;
                padPos.y = padY + _config.TakeoffHeight;
            }

            Vector3 forward = FlattenForward(face.sqrMagnitude > 0.0001f ? face : _launchForward);
            Quaternion padRot = Quaternion.LookRotation(forward, Vector3.up);
            GameObject instance = Object.Instantiate(prefab, padPos, padRot);
            ShipLandingPad pad = instance.GetComponentInChildren<ShipLandingPad>();
            // Pads have no transform sync: clients only get the spawn pose, so the pad is placed before the spawn.
            if (matchLandingPoint) {
                instance.transform.position += padPos - pad.LandingPoint.position;
                _destinationPoint = pad.LandingPoint.position;
            }

            NetworkServer.Spawn(instance);

            if (_previousPad != null)
                NetworkServer.Destroy(_previousPad.gameObject);

            _previousPad = _currentPad;
            _currentPad = pad;
            return pad;
        }

        private void SpawnDrops(ShipLandingPad pad) {
            if (_stations == null || _stations.Drops == null)
                return;

            Transform origin = pad != null ? pad.transform : (_ship != null ? _ship.transform : null);
            if (origin == null)
                return;

            int placed = 0;
            GameObject helm = FindDropPrefab(ShipModuleType.Control);
            if (helm != null) {
                SpawnDrop(helm, origin, placed);
                placed++;
            }

            for (int i = 0; i < _stations.Drops.Length; i++) {
                ShipStationCatalog.DropEntry entry = _stations.Drops[i];
                if (entry == null || entry.Prefab == null || entry.MinLoop > _model.LoopIndex)
                    continue;

                if (IsDropType(entry.Prefab, ShipModuleType.Control))
                    continue;

                for (int n = 0; n < entry.Count; n++) {
                    SpawnDrop(entry.Prefab, origin, placed);
                    placed++;
                }
            }
        }

        private void SpawnDrop(GameObject prefab, Transform origin, int placed) {
            float side = placed % 2 == 0 ? -1f : 1f;
            float along = placed / 2 * DROP_ALONG_STEP;
            Vector3 local = new Vector3(side * DROP_LATERAL, DROP_HEIGHT, DROP_ALONG_ORIGIN - along);
            GameObject item = Object.Instantiate(prefab, origin.TransformPoint(local), origin.rotation);
            NetworkServer.Spawn(item);
        }

        private GameObject FindDropPrefab(ShipModuleType type) {
            if (_stations == null || _stations.Drops == null)
                return null;

            for (int i = 0; i < _stations.Drops.Length; i++) {
                ShipStationCatalog.DropEntry entry = _stations.Drops[i];
                if (entry != null && IsDropType(entry.Prefab, type))
                    return entry.Prefab;
            }

            return null;
        }

        private static bool IsDropType(GameObject prefab, ShipModuleType type) {
            if (prefab == null)
                return false;

            ShipItem item = prefab.GetComponent<ShipItem>();
            return item != null && item.Type == type;
        }

        private FlightRunStats SampleStats(ShipBase ship) {
            FlightRunStats stats = new FlightRunStats {
                DodgeRangeScale = 1f,
                CruiseSeconds = EvaluateRouteWork()
            };

            ShipSocket[] sockets = ship != null ? ship.Sockets : null;
            stats.TotalThrust = ship != null ? ship.GetStatFull(ShipStatType.FlightSpeed) : 0f;

            float dodge = ship != null ? ship.GetStatFull(ShipStatType.DodgeRange) : 0f;
            if (dodge > 0f)
                stats.DodgeRangeScale = dodge;

            for (int i = 0; i < _contributors.Length; i++) {
                if (_contributors[i] != null)
                    _contributors[i].Contribute(sockets, _model.LoopIndex, stats);
            }

            stats.CruiseSeconds = Mathf.Max(1f, stats.CruiseSeconds);
            stats.DodgeRangeScale = Mathf.Max(0.1f, stats.DodgeRangeScale);
            return stats;
        }

        private float EvaluateRouteWork() {
            return _config.RouteWorkSeconds + _model.LoopIndex * _config.PerLoopCruiseSeconds;
        }

        private void RefreshTransitPreview() {
            float work = SampleStats(_ship).CruiseSeconds;
            Vector3 destination = _ship != null
                ? FlattenForward(_ship.transform.forward)
                : _launchForward;
            _transit.PreviewRoute(work, ReadFlightSpeed(), destination);
            if (_destinationPoint.sqrMagnitude < 0.0001f)
                _destinationPoint = RadarPoint(
                    _ship != null ? _ship.transform.position : Vector3.zero,
                    destination);
            CopyTransitToModel();
        }

        private void ApplyTransitFrame(bool tickWork) {
            if (CruiseIsTravel() && _ship != null && _model.Phase != ShipRunPhase.Build) {
                float distance = HorizontalDistance(_ship.transform.position, _destinationPoint);
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

        private float ReadFlightSpeed() {
            float speed = _ship != null ? _ship.GetStatFull(ShipStatType.FlightSpeed) : 1f;
            return Mathf.Max(ShipTransit.MinSpeed, speed);
        }

        private float EvaluateTransitAlignment() {
            if (_ship == null)
                return 1f;

            return ShipTransit.EvaluateAlignment(
                FlattenForward(_ship.transform.forward),
                _destinationForward);
        }

        private void CopyTransitToModel() {
            _model.TransitWorkRemaining = _transit.WorkRemaining;
            _model.TransitSpeed = _transit.Speed;
            _model.TransitAlignment = _transit.Alignment;
            _model.TransitDestination = _destinationPoint.sqrMagnitude > 0.0001f
                ? _destinationPoint
                : RadarPoint(
                    _ship != null ? _ship.transform.position : Vector3.zero,
                    _transit.DestinationDirection);
            _model.TransitSecondsRemaining = _transit.SecondsRemaining;
            _model.CruiseEndNetworkTime = _model.Phase == ShipRunPhase.Cruise
                ? NetworkTime.time + _model.TransitSecondsRemaining
                : 0d;
        }

        private void ChooseDestination(Vector3 cruiseStart) {
            float halfCone = _config.DestinationConeDegrees * 0.5f;
            float yaw = Random.Range(-halfCone, halfCone);
            _destinationForward = FlattenForward(Quaternion.AngleAxis(yaw, Vector3.up) * _launchForward);
            if (SelectedFlightMode() == ShipFlightMode.TravelInSpace) {
                float distance = Mathf.Max(
                    _config.ApproachDistance * 3f,
                    EvaluateRouteWork() * Mathf.Max(1f, _flightSettings.CruiseSpeed));
                float padY = _currentPad != null
                    ? _currentPad.transform.position.y + _config.TakeoffHeight
                    : cruiseStart.y;
                _destinationPoint = cruiseStart + _destinationForward * distance;
                _destinationPoint.y = padY;
                return;
            }

            _destinationPoint = RadarPoint(cruiseStart, _destinationForward);
        }

        private Vector3 RadarPoint(Vector3 origin, Vector3 direction) {
            float range = Mathf.Max(80f, _config.StationSpacing);
            float padY = _currentPad != null
                ? _currentPad.transform.position.y + _config.TakeoffHeight
                : origin.y;
            Vector3 point = origin + FlattenForward(direction) * range;
            point.y = padY;
            return point;
        }

        private void Publish() {
            if (_director != null)
                _director.ServerPublish();
        }

        internal void DebugForceRecenter() {
            if (_ship == null)
                return;

            Vector3 position = _ship.transform.position;
            Vector3 delta = new Vector3(-position.x, 0f, -position.z);
            if (delta.sqrMagnitude < 1f)
                delta = new Vector3(40f, 0f, -25f);

            ApplyWorldShift(delta);
        }

        private void RecenterIfFar() {
            if (_ship == null)
                return;

            Vector3 position = _ship.transform.position;
            float limit = _config.OriginRecenterDistance;
            if (position.x * position.x + position.z * position.z < limit * limit)
                return;

            ApplyWorldShift(new Vector3(-position.x, 0f, -position.z));
        }

        private void ApplyWorldShift(Vector3 delta) {
            _ship.ServerApplyWorldShift(delta);
            ShiftPad(_currentPad, delta);
            ShiftPad(_previousPad, delta);
            _destinationPoint += delta;
            if (_director != null)
                _director.ServerShiftWorld(delta, PadIdentity(_currentPad), PadIdentity(_previousPad));
        }

        private static NetworkIdentity PadIdentity(ShipLandingPad pad) =>
            pad != null ? pad.GetComponent<NetworkIdentity>() : null;

        private bool CruiseArrived() {
            if (CruiseIsTravel() == false)
                return _transit.HasArrived;

            return HorizontalDistance(_ship.transform.position, _destinationPoint) <= _config.ApproachDistance;
        }

        private bool CruiseIsTravel() {
            if (_ship != null && _ship.IsFlying)
                return _ship.ActiveFlightMode == ShipFlightMode.TravelInSpace;

            return SelectedFlightMode() == ShipFlightMode.TravelInSpace;
        }

        private ShipFlightMode SelectedFlightMode() {
            if (_hasModeOverride)
                return _modeOverride;

            return _flightSettings.FlightMode;
        }

        private float ReadTravelMetersPerSecond() {
            return _flightSettings.CruiseSpeed * ReadFlightSpeed();
        }

        private static void ShiftPad(ShipLandingPad pad, Vector3 delta) {
            if (pad != null)
                pad.transform.position += delta;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to) {
            float x = from.x - to.x;
            float z = from.z - to.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        private static bool TryGetDeckSurfaceWorldY(ShipBase ship, out float worldY) {
            worldY = 0f;
            if (ship.TryGetDeckSurfaceY(Vector3.zero, out float surfaceLocalY) == false)
                return false;

            worldY = ship.transform.TransformPoint(new Vector3(0f, surfaceLocalY, 0f)).y;
            return true;
        }

        private static Vector3 FlattenForward(Vector3 forward) {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
