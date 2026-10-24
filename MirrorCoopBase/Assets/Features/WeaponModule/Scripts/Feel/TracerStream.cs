using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // Glowing streaks from the muzzle to where a shot ended, from a fixed pool. They fly in the floor's frame like
    // bullets that left a moving ship, so the stream stays on a target the ship carries along.
    public sealed class TracerStream {
        private const float HALF = 0.5f;

        private readonly Transform _frame;
        private readonly WeaponFeelProfile _profile;
        private readonly Transform[] _tracers;
        private readonly Vector3[] _starts;
        private readonly Vector3[] _directions;
        private readonly float[] _distances;
        private readonly float[] _travelled;
        private readonly bool[] _isActive;

        private int _next;

        public TracerStream(Transform container, Transform template, Transform frame, WeaponFeelProfile profile, int poolSize) {
            _frame = frame;
            _profile = profile;
            _tracers = new Transform[poolSize];
            _starts = new Vector3[poolSize];
            _directions = new Vector3[poolSize];
            _distances = new float[poolSize];
            _travelled = new float[poolSize];
            _isActive = new bool[poolSize];
            for (int i = 0; i < poolSize; i++) {
                _tracers[i] = Object.Instantiate(template, container, false);
                _tracers[i].gameObject.SetActive(false);
            }
        }

        public void Launch(Vector3 worldStart, Vector3 worldEnd) {
            if (_tracers.Length == 0)
                return;

            int index = _next;
            _next = (_next + 1) % _tracers.Length;
            Vector3 start = _frame.InverseTransformPoint(worldStart);
            Vector3 path = _frame.InverseTransformPoint(worldEnd) - start;
            _starts[index] = start;
            _distances[index] = path.magnitude;
            _directions[index] = _distances[index] > 0f ? path / _distances[index] : Vector3.forward;
            _travelled[index] = 0f;
            if (_isActive[index] == false) {
                _isActive[index] = true;
                _tracers[index].gameObject.SetActive(true);
            }

            Apply(index);
        }

        public void Advance(float deltaTime) {
            for (int i = 0; i < _tracers.Length; i++) {
                if (_isActive[i] == false)
                    continue;

                _travelled[i] += _profile.TracerSpeedMetersPerSecond * deltaTime;
                if (_travelled[i] - _profile.TracerLengthMeters >= _distances[i]) {
                    Release(i);
                    continue;
                }

                Apply(i);
            }
        }

        public void ReleaseAll() {
            for (int i = 0; i < _tracers.Length; i++) {
                if (_isActive[i])
                    Release(i);
            }
        }

        private void Apply(int index) {
            float head = Mathf.Min(_travelled[index], _distances[index]);
            float tail = Mathf.Clamp(_travelled[index] - _profile.TracerLengthMeters, 0f, head);
            Vector3 direction = _directions[index];
            Transform tracer = _tracers[index];
            tracer.localPosition = _starts[index] + direction * ((head + tail) * HALF);
            tracer.localRotation = Quaternion.LookRotation(direction);
            tracer.localScale = new Vector3(_profile.TracerWidthMeters, _profile.TracerWidthMeters, Mathf.Max(head - tail, _profile.TracerWidthMeters));
        }

        private void Release(int index) {
            _isActive[index] = false;
            _tracers[index].gameObject.SetActive(false);
        }
    }
}
