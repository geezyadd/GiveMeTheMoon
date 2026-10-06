using Mirror;
using UnityEngine;
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
        [SerializeField] private EngineCatalog _engines;
        [SerializeField] private ShipPoseSync _poseSync;
        [SerializeField] private ShipStatEntity _stats;

        private ShipFlightSettings _flightSettings;

        private readonly ShipDeckCargo _cargo = new ShipDeckCargo();
        private ShipDeckGeometry _deckGeometry;
        private ShipModules _modules;
        private ShipRiders _riders;
        private ShipSeats _seats;
        private ShipFlightControl _flightControl;
        private int _fixedSteps;
        private bool _flying;

        private ShipRunModel _run;
        private IShipRunService _runService;

        public ShipLaunchLever Lever => _lever;
        public Transform Destination => _destination;
        public bool IsFlying => _flying;
        internal BoxCollider RideVolume => _rideVolume;
        internal float StandUpSpeed => _flightSettings.StandUpSpeed;
        internal bool ConfinesRidersToDeck => _flightSettings.ConfineRidersToDeck;
        internal float ReboardDelaySeconds => _flightSettings.ReboardDelaySeconds;
        internal NetworkIdentity NetIdentity => _poseSync.netIdentity;
        public ShipSocket[] Sockets => _sockets;
        public ShipDeckGeometry DeckGeometry => _deckGeometry;
        internal ShipModules Modules => _modules;
        internal ShipRiders Riders => _riders;
        internal ShipSeats Seats => _seats;
        internal ShipFlightControl FlightControl => _flightControl;
        internal ShipDeckCargo Cargo => _cargo;
        internal ShipPoseSync PoseSync => _poseSync;

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
        private void Construct(
            EngineCatalog engines,
            ShipFlightSettings settings,
            ShipRunModel run,
            IShipRunService runService) {
            if (_engines == null)
                _engines = engines;

            _flightSettings = settings;

            _run = run;
            _runService = runService;
        }

        private void Awake() {
            _deckGeometry = new ShipDeckGeometry(transform, _deck, _deckColliders);
            _modules = new ShipModules(_sockets, _stats, _engines);
            _riders = new ShipRiders(this, _deckGeometry, _rideVolume, _flightSettings);
            _seats = new ShipSeats(this, _sockets, _riders);
            _flightControl = new ShipFlightControl(_flightSettings, _modules, _seats);
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
            Quaternion routeHeading,
            float takeoffSeconds,
            float dodgeRangeScale,
            ShipFlightMode mode) {
            if (NetworkServer.active == false || _flying)
                return;

            _flightControl.BeginTakeoff(
                from,
                hover,
                heading,
                routeHeading,
                takeoffSeconds,
                dodgeRangeScale,
                mode);
            SleepBody();
            _flying = true;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(true);

            _riders.CollectRidersInVolume();
            _riders.BindRiders();
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
            _modules.ClearInstalledModules();
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
            if (flying) {
                _riders.CollectRidersInVolume();
                _riders.BindRiders();
            }
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
                _cargo.ServerReleaseGrabbed(transform, this, _poseSync);

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
                _poseSync.ApplyInterpolated(_riders.HasOwnedHelmPilot());
            }

            _cargo.Follow(transform, this, NetworkServer.active, _poseSync);
            _riders.FollowRiders(Time.deltaTime);
            if (_flying)
                _riders.CatchDeckRiders();
        }

        internal void ApplyDisplayPose(Vector3 position, Quaternion rotation) {
            transform.SetPositionAndRotation(position, rotation);
            if (_body == null)
                return;

            _body.position = position;
            _body.rotation = rotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

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
