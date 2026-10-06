using System.Collections.Generic;
using Features.GrabModule.Scripts;
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

        private readonly List<ShipRider> _riders = new List<ShipRider>();
        private readonly List<ShipRider> _insideVolume = new List<ShipRider>();
        private readonly ShipFlight _flight = new ShipFlight();
        private readonly ShipDeckCargo _cargo = new ShipDeckCargo();
        private readonly HelmSteer _helmSteer = new HelmSteer();
        private ShipDeckGeometry _deckGeometry;
        private ShipModules _modules;
        private bool _debugSteerActive;
        private float _debugSteer;
        private int _fixedSteps;
        private bool _flying;
        private bool _controlsLocked;

        private ShipRunModel _run;
        private IShipRunService _runService;

        public ShipLaunchLever Lever => _lever;
        public Transform Destination => _destination;
        public bool IsFlying => _flying;
        internal BoxCollider RideVolume => _rideVolume;
        internal ShipFlightMode ActiveFlightMode => _flight.Mode;
        internal float StandUpSpeed => _flightSettings.StandUpSpeed;
        internal bool ConfinesRidersToDeck => _flightSettings.ConfineRidersToDeck;
        internal float ReboardDelaySeconds => _flightSettings.ReboardDelaySeconds;
        internal NetworkIdentity NetIdentity => _poseSync.netIdentity;
        public ShipSocket[] Sockets => _sockets;
        public ShipDeckGeometry DeckGeometry => _deckGeometry;
        internal ShipModules Modules => _modules;

        internal bool IsTakeoffComplete => _flight.IsTakeoffComplete;
        internal ShipPoseSync PoseSync => _poseSync;
        internal bool HasLanded => _flight.HasLanded;

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
            if (_poseSync != null)
                _poseSync.BindShip(this, transform);

            _modules.ApplyDefaultStats();
        }

        internal void DebugBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            if (_insideVolume.Contains(rider) == false)
                _insideVolume.Add(rider);

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(this);
        }

        internal void RegisterRider(ShipRider rider) {
            if (rider == null)
                return;

            if (_insideVolume.Contains(rider) == false)
                _insideVolume.Add(rider);

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            TryBindRider(rider);
        }

        internal void SetVolumeOverlap(ShipRider rider, bool inside) {
            if (rider == null)
                return;

            if (inside) {
                if (_insideVolume.Contains(rider) == false)
                    _insideVolume.Add(rider);

                if (_flying)
                    TryBindRider(rider);

                return;
            }

            _insideVolume.Remove(rider);
            // Binding turns the rider's collisions off, which raises this exit; a bound rider is released by its own path.
            if (rider.IsRiding)
                return;

            UnregisterRider(rider);
        }

        internal void UnregisterRider(ShipRider rider) {
            if (_flying)
                return;

            if (rider != null && NetworkServer.active)
                rider.ServerRelease();

            _riders.Remove(rider);
        }

        internal void TrackRider(ShipRider rider) {
            if (_riders.Contains(rider) == false)
                _riders.Add(rider);
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

            _flight.BeginTakeoff(
                from,
                hover,
                heading,
                routeHeading,
                _flightSettings,
                takeoffSeconds,
                dodgeRangeScale,
                mode);
            _helmSteer.Clear();
            _controlsLocked = false;
            SleepBody();
            _flying = true;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(true);

            CollectRidersInVolume();
            BindRiders();
        }

        internal void BeginCruise() {
            if (NetworkServer.active == false || _flying == false)
                return;

            _controlsLocked = false;
            _flight.BeginCruise();
        }

        internal void BeginLanding(Vector3 padPoint, float landingSeconds, Vector3 faceDirection) {
            if (NetworkServer.active == false || _flying == false)
                return;

            _controlsLocked = true;
            _flight.SetManualHeading(false);
            _flight.SetSteer(0f);
            Vector3 flat = faceDirection;
            flat.y = 0f;
            Quaternion heading = flat.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(flat.normalized, Vector3.up)
                : transform.rotation;
            _flight.BeginLanding(padPoint, landingSeconds, heading);
        }

        internal void ServerLockFlight() {
            _controlsLocked = true;
            _flight.SetManualHeading(false);
            _flight.SetSteer(0f);
        }

        internal void ServerFinishFlight() {
            if (NetworkServer.active == false)
                return;

            _flight.Stop();
            _flying = false;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(false);

            WakeBody();
            ServerReleaseRiders();
        }

        internal void ServerResetForBuild(Vector3 berth, Quaternion rotation) {
            if (NetworkServer.active == false)
                return;

            if (_flying) {
                _flight.Stop();
                _flying = false;
                if (_poseSync != null)
                    _poseSync.ServerSetFlying(false);
            }

            // Seated riders stay bound at a station too: release them on every reset, or one seated at the helm keeps
            // hanging at the seat after the ship and its seats are reset.
            ServerReleaseRiders();
            ClearAllOccupants();
            _modules.ClearInstalledModules();
            _controlsLocked = false;
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

            _flight.Stop();
            _controlsLocked = false;
            FollowRiders(0f);
            _flying = false;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(false);

            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                if (rider != null)
                    ServerUnseat(rider);
            }

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
                CollectRidersInVolume();
                BindRiders();
            }
        }

        internal void SetDebugSteer(bool active, float steer) {
            _debugSteerActive = active;
            _debugSteer = Mathf.Clamp(steer, -1f, 1f);
        }

        internal void DebugFace(Vector3 worldForward) {
            _flight.DebugFace(worldForward);
        }

        internal void ServerApplyWorldShift(Vector3 delta) {
            _flight.ShiftWorld(delta);
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

        internal int DeckCargoCount => _cargo.AttachedCount;

        internal void DebugAttachDeckItem(Grabbable grabbable) {
            _cargo.DebugAttach(grabbable, transform, _poseSync);
        }

        internal bool TryMeasureDeckCargo(out Vector3 localPosition, out float drift) {
            return _cargo.TryMeasure(transform, out localPosition, out drift);
        }

        internal void ClientAttachDeckItem(uint netId, Vector3 localPosition, Quaternion localRotation) {
            _cargo.ClientAttach(netId, localPosition, localRotation, transform);
        }

        internal void ClientDetachDeckItem(uint netId) {
            _cargo.ClientDetach(netId, transform);
        }

        internal void ClientClearDeckCargo() {
            _cargo.DetachAll(transform, null, false);
        }

        internal void ServerSetSteer(uint riderNetId, float lateral) {
            if (NetworkServer.active == false)
                return;

            if (IsHelmOccupant(riderNetId) == false)
                return;

            _helmSteer.Set(riderNetId, lateral);
        }

        internal bool ServerTrySit(ShipRider rider, ShipSocket socket) {
            if (NetworkServer.active == false || rider == null || socket == null)
                return false;

            if (socket.CanSeat(rider.netId) == false)
                return false;

            // A rider holds one seat: sitting down elsewhere frees the previous seat (and the helm with its steer).
            ClearOccupant(rider);
            if (socket.ServerTrySit(rider.netId) == false)
                return false;

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            Vector3 seatOffset = socket.ResolveSitLocalOffset(transform);
            rider.BindToSeat(this, seatOffset);
            bool helm = socket.Seat != null && socket.Seat.Role == ShipSeatRole.Helm;
            rider.ServerLockSeat(seatOffset, helm, NetIdentity);
            return true;
        }

        internal void ClientSyncSeat(ShipSocket socket, uint previousOccupant, uint occupant) {
            if (NetworkServer.active)
                return;

            if (TryGetOwnedRider(occupant, out ShipRider seated)) {
                if (_riders.Contains(seated) == false)
                    _riders.Add(seated);

                seated.BindToSeat(this, socket.ResolveSitLocalOffset(transform));
                return;
            }

            // A rider who switched seats is still seated on the other socket; only standing up releases it.
            if (_flying == false && IsSeatedOnShip(previousOccupant) == false && TryGetOwnedRider(previousOccupant, out ShipRider stood))
                stood.ReleaseFromPlatform();
        }

        internal void ServerRemoveRider(ShipRider rider) {
            if (NetworkServer.active == false)
                return;

            ClearOccupant(rider);
            _riders.Remove(rider);
            _insideVolume.Remove(rider);
        }

        internal void ServerStand(ShipRider rider) {
            if (NetworkServer.active == false || rider == null)
                return;

            ServerUnseat(rider);
            // At a station the deck is a static platform: the rider walks on it under physics.
            if (_flying == false)
                rider.ServerRelease();
        }

        private void ServerUnseat(ShipRider rider) {
            ClearOccupant(rider);
            rider.ServerUnlockSeat();
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
                bool canSteer = _controlsLocked == false && _flight.AllowsSteer;
                bool manual = canSteer && (_modules.HasControlModule() || _debugSteerActive);
                _flight.SetManualHeading(manual);
                float steer = _debugSteerActive
                    ? _debugSteer
                    : canSteer && HasHelmPilot() ? ReadHelmSteer() : 0f;
                _flight.SetSteer(steer);
                PushTravelSpeed();
                SimulateFlight(Time.deltaTime);
                if (_flight.IsActive)
                    ApplyDisplayPose(_flight.Position, _flight.Rotation);

                if (_poseSync != null)
                    _poseSync.ServerPublish(transform.position, transform.rotation, _flight.Velocity);

                if (_runService != null)
                    _runService.ServerTick();
            }
            else if (_flying && _poseSync != null) {
                _poseSync.ApplyInterpolated(HasOwnedHelmPilot());
            }

            _cargo.Follow(transform, this, NetworkServer.active, _poseSync);
            FollowRiders(Time.deltaTime);
            if (_flying)
                CatchDeckRiders();
        }

        private void PushTravelSpeed() {
            if (_flight.Mode != ShipFlightMode.TravelInSpace || _flightSettings == null)
                return;

            float stat = Mathf.Max(ShipTransit.MinSpeed, _modules.GetStatFull(ShipStatType.FlightSpeed));
            _flight.SetTravelSpeed(_flightSettings.CruiseSpeed * stat);
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

        private void SimulateFlight(float dt) {
            const float step = 1f / 60f;
            float left = dt;
            while (left > 0f) {
                float slice = Mathf.Min(step, left);
                _flight.Simulate(slice);
                left -= slice;
            }
        }

        private bool HasOwnedHelmPilot() {
            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                if (rider != null && rider.isOwned && rider.IsHelmSeat)
                    return true;
            }

            return false;
        }

        private float ReadHelmSteer() {
            uint occupant = HelmOccupantNetId();
            if (occupant == 0)
                return 0f;

            for (int i = 0; i < _riders.Count; i++) {
                ShipRider rider = _riders[i];
                if (rider == null || rider.netId != occupant)
                    continue;

                if (rider.isOwned)
                    return rider.CurrentSteer;
            }

            return _helmSteer.Read(occupant);
        }

        private uint HelmOccupantNetId() {
            if (_sockets == null)
                return 0;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.HasHelmPilot)
                    return socket.OccupantNetId;
            }

            return 0;
        }

        private bool HasHelmPilot() {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null && _sockets[i].HasHelmPilot)
                    return true;
            }

            return false;
        }

        private bool IsHelmOccupant(uint riderNetId) {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.HasHelmPilot && socket.OccupantNetId == riderNetId)
                    return true;
            }

            return false;
        }

        private bool IsSeatedOnShip(uint riderNetId) {
            if (_sockets == null || riderNetId == 0)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null && _sockets[i].OccupantNetId == riderNetId)
                    return true;
            }

            return false;
        }

        private void ClearOccupant(ShipRider rider) {
            if (_sockets == null || rider == null)
                return;

            if (IsHelmOccupant(rider.netId))
                _helmSteer.Clear();

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerStand(rider.netId);
            }
        }

        private static bool TryGetOwnedRider(uint netId, out ShipRider rider) {
            rider = null;
            if (netId == 0 || NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity) == false)
                return false;

            return identity.isOwned && identity.TryGetComponent(out rider);
        }

        private void ClearAllOccupants() {
            _helmSteer.Clear();
            if (_sockets == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearOccupant();
            }
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

        private void CollectRidersInVolume() {
            if (_rideVolume != null) {
                Vector3 center = _rideVolume.transform.TransformPoint(_rideVolume.center);
                Vector3 halfExtents = Vector3.Scale(_rideVolume.size, _rideVolume.transform.lossyScale) * 0.5f;
                int hits = Physics.OverlapBoxNonAlloc(
                    center,
                    halfExtents,
                    RiderScratch,
                    _rideVolume.transform.rotation,
                    ~0,
                    QueryTriggerInteraction.Collide);

                for (int i = 0; i < hits; i++) {
                    Collider hit = RiderScratch[i];
                    if (hit == null)
                        continue;

                    RegisterRider(hit.GetComponentInParent<ShipRider>());
                }
            }

            ShipRider[] riders = FindObjectsByType<ShipRider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Vector3 origin = transform.position;
            const float radiusSq = 64f;
            for (int i = 0; i < riders.Length; i++) {
                ShipRider rider = riders[i];
                if (rider == null)
                    continue;

                if ((rider.transform.position - origin).sqrMagnitude > radiusSq)
                    continue;

                RegisterRider(rider);
            }
        }

        private void CatchDeckRiders() {
            for (int i = 0; i < _insideVolume.Count; i++)
                TryBindRider(_insideVolume[i]);
        }

        private void TryBindRider(ShipRider rider) {
            if (rider == null || rider.IsRiding)
                return;

            if (rider.WantsLand(this) == false)
                return;

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(this);
        }

        // Each peer boards only its own player; the others follow their owner's synced ride (ShipRider hooks).
        private void BindRiders() {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                ShipRider rider = _riders[i];
                if (rider == null || rider.isOwned == false)
                    continue;

                if (IsAboveDeck(rider.transform.position) == false) {
                    _riders.RemoveAt(i);
                    continue;
                }

                rider.BindToPlatform(this);
            }
        }

        private bool IsAboveDeck(Vector3 worldPosition) {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            if (_deckGeometry.TryClosestDeckWalk(local, out _, out float dx, out float dz) == false)
                return false;

            float edge = _flightSettings.BoardingEdgeTolerance;
            if (dx * dx + dz * dz > edge * edge)
                return false;

            if (_deckGeometry.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            float height = local.y - surfaceY;
            return height >= _flightSettings.BoardingMinHeight && height <= _flightSettings.BoardingMaxHeight;
        }

        private void FollowRiders(float dt) {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].Follow(dt);
            }
        }

        internal void ServerReleaseRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].ServerRelease();
            }
        }

        private static readonly Collider[] RiderScratch = new Collider[16];
    }
}
