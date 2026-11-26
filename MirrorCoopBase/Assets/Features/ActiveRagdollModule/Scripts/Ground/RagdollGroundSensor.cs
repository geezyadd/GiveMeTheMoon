using System.Collections.Generic;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Decides each physics step whether the body stands and on what. Ground samples come from the support probe
    // cast down from the root limb and from every limb's contacts during the last simulation; the highest sample
    // a body can stand on wins. With no such sample, a body wedged between surfaces whose normals point up on
    // average still counts as standing.
    [DefaultExecutionOrder(-85)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class RagdollGroundSensor : MonoBehaviour {
        private readonly SupportProbe _probe = new();
        private readonly List<GroundSample> _contactSamples = new();
        private RagdollBody _body;
        private Vector3 _wedgedNormalSum;
        private GroundSample _highestWedgedContact;
        private bool _hasWedgedContact;
        private float _standPauseLeft;

        public bool IsGrounded { get; private set; }
        public Vector3 GroundPoint { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        // The moving carrier the body stands on (ship deck, platform), or null.
        public ICarrier Carrier { get; private set; }
        public float SecondsSinceGrounded { get; private set; }
        // Off for a moment after a jump, so the hover does not cancel it.
        public bool CanStand => _standPauseLeft <= 0f;

        // The body does not stand for this long (a jump takes off).
        public void PauseStanding(float seconds) {
            _standPauseLeft = Mathf.Max(_standPauseLeft, seconds);
            IsGrounded = false;
        }

        private void Awake() {
            _body = GetComponent<RagdollBody>();
        }

        private void OnEnable() {
            foreach (RagdollLimb limb in _body.Limbs)
                limb.Touching += OnLimbTouching;
        }

        private void OnDisable() {
            foreach (RagdollLimb limb in _body.Limbs)
                limb.Touching -= OnLimbTouching;
        }

        private void FixedUpdate() {
            if (_standPauseLeft > 0f)
                _standPauseLeft -= Time.fixedDeltaTime;

            RagdollLimb root = _body.Root;
            if (root != null && root.HasTarget && CanStand && TryFindGround(root, out GroundSample ground))
                Stand(ground);
            else
                Fly();

            _contactSamples.Clear();
            _wedgedNormalSum = Vector3.zero;
            _hasWedgedContact = false;
        }

        private bool TryFindGround(RagdollLimb root, out GroundSample ground) {
            bool found = TryProbe(root, out ground);
            foreach (GroundSample sample in _contactSamples) {
                if (found == false || sample.Point.y > ground.Point.y) {
                    ground = sample;
                    found = true;
                }
            }

            if (found || _hasWedgedContact == false || IsStandable(_wedgedNormalSum.normalized) == false)
                return found;

            ground = new GroundSample(_highestWedgedContact.Point, _wedgedNormalSum.normalized,
                _highestWedgedContact.Collider);
            return true;
        }

        private bool TryProbe(RagdollLimb root, out GroundSample ground) {
            RagdollStandSettings settings = _body.Profile.Stand;
            float length = root.Target.Height + settings.ProbeExtraLength;
            ground = default;
            if (_probe.TryCast(root.Rigidbody.position, Vector3.down, settings.ProbeRadius, length,
                    settings.GroundMask, transform, null, out RaycastHit hit) == false)
                return false;

            if (IsStandable(hit.normal) == false || IsSupportBody(hit.rigidbody) == false)
                return false;

            ground = new GroundSample(hit.point, hit.normal, hit.collider);
            return true;
        }

        private void OnLimbTouching(RagdollLimb limb, Collision collision) {
            if (collision.transform.IsChildOf(transform) || IsSupportBody(collision.rigidbody) == false)
                return;

            for (int i = 0; i < collision.contactCount; i++) {
                ContactPoint contact = collision.GetContact(i);
                GroundSample sample = new(contact.point, contact.normal, collision.collider);
                if (IsStandable(contact.normal))
                    _contactSamples.Add(sample);
                else if (contact.normal.y > 0f)
                    AddWedgedContact(sample);
            }
        }

        private void AddWedgedContact(GroundSample sample) {
            _wedgedNormalSum += sample.Normal;
            if (_hasWedgedContact == false || sample.Point.y > _highestWedgedContact.Point.y)
                _highestWedgedContact = sample;
            _hasWedgedContact = true;
        }

        private void Stand(GroundSample ground) {
            IsGrounded = true;
            GroundPoint = ground.Point;
            GroundNormal = ground.Normal;
            Carrier = _probe.CarrierOf(ground.Collider);
            SecondsSinceGrounded = 0f;
        }

        private void Fly() {
            IsGrounded = false;
            GroundNormal = Vector3.up;
            Carrier = null;
            SecondsSinceGrounded += Time.fixedDeltaTime;
        }

        private bool IsStandable(Vector3 normal) => Vector3.Angle(Vector3.up, normal) <= _body.Profile.Stand.MaxStandSlope;

        // Still ground, kinematic bodies and heavy bodies hold the body up; light loose objects do not.
        private bool IsSupportBody(Rigidbody support) =>
            support == null || support.isKinematic || support.mass >= _body.Profile.Stand.MinSupportMass;

        private void OnDrawGizmosSelected() {
            if (Application.isPlaying == false)
                return;

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(GroundPoint, 0.08f);
            Gizmos.DrawLine(GroundPoint, GroundPoint + GroundNormal * 0.4f);
        }
    }
}
