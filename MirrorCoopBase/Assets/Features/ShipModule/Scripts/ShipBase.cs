using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts.Damage;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Hull;
using Features.ShipModule.Scripts.Impacts;
using Features.ShipModule.Scripts.Repair;
using Mirror;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Features.ShipModule.Scripts {
    public sealed class ShipBase : MonoBehaviour {
        [SerializeField] private ShipSocket[] _sockets;
        [SerializeField] private ShipLaunchLever _lever;
        [SerializeField] private Transform _destination;
        [SerializeField] private Rigidbody _body;
        [SerializeField] private BoxCollider _rideVolume;
        [SerializeField] private BoxCollider _deck;
        [SerializeField] private BoxCollider[] _deckColliders;
        [SerializeField] private ShipPoseSync _poseSync;
        [SerializeField] private ShipStatEntity _stats;
        [SerializeField] private ShipSocketsSync _socketStates;
        [SerializeField] private ShipStatsSync _statsSync;
        [SerializeField] private ShipHullVolume _hullVolume;
        [SerializeField] private ShipImpactSync _impactSync;
        [SerializeField] private ShipConditionSync _conditionSync;
        [SerializeField] private ShipHullDamagePoint[] _damagePoints;
        [SerializeField] private ShipToolRack _toolRack;

        private ShipFlightSettings _flightSettings;
        private ShipAccumulativeStatsConfiguration _statsConfiguration;

        private ShipDeckCargo _cargo;
        private ShipDeckGeometry _deckGeometry;
        private ShipModules _modules;
        private ShipHullStat _hull;
        private ShipRiders _riders;
        private ShipDeckRideRule _rideRule;
        private ShipSeats _seats;
        private ShipSteering _steering;
        private ShipFlightControl _flightControl;
        private int _fixedSteps;
        private bool _flying;

        private IReadOnlyShipRunModel _run;
        private IShipRunService _runService;
        private CharacterMovableModel _characterMovableModel;
        private ShipRiderKeyModel _shipRiderKeyModel;

        public ShipLaunchLever Lever => _lever;
        public Transform Destination => _destination;
        public bool IsFlying => _flying;
        internal BoxCollider RideVolume => _rideVolume;
        internal bool ConfinesRidersToDeck => _flightSettings.ConfineRidersToDeck;
        internal float ReboardDelaySeconds => _flightSettings.ReboardDelaySeconds;
        internal NetworkIdentity NetIdentity => _poseSync.netIdentity;
        public ShipSocket[] Sockets => _sockets;
        internal ShipSocketsSync SocketStates => _socketStates;
        public IShipDeckGeometry DeckGeometry => _deckGeometry;
        internal IShipModules Modules => _modules;
        internal IShipHullStat Hull => _hull;
        internal IShipRiders Riders => _riders;
        internal IShipDeckRideRule RideRule => _rideRule;
        internal IShipSeats Seats => _seats;
        internal IShipFlightControl FlightControl => _flightControl;
        internal IShipDeckCargo Cargo => _cargo;
        internal ShipPoseSync PoseSync => _poseSync;
        internal ShipHullVolume HullVolume => _hullVolume;
        internal ShipImpactSync ImpactSync => _impactSync;
        internal ShipConditionSync ConditionSync => _conditionSync;
        public IReadOnlyList<ShipHullDamagePoint> DamagePoints => _damagePoints;
        internal ShipToolRack ToolRack => _toolRack;
        internal IShipKnockback Knockback => _flightControl;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IShipRidersDebug RidersDebug => _riders;
        // Public for the loop tests: their pilot faces the ship to the destination, as a crew does by radar.
        public IShipFlightControlDebug FlightControlDebug => _flightControl;
        internal IShipDeckCargoDebug CargoDebug => _cargo;
#endif

        public bool CanLaunch {
            get {
                if (_flying || _sockets == null)
                    return false;

                if (_run != null && (_run.Phase != ShipRunPhase.Build || _run.LaunchLocked))
                    return false;

                bool anyRequired = false;
                for (int i = 0; i < _sockets.Length; i++) {
                    ShipSocket socket = _sockets[i];
                    if (socket == null || socket.RequiredForLaunch == false)
                        continue;

                    anyRequired = true;
                    if (socket.IsOccupied == false)
                        return false;
                }

                return anyRequired;
            }
        }

        [Inject]
        private void InjectDependencies(
            ShipFlightSettings settings,
            ShipAccumulativeStatsConfiguration statsConfiguration,
            IReadOnlyShipRunModel run,
            IShipRunService runService,
            CharacterMovableModel characterMovableModel,
            ShipRiderKeyModel shipRiderKeyModel) {
            _flightSettings = settings;
            _statsConfiguration = statsConfiguration;

            _run = run;
            _runService = runService;
            _characterMovableModel = characterMovableModel;
            _shipRiderKeyModel = shipRiderKeyModel;
        }

        private void Awake() {
            Assert.IsTrue(
                _sockets != null && _stats != null && _socketStates != null && _statsSync != null && _hullVolume != null && _impactSync != null
                && _conditionSync != null && _damagePoints != null && _toolRack != null,
                name + " needs its sockets, socket states, stat entity, stats sync, hull volume, impact sync, condition sync, damage points and tool rack wired.");
            _deckGeometry = new ShipDeckGeometry(transform, _deck, _deckColliders);
            ShipStatSheet statSheet = new ShipStatSheet(_stats);
            _modules = new ShipModules(_sockets, statSheet, _statsSync, _conditionSync, ReadStatsConfiguration);
            _hull = new ShipHullStat(statSheet, _modules);
            _rideRule = new ShipDeckRideRule(_deckGeometry, ReadFlightSettings);
            _riders = new ShipRiders(this, _rideRule, ReadCharacterMovables, ReadRiderKeys);
            _seats = new ShipSeats(this, _sockets, _riders);
            _steering = new ShipSteering(_sockets);
            _flightControl = new ShipFlightControl(ReadFlightSettings, _modules, _steering);
            _cargo = new ShipDeckCargo(transform, this, _poseSync);
            if (_poseSync != null)
                _poseSync.BindShip(this, transform);

            _modules.ApplyDefaultStats();
        }

        internal bool ServerRequestLaunch() {
            return NetworkServer.active && _runService != null && _runService.ServerTryLaunch(this);
        }

        internal void BeginTakeoff(
            Vector3 from,
            Vector3 hover,
            Quaternion heading,
            float takeoffSeconds,
            float dodgeRangeScale,
            ShipFlightMode mode) {
            if (NetworkServer.active == false || _flying)
                return;

            _flightControl.BeginTakeoff(
                from,
                hover,
                heading,
                takeoffSeconds,
                dodgeRangeScale,
                mode);
            SleepBody();
            _flying = true;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(true);

            _riders.BoardRidersOverDeck();
        }

        internal void BeginCruise() {
            if (NetworkServer.active == false || _flying == false)
                return;

            _flightControl.BeginCruise();
        }

        internal void BeginLanding(Vector3 padPoint, float landingSeconds, Vector3 faceDirection) {
            if (NetworkServer.active == false || _flying == false)
                return;

            _flightControl.BeginLanding(padPoint, landingSeconds, faceDirection, transform.rotation);
        }

        internal void ServerFinishFlight() {
            if (NetworkServer.active == false)
                return;

            _flightControl.Stop();
            _flying = false;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(false);

            WakeBody();
            _riders.ServerReleaseRiders();
        }

        internal void ServerResetForBuild(Vector3 berth, Quaternion rotation) {
            if (NetworkServer.active == false)
                return;

            if (_flying) {
                _flightControl.Stop();
                _flying = false;
                if (_poseSync != null)
                    _poseSync.ServerSetFlying(false);
            }

            // Seated riders stay bound at a station too: release them on every reset, or one seated at the helm keeps
            // hanging at the seat after the ship and its seats are reset.
            _riders.ServerReleaseRiders();
            _seats.ClearAllOccupants();
            _modules.ResetInstalledModules();
            _conditionSync.ServerClearOpenBreaches();
            // A run starts with this reset: full hull for the new max, and a ship with no stat modules publishes
            // nothing on uninstall, so the repair's publish covers that too.
            _hull.ServerRepair();
            _flightControl.UnlockControls();
            if (_lever != null)
                _lever.ServerReset();

            ApplyDisplayPose(berth, rotation);
            SleepBody();
            if (_poseSync != null)
                _poseSync.ServerSnap(berth, rotation);
        }

        internal void ServerSettleAfterLanding() {
            if (NetworkServer.active == false)
                return;

            _flightControl.Stop();
            _flightControl.UnlockControls();
            _riders.FollowRiders(0f);
            _flying = false;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(false);

            _seats.ServerUnseatRiders();

            if (_lever != null)
                _lever.ServerReset();

            SleepBody();
            if (_poseSync != null)
                _poseSync.ServerSnap(transform.position, transform.rotation);
        }

        internal void OnClientFlightChanged(bool flying) {
            if (NetworkServer.active)
                return;

            // Riders are released by the server (ShipRider.ServerRelease) after the landing snap and origin shift.
            _flying = flying;
            if (flying)
                _riders.BoardRidersOverDeck();
        }

        internal void ServerApplyWorldShift(Vector3 delta) {
            _flightControl.ShiftWorld(delta);
            Vector3 position = transform.position + delta;
            ApplyDisplayPose(position, transform.rotation);
            if (_poseSync == null)
                return;

            _poseSync.ServerRecordWorldShift(delta);

            // A snap ends the flight on clients (it doubles as the landing berth snap), so a shift in flight is sent
            // as a shift of the client's interpolation buffer instead.
            if (_flying)
                _poseSync.ServerShift(delta, position, transform.rotation);
            else
                _poseSync.ServerSnap(position, transform.rotation);
        }

        internal void ServerRemoveRider(ShipRider rider) {
            if (NetworkServer.active == false)
                return;

            _seats.ClearOccupant(rider);
            _riders.Remove(rider);
        }

        private void FixedUpdate() {
            _fixedSteps += 1;
        }

        internal int ConsumeFixedSteps() {
            int steps = _fixedSteps;
            _fixedSteps = 0;
            return steps;
        }

        private void LateUpdate() {
            // Before this frame's flight step: an item released this frame is still at the hand's pose for the ship's
            // pose of the last frame, so it goes back onto the deck exactly where it left the hand.
            if (NetworkServer.active)
                _cargo.ServerReleaseGrabbed();

            if (_flying && NetworkServer.active) {
                _flightControl.ServerStep(Time.deltaTime);
                if (_flightControl.IsActive)
                    ApplyDisplayPose(_flightControl.Position, _flightControl.Rotation);

                if (_poseSync != null)
                    _poseSync.ServerPublish(transform.position, transform.rotation, _flightControl.Velocity);

                if (_runService != null)
                    _runService.ServerTick();
            }
            else if (_flying && _poseSync != null) {
                _poseSync.ApplyInterpolated(_steering.IsSteeredByLocalPlayer());
            }

            _cargo.Follow(NetworkServer.active);
            _riders.FollowRiders(Time.deltaTime);
            if (_flying)
                _riders.CatchDeckRiders();
        }

        internal Vector3 ToShipLocal(Vector3 worldPosition) =>
            Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);

        internal void ApplyDisplayPose(Vector3 position, Quaternion rotation) {
            transform.SetPositionAndRotation(position, rotation);
            if (_body == null)
                return;

            _body.position = position;
            _body.rotation = rotation;
            _body.StopIfDynamic();
        }

        // The injected settings are read on use, so the parts do not depend on injection before Awake.
        private ShipFlightSettings ReadFlightSettings() =>
            _flightSettings;

        private ShipAccumulativeStatsConfiguration ReadStatsConfiguration() =>
            _statsConfiguration;

        private CharacterMovableModel ReadCharacterMovables() =>
            _characterMovableModel;

        private ShipRiderKeyModel ReadRiderKeys() =>
            _shipRiderKeyModel;

        private void SleepBody() {
            if (_body == null)
                return;

            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.None;
        }

        private void WakeBody() {
            if (_body == null)
                return;

            _body.interpolation = RigidbodyInterpolation.None;
        }
    }
}
