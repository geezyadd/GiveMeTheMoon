using System;
using UnityEngine;

namespace Features.AttackModule {
    // When the parts of an attack happen, seconds since its start: the limb's reach for the target, the window in which
    // it may hit, and when the attack is over and the next one may start.
    [Serializable]
    public struct AttackTiming {
        [Tooltip("Until this time the attack is the plain animation (its wind-up); then the limb starts reaching for the target.")]
        [SerializeField, Min(0f)] private float _reachStartSeconds;
        [Tooltip("The limb reaches fully for the target by this time.")]
        [SerializeField, Min(0f)] private float _reachFullSeconds;
        [Tooltip("The limb keeps reaching fully until this time.")]
        [SerializeField, Min(0f)] private float _reachReleaseSeconds;
        [Tooltip("The limb is back to the plain animation by this time.")]
        [SerializeField, Min(0f)] private float _reachEndSeconds;
        [Tooltip("The limb may hit the target from this time...")]
        [SerializeField, Min(0f)] private float _hitStartSeconds;
        [Tooltip("...until this time.")]
        [SerializeField, Min(0f)] private float _hitEndSeconds;
        [Tooltip("The attack is over at this time: a press before it is ignored.")]
        [SerializeField, Min(0f)] private float _durationSeconds;

        public AttackTiming(float reachStartSeconds, float reachFullSeconds, float reachReleaseSeconds,
            float reachEndSeconds, float hitStartSeconds, float hitEndSeconds, float durationSeconds) {
            _reachStartSeconds = reachStartSeconds;
            _reachFullSeconds = reachFullSeconds;
            _reachReleaseSeconds = reachReleaseSeconds;
            _reachEndSeconds = reachEndSeconds;
            _hitStartSeconds = hitStartSeconds;
            _hitEndSeconds = hitEndSeconds;
            _durationSeconds = durationSeconds;
        }

        public float ReachStartSeconds => _reachStartSeconds;
        public float ReachFullSeconds => _reachFullSeconds;
        public float ReachReleaseSeconds => _reachReleaseSeconds;
        public float ReachEndSeconds => _reachEndSeconds;
        public float HitStartSeconds => _hitStartSeconds;
        public float HitEndSeconds => _hitEndSeconds;
        public float DurationSeconds => _durationSeconds;
    }
}
