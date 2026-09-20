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
        private readonly EngineCatalog _engines;
        private readonly IFlightStatContributor[] _contributors = System.Array.Empty<IFlightStatContributor>();
        private readonly List<CruiseRock> _rocks = new List<CruiseRock>();

        private ShipRunDirector _director;
        private ShipBase _ship;
        private ShipLandingPad _currentPad;
        private ShipLandingPad _previousPad;
        private float _wreckUntil;
        private Vector3 _launchForward = Vector3.forward;
        private float _dodgeRangeScale = 1f;
        private float _nextRockSpawn;

        public ShipRunService(
            ShipRunModel model,
            ShipRunConfig config,
            ShipStationCatalog stations,
            EngineCatalog engines) {
            _model = model;
            _config = config;
            _stations = stations;
            _engines = engines;
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
            _dodgeRangeScale = 1f;
            _nextRockSpawn = 0f;
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
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Takeoff) {
                if (_ship.IsTakeoffComplete == false)
                    return;

                _ship.BeginCruise();
                float cruiseSeconds = SampleStats(_ship).CruiseSeconds;
                _model.Phase = ShipRunPhase.Cruise;
                _model.CruiseEndNetworkTime = NetworkTime.time + cruiseSeconds;
                _nextRockSpawn = Time.time;
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Cruise) {
                TickRocks();
                if (NetworkTime.time < _model.CruiseEndNetworkTime)
                    return;

                BeginLanding();
                return;
            }

            if (_model.Phase == ShipRunPhase.Landing) {
                if (_ship.HasLanded)
                    FinishAtCurrentPose();
            }
        }

        private void BeginLanding() {
            ClearRocks();
            ShipLandingPad next = SpawnNextPad();
            if (next == null) {
                FinishAtCurrentPose();
                return;
            }

            _ship.BeginLanding(next.LandingPoint.position, _config.LandingSeconds);
            _model.Phase = ShipRunPhase.Landing;
            Publish();
        }

        private void FinishAtCurrentPose() {
            ClearRocks();
            Vector3 wreckPos = _ship.transform.position;
            Quaternion wreckRot = _ship.transform.rotation * Quaternion.Euler(12f, 0f, 18f);
            _ship.ServerFinishFlight();
            _director.ServerPlaceWreck(wreckPos, wreckRot);

            if (_model.Phase != ShipRunPhase.Landing)
                SpawnNextPad();

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
            _model.CruiseEndNetworkTime = 0d;
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

            Vector3 forward = _launchForward;
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

        private ShipLandingPad SpawnNextPad() {
            GameObject prefab = _stations != null ? _stations.PadPrefab : null;
            if (prefab == null)
                return _currentPad;

            Vector3 origin = _ship != null ? _ship.transform.position : Vector3.zero;
            float padY = _currentPad != null ? _currentPad.transform.position.y : origin.y;
            Vector3 padPos = origin + _launchForward * _config.StationSpacing;
            padPos.y = padY + _config.TakeoffHeight;
            Quaternion padRot = Quaternion.LookRotation(_launchForward, Vector3.up);
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
                DodgeRangeScale = 1f
            };

            ShipSocket[] sockets = ship != null ? ship.Sockets : null;
            float thrust = 0f;
            if (sockets != null && _engines != null) {
                for (int i = 0; i < sockets.Length; i++) {
                    ShipSocket socket = sockets[i];
                    if (socket == null || _engines.TryGet(socket.InstalledView, out _) == false)
                        continue;

                    thrust += 1f;
                }
            }

            stats.TotalThrust = thrust;

            stats.CruiseSeconds = _config.CruiseSeconds
                + _model.LoopIndex * _config.PerLoopCruiseSeconds
                + thrust * _config.ThrustCruiseBonus;

            for (int i = 0; i < _contributors.Length; i++) {
                if (_contributors[i] != null)
                    _contributors[i].Contribute(sockets, _model.LoopIndex, stats);
            }

            stats.CruiseSeconds = Mathf.Max(1f, stats.CruiseSeconds);
            stats.DodgeRangeScale = Mathf.Max(0.1f, stats.DodgeRangeScale);
            return stats;
        }

        private void Publish() {
            if (_director != null)
                _director.ServerPublish(_model.Phase, _model.LoopIndex, _model.CruiseEndNetworkTime, _model.LaunchLocked);
        }

        private static Vector3 FlattenForward(Vector3 forward) {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
