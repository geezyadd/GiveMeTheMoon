using Features.GameCoreModule.Contracts;
using Game.Connection;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public sealed class ShipRunService : IShipRunService, IGameplaySession, IShipFloorReferenceProvider, IShipRiderRelease {
        private const float WRECK_PITCH_DEGREES = 12f;
        private const float WRECK_ROLL_DEGREES = 18f;

        private readonly ShipRunModel _model;
        private readonly ShipRunConfig _config;
        private readonly ShipFlightSettings _flightSettings;
        private readonly IShipRunBinding _binding;
        private readonly IShipStationPads _pads;
        private readonly IShipStationDropService _drops;
        private readonly IShipFlightStatService _flightStats;
        private readonly IShipRoute _route;
        private readonly IShipWorldShiftService _worldShift;

        private float _wreckUntil;
        private ShipRadarService _radar;
        private readonly ConnectionSpawnModel _spawn;

        private ShipBase _ship => _binding.Ship;
        private ShipRunDirector _director => _binding.Director;
        private ShipLandingPad _currentPad => _pads.CurrentPad;

        public ShipRunService(
            ShipRunModel model,
            ShipRunConfig config,
            ShipFlightSettings flightSettings,
            ShipRadarService radar,
            ConnectionSpawnModel spawn,
            IShipRunBinding binding,
            IShipStationPads pads,
            IShipStationDropService drops,
            IShipFlightStatService flightStats,
            IShipRoute route,
            IShipWorldShiftService worldShift) {
            _model = model;
            _config = config;
            _flightSettings = flightSettings;
            _radar = radar;
            _spawn = spawn;
            _binding = binding;
            _pads = pads;
            _drops = drops;
            _flightStats = flightStats;
            _route = route;
            _worldShift = worldShift;
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

        public void Bind(ShipRunDirector director, ShipBase ship, ShipLandingPad startPad) {
            _binding.Bind(director, ship);
            _pads.Reset(startPad);
            _model.ResetMatch();
            if (ship != null && startPad != null)
                ship.ServerResetForBuild(startPad.BuildBerth.position, startPad.BuildBerth.rotation);

            if (_radar != null)
                _radar.BindShip(ship);
            _route.RefreshPreview();
            Publish();
        }

        public void Unbind(ShipRunDirector director) {
            if (_director != director)
                return;

            ResetRun();
        }

        private void ResetRun() {
            if (NetworkServer.active && _ship != null && _currentPad != null)
                _ship.ServerResetForBuild(_currentPad.BuildBerth.position, _currentPad.BuildBerth.rotation);

            _binding.Clear();
            _pads.Reset(null);
            _wreckUntil = 0f;
            _route.Reset();
            if (_radar != null)
                _radar.UnbindShip();
            _model.ResetMatch();
        }

        public bool ServerTryLaunch(ShipBase ship) {
            if (NetworkServer.active == false || ship == null || ship != _ship)
                return false;

            if (_model.Phase != ShipRunPhase.Build || _model.LaunchLocked)
                return false;

            if (ship.CanLaunch == false)
                return false;

            FlightRunStats stats = _flightStats.Sample(ship, _model.LoopIndex);
            Vector3 from = ship.transform.position;
            Vector3 hover = _route.PlanLaunch(from, ship.transform.forward);
            ShipFlightMode mode = _route.SelectedFlightMode;
            Quaternion heading = Quaternion.LookRotation(_route.LaunchForward, Vector3.up);
            Quaternion routeHeading = Quaternion.LookRotation(_route.DestinationForward, Vector3.up);
            ship.BeginTakeoff(
                from,
                hover,
                heading,
                routeHeading,
                _config.TakeoffSeconds,
                stats.DodgeRangeScale,
                mode);
            if (mode == ShipFlightMode.TravelInSpace)
                SpawnNextPad(_route.DestinationPoint, _route.DestinationForward, true);

            _route.BeginRoute(stats.CruiseSeconds);
            _route.ApplyFrame(false);
            _model.LastAbortReason = ShipRunAbortReason.None;
            _model.Phase = ShipRunPhase.Takeoff;
            Publish();
            return true;
        }

        public void ServerAbort(ShipRunAbortReason reason) {
            if (NetworkServer.active == false || _ship == null || _director == null)
                return;

            if (_model.Phase != ShipRunPhase.Takeoff
                && _model.Phase != ShipRunPhase.Cruise
                && _model.Phase != ShipRunPhase.Landing)
                return;

            _model.LastAbortReason = reason;
            FinishAtCurrentPose();
        }

        public void ServerTick() {
            if (NetworkServer.active == false || _ship == null || _director == null)
                return;

            if (_model.Phase == ShipRunPhase.Wreck) {
                if (Time.time < _wreckUntil)
                    return;

                _model.Phase = ShipRunPhase.Build;
                _route.RefreshPreview();
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Build) {
                _route.RefreshPreview();
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Takeoff) {
                _route.ApplyFrame(false);
                Publish();
                if (_ship.IsTakeoffComplete == false)
                    return;

                _ship.BeginCruise();
                _route.BeginCruise();
                _route.ApplyFrame(false);
                _model.Phase = ShipRunPhase.Cruise;
                Publish();
                return;
            }

            if (_model.Phase == ShipRunPhase.Cruise) {
                _route.ApplyFrame(_route.IsTravel == false);
                Publish();
                if (_route.HasArrived) {
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
            if (_route.IsTravel) {
                if (_currentPad == null) {
                    FinishAtCurrentPose();
                    return;
                }

                _route.SetDestinationPoint(_currentPad.LandingPoint.position);
                _ship.BeginLanding(_route.DestinationPoint, _config.LandingSeconds, _route.DestinationForward);
                _model.Phase = ShipRunPhase.Landing;
                Publish();
                return;
            }

            ShipLandingPad next = SpawnNextPad(_route.DestinationPoint, _route.DestinationForward, false);
            if (next == null) {
                FinishAtCurrentPose();
                return;
            }

            _ship.BeginLanding(next.LandingPoint.position, _config.LandingSeconds, _route.DestinationForward);
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
                SpawnNextPad(origin + _route.DestinationForward * _config.StationSpacing, _route.DestinationForward, false);
            }

            ShipLandingPad pad = _currentPad;
            Vector3 berth = pad != null ? pad.BuildBerth.position : wreckPos;
            Quaternion berthRot = pad != null
                ? pad.BuildBerth.rotation
                : Quaternion.LookRotation(_route.LaunchForward, Vector3.up);
            _ship.ServerResetForBuild(berth, berthRot);
            _wreckUntil = Time.time + _config.WreckSettleSeconds;
            EnterStation(pad, ShipRunPhase.Wreck);
        }

        private void EnterStation(ShipLandingPad pad, ShipRunPhase phase) {
            _worldShift.RecenterIfFar();
            _drops.SpawnDrops(pad, _model.LoopIndex);
            if (pad != null)
                _spawn.Set(pad.PlayerSpawn.position, pad.PlayerSpawn.rotation);

            _model.LoopIndex += 1;
            _model.LaunchLocked = _config.MaxLoops > 0 && _model.LoopIndex >= _config.MaxLoops;
            _model.Phase = phase;
            _route.EndRoute();
            Publish();
        }

        private ShipLandingPad SpawnNextPad(Vector3 padPos, Vector3 face, bool matchLandingPoint) {
            Vector3 forward = _route.PadForward(face);
            if (_pads.TrySpawnNext(padPos, forward, matchLandingPoint, out ShipLandingPad pad) && matchLandingPoint)
                _route.SetDestinationPoint(pad.LandingPoint.position);

            return pad;
        }

        private void Publish() {
            if (_director != null)
                _director.ServerPublish();
        }

        private static bool TryGetDeckSurfaceWorldY(ShipBase ship, out float worldY) {
            worldY = 0f;
            if (ship.TryGetDeckSurfaceY(Vector3.zero, out float surfaceLocalY) == false)
                return false;

            worldY = ship.transform.TransformPoint(new Vector3(0f, surfaceLocalY, 0f)).y;
            return true;
        }
    }
}
