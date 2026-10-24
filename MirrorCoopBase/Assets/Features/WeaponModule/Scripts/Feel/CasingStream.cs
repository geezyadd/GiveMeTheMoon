using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // Spent casings from a fixed pool: thrown out of the eject port, tumbling, falling, bouncing on the floor and
    // piling up there, then shrinking away. They move in the floor's frame, so on a ship they ride along with it, and
    // they are not physics bodies, so they never push a player. When the pool is used up the oldest casing is reused.
    public sealed class CasingStream {
        private const float FULL_TURN_DEGREES = 360f;
        private const float LYING_PITCH_DEGREES = 90f;

        private readonly Transform _frame;
        private readonly IWeaponFeelFloor _floor;
        private readonly WeaponFeelProfile _profile;
        private readonly Transform[] _casings;
        private readonly Vector3 _casingScale;
        private readonly Vector3[] _positions;
        private readonly Vector3[] _velocities;
        private readonly Quaternion[] _rotations;
        private readonly Vector3[] _spinAxes;
        private readonly float[] _spinSpeeds;
        private readonly float[] _ages;
        private readonly bool[] _isActive;
        private readonly bool[] _isResting;

        private int _next;

        public int LandedThisFrame { get; private set; }

        public CasingStream(Transform container, Transform template, IWeaponFeelFloor floor, WeaponFeelProfile profile) {
            _frame = floor.Frame;
            _floor = floor;
            _profile = profile;
            int count = profile.CasingPoolSize;
            _casings = new Transform[count];
            _casingScale = template.localScale;
            _positions = new Vector3[count];
            _velocities = new Vector3[count];
            _rotations = new Quaternion[count];
            _spinAxes = new Vector3[count];
            _spinSpeeds = new float[count];
            _ages = new float[count];
            _isActive = new bool[count];
            _isResting = new bool[count];
            for (int i = 0; i < count; i++) {
                _casings[i] = Object.Instantiate(template, container, false);
                _casings[i].gameObject.SetActive(false);
            }
        }

        public void Eject(Vector3 worldPosition, Vector3 worldVelocity) {
            if (_casings.Length == 0)
                return;

            int index = _next;
            _next = (_next + 1) % _casings.Length;
            _positions[index] = _frame.InverseTransformPoint(worldPosition);
            _velocities[index] = _frame.InverseTransformDirection(worldVelocity);
            _rotations[index] = Random.rotation;
            _spinAxes[index] = Random.onUnitSphere;
            _spinSpeeds[index] = _profile.CasingSpinDegreesPerSecond * Random.Range(_profile.CasingSpinMinShare, _profile.CasingSpinMaxShare);
            _ages[index] = 0f;
            _isResting[index] = false;
            if (_isActive[index] == false) {
                _isActive[index] = true;
                _casings[index].gameObject.SetActive(true);
            }

            Apply(index);
        }

        public void Advance(float deltaTime) {
            LandedThisFrame = 0;
            Vector3 frameGravity = _frame.InverseTransformDirection(Physics.gravity);
            for (int i = 0; i < _casings.Length; i++) {
                if (_isActive[i] == false)
                    continue;

                _ages[i] += deltaTime;
                if (_isResting[i] == false)
                    Fly(i, frameGravity, deltaTime);

                if (IsFadedOut(i)) {
                    Release(i);
                    continue;
                }

                Apply(i);
            }
        }

        public void ReleaseAll() {
            for (int i = 0; i < _casings.Length; i++) {
                if (_isActive[i])
                    Release(i);
            }
        }

        private void Fly(int index, Vector3 frameGravity, float deltaTime) {
            _velocities[index] += frameGravity * deltaTime;
            _positions[index] += _velocities[index] * deltaTime;
            _rotations[index] = Quaternion.AngleAxis(_spinSpeeds[index] * deltaTime, _spinAxes[index]) * _rotations[index];
            if (_velocities[index].y >= 0f || _floor.TryGetFloorHeight(_positions[index], out float floorHeight) == false)
                return;

            if (_positions[index].y <= floorHeight + _profile.CasingRestHeight)
                Bounce(index, floorHeight);
        }

        private void Bounce(int index, float floorHeight) {
            Vector3 velocity = _velocities[index];
            velocity.y = -velocity.y * _profile.CasingBounciness;
            velocity.x *= _profile.CasingFloorFriction;
            velocity.z *= _profile.CasingFloorFriction;
            _velocities[index] = velocity;
            _spinSpeeds[index] *= _profile.CasingFloorFriction;
            _positions[index].y = floorHeight + _profile.CasingRestHeight;
            if (velocity.y < _profile.CasingRestSpeed)
                Rest(index, floorHeight);
        }

        // A resting casing lies on its side with a random heading, a little above the floor, so later ones lie on top
        // of earlier ones as a heap.
        private void Rest(int index, float floorHeight) {
            _isResting[index] = true;
            _velocities[index] = Vector3.zero;
            _positions[index].y = floorHeight + _profile.CasingRestHeight + Random.Range(0f, _profile.CasingPileJitter);
            _rotations[index] = Quaternion.Euler(0f, Random.Range(0f, FULL_TURN_DEGREES), 0f) * Quaternion.Euler(LYING_PITCH_DEGREES, 0f, 0f);
            LandedThisFrame++;
        }

        private bool IsFadedOut(int index) =>
            _ages[index] >= _profile.CasingLifetimeSeconds + _profile.CasingFadeSeconds;

        private void Apply(int index) {
            Transform casing = _casings[index];
            casing.localPosition = _positions[index];
            casing.localRotation = _rotations[index];
            casing.localScale = _casingScale * FadeScale(index);
        }

        private float FadeScale(int index) {
            float fadingSeconds = _ages[index] - _profile.CasingLifetimeSeconds;
            if (fadingSeconds <= 0f)
                return 1f;

            return _profile.CasingFadeSeconds > 0f ? 1f - fadingSeconds / _profile.CasingFadeSeconds : 0f;
        }

        private void Release(int index) {
            _isActive[index] = false;
            _casings[index].gameObject.SetActive(false);
        }
    }
}
