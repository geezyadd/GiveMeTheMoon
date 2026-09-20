using System.Collections.Generic;
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
        [SerializeField] private EngineCatalog _engines;
        [SerializeField] private ShipPoseSync _poseSync;

        private ShipFlightSettings _flightSettings;

        private readonly List<ShipRider> _riders = new List<ShipRider>();
        private readonly List<ShipRider> _insideVolume = new List<ShipRider>();
        private readonly ShipFlight _flight = new ShipFlight();
        private float _helmSteer;
        private bool _flying;

        private ShipRunModel _run;
        private ShipRunService _runService;

        public ShipLaunchLever Lever => _lever;
        public Transform Destination => _destination;
        public bool IsFlying => _flying;
        internal ShipSocket[] Sockets => _sockets;

        internal bool ContainsDeckWalk(Vector3 localOffset, float inset) {
            GetDeckWalkLimits(inset, out float minX, out float maxX, out float minZ, out float maxZ);
            return localOffset.x >= minX && localOffset.x <= maxX
                && localOffset.z >= minZ && localOffset.z <= maxZ;
        }

        internal void ClampDeckWalk(ref Vector3 localOffset, float inset) {
            GetDeckWalkLimits(inset, out float minX, out float maxX, out float minZ, out float maxZ);
            localOffset.x = Mathf.Clamp(localOffset.x, minX, maxX);
            localOffset.z = Mathf.Clamp(localOffset.z, minZ, maxZ);
        }

        private void GetDeckWalkLimits(float inset, out float minX, out float maxX, out float minZ, out float maxZ) {
            inset = Mathf.Max(0f, inset);
            if (_deck == null) {
                minX = -2.2f + inset;
                maxX = 2.2f - inset;
                minZ = -3.8f + inset;
                maxZ = 3.8f - inset;
                return;
            }

            Vector3 scale = _deck.transform.localScale;
            Vector3 half = Vector3.Scale(_deck.size, scale) * 0.5f;
            Vector3 center = _deck.transform.localPosition + Vector3.Scale(_deck.center, scale);
            minX = center.x - half.x + inset;
            maxX = center.x + half.x - inset;
            minZ = center.z - half.z + inset;
            maxZ = center.z + half.z - inset;
            if (minX > maxX) {
                minX = center.x;
                maxX = center.x;
            }

            if (minZ > maxZ) {
                minZ = center.z;
                maxZ = center.z;
            }
        }
        internal bool IsTakeoffComplete => _flight.IsTakeoffComplete;
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
            ShipRunService runService) {
            if (_engines == null)
                _engines = engines;

            _flightSettings = settings;

            _run = run;
            _runService = runService;
        }

        private void Awake() {
            if (_poseSync != null)
                _poseSync.BindShip(this, transform);
        }

        internal void BindDestination(Transform destination) {
            _destination = destination;
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
            UnregisterRider(rider);
        }

        internal void UnregisterRider(ShipRider rider) {
            if (_flying)
                return;

            if (rider != null)
                rider.ReleaseFromPlatform();

            _riders.Remove(rider);
        }

        internal void NotifyRiderLeft(ShipRider rider) {
            _riders.Remove(rider);
            ClearOccupant(rider);
        }

        internal bool ServerRequestLaunch() {
            return NetworkServer.active && _runService != null && _runService.ServerTryLaunch(this);
        }

        internal void BeginTakeoff(
            Vector3 from,
            Vector3 hover,
            Quaternion heading,
            float takeoffSeconds,
            float dodgeRangeScale) {
            if (NetworkServer.active == false || _flying)
                return;

            _flight.BeginTakeoff(
                from,
                hover,
                heading,
                _flightSettings,
                takeoffSeconds,
                dodgeRangeScale);
            _helmSteer = 0f;
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

            _flight.BeginCruise();
        }

        internal void BeginLanding(Vector3 padPoint, float landingSeconds) {
            if (NetworkServer.active == false || _flying == false)
                return;

            _flight.SetManualHeading(false);
            _flight.SetSteer(0f);
            _flight.BeginLanding(padPoint, landingSeconds);
        }

        internal void ServerFinishFlight() {
            if (NetworkServer.active == false)
                return;

            _flight.Stop();
            _flying = false;
            if (_poseSync != null)
                _poseSync.ServerSetFlying(false);

            WakeBody();
            ReleaseRiders();
        }

        internal void ServerResetForBuild(Vector3 berth, Quaternion rotation) {
            if (NetworkServer.active == false)
                return;

            ClearAllOccupants();
            ClearInstalledModules();
            if (_lever != null)
                _lever.ServerReset();

            ApplyDisplayPose(berth, rotation);
            SleepBody();
            if (_poseSync != null)
                _poseSync.ServerSnap(berth, rotation);
        }

        internal void OnClientFlightChanged(bool flying) {
            if (NetworkServer.active)
                return;

            _flying = flying;
            if (flying) {
                CollectRidersInVolume();
                BindRiders();
                return;
            }

            ReleaseRiders();
        }

        internal void ServerSetSteer(uint riderNetId, float lateral) {
            if (NetworkServer.active == false)
                return;

            if (IsHelmOccupant(riderNetId) == false) {
                _helmSteer = 0f;
                return;
            }

            _helmSteer = lateral;
        }

        internal bool ServerTrySit(ShipRider rider, ShipSocket socket) {
            if (NetworkServer.active == false || rider == null || socket == null)
                return false;

            if (socket.ServerTrySit(rider.netId) == false)
                return false;

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(this);
            bool helm = socket.Seat != null && socket.Seat.Role == ShipSeatRole.Helm;
            rider.ServerLockSeat(socket.ResolveSitLocalOffset(transform), helm);
            return true;
        }

        internal void ServerStand(ShipRider rider) {
            if (NetworkServer.active == false || rider == null)
                return;

            ClearOccupant(rider);
            rider.ServerUnlockSeat();
        }

        private void LateUpdate() {
            if (_flying && NetworkServer.active) {
                _flight.SetManualHeading(_flight.AllowsSteer && HasControlModule());
                _flight.SetSteer(_flight.AllowsSteer && HasHelmPilot() ? ReadHelmSteer() : 0f);
                SimulateFlight(Time.deltaTime);
                if (_flight.IsActive)
                    ApplyDisplayPose(_flight.Position, _flight.Rotation);

                if (_poseSync != null)
                    _poseSync.ServerPublish(transform.position, transform.rotation, _flight.Velocity);
            }
            else if (_flying && _poseSync != null) {
                _poseSync.ApplyInterpolated(HasOwnedHelmPilot());
            }

            FollowRiders(Time.deltaTime);
            if (_flying)
                CatchLandingRiders();
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

            return _helmSteer;
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

        private bool HasControlModule() {
            if (_sockets == null)
                return false;

            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket != null && socket.AcceptedType == ShipModuleType.Control && socket.IsOccupied)
                    return true;
            }

            return false;
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

        private void ClearOccupant(ShipRider rider) {
            if (_sockets == null || rider == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerStand(rider.netId);
            }
        }

        private void ClearAllOccupants() {
            if (_sockets == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearOccupant();
            }
        }

        private void ClearInstalledModules() {
            if (_sockets == null)
                return;

            for (int i = 0; i < _sockets.Length; i++) {
                if (_sockets[i] != null)
                    _sockets[i].ServerClearInstall();
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

        private void CatchLandingRiders() {
            for (int i = 0; i < _insideVolume.Count; i++)
                TryBindRider(_insideVolume[i]);
        }

        private void TryBindRider(ShipRider rider) {
            if (_flying == false || rider == null || rider.IsRiding)
                return;

            if (rider.WantsLand(this) == false)
                return;

            if (_riders.Contains(rider) == false)
                _riders.Add(rider);

            rider.BindToPlatform(this);
        }

        private void BindRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].BindToPlatform(this);
            }
        }

        private void FollowRiders(float dt) {
            for (int i = _riders.Count - 1; i >= 0; i--) {
                if (_riders[i] != null)
                    _riders[i].Follow(dt);
            }
        }

        private void ReleaseRiders() {
            for (int i = 0; i < _riders.Count; i++) {
                if (_riders[i] != null)
                    _riders[i].ReleaseFromPlatform();
            }
        }

        private static readonly Collider[] RiderScratch = new Collider[16];
    }
}
