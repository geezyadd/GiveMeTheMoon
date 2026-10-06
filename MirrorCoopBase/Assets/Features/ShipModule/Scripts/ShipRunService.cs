using Features.GameCoreModule.Contracts;
using Game.Connection;
using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The run's phase machine: Build -> Takeoff -> Cruise -> Landing -> Build, or Wreck after an abort.
    public sealed class ShipRunService : IShipRunService, IGameplaySession {
        private const float WRECK_PITCH_DEGREES = 12f;
        private const float WRECK_ROLL_DEGREES = 18f;

        private readonly ShipRunModel _model;
        private readonly ShipRunConfig _config;
        private readonly IShipRunBinding _binding;
        private readonly IShipStationPads _pads;
        private readonly IShipStationDropService _drops;
        private readonly IShipFlightStatService _flightStats;
        private readonly IShipRoute _route;
        private readonly IShipWorldShiftService _worldShift;

        private float _wreckUntil;
        private ShipRadarService _radar;
        private readonly ConnectionSpawnModel _spawn;

        private ShipBase Ship => _binding.Ship;
        private ShipRunDirector Director => _binding.Director;
        private ShipLandingPad CurrentPad => _pads.CurrentPad;

        public ShipRunService(
            ShipRunModel model,
            ShipRunConfig config,
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
            if (Director != director)
                return;

            ResetRun();
        }

        private void ResetRun() {
            if (NetworkServer.active && Ship != null && CurrentPad != null)
                Ship.ServerResetForBuild(CurrentPad.BuildBerth.position, CurrentPad.BuildBerth.rotation);

            _binding.Clear();
            _pads.Reset(null);
            _wreckUntil = 0f;
            _route.Reset();
            if (_radar != null)
                _radar.UnbindShip();
            _model.ResetMatch();
        }

        public bool ServerTryLaunch(ShipBase ship) {
            if (NetworkServer.active == false || ship == null || ship != Ship)
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
            if (NetworkServer.active == false || Ship == null || Director == null)
                return;

            if (_model.Phase != ShipRunPhase.Takeoff
                && _model.Phase != ShipRunPhase.Cruise
                && _model.Phase != ShipRunPhase.Landing)
                return;

            _model.LastAbortReason = reason;
            FinishAtCurrentPose();
        }

        public void ServerTick() {
            if (NetworkServer.active == false || Ship == null || Director == null)
                return;

            switch (_model.Phase) {
                case ShipRunPhase.Wreck:
                    TickWreck();
                    return;
                case ShipRunPhase.Build:
                    _route.RefreshPreview();
                    Publish();
                    return;
                case ShipRunPhase.Takeoff:
                    TickTakeoff();
                    return;
                case ShipRunPhase.Cruise:
                    TickCruise();
                    return;
                case ShipRunPhase.Landing:
                    if (Ship.HasLanded)
                        FinishLanded();
                    return;
            }
        }

        private void TickWreck() {
            if (Time.time < _wreckUntil)
                return;

            _model.Phase = ShipRunPhase.Build;
            _route.RefreshPreview();
            Publish();
        }

        private void TickTakeoff() {
            _route.ApplyFrame(false);
            Publish();
            if (Ship.IsTakeoffComplete == false)
                return;

            Ship.BeginCruise();
            _route.BeginCruise();
            _route.ApplyFrame(false);
            _model.Phase = ShipRunPhase.Cruise;
            Publish();
        }

        private void TickCruise() {
            _route.ApplyFrame(_route.IsTravel == false);
            Publish();
            if (_route.HasArrived == false)
                return;

            Ship.ServerLockFlight();
            BeginLanding();
        }

        private void BeginLanding() {
            if (_route.IsTravel) {
                if (CurrentPad == null) {
                    FinishAtCurrentPose();
                    return;
                }

                _route.SetDestinationPoint(CurrentPad.LandingPoint.position);
                Ship.BeginLanding(_route.DestinationPoint, _config.LandingSeconds, _route.DestinationForward);
                _model.Phase = ShipRunPhase.Landing;
                Publish();
                return;
            }

            ShipLandingPad next = SpawnNextPad(_route.DestinationPoint, _route.DestinationForward, false);
            if (next == null) {
                FinishAtCurrentPose();
                return;
            }

            Ship.BeginLanding(next.LandingPoint.position, _config.LandingSeconds, _route.DestinationForward);
            _model.Phase = ShipRunPhase.Landing;
            Publish();
        }

        private void FinishLanded() {
            Ship.ServerSettleAfterLanding();
            EnterStation(CurrentPad, ShipRunPhase.Build);
            // After EnterStation's origin shift: riders still bound are carried by it, released ones would stay behind.
            Ship.Riders.ServerReleaseRiders();
        }

        private void FinishAtCurrentPose() {
            Vector3 wreckPos = Ship.transform.position;
            Quaternion wreckRot = Ship.transform.rotation * Quaternion.Euler(WRECK_PITCH_DEGREES, 0f, WRECK_ROLL_DEGREES);
            Ship.ServerFinishFlight();
            Director.ServerPlaceWreck(wreckPos, wreckRot);

            if (_model.Phase != ShipRunPhase.Landing) {
                Vector3 origin = Ship != null ? Ship.transform.position : Vector3.zero;
                SpawnNextPad(origin + _route.DestinationForward * _config.StationSpacing, _route.DestinationForward, false);
            }

            ShipLandingPad pad = CurrentPad;
            Vector3 berth = pad != null ? pad.BuildBerth.position : wreckPos;
            Quaternion berthRot = pad != null
                ? pad.BuildBerth.rotation
                : Quaternion.LookRotation(_route.LaunchForward, Vector3.up);
            Ship.ServerResetForBuild(berth, berthRot);
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
            if (Director != null)
                Director.ServerPublish();
        }
    }
}
