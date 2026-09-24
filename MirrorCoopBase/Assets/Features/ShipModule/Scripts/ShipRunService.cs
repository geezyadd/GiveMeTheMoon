using System.Collections.Generic;
using Features.GameCoreModule.Scripts;
using Game.Connection;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunService : IGameplaySession {
        private readonly ShipRunModel _model;
        private readonly ShipRunConfig _config;
        private readonly ShipStationCatalog _stations;
        private readonly IFlightStatContributor[] _contributors = System.Array.Empty<IFlightStatContributor>();
        private readonly List<CruiseRock> _rocks = new List<CruiseRock>();

        private ShipRunDirector _director;
        private ShipBase _ship;
        private ShipLandingPad _currentPad;
        private ShipLandingPad _previousPad;
        private float _wreckUntil;
        private Vector3 _launchForward = Vector3.forward;
        private Vector3 _destinationPoint;
        private Vector3 _destinationForward = Vector3.forward;
        private float _dodgeRangeScale = 1f;
        private float _nextRockSpawn;
        private readonly ShipTransit _transit = new ShipTransit();
        private ShipRadarService _radar;

        public ShipRunService(
            ShipRunModel model,
            ShipRunConfig config,
            ShipStationCatalog stations,
            ShipRadarService radar) {
            _model = model;
            _config = config;
            _stations = stations;
            _radar = radar;
        }

        public void CleanupGameplay() {
            ResetRun();
        }

        public void RestartGameplay() {
            ResetRun();
        }

        internal void Bind(ShipRunDirector director, ShipBase ship, ShipLandingPad startPad) {
            _director = director;
            _ship = ship;
            _currentPad = startPad;
            _previousPad = null;
            _model.ResetMatch();
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
            ClearRocks();
            _director = null;
            _ship = null;
            _currentPad = null;
            _previousPad = null;
            _wreckUntil = 0f;
            _launchForward = Vector3.forward;
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            _dodgeRangeScale = 1f;
            _nextRockSpawn = 0f;
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
            _dodgeRangeScale = stats.DodgeRangeScale;
            Vector3 from = ship.transform.position;
            _launchForward = FlattenForward(ship.transform.forward);
            Vector3 hover = from
                + _launchForward * _config.TakeoffForward
                + Vector3.up * _config.TakeoffHeight;
            Quaternion heading = Quaternion.LookRotation(_launchForward, Vector3.up);
            ship.BeginTakeoff(from, hover, heading, _config.TakeoffSeconds, stats.DodgeRangeScale);
            ChooseDestination(hover);
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
            ClearRocks();
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
                FlightRunStats stats = SampleStats(_ship);
                _dodgeRangeScale = stats.DodgeRangeScale;
                _transit.SetSpeed(ReadFlightSpeed());
                ApplyTransitFrame(false);
                _model.Phase = ShipRunPhase.Cruise;
                _nextRockSpawn = Time.time;
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Cruise) {
                TickRocks();
                ApplyTransitFrame(true);
                Publish();
                if (_transit.HasArrived) {
                    _ship.ServerLockFlight();
                    BeginLanding();
                }

                return;
            }

            if (_model.Phase == ShipRunPhase.Landing) {
                if (_ship.HasLanded)
                    FinishAtCurrentPose();
            }
        }

        private void BeginLanding() {
            ClearRocks();
            ShipLandingPad next = SpawnNextPad(_destinationPoint, _destinationForward);
            if (next == null) {
                FinishAtCurrentPose();
                return;
            }

            _ship.BeginLanding(next.LandingPoint.position, _config.LandingSeconds, _destinationForward);
            _model.Phase = ShipRunPhase.Landing;
            Publish();
        }

        private void FinishAtCurrentPose() {
            ClearRocks();
            Vector3 wreckPos = _ship.transform.position;
            Quaternion wreckRot = _ship.transform.rotation * Quaternion.Euler(12f, 0f, 18f);
            _ship.ServerFinishFlight();
            _director.ServerPlaceWreck(wreckPos, wreckRot);

            if (_model.Phase != ShipRunPhase.Landing) {
                Vector3 origin = _ship != null ? _ship.transform.position : Vector3.zero;
                SpawnNextPad(origin + _destinationForward * _config.StationSpacing, _destinationForward);
            }

            ShipLandingPad pad = _currentPad;

            Vector3 berth = pad != null ? pad.BuildBerth.position : wreckPos;
            Quaternion berthRot = pad != null ? pad.BuildBerth.rotation : Quaternion.LookRotation(_launchForward, Vector3.up);
            _ship.ServerResetForBuild(berth, berthRot);
            SpawnDrops(pad);
            if (pad != null)
                ConnectionNetworkManager.SetSpawn(pad.PlayerSpawn.position, pad.PlayerSpawn.rotation);

            _model.LoopIndex += 1;
            _model.LaunchLocked = _config.MaxLoops > 0 && _model.LoopIndex >= _config.MaxLoops;
            _model.Phase = ShipRunPhase.Wreck;
            _transit.Reset();
            _destinationPoint = Vector3.zero;
            _destinationForward = Vector3.forward;
            CopyTransitToModel();
            _wreckUntil = Time.time + _config.WreckSettleSeconds;
            Publish();
        }

        private void TickRocks() {
            if (_ship == null || Time.time < _nextRockSpawn)
                return;

            _nextRockSpawn = Time.time + _config.RockSpawnInterval;
            SpawnRock();
        }

        private void SpawnRock() {
            GameObject prefab = _stations != null ? _stations.RockPrefab : null;
            if (prefab == null || _ship == null)
                return;

            Vector3 forward = FlattenForward(_ship.transform.forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            else
                right.Normalize();

            Vector3 incoming = (Vector3.up * 2f + forward).normalized;
            float corridor = _config.RockLateral * _dodgeRangeScale;
            float lateral = Random.value < 0.55f
                ? Random.Range(-corridor * 0.35f, corridor * 0.35f)
                : Random.Range(-corridor, corridor);
            Vector3 origin = _ship.transform.position
                + incoming * _config.RockSpawnAhead
                + right * lateral;
            GameObject instance = Object.Instantiate(
                prefab,
                origin,
                Quaternion.LookRotation(-incoming, Vector3.up));
            NetworkServer.Spawn(instance);
            CruiseRock rock = instance.GetComponent<CruiseRock>();
            if (rock == null) {
                NetworkServer.Destroy(instance);
                return;
            }

            float lifetime = _config.RockSpawnAhead / _config.RockSpeed + 3f;
            rock.ServerLaunch(-incoming * _config.RockSpeed, lifetime);
            _rocks.Add(rock);
        }

        private void ClearRocks() {
            for (int i = 0; i < _rocks.Count; i++) {
                CruiseRock rock = _rocks[i];
                if (rock == null)
                    continue;

                if (NetworkServer.active)
                    NetworkServer.Destroy(rock.gameObject);
                else
                    Object.Destroy(rock.gameObject);
            }

            _rocks.Clear();
        }

        private ShipLandingPad SpawnNextPad(Vector3 padPos, Vector3 face) {
            GameObject prefab = _stations != null ? _stations.PadPrefab : null;
            if (prefab == null)
                return _currentPad;

            Vector3 origin = _ship != null ? _ship.transform.position : padPos;
            float padY = _currentPad != null ? _currentPad.transform.position.y : origin.y;
            padPos.y = padY + _config.TakeoffHeight;
            Vector3 forward = FlattenForward(face.sqrMagnitude > 0.0001f ? face : _launchForward);
            Quaternion padRot = Quaternion.LookRotation(forward, Vector3.up);
            GameObject instance = Object.Instantiate(prefab, padPos, padRot);
            NetworkServer.Spawn(instance);
            ShipLandingPad pad = instance.GetComponentInChildren<ShipLandingPad>();
            if (pad == null) {
                NetworkServer.Destroy(instance);
                return _currentPad;
            }

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
            float along = placed / 2 * 1.8f;
            Vector3 local = new Vector3(side * 5.5f, 1f, 8f - along);
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

        private void ChooseDestination(Vector3 origin) {
            float halfCone = _config.DestinationConeDegrees * 0.5f;
            float yaw = Random.Range(-halfCone, halfCone);
            _destinationForward = FlattenForward(Quaternion.AngleAxis(yaw, Vector3.up) * _launchForward);
            _destinationPoint = RadarPoint(origin, _destinationForward);
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

        private static Vector3 FlattenForward(Vector3 forward) {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
