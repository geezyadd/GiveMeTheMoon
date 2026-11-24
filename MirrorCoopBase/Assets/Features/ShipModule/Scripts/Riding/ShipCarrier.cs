using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.ShipModule.Scripts.Riding {
    // The ship as a carrier: runs the flight step (the server's, or a client's pose from its buffer) first in each
    // physics step, before anything that rides it,
    // and keeps the ship's pose of the last step and of this one, so a body on the deck can be carried exactly as far
    // as the ship moved (ICarrier). It sits on the ship's root, where the deck colliders find it; a copy finds it from
    // the ship's network identity below.
    // The ship never jumps by flying: a step longer than a few metres is a placement (the world shift at a landing, a
    // snap to the berth) that moves the players itself, so it is no motion to carry anyone by.
    [DefaultExecutionOrder(-200)]
    public sealed class ShipCarrier : MonoBehaviour, ICarrier {
        [SerializeField] private ShipBase _ship;
        [Tooltip("How high above the deck surface a body in the air still rides with the ship, metres (a jump).")]
        [SerializeField, Min(0f)] private float _rideHeight = 6f;
        [Tooltip("How far past the deck's walk boxes a body in the air still rides with the ship, metres.")]
        [SerializeField, Min(0f)] private float _rideEdgeMargin = 0.3f;
        [Tooltip("A step longer than this, metres, is a placement (world shift, snap), not a move: nobody is carried by it.")]
        [SerializeField, Min(0.1f)] private float _maxStepMove = 5f;

        private bool _hasPose;

        public uint CarrierId => _ship.NetIdentity != null ? _ship.NetIdentity.netId : 0;
        public Pose PoseBefore { get; private set; }
        public Pose PoseNow { get; private set; }
        public Quaternion DrawnRotation => _ship.transform.rotation;

        private void FixedUpdate() {
            _ship.ServerFlightStep(Time.fixedDeltaTime);
            _ship.ClientFlightStep(Time.fixedDeltaTime);
            Pose now = _ship.TakeStepPose();
            bool placed = _hasPose && (now.position - PoseNow.position).sqrMagnitude > _maxStepMove * _maxStepMove;
            PoseBefore = _hasPose && placed == false ? PoseNow : now;
            PoseNow = now;
            _hasPose = true;
        }

        // The bodies on the deck stand where the last step carried them, so the point is read in that step's pose.
        public bool Holds(Vector3 worldPoint) {
            Vector3 local = CarrierMotion.ToLocalPoint(PoseBefore, worldPoint);
            IShipDeckGeometry deck = _ship.DeckGeometry;
            if (deck == null || deck.TryClosestDeckWalk(local, out _, out float dx, out float dz) == false)
                return false;

            if (dx * dx + dz * dz > _rideEdgeMargin * _rideEdgeMargin)
                return false;

            if (deck.TryGetDeckSurfaceY(local, out float surfaceY) == false)
                return false;

            return local.y >= surfaceY - _rideEdgeMargin && local.y <= surfaceY + _rideHeight;
        }
    }
}
