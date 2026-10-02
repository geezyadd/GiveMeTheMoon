using Mirror;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The GameObject stays active while pooled: hiding is done with the renderers, so Mirror keeps spawning and
    // syncing the rock normally and every peer can show or hide it on its own render clock.
    public sealed class CruiseRock : NetworkBehaviour {
        private const float MIN_LIFETIME = 0.2f;

        [SerializeField] private Renderer[] _renderers;

        [SyncVar]
        private CruiseRockFlight _flight;

        [SyncVar]
        private NetworkIdentity _clockIdentity;

        private ShipPoseSync _clock;
        private bool _visible = true;

        // Called before NetworkServer.Spawn, so the binding is part of the spawn payload.
        internal void ServerBind(ShipPoseSync clock) {
            _clock = clock;
            _clockIdentity = clock.netIdentity;
        }

        [Server]
        internal void ServerLaunch(Vector3 start, Quaternion rotation, Vector3 velocity, float lifetime) {
            double now = NetworkTime.time;
            _flight = new CruiseRockFlight {
                Start = start,
                Rotation = rotation,
                Velocity = velocity,
                LaunchTime = now,
                EndTime = now + Mathf.Max(MIN_LIFETIME, lifetime),
                ShiftEpoch = _clock.ShiftEpoch
            };
            ApplyFlight();
        }

        internal bool ServerExpired() =>
            isServer && NetworkTime.time >= _flight.EndTime;

        [Server]
        internal void ServerPark() {
            double now = NetworkTime.time;
            if (_flight.EndTime <= now)
                return;

            CruiseRockFlight flight = _flight;
            flight.EndTime = now;
            _flight = flight;
            ApplyFlight();
        }

        private void LateUpdate() =>
            ApplyFlight();

        private void ApplyFlight() {
            if (TryGetClock(out ShipPoseSync clock) == false) {
                SetVisible(false);
                return;
            }

            double time = clock.DisplayTime;
            bool visible = _flight.IsVisibleAt(time);
            SetVisible(visible);
            if (visible == false)
                return;

            Vector3 position = _flight.PositionAt(time) + clock.ShiftSince(_flight.ShiftEpoch);
            transform.SetPositionAndRotation(position, _flight.Rotation);
        }

        private bool TryGetClock(out ShipPoseSync clock) {
            if (_clock == null && _clockIdentity != null)
                _clockIdentity.TryGetComponent(out _clock);

            clock = _clock;
            return clock != null;
        }

        private void SetVisible(bool visible) {
            if (_visible == visible)
                return;

            _visible = visible;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].enabled = visible;
        }
    }
}
