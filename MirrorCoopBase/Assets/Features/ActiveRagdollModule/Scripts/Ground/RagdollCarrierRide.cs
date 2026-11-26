using System;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Lets a ragdoll ride a moving carrier (a platform, the ship's deck). The body boards what its ground sensor stands
    // on and stays on it in the air while above it, so a jump keeps up. The carry goes through every limb's velocity,
    // so physics moves the body and interpolation stays smooth: at the end of each physics step
    // (RagdollCarrierRideStepEnd) the carrier's exact move of each limb's point, over the step time, is added to the
    // limb's velocity and the relative velocity is turned with the carrier; at the start of the next step (here,
    // before any force) that addition is taken off again. In between velocities are relative to the carrier, so
    // standing, walking and jumping work as on still ground. Boarding takes the carrier's speed off the limbs;
    // leaving gives it back, so the body flies on with the carrier's speed. A network copy of a body rides the carrier
    // its owner's state names instead of what it stands on, so both agree on what the body moves with.
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(RagdollBody), typeof(RagdollGroundSensor))]
    public sealed class RagdollCarrierRide : MonoBehaviour, ICarrierRider {
        // A carrier that moves farther than this in one step jumped; the body is moved with it at once.
        private const float MaxCarryPerStep = 5f;

        private RagdollBody _body;
        private RagdollGroundSensor _ground;
        private Vector3[] _addedCarry = Array.Empty<Vector3>();
        private bool _isCopy;
        private ICarrier _copyCarrier;
        private ICarrier _caughtBy;

        // The carrier the body moves with now, or null on still ground.
        public ICarrier Carrier { get; private set; }

        public void RideAsCopy(bool isCopy) {
            _isCopy = isCopy;
            if (isCopy == false)
                _copyCarrier = null;
        }

        public void SetCopyCarrier(ICarrier carrier) =>
            _copyCarrier = carrier;

        // The body was put somewhere else: the carrier is forgotten without giving its speed back.
        public void DropCarrier() {
            Carrier = null;
            _copyCarrier = null;
            Array.Clear(_addedCarry, 0, _addedCarry.Length);
        }

        public void CatchInAir(ICarrier carrier) =>
            _caughtBy = carrier;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            _ground = GetComponent<RagdollGroundSensor>();
        }

        private void OnDisable() {
            Carrier = null;
            Array.Clear(_addedCarry, 0, _addedCarry.Length);
        }

        private void FixedUpdate() {
            EnsureCarrySlots();
            TakeCarryOff();
            if (TryBoardCatch())
                return;

            ICarrier wanted = PickCarrier();
            if (wanted == Carrier)
                return;

            if (Carrier != null)
                ShiftVelocities(CarrierPointVelocities, 1f);
            Carrier = wanted;
            if (Carrier != null)
                ShiftVelocities(CarrierPointVelocities, -1f);
        }

        // At the end of the physics step: the carrier's move of each limb for this step, as velocity.
        internal void AddCarry() {
            if (Carrier == null)
                return;

            Pose before = Carrier.PoseBefore;
            Pose now = Carrier.PoseNow;
            Quaternion turn = CarrierMotion.TurnOf(before, now);
            if (TryJumpWithCarrier(before, now, turn))
                return;

            for (int i = 0; i < _body.Limbs.Count; i++) {
                Rigidbody rigidbody = _body.Limbs[i].Rigidbody;
                if (rigidbody.isKinematic)
                    continue;

                Vector3 position = rigidbody.position;
                Vector3 move = CarrierMotion.CarryPoint(before, now, position) - position;
                _addedCarry[i] = move / Time.fixedDeltaTime;
                rigidbody.linearVelocity = turn * rigidbody.linearVelocity + _addedCarry[i];
            }
        }

        // A carrier that jumped (a long stall closed at once) takes the body along at once: carried by velocity, it
        // would sweep through everything on the way.
        private bool TryJumpWithCarrier(Pose before, Pose now, Quaternion turn) {
            Vector3 root = _body.Root.Rigidbody.position;
            Vector3 move = CarrierMotion.CarryPoint(before, now, root) - root;
            if (move.sqrMagnitude <= MaxCarryPerStep * MaxCarryPerStep)
                return false;

            _body.MoveAllBy(move);
            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.Rigidbody.isKinematic == false)
                    limb.Rigidbody.linearVelocity = turn * limb.Rigidbody.linearVelocity;
            return true;
        }

        // A catch in the air: the body rides the carrier from this step at rest on it (relative velocity zero, the spin
        // kept). A copy rides what its owner's packets name, so it is never caught.
        private bool TryBoardCatch() {
            ICarrier carrier = _caughtBy;
            _caughtBy = null;
            if (carrier == null || _isCopy)
                return false;

            Carrier = carrier;
            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.Rigidbody.isKinematic == false)
                    limb.Rigidbody.linearVelocity = Vector3.zero;
            return true;
        }

        // What the body stands on, or the carrier it jumped from while it is still above it.
        private ICarrier PickCarrier() {
            if (_isCopy)
                return _copyCarrier;

            if (_ground.Carrier != null)
                return _ground.Carrier;

            RagdollLimb root = _body.Root;
            return Carrier != null && root != null && Carrier.Holds(root.Rigidbody.position) ? Carrier : null;
        }

        private void TakeCarryOff() {
            for (int i = 0; i < _addedCarry.Length; i++) {
                Rigidbody rigidbody = _body.Limbs[i].Rigidbody;
                if (rigidbody.isKinematic == false)
                    rigidbody.linearVelocity -= _addedCarry[i];
                _addedCarry[i] = Vector3.zero;
            }
        }

        private void ShiftVelocities(Func<Rigidbody, Vector3> velocityOf, float sign) {
            foreach (RagdollLimb limb in _body.Limbs)
                if (limb.Rigidbody.isKinematic == false)
                    limb.Rigidbody.linearVelocity += sign * velocityOf(limb.Rigidbody);
        }

        private Vector3 CarrierPointVelocities(Rigidbody rigidbody) =>
            CarrierMotion.PointVelocity(Carrier.PoseBefore, Carrier.PoseNow, rigidbody.position, Time.fixedDeltaTime);

        private void EnsureCarrySlots() {
            if (_addedCarry.Length != _body.Limbs.Count)
                _addedCarry = new Vector3[_body.Limbs.Count];
        }
    }
}
